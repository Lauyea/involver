// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.

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
            <div id="${toastId}" class="toast" data-autohide="false">
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
        $(toastElement).toast('show');
        // Remove the element from the DOM after it has been hidden
        $(toastElement).on('hidden.bs.toast', function () {
            $(this).remove();
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

function AgreeMessage(agreeBtn, messageId) {
    $.ajax({
        method: 'get',
        url: "/Agree/AgreeMessage?messageId=" + messageId,
        error: function (xhr, status, err) {
            if (xhr.status === 401 || xhr.status === 403) {
                alert("請先登入");
            }
            else {
                alert("系統錯誤：未搜索到指定評論");
            }
        }
    }).done(function (res) {
        $(agreeBtn).find('span').text(res);
        GetAgreeToasts();
    });
}

function AgreeComment(agreeBtn, commentId) {
    $.ajax({
        method: 'get',
        url: "/Agree/AgreeComment?commentId=" + commentId,
        error: function (xhr, status, err) {
            if (xhr.status === 401 || xhr.status === 403) {
                alert("請先登入");
            }
            else {
                alert("系統錯誤：未搜索到指定評論");
            }
        }
    }).done(function (res) {
        $(agreeBtn).find('span').text(res);
        GetAgreeToasts();
    });
}

function GetAgreeToasts() {
    $.ajax({
        method: 'get',
        url: "/Toast/GetAgreeToasts",
        error: function (xhr, status, err) {
            if (xhr.status === 401 || xhr.status === 403) {
                alert("請先登入");
            }
            else {
                alert("系統錯誤：未搜索到指定評論");
            }
        }
    }).done(function (res) {
        $("#toasts").html(res);
        $('.toast').toast('show');
    });
}

function SetDarkMode() {
    $.ajax({
        method: 'get',
        url: "/DarkMode/Set",
        error: function (xhr, status, err) {
            alert(err)
        }
    }).done(function () {
        $("#theme-icon").toggleClass('fa-sun fa-moon');
        $("#layout-body").toggleClass('bootstrap-dark bootstrap');
    });
}

function Share() {
    $.ajax({
        method: 'get',
        url: "/Share/Get",
        error: function (xhr, status, err) {
            alert(err)
        },
        success: function (res) {
            if (res === "") {
                $("#myTooltip").html("複製成功");
            }
            else {
                $("#myTooltip").html(res);
            }
        }
    });
}

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

function ReadNotification(id, url) {
    document.getElementById("notificationStack").removeAttribute("data-count");

    $.ajax({
        method: 'post',
        url: url,
        beforeSend: function (xhr) {
            xhr.setRequestHeader("X-CSRF-TOKEN",
                $('input:hidden[name="__RequestVerificationToken"]').val());
        },
        data: { userId: id },
        error: function (xhr, status, err) {
            alert(err)
        }
    });

    document.getElementById("notificationClick").removeAttribute("onclick");
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
