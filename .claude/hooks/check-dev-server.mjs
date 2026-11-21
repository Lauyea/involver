// Stop hook：Claude 要結束這一輪時，如果用 .claude/launch.json 啟動的 dev server（dotnet run --launch-profile Involver）還在跑，
// 就擋下一次，提醒先用 preview_stop 停掉、關閉瀏覽器分頁、執行 dotnet build-server shutdown（AGENTS.md「驗證完要釋放資源」）。
// 只認 launch.json 的啟動參數，使用者自己在 Visual Studio 跑的網站不會被當成要關掉的對象。
// stop_hook_active 為 true（已經被擋過一次）時一律放行，避免無限迴圈。

import { execFileSync } from 'node:child_process';
import net from 'node:net';

const PORT = 5000;

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
 * 檢查本機連接埠是否有程式在監聽
 * @param {number} port - 連接埠
 * @returns {Promise<boolean>} 有在監聽時為 true
 */
const isListeningAsync = (port) => new Promise((resolve) => {
  const socket = net.connect({ host: '127.0.0.1', port, timeout: 300 });
  socket.once('connect', () => {
    socket.destroy();
    resolve(true);
  });
  socket.once('timeout', () => {
    socket.destroy();
    resolve(false);
  });
  socket.once('error', () => resolve(false));
});

/**
 * 是否有 launch.json 啟動的 dotnet run 行程
 * @returns {boolean} 找到時為 true
 */
const hasPreviewServer = () => {
  try {
    const output = execFileSync('powershell', [
      '-NoProfile',
      '-Command',
      "Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" | Where-Object { $_.CommandLine -like '*--launch-profile Involver*' } | Measure-Object | Select-Object -ExpandProperty Count",
    ], { encoding: 'utf8', timeout: 5000 });
    return Number.parseInt(output.trim(), 10) > 0;
  } catch {
    return false;
  }
};

const input = await readInputAsync();
if (input?.stop_hook_active || process.platform !== 'win32') {
  process.exit(0);
}

if (await isListeningAsync(PORT) && hasPreviewServer()) {
  process.stdout.write(JSON.stringify({
    decision: 'block',
    reason: 'launch.json 啟動的 dev server（port 5000）還在執行。驗證已完成的話，請用 preview_stop 停掉它、關閉 Browser pane 的分頁，並執行 dotnet build-server shutdown；如果使用者要求保留，就直接結束。',
  }));
}
