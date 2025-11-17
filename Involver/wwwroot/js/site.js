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
            <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true" data-autohide="false">
                <div class="toast-header">
                    ${badgeClass ? `<span class="dot mr-2 ${badgeClass}"></span>` : ''}
                    <strong class="mr-auto">${toast.header}</strong>
                    <button type="button" class="ml-2 mb-1 close" data-dismiss="toast" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
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

//images add img-fluid class
//$("img").addClass("img-fluid");

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
 * Handles copying a share link to the clipboard and tracking the share event.
 * @param {HTMLElement} btn - The button element that was clicked.
 */
async function copyShareLinkAndTrackAsync(btn) {
    const $btn = $(btn);
    const contentType = $btn.data('type');
    const contentId = $btn.data('id');
    const baseUrl = `${window.location.protocol}//${window.location.host}`;
    // Construct the URL based on content type
    const shareUrl = `${baseUrl}/${contentType}s/Details/${contentId}`;

    try {
        await navigator.clipboard.writeText(shareUrl);

        // Show tooltip
        $btn.tooltip('show');
        setTimeout(() => $btn.tooltip('hide'), 2000); // Hide after 2 seconds

        // Get the anti-forgery token
        const token = $('input[name="__RequestVerificationToken"]').val();

        // Send tracking request to the API
        await $.ajax({
            url: '/api/shares',
            type: 'POST',
            contentType: 'application/json',
            headers: {
                'RequestVerificationToken': token
            },
            data: JSON.stringify({
                contentId: contentId,
                contentType: contentType
            }),
            error: function (xhr) {
                if (xhr.status === 401) {
                    // Silently fail if not logged in, or prompt to log in
                    console.log('User not authenticated for tracking share.');
                } else {
                    console.error('Error tracking share event.');
                }
            }
        });

    } catch (err) {
        console.error('Failed to copy: ', err);
        // Optionally, provide feedback to the user that the copy failed
    }
}

$(function () {
    // Initialize tooltips for all copy buttons
    $('.btn-copy-link').each(function () {
        $(this).tooltip({
            trigger: 'manual',
            placement: 'bottom'
        });
    });

    // Attach click handler
    $(document).on('click', '.btn-copy-link', function (e) {
        e.preventDefault();
        copyShareLinkAndTrackAsync(this);
    });
});

function FollowAuthor(btn, id) {
    $.ajax({
        method: 'get',
        url: "/Follow/FollowAuthor?id=" + id,
        error: function (xhr, status, err) {
            alert(err)
        }
    }).done(function () {
        $(btn).toggleClass('disabled  ');
        if ($(btn).text() === "追蹤作者") {
            $(btn).text("取消追蹤");
        }
        else {
            $(btn).text("追蹤作者");
        }
    });
}

function FollowNovel(btn, id) {
    $.ajax({
        method: 'get',
        url: "/Follow/FollowNovel?id=" + id,
        error: function (xhr, status, err) {
            alert(err);
        }
    }).done(function () {
        let $btn = $(btn);
        // 切換狀態
        if ($btn.hasClass("btn-primary")) {
            $btn.removeClass("btn-primary").addClass("btn-secondary");
            $btn.text("取消追蹤");
        } else {
            $btn.removeClass("btn-secondary").addClass("btn-primary");
            $btn.text("追蹤創作");
        }
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

/*觀看紀錄 Modal 操作*/
$(document).on('click', '.view-chart-trigger', function (e) {
    e.preventDefault();

    var type = $(this).data('type');
    var id = $(this).data('id');
    var url = `/StatisticalData/PartialViewRecord?handler=ViewRecord&type=${type}&id=${id}`;

    $('#modal').load(url, function () {
        $('#viewRecordModal').modal('show');
    });
});

$(document).on('hidden.bs.modal', '#viewRecordModal', function () {
    $(this).remove();
});
/*觀看紀錄 Modal 操作*/

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

    function loadMoreIfNeeded() {
        if (noMoreData || isLoading) {
            return;
        }

        // 判斷是否滾到接近底部，或是頁面高度不足以出現滾動條
        const nearBottom = $(window).scrollTop() + $(window).height() >= $(document).height() - 200;
        const notScrollable = $(document).height() <= $(window).height() + 200;

        if (nearBottom || notScrollable) {
            isLoading = true;
            $(loadingSelector).show();
            page++;

            const requestData = {
                handler: handlerName,
                pageIndex: page,
                ...additionalParams
            };

            $.ajax({
                url: window.location.pathname,
                type: 'GET',
                data: requestData,
                success: function (data) {
                    if (data.trim().length > 0) {
                        $(containerSelector).append(data);
                        isLoading = false;
                        $(loadingSelector).hide();
                        // 再次檢查是否需要載入更多
                        loadMoreIfNeeded();
                    } else {
                        noMoreData = true;
                        $(loadingSelector).hide();
                    }
                },
                error: function () {
                    isLoading = false;
                    $(loadingSelector).hide();
                    console.error('Error loading more content.');
                }
            });
        }
    }

    // 綁定滾動事件
    $(window).scroll(loadMoreIfNeeded);

    // 初始化時先檢查一次（避免畫面太短不觸發）
    loadMoreIfNeeded();
}
