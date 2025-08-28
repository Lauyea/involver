const commentsApp = {
    data() {
        return {
            comments: [],
            isLoading: true,
            episodeId: null
        };
    },
    mounted() {
        const el = document.getElementById('comments-app');
        this.episodeId = el.dataset.episodeId;
        this.fetchComments();
    },
    methods: {
        async fetchComments() {
            this.isLoading = true;
            try {
                const response = await fetch(`/api/CommentsApi/${this.episodeId}`);
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                this.comments = await response.json();
            } catch (error) {
                console.error('There has been a problem with your fetch operation:', error);
            } finally {
                this.isLoading = false;
            }
        },
        formatDate(dateString) {
            const options = { year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' };
            return new Date(dateString).toLocaleDateString(undefined, options);
        }
    }
};

Vue.createApp(commentsApp).mount('#comments-app');
