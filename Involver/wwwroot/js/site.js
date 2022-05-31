// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.
//$("img").addClass("img-fluid");

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
    });
}

function SetDarkMode() {
    $.ajax({
        method: 'get',
        url: "/DarkMode/Set",
        error: function (xhr, status, err) {
            alert(err)
        }
    });
}