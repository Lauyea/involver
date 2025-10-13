import { createApp } from 'vue';

const app = createApp({
    data() {
        return {
            votings: [],
            isLoading: true,
            error: null,
            selectedOptions: {},
            voteSettings: {},
            newVoting: {
                title: '',
                policy: 0,
                limit: 0,
                threshold: 0,
                numberLimit: null,
                coinLimit: null,
                deadLine: '',
                options: [
                    { content: '' },
                    { content: '' },
                    { content: '' }
                ]
            },
            episodeId: null,
            isProfessional: false
        };
    },
    mounted() {
        const el = document.getElementById('voting-app');
        this.episodeId = parseInt(el.dataset.episodeId, 10);
        this.isProfessional = el.dataset.isProfessional === 'true';
        this.fetchVotingsAsync();
    },
    watch: {
        votings(newVotings) {
            this.$nextTick(() => {
                newVotings.forEach(voting => {
                    // Initialize vote settings for each new voting
                    if (!this.voteSettings[voting.votingID]) {
                        this.voteSettings[voting.votingID] = {
                            isVirtual: true, // Default to virtual
                            value: voting.threshold // Default to minimum price
                        };
                    }

                    if (voting.limit === 0 && voting.deadLine && !voting.end) {
                        this.countDown(voting.deadLine, `CountDownID${voting.votingID}`);
                    }
                });
            });
        }
    },
    methods: {
        countDown(countDownTime, id) {
            var countDownDate = new Date(countDownTime).getTime();

            var x = setInterval(() => {
                var now = new Date().getTime();
                var distance = countDownDate - now;

                var days = Math.floor(distance / (1000 * 60 * 60 * 24));
                var hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
                var minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
                var seconds = Math.floor((distance % (1000 * 60)) / 1000);

                var element = document.getElementById(id);
                if (element) {
                    if (distance < 0) {
                        clearInterval(x);
                        element.innerHTML = "投票已截止";
                        // Optionally, find the voting and set end = true
                        const voting = this.votings.find(v => `CountDownID${v.votingID}` === id);
                        if(voting) {
                            voting.end = true;
                        }
                    } else {
                        element.innerHTML = `剩餘: ${days}天 ${hours}時 ${minutes}分 ${seconds}秒 `;
                    }
                }
            }, 1000);
        },
        getDisplayText(voting, option) {
            return voting.policy === 1 ? `${option.totalCoins} In幣` : `${option.votesCount} 票`;
        },
        getPercent(voting, option) {
            if (voting.policy === 1) {
                const totalCoins = voting.totalCoins;
                if (totalCoins === 0) return "0";
                const ratio = (option.totalCoins / totalCoins) * 100;
                return ratio.toFixed(0);
            } else {
                const totalNumber = voting.totalNumber;
                if (totalNumber === 0) return "0";
                const ratio = (option.votesCount / totalNumber) * 100;
                return ratio.toFixed(0);
            }
        },
        async castVoteAsync(votingId) {
            const optionId = this.selectedOptions[votingId];
            if (!optionId) {
                alert('請選擇一個選項');
                return;
            }

            const voting = this.votings.find(v => v.votingID === votingId);
            const settings = this.voteSettings[votingId];

            let value = voting.policy === 0 ? voting.threshold : settings.value;

            if (value < voting.threshold) {
                alert(`票價至少需要 ${voting.threshold} In幣。`);
                return;
            }

            const payload = {
                votingId: votingId,
                optionId: optionId,
                value: value,
                isVirtual: settings.isVirtual
            };

            try {
                const response = await fetch('/api/v1/votings/cast', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload),
                });

                if (response.ok) {
                    const result = await response.json();

                    // Show achievement toasts
                    if (result.toasts && result.toasts.length > 0) {
                        showGlobalToasts(result.toasts);
                    }

                    // Show mission completion message
                    if (result.missionMessage) {
                        alert(result.missionMessage);
                    }

                    this.fetchVotingsAsync(); // Refresh UI
                } else {
                    const errorData = await response.text();
                    alert(`投票失敗: ${errorData}`);
                }
            } catch (err) {
                this.error = err.message;
                console.error(err);
                alert(`發生錯誤: ${err.message}`);
            }
        },
        async createVotingAsync() {
            const validOptions = this.newVoting.options.filter(opt => opt.content.trim() !== '');

            if (validOptions.length < 2) {
                alert('At least two voting options are required.');
                return;
            }

            for (const opt of validOptions) {
                if (opt.content.trim().length < 2) {
                    alert('Option content must be at least 2 characters long.');
                    return;
                }
            }

            const payload = {
                episodeId: this.episodeId,
                ...this.newVoting
            };

            if (payload.deadLine === '') {
                payload.deadLine = null;
            }

            try {
                const response = await fetch('/api/v1/votings', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload),
                });

                if (response.ok) {
                    $('#createVotingModal').modal('hide');
                    this.fetchVotingsAsync();
                }
                if (response.status === 401) {
                    alert('請先登入，才能建立投票。');
                    window.location.href = '/Identity/Account/Login';
                } else {
                    const errorText = await response.text();
                    try {
                        const errorData = JSON.parse(errorText);
                        // Handle validation errors from ModelState
                        let errorMessage = "Please correct the following errors:\n";
                        for (const key in errorData.errors) {
                            errorMessage += `- ${errorData.errors[key].join('\n- ')}\n`;
                        }
                        alert(errorMessage);
                    } catch (e) {
                        // Handle simple string errors
                        alert(`Error: ${errorText}`);
                    }
                }
            } catch (err) {
                this.error = err.message;
                console.error(err);
            }
        },
        addOption() {
            if (this.newVoting.options.length < 5) {
                this.newVoting.options.push({ content: '' });
            }
        },
        removeOption() {
            if (this.newVoting.options.length > 2) {
                this.newVoting.options.pop();
            }
        },
        showCreateVotingModal() {
            if (!this.isProfessional) {
                this.newVoting.policy = 0; // Force policy to Equality for non-pro users
            }
            $('#createVotingModal').modal('show');
        },
        async fetchVotingsAsync() {
            this.isLoading = true;
            this.error = null;
            try {
                const response = await fetch(`/api/v1/votings/ByEpisode/${this.episodeId}`);
                if (response.ok) {
                    const data = await response.json();
                    this.votings = data;

                    // 同步建立 voteSettings，避免 undefined
                    data.forEach(voting => {
                        if (!this.voteSettings[voting.votingID]) {
                            this.voteSettings[voting.votingID] = {
                                isVirtual: true,
                                value: voting.threshold
                            };
                        }
                    });
                } else if (response.status === 404) {
                    this.votings = [];
                } else {
                    throw new Error('Failed to fetch votings');
                }
            } catch (err) {
                this.error = err.message;
                console.error(err);
            } finally {
                this.isLoading = false;
            }
        }
    }
});

app.mount('#voting-app');
