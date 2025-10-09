import { createApp } from 'vue';

const app = createApp({
    data() {
        return {
            votings: [],
            isLoading: true,
            error: null,
            selectedOptions: {},
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
            episodeId: null
        };
    },
    mounted() {
        const el = document.getElementById('voting-app');
        this.episodeId = parseInt(el.dataset.episodeId, 10);
        this.fetchVotingsAsync();
    },
    methods: {
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
                alert('Please select an option.');
                return;
            }

            try {
                const response = await fetch('/api/v1/votings/cast', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ votingId, optionId }),
                });

                if (response.ok) {
                    this.fetchVotingsAsync();
                } else {
                    const errorData = await response.text();
                    alert(`Error: ${errorData}`);
                }
            } catch (err) {
                this.error = err.message;
                console.error(err);
            }
        },
        async createVotingAsync() {
            const payload = {
                episodeId: this.episodeId,
                ...this.newVoting
            };

            try {
                const response = await fetch('/api/v1/votings', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload),
                });

                if (response.ok) {
                    $('#createVotingModal').modal('hide');
                    this.fetchVotingsAsync();
                } else {
                    const errorData = await response.json();
                    alert(`Error: ${JSON.stringify(errorData)}`);
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
            $('#createVotingModal').modal('show');
        },
        async fetchVotingsAsync() {
            this.isLoading = true;
            this.error = null;
            try {
                const response = await fetch(`/api/v1/votings/ByEpisode/${this.episodeId}`);
                if (response.ok) {
                    this.votings = await response.json();
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
