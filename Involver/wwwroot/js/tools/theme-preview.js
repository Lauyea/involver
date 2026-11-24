/**
 * 主題檢查頁（Pages/Theme）：顯示 CSS 變數目前的色值，並即時計算「底色＋前景色」的對比。
 * 切換淺色、深色模式時（<html data-bs-theme> 改變）會重新計算。
 * 頁首的 data-theme-choice 按鈕可以直接切換主題。
 *
 * 標記：
 *   - data-contrast-bg / data-contrast-fg：底色與前景色的 CSS 變數名稱，結果寫進裡面的 .theme-preview__ratio
 *   - .theme-preview__ratio 的 data-ratio-min：合格門檻，預設 4.5（一般文字）；圖示與框線用 3
 *   - .theme-preview__ratio 的 data-ratio-label：數字前面的說明文字
 *   - data-color-value：要顯示色值的 CSS 變數名稱
 */

const DEFAULT_MIN_RATIO = 4.5;

/** 導覽列主題選單的圖示，和 color-mode-toggler.js 相同 */
const THEME_ICONS = {
  auto: 'fa-circle-half-stroke',
  dark: 'fa-moon',
  light: 'fa-sun',
};

const canvasContext = document.createElement('canvas').getContext('2d', { willReadFrequently: true });

/**
 * 把任何 CSS 顏色字串轉成 sRGB（交給 canvas 解析，color-mix()、oklab() 也可以）
 * @param {string} color - CSS 顏色
 * @returns {number[]|null} [r, g, b]，無法解析時為 null
 */
const toRgb = (color) => {
  if (!color || !canvasContext || !CSS.supports('color', color)) {
    return null;
  }

  canvasContext.clearRect(0, 0, 1, 1);
  canvasContext.fillStyle = '#000';
  canvasContext.fillStyle = color;
  canvasContext.fillRect(0, 0, 1, 1);
  const [r, g, b] = canvasContext.getImageData(0, 0, 1, 1).data;
  return [r, g, b];
};

/**
 * WCAG 相對亮度
 * @param {number[]} rgb - [r, g, b]
 * @returns {number} 0～1
 */
const luminance = (rgb) => {
  const [r, g, b] = rgb.map((channel) => {
    const value = channel / 255;
    return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
  });
  return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
};

/**
 * WCAG 對比
 * @param {number[]} first - [r, g, b]
 * @param {number[]} second - [r, g, b]
 * @returns {number} 1～21
 */
const contrastRatio = (first, second) => {
  const [light, dark] = [luminance(first), luminance(second)].sort((a, b) => b - a);
  return (light + 0.05) / (dark + 0.05);
};

/**
 * 讀取元素上 CSS 變數的值
 * @param {Element} element - 元素
 * @param {string} name - 變數名稱（例如 --md-primary）
 * @returns {string} 變數值，沒有定義時為空字串
 */
const readVariable = (element, name) => getComputedStyle(element).getPropertyValue(name).trim();

/**
 * 讀取使用者選擇的主題（color-mode-toggler.js 存在 localStorage）
 * @returns {string} light、dark 或 auto；無法讀取時為 auto
 */
const readStoredTheme = () => {
  try {
    return localStorage.getItem('theme') ?? 'auto';
  } catch {
    return 'auto';
  }
};

/**
 * 更新所有對比與色值
 */
const updateAll = () => {
  document.querySelectorAll('[data-contrast-bg][data-contrast-fg]').forEach((element) => {
    const output = element.querySelector('.theme-preview__ratio');
    const background = toRgb(readVariable(element, element.dataset.contrastBg));
    const foreground = toRgb(readVariable(element, element.dataset.contrastFg));
    if (!output || !background || !foreground) {
      return;
    }

    const ratio = contrastRatio(background, foreground);
    const minRatio = Number(output.dataset.ratioMin ?? DEFAULT_MIN_RATIO);
    const passed = ratio >= minRatio;
    const label = output.dataset.ratioLabel ? `${output.dataset.ratioLabel} ` : '';
    output.textContent = `${label}${ratio.toFixed(2)} ${passed ? '✓' : `✗（需 ${minRatio}）`}`;
    output.classList.toggle('theme-preview__ratio--fail', !passed);
  });

  document.querySelectorAll('[data-color-value]').forEach((element) => {
    element.textContent = readVariable(element, element.dataset.colorValue).toUpperCase();
  });

  const storedTheme = readStoredTheme();
  document.querySelectorAll('[data-theme-choice]').forEach((button) => {
    const pressed = button.dataset.themeChoice === storedTheme;
    button.classList.toggle('active', pressed);
    button.setAttribute('aria-pressed', String(pressed));
  });
};

new MutationObserver(updateAll).observe(document.documentElement, {
  attributeFilter: ['data-bs-theme'],
  attributes: true,
});

/**
 * 切換主題，規則和 color-mode-toggler.js 相同，並同步導覽列的主題選單
 * @param {string} theme - light、dark 或 auto
 */
const applyTheme = (theme) => {
  try {
    localStorage.setItem('theme', theme);
  } catch {
    // 無法儲存時只切換這一頁
  }

  const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
  let resolved = theme;
  if (theme === 'auto') {
    resolved = prefersDark ? 'dark' : 'light';
  }

  document.documentElement.setAttribute('data-bs-theme', resolved);

  // 同步導覽列的主題選單（選中的項目與圖示）
  document.querySelectorAll('[data-bs-theme-value]').forEach((item) => {
    const selected = item.dataset.bsThemeValue === theme;
    item.classList.toggle('active', selected);
    item.setAttribute('aria-pressed', String(selected));
  });

  const navbarIcon = document.querySelector('#theme-icon');
  if (navbarIcon) {
    navbarIcon.className = `fa-solid ${THEME_ICONS[theme]} my-1`;
  }

  updateAll();
};

document.querySelectorAll('[data-theme-choice]').forEach((button) => {
  button.addEventListener('click', () => applyTheme(button.dataset.themeChoice));
});

updateAll();
