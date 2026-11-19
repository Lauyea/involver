import { createChart, destroyChart } from '/js/charts/chart-theme.js';

/** 彈窗元素 ID */
const MODAL_ID = 'viewRecordModal';

/**
 * 取得作品或文章過去 30 天的觀看紀錄
 * @param {string} type - novel 或 article
 * @param {string} id - 作品或文章 ID
 * @returns {Promise<{ title: string, labels: string[], daily: number[], cumulative: number[] }>} 觀看紀錄
 */
const getViewRecordAsync = async (type, id) => {
  const params = new URLSearchParams({ type, id });
  const response = await fetch(`/api/Views/Daily?${params}`);
  if (!response.ok) {
    throw new Error(`觀看紀錄載入失敗：${response.status}`);
  }

  return response.json();
};

/**
 * 建立彈窗的 HTML 結構；標題以 textContent 填入，避免 XSS
 * @param {string} title - 作品或文章標題
 * @returns {HTMLElement} 彈窗元素
 */
const buildModal = (title) => {
  const wrapper = document.createElement('div');
  wrapper.innerHTML = `
    <div class="modal fade" id="${MODAL_ID}" tabindex="-1" aria-labelledby="${MODAL_ID}Label" aria-hidden="true">
      <div class="modal-dialog modal-lg modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="${MODAL_ID}Label"></h5>
            <button type="button" class="btn btn-sm btn-outline-secondary ms-auto me-2" data-view-record-toggle>切換檢視</button>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="關閉"></button>
          </div>
          <div class="modal-body">
            <div class="view-record__body" data-view-record-combined>
              <canvas></canvas>
            </div>
            <div class="d-none" data-view-record-separate>
              <div class="view-record__body"><canvas></canvas></div>
              <hr />
              <div class="view-record__body"><canvas></canvas></div>
            </div>
          </div>
        </div>
      </div>
    </div>`;

  const modal = wrapper.firstElementChild;
  modal.querySelector('.modal-title').textContent = `${title} - 過去 30 日觀看紀錄`;
  return modal;
};

/**
 * 每日觀看數（長條）與累積觀看數（折線，右側座標軸）合併在一張圖
 * @param {object} record - 觀看紀錄
 * @returns {object} Chart.js 設定
 */
const buildCombinedConfig = (record) => ({
  type: 'bar',
  data: {
    labels: record.labels,
    datasets: [
      { type: 'bar', label: '每日觀看數', data: record.daily, colorIndex: 0, yAxisID: 'y' },
      { type: 'line', label: '累積觀看數', data: record.cumulative, colorIndex: 1, tension: 0.1, yAxisID: 'y1' },
    ],
  },
  options: {
    interaction: { mode: 'index', intersect: false },
    scales: {
      y: { beginAtZero: true, position: 'left', ticks: { precision: 0 }, title: { display: true, text: '每日觀看數' } },
      y1: { position: 'right', grid: { drawOnChartArea: false }, ticks: { precision: 0 }, title: { display: true, text: '累積觀看數' } },
    },
  },
});

/**
 * 單一數列的折線圖
 * @param {string[]} labels - 日期
 * @param {string} label - 數列名稱
 * @param {number[]} data - 數值
 * @param {number} colorIndex - 調色盤顏色
 * @param {boolean} beginAtZero - Y 軸是否從 0 開始
 * @returns {object} Chart.js 設定
 */
const buildLineConfig = (labels, label, data, colorIndex, beginAtZero) => ({
  type: 'line',
  data: { labels, datasets: [{ label, data, colorIndex, fill: true, tension: 0.1 }] },
  options: {
    plugins: { title: { display: true, text: label } },
    scales: { x: {}, y: { beginAtZero, ticks: { precision: 0 } } },
  },
});

/**
 * 開啟觀看紀錄彈窗；關閉時銷毀圖表並移除彈窗
 * @param {string} type - novel 或 article
 * @param {string} id - 作品或文章 ID
 */
const openViewRecordModalAsync = async (type, id) => {
  const record = await getViewRecordAsync(type, id);

  document.getElementById(MODAL_ID)?.remove();
  const modal = buildModal(record.title);
  document.body.appendChild(modal);

  const combined = modal.querySelector('[data-view-record-combined]');
  const separate = modal.querySelector('[data-view-record-separate]');
  let charts = [];
  let isCombined = true;

  const render = () => {
    charts.forEach(destroyChart);
    combined.classList.toggle('d-none', !isCombined);
    separate.classList.toggle('d-none', isCombined);

    if (isCombined) {
      charts = [createChart(combined.querySelector('canvas'), buildCombinedConfig(record))];
      return;
    }

    const [dailyCanvas, cumulativeCanvas] = separate.querySelectorAll('canvas');
    charts = [
      createChart(dailyCanvas, buildLineConfig(record.labels, '每日觀看數', record.daily, 0, true)),
      createChart(cumulativeCanvas, buildLineConfig(record.labels, '累積觀看數', record.cumulative, 1, false)),
    ];
  };

  modal.querySelector('[data-view-record-toggle]').addEventListener('click', () => {
    isCombined = !isCombined;
    render();
  });

  modal.addEventListener('shown.bs.modal', render, { once: true });
  modal.addEventListener('hidden.bs.modal', () => {
    charts.forEach(destroyChart);
    bootstrap.Modal.getInstance(modal)?.dispose();
    modal.remove();
  }, { once: true });

  new bootstrap.Modal(modal).show();
};

// 事件委派：列表以無限滾動載入的項目也能觸發
document.addEventListener('click', (event) => {
  const trigger = event.target.closest('.view-chart-trigger');
  if (!trigger) {
    return;
  }

  event.preventDefault();
  openViewRecordModalAsync(trigger.dataset.type, trigger.dataset.id).catch((error) => {
    console.error(error);
    alert('觀看紀錄載入失敗，請稍後再試。');
  });
});
