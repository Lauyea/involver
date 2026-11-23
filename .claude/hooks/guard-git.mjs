// PreToolUse hook（Bash、PowerShell）：守住 git commit 與 git push 的規則（AGENTS.md「Agent 工作流程」）
// - 在 master 上 commit，或 push 到 master：直接擋下（master 只能透過 PR 修改）
// - 其他分支的 commit：程式碼變更超過 200 行時，要先 code review 並記錄（見下面的 --mark-reviewed），
//   暫存內容和記錄相符才放行，否則擋下並提醒先 review。不跳確認視窗：
//   「commit 前先問」由 Agent 在對話裡詢問，使用者在對話裡同意後不必再點一次。
// 不是 git commit / push 的指令一律放行，不輸出任何東西。
//
// 記錄 review（在儲存庫裡執行，code review 完成、修正都已 git add 之後）：
//   node .claude/hooks/guard-git.mjs --mark-reviewed
// 會把目前暫存內容的 tree hash 寫進 git 目錄（各 worktree 分開）。review 之後又改了程式碼，
// tree hash 就對不上，要重新確認後再記錄一次。

import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';

const PROTECTED = ['master', 'main'];

// 程式碼變更超過這個行數，commit 前要先 code review（AGENTS.md「Agent 工作流程」）
const REVIEW_THRESHOLD = 200;

/**
 * 讀取 stdin 的 hook 輸入
 * @returns {Promise<Object>} hook 輸入的 JSON
 */
const readInputAsync = async () => {
  const chunks = [];
  for await (const chunk of process.stdin) {
    chunks.push(chunk);
  }

  return JSON.parse(Buffer.concat(chunks).toString('utf8') || '{}');
};

/**
 * 取得目前分支名稱
 * @param {string} cwd - 指令執行的目錄
 * @returns {string} 分支名稱，取不到時回傳空字串
 */
const getBranch = (cwd) => {
  try {
    return execFileSync('git', ['rev-parse', '--abbrev-ref', 'HEAD'], { cwd, encoding: 'utf8' }).trim();
  } catch {
    return '';
  }
};

/**
 * 取得記錄 review 結果的檔案路徑（在 git 目錄裡，各 worktree 分開）
 * @param {string} cwd - 指令執行的目錄
 * @returns {string} 檔案路徑，取不到時回傳空字串
 */
const getReviewMarkerPath = (cwd) => {
  try {
    const relative = execFileSync('git', ['rev-parse', '--git-path', 'claude-reviewed-tree'], { cwd, encoding: 'utf8' }).trim();
    return path.resolve(cwd, relative);
  } catch {
    return '';
  }
};

/**
 * 取得目前暫存內容（index）的 tree hash。只會寫入 git 物件，不改變 index 或工作目錄。
 * @param {string} cwd - 指令執行的目錄
 * @returns {string} tree hash，取不到時回傳空字串
 */
const getStagedTree = (cwd) => {
  try {
    return execFileSync('git', ['write-tree'], { cwd, encoding: 'utf8' }).trim();
  } catch {
    return '';
  }
};

/**
 * 暫存內容是否就是記錄過 review 的內容
 * @param {string} cwd - 指令執行的目錄
 * @returns {boolean} 相符時為 true
 */
const isStagedTreeReviewed = (cwd) => {
  const markerPath = getReviewMarkerPath(cwd);
  const tree = getStagedTree(cwd);
  if (!markerPath || !tree || !existsSync(markerPath)) {
    return false;
  }

  return readFileSync(markerPath, 'utf8').trim() === tree;
};

/**
 * 輸出 PreToolUse 的權限決定
 * @param {'deny'|'ask'} decision - 擋下或詢問使用者
 * @param {string} reason - 顯示給使用者與 Claude 的原因
 */
const decide = (decision, reason) => {
  process.stdout.write(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: 'PreToolUse',
      permissionDecision: decision,
      permissionDecisionReason: reason,
    },
  }));
};

/**
 * 計算這次 commit 的程式碼變更行數（新增＋刪除）。
 * 只算 .cs、.cshtml、.js、.css；不算 migration 的 Designer 與 snapshot、wwwroot/lib。
 * @param {string} cwd - 指令執行的目錄
 * @param {boolean} includeUnstaged - git commit -a 時連未 staged 的變更一起算
 * @returns {number} 變更行數，取不到時回傳 0
 */
const countCodeLines = (cwd, includeUnstaged) => {
  try {
    const output = execFileSync('git', [
      'diff', includeUnstaged ? 'HEAD' : '--cached', '--numstat', '--',
      '*.cs', '*.cshtml', '*.js', '*.css',
      ':(exclude,glob)**/Migrations/*.Designer.cs',
      ':(exclude,glob)**/*ModelSnapshot.cs',
      ':(exclude)Involver/wwwroot/lib',
    ], { cwd, encoding: 'utf8' });
    return output.split('\n')
      .map((line) => line.split('\t'))
      .reduce((sum, [added, deleted]) => sum + (Number(added) || 0) + (Number(deleted) || 0), 0);
  } catch {
    return 0;
  }
};

if (process.argv.includes('--mark-reviewed')) {
  const cwd = process.cwd();
  const markerPath = getReviewMarkerPath(cwd);
  const tree = getStagedTree(cwd);
  if (!markerPath || !tree) {
    console.error('記錄失敗：請在儲存庫裡執行。');
    process.exit(1);
  }

  writeFileSync(markerPath, `${tree}\n`, 'utf8');
  console.log(`已記錄 review：暫存內容 ${tree.slice(0, 12)}（程式碼變更 ${countCodeLines(cwd, false)} 行）。之後再修改並 git add，要重新記錄。`);
  process.exit(0);
}

const input = await readInputAsync();
const command = String(input?.tool_input?.command ?? '');

// 以 ; && || | 與換行切成多段，逐段判斷，避免 echo 'git commit' 之類的誤判太多
const segments = command.split(/;|&&|\|\||\||\r?\n/).map((s) => s.trim());
const commitSegment = segments.find((s) => /^(?:\S+=\S+\s+)*git(?:\s+-C\s+\S+|\s+-c\s+\S+)*\s+commit\b/.test(s));
const isCommit = Boolean(commitSegment);
const pushSegment = segments.find((s) => /^(?:\S+=\S+\s+)*git(?:\s+-C\s+\S+|\s+-c\s+\S+)*\s+push\b/.test(s));

if (!isCommit && !pushSegment) {
  process.exit(0);
}

const branch = getBranch(input?.cwd || process.cwd());

if (pushSegment) {
  // git push [選項] [遠端] [refspec...]：refspec 的目標（冒號右邊）是 master，或沒有 refspec 且目前就在 master
  const positional = pushSegment.replace(/^.*?\bpush\b/, '').trim().split(/\s+/).filter((a) => a && !a.startsWith('-'));
  const targets = positional.slice(1)
    .map((r) => r.split(':').pop().replace(/^\+/, '').replace(/^refs\/heads\//, ''))
    // git push origin HEAD 推的是目前分支
    .map((t) => (t === 'HEAD' ? branch : t));
  const pushesProtected = targets.some((t) => PROTECTED.includes(t))
    || (targets.length === 0 && PROTECTED.includes(branch));
  if (pushesProtected) {
    decide('deny', 'master 有分支保護，只能透過 PR 修改，不能直接 push。請在 feature/、fix/、debug/ 分支上 push 後開 PR。');
    process.exit(0);
  }
}

if (isCommit) {
  if (PROTECTED.includes(branch)) {
    decide('deny', `目前在 ${branch} 分支。master 只能透過 PR 修改：請先建立 feature/、fix/ 或 debug/ 分支再 commit。`);
  } else {
    const cwd = input?.cwd || process.cwd();
    const commitsAll = /\s(?:-[a-zA-Z]*a[a-zA-Z]*|--all)(?:\s|$)/.test(commitSegment);
    const lines = countCodeLines(cwd, commitsAll);
    // 沒超過門檻、或暫存內容已經記錄過 review，就不輸出：使用者已經在對話裡同意 commit，不必再點一次
    if (lines > REVIEW_THRESHOLD) {
      if (commitsAll) {
        decide('deny', `程式碼變更有 ${lines} 行，超過 ${REVIEW_THRESHOLD} 行。請不要用 git commit -a：先 git add 要 commit 的檔案，做 code review 並執行 node .claude/hooks/guard-git.mjs --mark-reviewed，再 commit。`);
      } else if (!isStagedTreeReviewed(cwd)) {
        decide('deny', `程式碼變更有 ${lines} 行，超過 ${REVIEW_THRESHOLD} 行，暫存內容還沒有記錄 code review（或 review 之後又改過）。請先做 code review（Claude Code 用 /code-review），修正後 git add，執行 node .claude/hooks/guard-git.mjs --mark-reviewed，再 commit。`);
      }
    }
  }
}
