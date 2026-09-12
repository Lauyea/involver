const { createApp } = Vue;

const app = createApp({
    data() {
        return {
            token: '',
            password: '',
            revokePassword: '',
            isLoading: false,
            isRevoking: false,
            error: null,
            revokeError: null,
            revokeSuccess: null,
            isExpired: false,
            isRevoked: false,
            showRevokeModal: false,
            preview: null
        };
    },
    mounted() {
        const el = document.getElementById('preview-app');
        if (el) {
            this.token = el.dataset.token || '';
            this.isExpired = el.dataset.isExpired === 'true';
            this.isRevoked = el.dataset.isRevoked === 'true';
        }

        // 容錯機制：如果 dataset 內沒有取到 token，由當前 URL 路徑解析
        if (!this.token) {
            const match = window.location.pathname.match(/\/Preview\/([^\/?#]+)/i);
            if (match) {
                this.token = decodeURIComponent(match[1]);
            }
        }

        this.$nextTick(() => {
            if (!this.isExpired && !this.isRevoked && this.$refs.passwordInput) {
                this.$refs.passwordInput.focus();
            }
        });
    },
    methods: {
        async submitPassword() {
            if (!this.password || !this.password.trim()) {
                this.error = '請輸入閱讀密碼。';
                return;
            }

            if (!this.token) {
                const match = window.location.pathname.match(/\/Preview\/([^\/?#]+)/i);
                if (match) {
                    this.token = decodeURIComponent(match[1]);
                }
            }

            if (!this.token) {
                this.error = '找不到試閱代碼 (Token)，請確認網址是否完整。';
                return;
            }

            this.isLoading = true;
            this.error = null;

            try {
                const response = await fetch(`/api/v1/previews/${encodeURIComponent(this.token)}/content`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ password: this.password })
                });

                let data = {};
                try {
                    const text = await response.text();
                    try {
                        data = JSON.parse(text);
                    } catch {
                        data = { message: text };
                    }
                } catch {
                    data = { message: '無法讀取伺服器回應' };
                }

                if (response.ok) {
                    this.preview = data;
                    // 安全要求：不可儲存密碼至 URL、localStorage 或 sessionStorage
                    this.password = '';
                    this.error = null;
                } else if (response.status === 410) {
                    if (data.message && data.message.includes('撤銷')) {
                        this.isRevoked = true;
                    } else {
                        this.isExpired = true;
                    }
                    this.error = data.message || '此試閱已失效。';
                } else if (response.status === 429) {
                    this.error = '嘗試次數過多，請稍後再試。';
                } else {
                    this.error = data.message || '密碼錯誤或驗證失敗。';
                }
            } catch (err) {
                console.error('Fetch error:', err);
                this.error = '無法連線至伺服器，請檢查網路後稍後再試。';
            } finally {
                this.isLoading = false;
            }
        },

        async submitRevoke() {
            if (!this.revokePassword || !this.revokePassword.trim()) {
                this.revokeError = '請輸入撤銷密碼。';
                return;
            }

            if (!this.token) {
                const match = window.location.pathname.match(/\/Preview\/([^\/?#]+)/i);
                if (match) {
                    this.token = decodeURIComponent(match[1]);
                }
            }

            this.isRevoking = true;
            this.revokeError = null;
            this.revokeSuccess = null;

            try {
                const response = await fetch(`/api/v1/previews/${encodeURIComponent(this.token)}/revoke`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ revokePassword: this.revokePassword })
                });

                let data = {};
                try {
                    const text = await response.text();
                    try {
                        data = JSON.parse(text);
                    } catch {
                        data = { message: text };
                    }
                } catch {
                    data = { message: '無法讀取伺服器回應' };
                }

                if (response.ok) {
                    this.revokeSuccess = data.message || '試閱已成功撤銷。';
                    this.isRevoked = true;
                    this.preview = null;
                    this.revokePassword = '';
                    setTimeout(() => {
                        this.showRevokeModal = false;
                        this.revokeSuccess = null;
                    }, 1800);
                } else if (response.status === 429) {
                    this.revokeError = '嘗試次數過多，請稍後再試。';
                } else {
                    this.revokeError = data.message || '撤銷失敗，請確認撤銷密碼是否正確。';
                }
            } catch (err) {
                console.error('Revoke fetch error:', err);
                this.revokeError = '無法連線至伺服器，請稍後再試。';
            } finally {
                this.isRevoking = false;
            }
        },

        closeRevokeModal() {
            if (this.isRevoking) return;
            this.showRevokeModal = false;
            this.revokePassword = '';
            this.revokeError = null;
            this.revokeSuccess = null;
        },

        lockAgain() {
            this.preview = null;
            this.password = '';
            this.error = null;
            this.$nextTick(() => {
                if (this.$refs.passwordInput) {
                    this.$refs.passwordInput.focus();
                }
            });
        },

        formatDate(dateString) {
            if (!dateString) return '';
            const date = new Date(dateString);
            return date.toLocaleString();
        }
    }
});

app.mount('#preview-app');
