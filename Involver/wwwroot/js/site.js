// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.

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
            alert(err)
        }
    }).done(function () {
        $(btn).toggleClass('disabled  ');
        if ($(btn).text() === "追蹤創作") {
            $(btn).text("取消追蹤");
        }
        else {
            $(btn).text("追蹤創作");
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