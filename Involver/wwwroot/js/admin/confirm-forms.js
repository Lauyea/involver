/**
 * 送出帶有 data-confirm 的表單前先確認。
 * 確認文字放在屬性裡（由 Razor 編碼），不要把使用者輸入的內容寫進 inline script。
 */
document.addEventListener('submit', (event) => {
  const message = event.target.dataset?.confirm;
  if (message && !window.confirm(message)) {
    event.preventDefault();
  }
});
