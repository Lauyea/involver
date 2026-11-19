/**
 * 呼叫站內 API 的共用函式：自動帶上 anti-forgery token（header 名稱見 Program.cs 的 AddAntiforgery）。
 */

const CSRF_HEADER = 'X-CSRF-TOKEN';

/**
 * 取得頁面上的 anti-forgery token
 * @returns {string|null} token；頁面上沒有時為 null
 */
export const getCsrfToken = () => {
  const input = document.querySelector('input[name="__RequestVerificationToken"]');
  return input ? input.value : null;
};

/**
 * 送出 JSON 請求
 * @param {string} url - API 網址
 * @param {string} method - HTTP 方法
 * @param {Object} [body] - 要送出的資料
 * @param {Object} [options] - 其他選項
 * @param {boolean} [options.keepalive=false] - 離開頁面後仍送出（例如點擊連結時記錄點擊）
 * @returns {Promise<Response>} 回應
 */
export const sendJsonAsync = async (url, method, body, { keepalive = false } = {}) => {
  const headers = { 'Content-Type': 'application/json' };
  const token = getCsrfToken();
  if (token) {
    headers[CSRF_HEADER] = token;
  }

  const response = await fetch(url, {
    method,
    headers,
    credentials: 'same-origin',
    keepalive,
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!response.ok) {
    throw new Error(`${method} ${url} failed: ${response.status}`);
  }

  return response;
};
