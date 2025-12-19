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