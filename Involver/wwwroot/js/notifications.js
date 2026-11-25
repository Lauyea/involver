import { getCsrfHeaders } from '/js/api-client.js';

/**
 * 導覽列的通知鈴鐺：載入通知下拉選單，處理「全部標為已讀」。
 * 選單的 HTML 由 /Feed/Notifications 產生；未登入時 _LoginPartial 不會輸出 #notification，直接略過。
 */

/**
 * 載入通知下拉選單到 #notification
 * @param {HTMLElement} container - 通知的容器（帶 data-notification-url）
 * @returns {Promise<void>}
 */
const getNotificationsAsync = async (container) => {
  const url = container.dataset.notificationUrl;

  try {
    const response = await fetch(url, { credentials: 'same-origin' });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    container.innerHTML = await response.text();
  } catch (error) {
    // 保留預設的鈴鐺，只在 console 記錄，不在導覽列顯示錯誤訊息
    console.error('載入通知失敗：', error);
  }
};

/**
 * 全部標為已讀：送出後移除未讀樣式與數字，選單保持開著
 * @param {HTMLElement} container - 通知的容器（帶 data-mark-all-url）
 * @param {HTMLButtonElement} button - 「全部標為已讀」按鈕
 * @returns {Promise<void>}
 */
const updateAllReadAsync = async (container, button) => {
  button.disabled = true;

  try {
    const response = await fetch(container.dataset.markAllUrl, {
      method: 'POST',
      credentials: 'same-origin',
      headers: getCsrfHeaders(),
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    container.querySelectorAll('.notification-item--unread').forEach((item) => {
      item.classList.remove('notification-item--unread');
      item.querySelector('.notification-item__dot')?.remove();
    });
    container.querySelector('.notification-bell__badge')?.remove();

    const bell = container.querySelector('.notification-bell');
    if (bell) {
      bell.setAttribute('aria-label', '通知');
      bell.title = '通知';
    }

    // 按鈕移除前把焦點移到「查看所有通知」，避免焦點掉到 body
    const nextFocus = container.querySelector('.notification-menu__footer');
    button.remove();
    nextFocus?.focus();
  } catch (error) {
    console.error('標為已讀失敗：', error);
    button.disabled = false;
  }
};

const container = document.getElementById('notification');

if (container) {
  container.addEventListener('click', (event) => {
    const button = event.target.closest('[data-notification-mark-all]');
    if (button) {
      updateAllReadAsync(container, button);
    }
  });

  getNotificationsAsync(container);
}
