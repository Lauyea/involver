/**
 * 設定無限滾動
 * @param {string} containerSelector - 內容容器的選擇器，例如 '#articles-container'
 * @param {string} loadingSelector - 加載指示器的選擇器，例如 '#loading'
 * @param {string} handlerName - Razor Page 中用於加載更多的處理器名稱，例如 'LoadMore'
 * @param {object} additionalParams - 附加到 AJAX 請求的額外參數
 */
function setupInfiniteScroll(containerSelector, loadingSelector, handlerName, additionalParams = {}) {
    let page = 1;
    let isLoading = false;
    let noMoreData = false;

    // 獲取加載指示器元素 (只查詢一次以提高效能)
    const loadingElement = document.querySelector(loadingSelector);

    if (!loadingElement) {
        console.error('無限滾動錯誤：找不到加載指示器', loadingSelector);
        return;
    }

    function loadMoreIfNeeded() {
        if (noMoreData || isLoading) {
            return;
        }

        // 判斷滾動位置
        // window.scrollY = 當前滾動的垂直距離
        // window.innerHeight = 瀏覽器視窗的可視高度
        // document.documentElement.scrollHeight = 整個文件的總高度
        const nearBottom = window.scrollY + window.innerHeight >= document.documentElement.scrollHeight - 200;
        const notScrollable = document.documentElement.scrollHeight <= window.innerHeight + 200;

        if (nearBottom || notScrollable) {
            isLoading = true;
            // 顯示加載動畫
            loadingElement.style.display = 'block';
            page++;

            // 準備請求參數
            const requestData = {
                handler: handlerName,
                pageIndex: page,
                ...additionalParams
            };

            // 將參數物件轉換為 URL 查詢字串
            const params = new URLSearchParams(requestData);
            // 組合最終的請求 URL
            const url = `${window.location.pathname}?${params.toString()}`;

            // 使用 fetch API 執行 AJAX (GET) 請求
            fetch(url, {
                method: 'GET',
                headers: {
                    // 告知伺服器這是一個 AJAX 請求 (Razor Pages 可能需要)
                    'X-Requested-With': 'XMLHttpRequest'
                }
            })
                .then(response => {
                    if (!response.ok) {
                        throw new Error(`HTTP 錯誤! 狀態: ${response.status}`);
                    }
                    // 因為我們預期的是 HTML 片段，所以使用 .text()
                    return response.text();
                })
                .then(data => {
                    if (data.trim().length > 0) {
                        // 獲取內容容器
                        const containerElement = document.querySelector(containerSelector);
                        if (!containerElement) {
                            console.error('無限滾動錯誤：找不到內容容器', containerSelector);
                            isLoading = false; // 即使出錯也要重設狀態
                            loadingElement.style.display = 'none';
                            return;
                        }

                        // 附加 HTML 內容
                        // .insertAdjacentHTML('beforeend', ...) 等同於 jQuery 的 .append(htmlString)
                        containerElement.insertAdjacentHTML('beforeend', data);

                        isLoading = false;
                        loadingElement.style.display = 'none';

                        // 再次檢查，以防新內容仍未填滿視窗導致無法滾動
                        loadMoreIfNeeded();
                    } else {
                        // 沒有更多數據了
                        noMoreData = true;
                        loadingElement.style.display = 'none';
                    }
                })
                .catch(error => {
                    isLoading = false;
                    loadingElement.style.display = 'none';
                    console.error('加載更多內容時出錯:', error);
                });
        }
    }

    // 綁定滾動事件
    window.addEventListener('scroll', loadMoreIfNeeded);

    // 初始化時先檢查一次
    // 使用 setTimeout 確保 DOM 初始渲染完成後再執行
    setTimeout(loadMoreIfNeeded, 100);
}

/* 觀看紀錄 Modal 操作*/

/**
 * 處理 .view-chart-trigger 的點擊事件 (事件委派)
 */
document.addEventListener('click', async function (e) {
    // 檢查點擊的目標是否為在'.view-chart-trigger' 內
    const triggerElement = e.target.closest('.view-chart-trigger');

    // 如果點擊的不是 triggerElement，就結束
    if (!triggerElement) {
        return;
    }

    // 執行原始的 click 邏輯
    e.preventDefault(); // 防止連結跳轉

    const type = triggerElement.dataset.type;
    const id = triggerElement.dataset.id;
    const url = `/StatisticalData/PartialViewRecord?handler=ViewRecord&type=${type}&id=${id}`;

    const modalContainer = document.getElementById('modal');
    if (!modalContainer) {
        console.error('Modal container #modal not found');
        return;
    }

    try {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`Failed to load partial view: ${response.statusText}`);
        }
        const html = await response.text();

        modalContainer.innerHTML = html;

        // 找到由 .cshtml  載入的 (但未執行的) script 標籤
        const originalScript = modalContainer.querySelector('script');

        if (originalScript) {
            // 建立一個 *新的* script 元素
            const newScript = document.createElement('script');

            // 將舊 script  的內容複製到新 script
            newScript.textContent = originalScript.textContent;

            // 將新 script 附加到 modal 元素上
            // 瀏覽器會 *執行* 透過 DOM API 附加的 script
            modalContainer.appendChild(newScript);

            // 移除原本那個無用的 script 標籤
            originalScript.remove();
        }

        // 執行 load 的回呼函式 (callback)
        // 找到剛剛注入的 modal 元素
        const modalElement = document.getElementById('viewRecordModal');

        if (modalElement && typeof bootstrap !== 'undefined' && bootstrap.Modal) {
            // 使用 Bootstrap 5/4 的原生 JS API 顯示 modal
            const bsModal = new bootstrap.Modal(modalElement);
            bsModal.show();
        } else {
            console.error('#viewRecordModal not found inside loaded HTML or Bootstrap JS is missing.');
        }

    } catch (err) {
        console.error('Error loading modal content:', err);
        alert('Error loading content.');
    }
});

/**
 * 監聽 Modal 隱藏事件
 */
document.addEventListener('hidden.bs.modal', function (e) {
    // 檢查觸發此事件的是否為 #viewRecordModal
    if (e.target.id === 'viewRecordModal') {
        // First, destroy the Chart.js instances using the globally exposed function
        if (typeof window.destroyPartialCharts === 'function') {
            window.destroyPartialCharts();
            window.destroyPartialCharts = null; // Clean up the global scope
        }

        const modalElement = e.target;
        // 從元素獲取 Bootstrap Modal 實例
        const modalInstance = bootstrap.Modal.getInstance(modalElement);
        if (modalInstance) {
            // 使用 Bootstrap API 銷毀實例，這會處理事件和 backdrop
            modalInstance.dispose();
        }
        // 從 DOM 中移除 modal 的 HTML
        modalElement.remove();
    }
});