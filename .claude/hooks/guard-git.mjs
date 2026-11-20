// PreToolUse hook（Bash、PowerShell）：守住 git commit 與 git push 的規則（AGENTS.md「Agent 工作流程」）
// - 在 master 上 commit，或 push 到 master：直接擋下（master 只能透過 PR 修改）
// - 其他分支的 commit：跳出確認視窗，讓使用者看過 commit 內容再決定（等同「commit 前先問」）
// 不是 git commit / push 的指令一律放行，不輸出任何東西。

import { execFileSync } from 'node:child_process';

const PROTECTED = ['master', 'main'];

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

const input = await readInputAsync();
const command = String(input?.tool_input?.command ?? '');

// 以 ; && || | 與換行切成多段，逐段判斷，避免 echo 'git commit' 之類的誤判太多
const segments = command.split(/;|&&|\|\||\||\r?\n/).map((s) => s.trim());
const isCommit = segments.some((s) => /^(?:\S+=\S+\s+)*git(?:\s+-C\s+\S+|\s+-c\s+\S+)*\s+commit\b/.test(s));
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
    decide('ask', `即將在 ${branch || '(未知分支)'} 分支 commit。請確認分支、檔案與 commit 訊息後再允許。`);
  }
}
