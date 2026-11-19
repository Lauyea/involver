import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

/** 調色盤的顏色數量，對應 site.css 的 --chart-color-1 ~ --chart-color-6 */
const PALETTE_SIZE = 6;

/** 填色區塊的透明度 */
const FILL_ALPHA = 0.25;

/** 目前畫面上的圖表；切換深淺色時一起更新 */
const activeCharts = new Set();

/**
 * 讀取 :root 上的 CSS 變數
 * @param {string} name - 變數名稱，例如 '--chart-text'
 * @returns {string} 變數值
 */
const readCssVar = (name) => getComputedStyle(document.documentElement).getPropertyValue(name).trim();

/**
 * 把 #rrggbb 顏色加上透明度
 * @param {string} hex - #rrggbb 或 #rgb
 * @param {number} alpha - 0 ~ 1
 * @returns {string} rgba() 顏色；無法解析時原樣回傳
 */
export const withAlpha = (hex, alpha) => {
  const match = /^#([0-9a-f]{3}|[0-9a-f]{6})$/i.exec(hex);
  if (!match) {
    return hex;
  }

  const value = match[1].length === 3
    ? match[1].split('').map((c) => c + c).join('')
    : match[1];
  const r = parseInt(value.slice(0, 2), 16);
  const g = parseInt(value.slice(2, 4), 16);
  const b = parseInt(value.slice(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
};

/**
 * 取得目前主題（深色／淺色）的圖表顏色
 * @returns {{ text: string, grid: string, palette: string[] }} 文字、格線與調色盤顏色
 */
export const getChartTheme = () => ({
  text: readCssVar('--chart-text'),
  grid: readCssVar('--chart-grid'),
  palette: Array.from({ length: PALETTE_SIZE }, (_, i) => readCssVar(`--chart-color-${i + 1}`)),
});

/**
 * 依 dataset 的 colorIndex 套上主題顏色。
 * colorIndex 是本專案自訂的欄位；perPoint 為 true（圓餅圖）時每個資料點各用一個顏色。
 * @param {object} config - Chart.js 設定
 * @param {{ palette: string[] }} theme - 主題顏色
 */
const applyDatasetColors = (config, theme) => {
  const datasets = config.data?.datasets ?? [];
  datasets.forEach((dataset, index) => {
    if (dataset.perPoint) {
      const colors = (dataset.data ?? []).map((_, i) => theme.palette[i % PALETTE_SIZE]);
      dataset.backgroundColor = colors;
      dataset.borderColor = readCssVar('--card-bg-color') || '#fff';
      return;
    }

    const color = theme.palette[(dataset.colorIndex ?? index) % PALETTE_SIZE];
    const isLine = (dataset.type ?? config.type) === 'line';
    dataset.borderColor = color;
    dataset.backgroundColor = isLine && !dataset.fill ? color : withAlpha(color, isLine ? FILL_ALPHA : 0.7);
    dataset.pointBackgroundColor = color;
  });
};

/**
 * 套用文字與格線顏色到全域預設值與每個座標軸
 * @param {object} options - Chart.js options
 * @param {{ text: string, grid: string }} theme - 主題顏色
 */
const applyScaleColors = (options, theme) => {
  Object.values(options?.scales ?? {}).forEach((scale) => {
    scale.ticks = { ...scale.ticks, color: theme.text };
    scale.grid = { ...scale.grid, color: theme.grid };
    if (scale.title) {
      scale.title.color = theme.text;
    }
  });
};

/**
 * 依目前主題更新全域預設值
 * @param {{ text: string, grid: string }} theme - 主題顏色
 */
const applyDefaults = (theme) => {
  Chart.defaults.color = theme.text;
  Chart.defaults.borderColor = theme.grid;
  Chart.defaults.font.family = readCssVar('--font-main') || Chart.defaults.font.family;
  Chart.defaults.maintainAspectRatio = false;
};

/**
 * 建立套用網站主題的圖表，並在切換深淺色時自動更新顏色
 * @param {HTMLCanvasElement} canvas - 畫布
 * @param {object} config - Chart.js 設定；dataset 可用 colorIndex、perPoint 指定調色盤顏色
 * @returns {Chart} 圖表
 */
export const createChart = (canvas, config) => {
  const theme = getChartTheme();
  applyDefaults(theme);
  applyDatasetColors(config, theme);
  applyScaleColors(config.options, theme);

  const chart = new Chart(canvas, config);
  activeCharts.add(chart);
  return chart;
};

/**
 * 銷毀圖表並停止追蹤主題變更
 * @param {Chart} chart - 圖表
 */
export const destroyChart = (chart) => {
  activeCharts.delete(chart);
  chart.destroy();
};

/**
 * 主題切換後重新套用顏色
 */
const refreshCharts = () => {
  const theme = getChartTheme();
  applyDefaults(theme);
  activeCharts.forEach((chart) => {
    applyDatasetColors(chart.config, theme);
    applyScaleColors(chart.options, theme);
    chart.update('none');
  });
};

// color-mode-toggler.js 切換主題時會改 <html data-bs-theme>
new MutationObserver(refreshCharts).observe(document.documentElement, {
  attributes: true,
  attributeFilter: ['data-bs-theme'],
});
