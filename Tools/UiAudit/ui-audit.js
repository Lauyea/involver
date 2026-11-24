/**
 * UI 自動檢查：在瀏覽器目前的頁面執行，回傳 AGENTS.md「前端開發規範」的 UI 規範檢查結果（JSON）。
 *
 * 用法：
 *   - Agent：把整個檔案內容交給瀏覽器工具執行（例如 Claude 的 javascript_tool），最後一個運算式就是結果。
 *   - 人工：貼到 DevTools Console 執行。
 *   - 第一次執行後，程式會存進 sessionStorage；同一個分頁之後（重新整理後也可以）只要執行：
 *       eval(sessionStorage.getItem('uiAudit'))
 *
 * 回傳 error 而不是檢查結果時，代表 viewport 是 0×0（瀏覽器面板被隱藏），這時量到的數字都不能用。
 *
 * 檢查項目：
 *   - overflow：頁面是否有水平捲動，以及超出視窗右緣的元素（手機寬度 375 px 最常見）
 *   - unlabeled：只有圖示、沒有可讀名稱的按鈕與連結（需要 aria-label）
 *   - smallTargets：高度或寬度小於 40 px 的觸控目標（段落內的文字連結不算）
 *   - missingAlt：沒有 alt 屬性的圖片
 *   - lowContrast：文字與背景對比不足（一般文字 < 4.5、大字 < 3）；背景是圖片、漸層或半透明色時略過。
 *     顏色用 1×1 canvas 解析，所以 color-mix() 算出的 oklab()、oklch()、color(srgb …) 也能比較。
 *
 * 每次修改 UI 都要在淺色、深色模式與 375 px 寬度各跑一次。切換主題：
 *   localStorage.setItem('theme', 'dark'); location.reload();
 */
// 用具名函式（不是箭頭函式），才能用 uiAudit.toString() 把自己存起來重複執行
(function uiAudit() {
  const MAX_ITEMS = 15;
  const MIN_TARGET = 40;

  try {
    sessionStorage.setItem('uiAudit', `(${uiAudit.toString()})();`);
  } catch {
    // sessionStorage 不能用時（例如被封鎖）不影響這次的檢查
  }

  if (window.innerWidth === 0 || window.innerHeight === 0) {
    return {
      error: 'viewport 是 0×0：瀏覽器面板被隱藏，量到的結果不能用。請先顯示面板或設定視窗大小（例如 resize_window），再執行 eval(sessionStorage.getItem(\'uiAudit\'))。',
    };
  }

  /**
   * 產生容易辨識的元素描述
   * @param {Element} element - 元素
   * @returns {string} 例如 a.btn.btn-sm「追蹤」
   */
  const describe = (element) => {
    const id = element.id ? `#${element.id}` : '';
    const classes = [...element.classList].slice(0, 3).map((c) => `.${c}`).join('');
    const text = (element.textContent || '').trim().replace(/\s+/g, ' ').slice(0, 30);
    return `${element.tagName.toLowerCase()}${id}${classes}${text ? `「${text}」` : ''}`;
  };

  /**
   * 元素是否看得見
   * @param {Element} element - 元素
   * @returns {boolean} 看得見時為 true
   */
  const isVisible = (element) => {
    const style = getComputedStyle(element);
    const rect = element.getBoundingClientRect();
    return style.display !== 'none' && style.visibility !== 'hidden' && Number(style.opacity) > 0 && rect.width > 0 && rect.height > 0;
  };

  // 用 1×1 canvas 把任何 CSS 顏色（rgb、oklab、oklch、color(srgb …) 等）轉成 sRGB；同一個字串只算一次
  const colorCanvas = document.createElement('canvas');
  colorCanvas.width = 1;
  colorCanvas.height = 1;
  const colorContext = colorCanvas.getContext('2d', { willReadFrequently: true });
  const colorCache = new Map();

  /**
   * 解析計算後的 CSS 顏色。color-mix() 的計算值常是 oklab()／color(srgb …)，所以交給 canvas 轉換，不用正規表示式
   * @param {string} value - CSS 顏色字串（getComputedStyle 的值）
   * @returns {number[]|null} [r, g, b, a]，r/g/b 為 0～255、a 為 0～1；無法解析時為 null
   */
  const parseColor = (value) => {
    if (colorCache.has(value)) {
      return colorCache.get(value);
    }

    let color = null;
    if (colorContext && CSS.supports('color', value)) {
      colorContext.clearRect(0, 0, 1, 1);
      colorContext.fillStyle = value;
      colorContext.fillRect(0, 0, 1, 1);
      const [r, g, b, a] = colorContext.getImageData(0, 0, 1, 1).data;
      color = [r, g, b, a / 255];
    }

    colorCache.set(value, color);
    return color;
  };

  /**
   * 相對亮度（WCAG）
   * @param {number[]} color - [r, g, b]
   * @returns {number} 0～1
   */
  const luminance = ([r, g, b]) => {
    const [lr, lg, lb] = [r, g, b].map((c) => {
      const s = c / 255;
      return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
    });
    return (0.2126 * lr) + (0.7152 * lg) + (0.0722 * lb);
  };

  /**
   * 找出元素實際的背景色；遇到背景圖片或漸層時回傳 null
   * @param {Element} element - 元素
   * @returns {number[]|null} [r, g, b]
   */
  const effectiveBackground = (element) => {
    for (let current = element; current; current = current.parentElement) {
      const style = getComputedStyle(current);
      if (style.backgroundImage !== 'none') {
        return null;
      }

      const color = parseColor(style.backgroundColor);
      if (color && color[3] >= 0.99) {
        return color.slice(0, 3);
      }

      if (color && color[3] > 0) {
        return null;
      }
    }

    return [255, 255, 255];
  };

  const viewportWidth = document.documentElement.clientWidth;

  // 水平捲動
  const overflowing = [...document.body.querySelectorAll('*')]
    .filter((element) => isVisible(element) && element.getBoundingClientRect().right > viewportWidth + 1)
    .filter((element) => !element.closest('[style*="overflow"], .table-responsive, .overflow-auto, .overflow-x-auto'))
    .slice(0, MAX_ITEMS)
    .map((element) => `${describe(element)} 右緣 ${Math.round(element.getBoundingClientRect().right)} px`);

  // 互動元素
  const interactive = [...document.querySelectorAll('a[href], button, [role="button"], input:not([type="hidden"]), select, textarea, summary')]
    .filter(isVisible);

  const unlabeled = interactive
    .filter((element) => ['A', 'BUTTON', 'SUMMARY'].includes(element.tagName) || element.getAttribute('role') === 'button')
    .filter((element) => {
      const text = (element.textContent || '').trim();
      const imageAlt = [...element.querySelectorAll('img[alt]')].some((img) => img.alt.trim());
      return !text && !imageAlt && !element.getAttribute('aria-label') && !element.getAttribute('aria-labelledby') && !element.getAttribute('title');
    })
    .slice(0, MAX_ITEMS)
    .map(describe);

  const smallTargets = interactive
    .filter((element) => !(element.tagName === 'A' && !element.classList.contains('btn') && element.closest('p, li, td, .card-text')))
    // stretched-link 用 ::after 撐滿整張卡片，實際點擊範圍是卡片
    .filter((element) => !element.classList.contains('stretched-link'))
    .filter((element) => !['checkbox', 'radio'].includes(element.type))
    .filter((element) => {
      const rect = element.getBoundingClientRect();
      return rect.height < MIN_TARGET || rect.width < MIN_TARGET;
    })
    .slice(0, MAX_ITEMS)
    .map((element) => {
      const rect = element.getBoundingClientRect();
      return `${describe(element)} ${Math.round(rect.width)}×${Math.round(rect.height)}`;
    });

  const missingAlt = [...document.querySelectorAll('img:not([alt])')]
    .filter(isVisible)
    .slice(0, MAX_ITEMS)
    .map((img) => img.getAttribute('src') || describe(img));

  // 對比：只檢查直接含有文字的元素
  const lowContrast = [...document.body.querySelectorAll('*')]
    .filter((element) => isVisible(element) && [...element.childNodes].some((n) => n.nodeType === Node.TEXT_NODE && n.textContent.trim()))
    .map((element) => {
      const style = getComputedStyle(element);
      const foreground = parseColor(style.color);
      const background = effectiveBackground(element);
      if (!foreground || !background) {
        return null;
      }

      const [light, dark] = [luminance(foreground), luminance(background)].sort((a, b) => b - a);
      const ratio = (light + 0.05) / (dark + 0.05);
      const size = Number.parseFloat(style.fontSize);
      const large = size >= 24 || (size >= 18.66 && Number(style.fontWeight) >= 700);
      return ratio < (large ? 3 : 4.5) ? `${describe(element)} 對比 ${ratio.toFixed(2)}` : null;
    })
    .filter(Boolean)
    .slice(0, MAX_ITEMS);

  const report = {
    url: location.pathname + location.search,
    viewport: `${window.innerWidth}×${window.innerHeight}`,
    theme: document.documentElement.getAttribute('data-bs-theme'),
    horizontalScroll: document.documentElement.scrollWidth > viewportWidth,
    overflow: overflowing,
    unlabeled,
    smallTargets,
    missingAlt,
    lowContrast,
  };
  report.summary = `水平捲動：${report.horizontalScroll ? '有' : '無'}；超出右緣 ${overflowing.length}、缺 aria-label ${unlabeled.length}、觸控目標過小 ${smallTargets.length}、缺 alt ${missingAlt.length}、對比不足 ${lowContrast.length}（每項最多列 ${MAX_ITEMS} 個）`;
  return report;
})();
