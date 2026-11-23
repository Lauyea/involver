/**
 * 追蹤或取消追蹤：依按鈕目前的樣式決定送 PUT（追蹤）或 DELETE（取消），再依伺服器回傳的狀態更新按鈕。
 * @param {HTMLElement} btn - 觸發事件的按鈕 DOM 元素，btn-secondary 代表已追蹤
 * @param {string} url - 追蹤 API 網址
 * @param {string} followLabel - 未追蹤時按鈕的文字
 * @returns {Promise<void>}
 */
const updateFollowAsync = async (btn, url, followLabel) => {
  const isFollowed = btn.classList.contains('btn-secondary');
  const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
  const headers = tokenInput ? { 'X-CSRF-TOKEN': tokenInput.value } : {};

  btn.disabled = true;
  try {
    const response = await fetch(url, {
      method: isFollowed ? 'DELETE' : 'PUT',
      headers,
      credentials: 'same-origin',
    });

    if (response.status === 401) {
      window.location.href = `/Identity/Account/Login?returnUrl=${encodeURIComponent(window.location.pathname)}`;
      return;
    }
    if (!response.ok) {
      throw new Error(`HTTP 錯誤! 狀態碼: ${response.status}`);
    }

    const { followed } = await response.json();
    btn.classList.toggle('btn-secondary', followed);
    btn.classList.toggle('btn-primary', !followed);
    btn.textContent = followed ? '取消追蹤' : followLabel;
  } catch (error) {
    alert(error.message);
    console.error('追蹤發生錯誤:', error);
  } finally {
    btn.disabled = false;
  }
};

/**
 * 追蹤/取消追蹤作者（由按鈕的 onclick 呼叫）
 * @param {HTMLElement} btn - 觸發事件的按鈕 DOM 元素
 * @param {string} id - 作者的 ProfileID
 * @returns {Promise<void>}
 */
const FollowAuthor = (btn, id) => updateFollowAsync(btn, `/api/v1/follows/authors/${encodeURIComponent(id)}`, '追蹤作者');

/**
 * 追蹤/取消追蹤小說（由按鈕的 onclick 呼叫）
 * @param {HTMLElement} btn - 觸發事件的按鈕 DOM 元素
 * @param {string|number} id - 小說的 ID
 * @returns {Promise<void>}
 */
const FollowNovel = (btn, id) => updateFollowAsync(btn, `/api/v1/follows/novels/${encodeURIComponent(id)}`, '追蹤創作');
