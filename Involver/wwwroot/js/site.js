// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.

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
            <div id="${toastId}" class="toast mb-3" role="alert" aria-live="assertive" aria-atomic="true" data-bs-autohide="false">
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
        // (Bootstrap 會自動從 data-bs-autohide="false" 讀取設定)
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
 * DOM 載入完成後執行的統一初始化代碼
 */
document.addEventListener('DOMContentLoaded', () => {

    // --- 1. 通知相關 ---
    fetchNotifications();

    // --- 2. 複製連結按鈕邏輯 ---
    const copyLinkTooltips = document.querySelectorAll('.btn-copy-link');
    copyLinkTooltips.forEach(tooltipTriggerEl => {
        new bootstrap.Tooltip(tooltipTriggerEl, {
            trigger: 'manual',
            placement: 'bottom'
        });
    });

    // 使用事件委派附加點擊處理器
    document.addEventListener('click', (e) => {
        const copyButton = e.target.closest('.btn-copy-link');
        if (copyButton) {
            e.preventDefault();
            copyShareLinkAndTrackAsync(copyButton);
        }
    });

    // --- 3. UI 通用邏輯 (Loading, Nav, GoTop) ---
    const loadingElement = document.getElementById('loading');
    const goTopButton = document.getElementById('gotop');
    const topHeadNav = document.querySelector('#TopHead nav');

    // 隱藏載入畫面
    if (loadingElement) {
        loadingElement.style.display = 'none';
    }

    // 啟用通用 Bootstrap Tooltips
    const commonTooltips = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    if (commonTooltips.length > 0) {
        commonTooltips.forEach(tooltipTriggerEl => {
            new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // --- 導覽列滾動效果 ---
    function handleNavScroll() {
        if (topHeadNav) {
            if (window.scrollY > 50) {
                topHeadNav.classList.add('navbar-scrolled');
            } else {
                topHeadNav.classList.remove('navbar-scrolled');
            }
        }
    }
    window.addEventListener('scroll', handleNavScroll);

    // --- GoTop & GoBottom 按鈕邏輯 ---
    const goBottomButton = document.getElementById('gobottom');
    let scrollTimeout;

    // 計時器函數，用於隱藏按鈕
    function setHideTimeout() {
        scrollTimeout = setTimeout(() => {
            if (goTopButton) goTopButton.classList.remove('visible');
            if (goBottomButton) goBottomButton.classList.remove('visible');
        }, 1500);
    }

    function handleScrollButtons() {
        // 顯示按鈕
        if (goTopButton) goTopButton.classList.add('visible');
        if (goBottomButton) goBottomButton.classList.add('visible');
        // 清除之前的計時器並重新設定
        clearTimeout(scrollTimeout);
        setHideTimeout();
    }
    window.addEventListener('scroll', handleScrollButtons);

    function setupButtonEvents(button) {
        if (button) {
            // 滑鼠移入時，清除計時器，保持按鈕可見
            button.addEventListener('mouseenter', () => clearTimeout(scrollTimeout));
            // 滑鼠移出時，重新設定計時器
            button.addEventListener('mouseleave', setHideTimeout);
        }
    }
    setupButtonEvents(goTopButton);
    setupButtonEvents(goBottomButton);

    if (goTopButton) {
        goTopButton.addEventListener('click', function (e) {
            e.preventDefault();
            window.scrollTo({ top: 0, behavior: 'smooth' });
            this.blur();
        });
    }

    if (goBottomButton) {
        goBottomButton.addEventListener('click', function (e) {
            e.preventDefault();
            window.scrollTo({ top: document.documentElement.scrollHeight, behavior: 'smooth' });
            this.blur();
        });
    }

    // 取 sticky nav 高度，避免 heading id anchor 被擋住
    const topHead = document.querySelector('#TopHead');
    const headerHeight = topHead ? topHead.offsetHeight + 16 : 0; // +16px，不要完全貼合 sticky nav

    // 寫到 root 的 CSS 變數
    document.documentElement.style.setProperty('--top-head-height', `${headerHeight}px`);
});