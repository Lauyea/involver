// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.

// 等待 DOM 內容完全載入後才執行
document.addEventListener('DOMContentLoaded', fetchNotifications);

/**
 * 顯示全域 Toast 通知
 * @param {Array<Object>} toasts - 一個包含 toast 物件的陣列
 * @param {string} toast.header - Toast 的標題
 * @param {string} toast.body - Toast 的內容
 * @param {number} toast.award - 獎勵值 (10: bronze, 30: silver, other: gold)
 */
function showGlobalToasts(toasts) {
    const toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        console.error('Toast container not found');
        return;
    }

    toasts.forEach(toast => {
        let badgeClass = '';
        switch (toast.award) {
            case 10: badgeClass = 'bronze'; break; // 設定於 DataAccess\Common\Parameters.cs 的 `BronzeBadgeAward`
            case 30: badgeClass = 'silver'; break;
            default: badgeClass = 'gold'; break;
        }

        const toastId = `toast-${Date.now()}-${Math.random()}`;
        const toastHtml = `
            <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true" data-autohide="false">
                <div class="toast-header">
                    ${badgeClass ? `<span class="dot me-2 ${badgeClass}"></span>` : ''}
                    <strong class="me-auto">${toast.header}</strong>
                    <button type="button" class="ms-2 mb-1 btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body">
                    ${toast.body}
                </div>
            </div>
        `;
        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastElement = document.getElementById(toastId);

        if (!toastElement) {
            console.error(`Failed to find newly created toast with ID: ${toastId}`);
            return;
        }

        // 檢查 Bootstrap JS 是否已載入
        if (typeof bootstrap === 'undefined' || typeof bootstrap.Toast === 'undefined') {
            console.error('Bootstrap JS (bootstrap.Toast) is not loaded. Cannot show toast.');
            return;
        }

        // 使用 Vanilla JS 建立 Bootstrap Toast 實例
        // (Bootstrap 會自動從 data-autohide="false" 讀取設定)
        const bsToast = new bootstrap.Toast(toastElement);

        bsToast.show();

        // 監聽 'hidden.bs.toast' 事件 (這是 Bootstrap 提供的標準 DOM 事件)
        toastElement.addEventListener('hidden.bs.toast', function () {
            // 當 Toast 隱藏後，從 DOM 中移除它
            // 在 'function' 回呼中, 'this' 會正確指向 toastElement
            this.remove();
        });
    });
}

/*FB's share function*/
(function (d, s, id) {
    var js, fjs = d.getElementsByTagName(s)[0];
    if (d.getElementById(id)) return;
    js = d.createElement(s); js.id = id;
    js.src = "https://connect.facebook.net/en_US/sdk.js#xfbml=1&version=v3.0";
    fjs.parentNode.insertBefore(js, fjs);
}(document, 'script', 'facebook-jssdk'));


/**
 * 切換深色模式
 */
async function SetDarkMode() {

    // 取得 Anti-Forgery Token
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenElement ? tokenElement.value : null;

    if (!token) {
        console.error('Anti-forgery token not found. Cannot change mode.');
        alert('無法切換模式，請重試。');
        return;
    }

    try {
        // 使用 fetch 發送 POST 請求
        const response = await fetch("/DarkMode/Set", {
            method: 'POST',
            headers: {
                // 在標頭中附加 Token
                'RequestVerificationToken': token
                // 注意：如果端點需要，可能還需要 'Content-Type'
            }
        });

        if (response.ok) {
            const themeIcon = document.getElementById("theme-icon");
            const layoutBody = document.getElementById("layout-body");

            if (themeIcon) {
                themeIcon.classList.toggle('fa-sun');
                themeIcon.classList.toggle('fa-moon');
            }
            if (layoutBody) {
                layoutBody.classList.toggle('bootstrap-dark');
                layoutBody.classList.toggle('bootstrap');
            }
        } else {
            // 處理 HTTP 錯誤 (例如 404, 500)
            alert(`切換失敗: ${response.statusText}`);
        }
    } catch (err) {
        alert(`網路錯誤: ${err.message}`);
    }
}

/**
 * 處理複製分享連結到剪貼簿並追蹤分享事件
 * @param {HTMLElement} btn - 被點擊的按鈕元素。
 */
async function copyShareLinkAndTrackAsync(btn) {
    // 從 data-* 屬性獲取資料
    const contentType = btn.dataset.type;
    const contentId = btn.dataset.id;
    const baseUrl = `${window.location.protocol}//${window.location.host}`;
    // 根據內容類型建構 URL
    const shareUrl = `${baseUrl}/${contentType}s/Details/${contentId}`;

    try {
        // 複製到剪貼簿
        await navigator.clipboard.writeText(shareUrl);

        // 顯示/隱藏 Bootstrap Tooltip
        const tooltip = bootstrap.Tooltip.getInstance(btn);
        if (tooltip) {
            tooltip.show();
            setTimeout(() => tooltip.hide(), 2000); // 2 秒後隱藏
        }

        // 獲取 anti-forgery token
        const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenEl ? tokenEl.value : null;

        if (!token) {
            console.log('Anti-forgery token not found. Skipping tracking.');
            // 即使沒有 token，複製也已經成功了，所以不用 throw error
            return;
        }

        // 使用 fetch API 發送追蹤請求
        const response = await fetch('/api/shares', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({
                contentId: contentId,
                contentType: contentType
            })
        });

        // 處理 API 回應
        if (!response.ok) {
            if (response.status === 401) {
                // 未登入，靜默失敗
                console.log('User not authenticated for tracking share.');
            } else {
                // 其他錯誤
                console.error('Error tracking share event.', response.status, response.statusText);
            }
        }

    } catch (err) {
        console.error('Failed to copy or track: ', err);
    }
}

/**
 * DOM 載入完成後執行的初始化代碼
 */
document.addEventListener('DOMContentLoaded', () => {

    // 初始化所有 .btn-copy-link 按鈕的 Tooltip
    const tooltipTriggerList = document.querySelectorAll('.btn-copy-link');
    tooltipTriggerList.forEach(tooltipTriggerEl => {
        new bootstrap.Tooltip(tooltipTriggerEl, {
            trigger: 'manual', // 我們將手動控制顯示/隱藏
            placement: 'bottom'
            //title: '已複製！' // 可以在這裡設定 tooltip 的標題。已經設定在原按鈕中。
        });
    });

    // 使用事件委派 (Event Delegation) 附加點擊處理器
    document.addEventListener('click', (e) => {
        // 使用 .closest() 來找到被點擊的目標或其祖先中符合 .btn-copy-link 的元素
        const copyButton = e.target.closest('.btn-copy-link');

        if (copyButton) {
            e.preventDefault(); // 防止預設行為
            copyShareLinkAndTrackAsync(copyButton); // 呼叫函式
        }
    });
});

/**
 * 追蹤/取消追蹤作者的功能。
 * 使用 Fetch API 執行 GET 請求。
 * @param {HTMLElement} btn - 觸發事件的按鈕 DOM 元素。
 * @param {string|number} id - 作者的 ID。
 */
function FollowAuthor(btn, id) {
    const url = `/Follow/FollowAuthor?id=${encodeURIComponent(id)}`;

    fetch(url, {
        method: 'GET'
    })
        .then(response => {
            if (!response.ok) {
                // 如果 HTTP 狀態碼不是 2xx，拋出錯誤
                throw new Error(`HTTP 錯誤! 狀態碼: ${response.status}`);
            }
        })
        .then(() => {
            if (btn.textContent.trim() === "追蹤作者") {
                btn.classList.remove("btn-primary");
                btn.classList.add("btn-secondary");
                btn.textContent = "取消追蹤";
            } else {
                btn.classList.remove("btn-secondary");
                btn.classList.add("btn-primary");
                btn.textContent = "追蹤作者";
            }
        })
        .catch(error => {
            alert(error.message);
            console.error('追蹤作者發生錯誤:', error);
        });
}

/**
 * 追蹤/取消追蹤小說的功能。
 * 使用 Fetch API 執行 GET 請求
 * @param {HTMLElement} btn - 觸發事件的按鈕 DOM 元素。
 * @param {string|number} id - 小說的 ID。
 */
function FollowNovel(btn, id) {
    const url = `/Follow/FollowNovel?id=${encodeURIComponent(id)}`;

    fetch(url, {
        method: 'GET'
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP 錯誤! 狀態碼: ${response.status}`);
            }
        })
        .then(() => {
            if (btn.classList.contains("btn-primary")) {
                btn.classList.remove("btn-primary");
                btn.classList.add("btn-secondary");
                btn.textContent = "取消追蹤";
            } else {
                btn.classList.remove("btn-secondary");
                btn.classList.add("btn-primary");
                btn.textContent = "追蹤創作";
            }
        })
        .catch(error => {
            alert(error.message);
            console.error('追蹤小說發生錯誤:', error);
        });
}

/**
 * 檢查通知
 */
function fetchNotifications() {
    // 找到儲存資料的容器元素
    const container = document.getElementById('notification-container');
    const notificationElement = document.getElementById('notification');

    // 檢查元素是否存在
    if (!container || !notificationElement) {
        console.error('必要元素 (notification-container 或 notification) 不存在。');
        return;
    }

    // 從 Data Attributes 讀取 URL 和 User ID
    // data-notification-url 會對應到 dataset.notificationUrl (注意駝峰命名法)
    const url = container.dataset.notificationUrl;
    // data-user-id 會對應到 dataset.userId
    const userId = container.dataset.userId;

    if (!url || !userId) {
        console.error('缺少必要的 Data Attribute (notification-url 或 user-id)。');
        //notificationElement.innerHTML = '<p style="color: red;">設定錯誤：無法載入通知。</p>'; // 沒有登入的情況，不顯示通知icon就好
        return;
    }

    const fullUrl = `${url}?userId=${userId}`;

    fetch(fullUrl, {
        method: 'GET'
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP 錯誤! 狀態碼: ${response.status}`);
            }
            return response.text();
        })
        .then(result => {
            notificationElement.innerHTML = result;
        })
        .catch(error => {
            // 處理錯誤
            console.error('獲取通知失敗:', error);
            notificationElement.innerHTML = '<p style="color: red;">載入通知失敗。</p>';
        });
}

/**
 * 讀取通知
 * @param {string} id - 使用者 ID
 * @param {string} url - 要發送請求的 URL
 */
async function ReadNotification(id, url) {
    // 原生 DOM 操作
    const notificationStack = document.getElementById("notificationStack");
    if (notificationStack) {
        notificationStack.removeAttribute("data-count");
    }

    // 取得 Anti-Forgery Token
    const tokenElement = document.querySelector('input[type="hidden"][name="__RequestVerificationToken"]');
    const token = tokenElement ? tokenElement.value : null;

    if (!token) {
        console.error('Anti-forgery token (__RequestVerificationToken) not found.');
        alert('無法讀取通知 (缺少 token)');
        return;
    }

    // 準備 POST 資料
    const formData = new URLSearchParams();
    formData.append('userId', id);

    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                // 設定標頭
                'X-CSRF-TOKEN': token,
                'Content-Type': 'application/x-www-form-urlencoded'
            },
            body: formData // 傳送 URL-encoded 的資料
        });

        if (!response.ok) {
            alert(`Error: ${response.statusText}`);
        }

    } catch (err) {
        alert(`Network error: ${err.message}`);
    }

    const notificationClick = document.getElementById("notificationClick");
    if (notificationClick) {
        notificationClick.removeAttribute("onclick");
    }
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
        // e.target 就是 modal 元素本人
        e.target.remove();
    }
});

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
