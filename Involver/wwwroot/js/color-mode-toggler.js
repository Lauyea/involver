/*!
 * 主題切換（淺色／深色／自動）
 * 改寫自 Bootstrap 文件的 color-modes.js（Copyright 2011-2024 The Bootstrap Authors，CC BY 3.0）。
 */

/**
 * 在 <head> 同步執行，先套用主題，避免頁面以錯誤的顏色閃一下。
 * 頁面上可以有多組 [data-bs-theme-value] 按鈕（頭像選單、頁尾），選中狀態會一起更新。
 * 沒有儲存過選擇時是「自動」，跟著系統的深色模式設定。
 */
(() => {
  const STORAGE_KEY = 'theme';
  const darkQuery = window.matchMedia('(prefers-color-scheme: dark)');

  /**
   * 讀取使用者選的主題
   * @returns {string} light、dark 或 auto
   */
  const getStoredTheme = () => {
    try {
      return localStorage.getItem(STORAGE_KEY) ?? 'auto';
    } catch {
      return 'auto';
    }
  };

  /**
   * 儲存使用者選的主題；無法儲存時（例如被封鎖）只套用到這一頁
   * @param {string} theme - light、dark 或 auto
   * @returns {void}
   */
  const setStoredTheme = (theme) => {
    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch {
      // 無法儲存時略過
    }
  };

  /**
   * 把主題套用到 <html data-bs-theme>；auto 依系統設定換成 light 或 dark
   * @param {string} theme - light、dark 或 auto
   * @returns {void}
   */
  const applyTheme = (theme) => {
    let resolved = theme;
    if (theme === 'auto') {
      resolved = darkQuery.matches ? 'dark' : 'light';
    }

    document.documentElement.setAttribute('data-bs-theme', resolved);
  };

  /**
   * 更新所有主題按鈕的選中狀態
   * @param {string} theme - light、dark 或 auto
   * @returns {void}
   */
  const showActiveTheme = (theme) => {
    document.querySelectorAll('[data-bs-theme-value]').forEach((button) => {
      const isActive = button.dataset.bsThemeValue === theme;
      button.classList.toggle('active', isActive);
      button.setAttribute('aria-pressed', String(isActive));
    });
  };

  applyTheme(getStoredTheme());

  darkQuery.addEventListener('change', () => {
    if (getStoredTheme() === 'auto') {
      applyTheme('auto');
    }
  });

  window.addEventListener('DOMContentLoaded', () => {
    showActiveTheme(getStoredTheme());

    // 用事件委派，之後才載入的按鈕也能切換
    document.addEventListener('click', (event) => {
      const button = event.target.closest('[data-bs-theme-value]');
      if (!button) {
        return;
      }

      const theme = button.dataset.bsThemeValue;
      setStoredTheme(theme);
      applyTheme(theme);
      showActiveTheme(theme);
    });
  });
})();
