document.addEventListener("DOMContentLoaded", function () {
    
    // 尋找頁面上所有的提交按鈕
    var submitButtons = document.querySelectorAll('button[type="submit"], input[type="submit"]');

    submitButtons.forEach(function (button) {
        // 因為 submit 會在 Constraint Validation 之後才觸發，所以改監聽 click 事件。
        button.addEventListener('click', function (event) {
            
            // 立即停止預設行為 (提交表單)
            event.preventDefault(); 

            const form = button.closest('form'); // 找到這個按鈕所屬的表單
            if (!form) {
                return; // 如果按鈕不在表單中，不做任何事
            }

            const needsRecaptcha = form.classList.contains('needs-recaptcha');

            // --- 1. CKEditor 驗證 (優先) ---
            // 解決 "textarea not focusable" 的問題
            if (typeof window.editor !== 'undefined' && window.editor && typeof window.editor.getData === 'function') {
                
                var editorTextarea = document.getElementById(window.editor.sourceElement.id);
                if (editorTextarea) {
                    editorTextarea.value = window.editor.getData(); // 同步內容
                }

                if (!window.editor.getData().trim()) {
                    alert("內容不能為空。"); 
                    return; // 驗證失敗，停止執行
                }
            }

            // --- 2. 手動觸發標準 HTML5 Constraint Validation ---
            if (!form.checkValidity()) {
                form.reportValidity(); // 顯示瀏覽器原生的驗證錯誤泡泡 
                return; // 驗證失敗，停止執行
            }

            // --- 3. 提交處理 ---
            // 此時，所有前端驗證都已通過

            // 讀取按鈕的 'formAction' 屬性。
            // 這會包含由 asp-page-handler 產生的正確 URL。
            const buttonFormAction = button.formAction;

            if (!needsRecaptcha) {
                // 將表單的 action 指向按鈕的 formaction
                form.action = buttonFormAction;

                // 如果是標準表單
                form.submit(); // 直接提交
            } else {
                // 如果是 reCAPTCHA v3 表單
                if (typeof grecaptcha === 'undefined' || !grecaptcha.ready) {
                    console.error("reCAPTCHA v3 API尚未載入。");
                    alert("無法驗證您的請求，請重新整理頁面。");
                    return;
                }

                const siteKey = form.getAttribute('data-sitekey');
                const action = form.getAttribute('data-action') || 'submit';

                if (!siteKey) {
                    console.error("表單缺少 'data-sitekey' 屬性。");
                    return;
                }
                
                // 執行 v3 驗證
                grecaptcha.ready(function() {
                    grecaptcha.execute(siteKey, { action: action }).then(function(token) {                            
                            let tokenInput = form.querySelector('input[name="g-recaptcha-response"]');
                            if (!tokenInput) {
                                tokenInput = document.createElement('input');
                                tokenInput.setAttribute('type', 'hidden');
                                tokenInput.setAttribute('name', 'g-recaptcha-response');
                                form.appendChild(tokenInput);
                            }
                            tokenInput.value = token;

                            // 將表單的 action 指向按鈕的 formaction
                            form.action = buttonFormAction;
                            
                            // 手動提交表單。因為是非同步去執行，需要等 reCAPTCHA 回應才能去 submit，
                            // submit 不能統一寫在 if 之後，只能在 function 裡面判斷完之後才能submit。
                            form.submit();

                        })
                        .catch(function (error) {
                            // 捕捉 execute 過程中的錯誤
                            console.error("reCAPTCHA v3 執行失敗: ", error);
                            alert("reCAPTCHA 驗證失敗，請檢查您的網路連線或稍後再試。");
                        });
                });
            }
        });
    });
});