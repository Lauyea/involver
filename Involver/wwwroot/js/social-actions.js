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