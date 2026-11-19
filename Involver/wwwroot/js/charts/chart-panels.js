import { createChart } from '/js/charts/chart-theme.js';

/**
 * 數字加上千分位
 * @param {number} value - 數值
 * @returns {string} 格式化後的字串
 */
const formatNumber = (value) => Number(value ?? 0).toLocaleString('zh-TW', { maximumFractionDigits: 2 });

/**
 * 依 ChartPanel View Component 輸出的設定建立 Chart.js 設定
 * @param {object} panel - data-chart-config 的內容
 * @returns {object} Chart.js 設定
 */
const buildConfig = (panel) => {
  const isCircular = panel.type === 'doughnut' || panel.type === 'pie';
  const datasets = panel.datasets.map((dataset) => ({
    label: dataset.label,
    data: dataset.data,
    type: dataset.type ?? undefined,
    colorIndex: dataset.colorIndex,
    perPoint: isCircular,
    fill: dataset.fill ?? false,
    tension: 0.25,
    pointRadius: panel.labels.length > 45 ? 0 : 2,
    yAxisID: isCircular ? undefined : (dataset.yAxisId ?? 'y'),
  }));

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: isCircular ? 'nearest' : 'index', intersect: isCircular },
    plugins: {
      legend: { display: isCircular || datasets.length > 1, position: 'bottom' },
      tooltip: {
        callbacks: {
          label: (context) => {
            const value = context.parsed?.y ?? context.parsed?.x ?? context.parsed;
            const label = context.dataset.label || context.label;
            return `${label}: ${formatNumber(isCircular ? context.parsed : value)}`;
          },
        },
      },
    },
  };

  if (!isCircular) {
    options.indexAxis = panel.horizontal ? 'y' : 'x';
    const valueAxis = panel.horizontal ? 'x' : 'y';
    const categoryAxis = panel.horizontal ? 'y' : 'x';
    options.scales = {
      [categoryAxis]: { stacked: panel.stacked ?? false, ticks: { autoSkip: true, maxRotation: 0 } },
      [valueAxis]: {
        beginAtZero: true,
        stacked: panel.stacked ?? false,
        ticks: { precision: 0, callback: (value) => formatNumber(value) },
        title: panel.yTitle ? { display: true, text: panel.yTitle } : undefined,
      },
    };

    if (datasets.some((dataset) => dataset.yAxisID === 'y1')) {
      options.scales.y1 = {
        beginAtZero: true,
        position: 'right',
        grid: { drawOnChartArea: false },
        ticks: { precision: 0, callback: (value) => formatNumber(value) },
        title: panel.y1Title ? { display: true, text: panel.y1Title } : undefined,
      };
    }
  }

  return { type: panel.type, data: { labels: panel.labels, datasets }, options };
};

/**
 * 初始化頁面上所有 ChartPanel 的圖表
 */
const initChartPanels = () => {
  document.querySelectorAll('canvas[data-chart-config]').forEach((canvas) => {
    try {
      const panel = JSON.parse(canvas.dataset.chartConfig);
      createChart(canvas, buildConfig(panel));
    } catch (error) {
      console.error('圖表設定無法解析：', error);
    }
  });
};

initChartPanels();
