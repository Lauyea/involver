import { sendJsonAsync } from '/js/api-client.js';

/**
 * 會員首頁 Feed 的互動：
 * 1. 卡片進入畫面時記錄曝光、點擊時記錄點擊（用來計算各區塊、各設定版本的 CTR）。
 * 2. 「不感興趣」：隱藏卡片並送出負面訊號，可以復原。
 * 3. 冷啟動的主分類最多選 3 個。
 */

const API_BASE = '/api/v1/recommendations';
const FLUSH_DELAY_MS = 1000;
const MAX_BATCH_SIZE = 60;
const VISIBLE_RATIO = 0.5;

const feed = document.getElementById('home-feed');

/**
 * 讀取卡片上的曝光資訊
 * @param {HTMLElement} card - 作品卡片
 * @returns {Object} 卡片識別與位置
 */
const getCardInfo = (card) => ({
  contentType: Number(card.dataset.contentType),
  contentId: Number(card.dataset.contentId),
  section: Number(card.dataset.section),
  position: Number(card.dataset.position),
  settingVersion: card.dataset.settingVersion ? Number(card.dataset.settingVersion) : null,
});

/**
 * 記錄曝光：收集一小段時間內進入畫面的卡片，一次送出
 */
class ImpressionTracker {
  /**
   * @param {string} feedRequestId - 這次頁面載入的識別碼
   */
  constructor(feedRequestId) {
    this.feedRequestId = feedRequestId;
    this.pending = [];
    this.timer = null;
    this.observer = new IntersectionObserver((entries) => this.onIntersect(entries), { threshold: VISIBLE_RATIO });
  }

  /**
   * 開始觀察卡片
   * @param {NodeListOf<HTMLElement>} cards - 作品卡片
   */
  observe(cards) {
    cards.forEach((card) => this.observer.observe(card));
  }

  /**
   * 卡片進入畫面
   * @param {IntersectionObserverEntry[]} entries - 觀察結果
   */
  onIntersect(entries) {
    entries
      .filter((entry) => entry.isIntersecting)
      .forEach((entry) => {
        this.observer.unobserve(entry.target);
        this.pending.push(getCardInfo(entry.target));
      });

    if (this.pending.length > 0 && this.timer === null) {
      this.timer = setTimeout(() => this.flushAsync(), FLUSH_DELAY_MS);
    }
  }

  /**
   * 送出累積的曝光
   * @param {boolean} [keepalive=false] - 離開頁面時仍送出
   * @returns {Promise<void>}
   */
  async flushAsync(keepalive = false) {
    clearTimeout(this.timer);
    this.timer = null;

    while (this.pending.length > 0) {
      const items = this.pending.splice(0, MAX_BATCH_SIZE);
      try {
        await sendJsonAsync(`${API_BASE}/impressions`, 'POST', { feedRequestId: this.feedRequestId, items }, { keepalive });
      } catch (error) {
        console.warn('Failed to record impressions.', error);
      }
    }
  }
}

/**
 * 記錄點擊（不阻擋連結）
 * @param {string} feedRequestId - 這次頁面載入的識別碼
 * @param {HTMLElement} card - 被點擊的卡片
 * @returns {Promise<void>}
 */
const createClickAsync = async (feedRequestId, card) => {
  try {
    await sendJsonAsync(`${API_BASE}/clicks`, 'POST', { feedRequestId, ...getCardInfo(card) }, { keepalive: true });
  } catch (error) {
    console.warn('Failed to record click.', error);
  }
};

/**
 * 「不感興趣」：隱藏卡片，顯示復原按鈕
 * @param {HTMLElement} card - 作品卡片
 * @returns {Promise<void>}
 */
const createFeedbackAsync = async (card) => {
  const { contentType, contentId } = getCardInfo(card);
  const button = card.querySelector('[data-dismiss-recommendation]');
  button.disabled = true;

  try {
    await sendJsonAsync(`${API_BASE}/feedback`, 'POST', { contentType, contentId });
  } catch (error) {
    console.warn('Failed to dismiss recommendation.', error);
    button.disabled = false;
    return;
  }

  const notice = document.createElement('div');
  notice.className = 'content-card__dismissed';
  notice.setAttribute('role', 'status');

  const text = document.createElement('span');
  text.textContent = '已隱藏，之後不會再推薦這部作品。';
  const undo = document.createElement('button');
  undo.type = 'button';
  undo.className = 'btn btn-sm btn-outline-secondary';
  undo.textContent = '復原';
  undo.addEventListener('click', () => deleteFeedbackAsync(card, notice));

  notice.append(text, undo);
  card.classList.add('content-card--dismissed');
  card.append(notice);
  undo.focus();
};

/**
 * 復原「不感興趣」
 * @param {HTMLElement} card - 作品卡片
 * @param {HTMLElement} notice - 已隱藏的提示
 * @returns {Promise<void>}
 */
const deleteFeedbackAsync = async (card, notice) => {
  const { contentType, contentId } = getCardInfo(card);
  try {
    await sendJsonAsync(`${API_BASE}/feedback/${contentType}/${contentId}`, 'DELETE');
  } catch (error) {
    console.warn('Failed to undo dismissal.', error);
    return;
  }

  notice.remove();
  card.classList.remove('content-card--dismissed');
  const button = card.querySelector('[data-dismiss-recommendation]');
  button.disabled = false;
  button.focus();
};

/**
 * 冷啟動的分類：至少選一個才能儲存（勾選上限由 site.js 的 data-max-checked 處理）
 * @param {HTMLFormElement} form - 分類表單
 * @returns {void}
 */
const requireSelection = (form) => {
  const boxes = [...form.querySelectorAll('input[type="checkbox"]')];
  const submit = form.querySelector('button[type="submit"]');

  const update = () => {
    submit.disabled = !boxes.some((box) => box.checked);
  };

  boxes.forEach((box) => box.addEventListener('change', update));
  update();
};

if (feed) {
  const feedRequestId = feed.dataset.feedRequestId;
  const cards = feed.querySelectorAll('.content-card[data-section]');

  if ('IntersectionObserver' in window) {
    const tracker = new ImpressionTracker(feedRequestId);
    tracker.observe(cards);
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'hidden') {
        tracker.flushAsync(true);
      }
    });
  }

  feed.addEventListener('click', (event) => {
    const dismiss = event.target.closest('[data-dismiss-recommendation]');
    if (dismiss) {
      createFeedbackAsync(dismiss.closest('.content-card'));
      return;
    }

    const link = event.target.closest('[data-feed-link]');
    const card = link?.closest('.content-card[data-section]');
    if (card) {
      createClickAsync(feedRequestId, card);
    }
  });

  feed.querySelectorAll('.feed-type-picker__form').forEach(requireSelection);
}
