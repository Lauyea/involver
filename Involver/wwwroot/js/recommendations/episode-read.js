import { sendJsonAsync } from '/js/api-client.js';

/**
 * 章節閱讀紀錄：章節結尾的標記進入畫面時，回報「讀到結尾」。每次頁面載入只送一次。
 * 標記上的 data-episode-id 指定章節；只有登入的會員頁面上才會有這個標記。
 */

const marker = document.querySelector('.episode-end-marker[data-episode-id]');

/**
 * 回報讀到結尾
 * @param {string} episodeId - 章節 ID
 * @returns {Promise<void>}
 */
const updateReachedEndAsync = async (episodeId) => {
  try {
    await sendJsonAsync(`/api/v1/episodes/${encodeURIComponent(episodeId)}/read`, 'PUT', { reachedEnd: true });
  } catch (error) {
    console.warn('Failed to record reading progress.', error);
  }
};

if (marker && 'IntersectionObserver' in window) {
  const observer = new IntersectionObserver((entries) => {
    if (entries.some((entry) => entry.isIntersecting)) {
      observer.disconnect();
      updateReachedEndAsync(marker.dataset.episodeId);
    }
  });
  observer.observe(marker);
}
