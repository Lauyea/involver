import { createChart } from '/js/charts/chart-theme.js';

/** 迷你趨勢圖顯示的天數 */
const SPARKLINE_DAYS = 7;

/**
 * 每種內容的畫布 ID 前綴與批次查詢 API
 */
const SOURCES = [
  { prefix: 'novel-chart-', endpoint: '/api/Views/GetBatchNovelViews' },
  { prefix: 'article-chart-', endpoint: '/api/Views/GetBatchArticleViews' },
];

/**
 * 取得最近幾天的日期字串（yyyy-MM-dd）
 * @returns {string[]} 由舊到新的日期
 */
const getRecentDates = () => {
  const dates = [];
  for (let i = SPARKLINE_DAYS - 1; i >= 0; i--) {
    const d = new Date();
    d.setDate(d.getDate() - i);
    dates.push(d.toISOString().split('T')[0]);
  }

  return dates;
};

/**
 * 批次取得多個作品或文章的每日觀看數
 * @param {string} endpoint - API 路徑
 * @param {string[]} ids - 作品或文章 ID
 * @returns {Promise<Object>} 以 ID 為 key 的觀看紀錄；失敗時為空物件
 */
const getBatchViewsAsync = async (endpoint, ids) => {
  const response = await fetch(endpoint, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(ids),
  });

  return response.ok ? response.json() : {};
};

/**
 * 在列表的每個畫布上畫出最近 7 天的觀看趨勢
 * @param {{ prefix: string, endpoint: string }} source - 內容種類
 */
const renderSparklinesAsync = async ({ prefix, endpoint }) => {
  const canvases = document.querySelectorAll(`canvas[id^="${prefix}"]`);
  if (canvases.length === 0) {
    return;
  }

  const ids = [...new Set(Array.from(canvases).map((canvas) => canvas.id.slice(prefix.length)))];
  const batchData = await getBatchViewsAsync(endpoint, ids);
  const labels = getRecentDates();

  canvases.forEach((canvas) => {
    const viewsData = batchData[canvas.id.slice(prefix.length)] || [];
    const data = labels.map((label) => viewsData.find((d) => d.date.startsWith(label))?.views ?? 0);

    createChart(canvas, {
      type: 'line',
      data: {
        labels,
        datasets: [{ data, colorIndex: 0, borderWidth: 2, pointRadius: 0, tension: 0.2 }],
      },
      options: {
        scales: { x: { display: false }, y: { display: false } },
        plugins: { legend: { display: false }, tooltip: { enabled: false } },
        maintainAspectRatio: false,
      },
    });
  });
};

SOURCES.forEach((source) => {
  renderSparklinesAsync(source).catch((error) => console.error('觀看趨勢載入失敗：', error));
});
