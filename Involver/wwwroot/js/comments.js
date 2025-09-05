import { createApp } from 'vue';

createApp({
    data() {
        return {
            comments: [],
            pagination: {},
            isLoading: true,
            sortBy: 'oldest',
            from: '',
            fromID: 0,
            isOrderFixed: false,
            editingComment: null, // null for new, comment object for editing
            newCommentContent: '' // For CKEditor content
        };
    },
    mounted() {
        const el = document.getElementById('comment-app');
        this.from = el.dataset.from;
        this.fromID = parseInt(el.dataset.fromId, 10);
        this.isOrderFixed = el.dataset.isOrderFixed === 'true';
        if (this.isOrderFixed) {
            this.sortBy = 'oldest';
        }
        this.fetchComments(1);
    },
    methods: {
        async fetchComments(page = 1) {
            this.isLoading = true;
            try {
                const response = await fetch(`/api/comments?from=${this.from}&fromID=${this.fromID}&page=${page}&sortBy=${this.sortBy}`);
                if (!response.ok) throw new Error('Failed to fetch comments');
                
                const paginationHeader = JSON.parse(response.headers.get('X-Pagination'));
                this.pagination = {
                    currentPage: paginationHeader.PageIndex,
                    totalPages: paginationHeader.TotalPages,
                    hasNext: paginationHeader.HasNextPage,
                    hasPrevious: paginationHeader.HasPreviousPage
                };

                const data = await response.json();
                this.comments = data.map(c => ({ ...c, isEditing: false, editableContent: '' }));
            } catch (error) {
                console.error(error);
            } finally {
                this.isLoading = false;
            }
        },
        async submitComment() {
            const content = window.CKEDITOR.instances['comment-editor'].getData();
            if (!content) return;

            if (this.editingComment) {
                // Update existing comment
                await this.saveComment({ ...this.editingComment, editableContent: content });
            } else {
                // Add new comment
                await this.addNewComment(content);
            }

            bootstrap.Modal.getInstance(document.getElementById('commentModal')).hide();
        },
        async addNewComment(content) {
            try {
                const response = await fetch('/api/comments', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ content: content, from: this.from, fromID: this.fromID })
                });
                if (!response.ok) throw new Error('Failed to add comment');
                await this.fetchComments(this.pagination.totalPages || 1); // Go to last page
            } catch (error) {
                console.error(error);
            }
        },
        async toggleAgree(comment) {
            const originalAgreesCount = comment.agreesCount;
            const originalIsAgreed = comment.isAgreedByCurrentUser;

            // Optimistic update
            comment.isAgreedByCurrentUser = !originalIsAgreed;
            comment.agreesCount += originalIsAgreed ? -1 : 1;

            try {
                const response = await fetch(`/api/comments/${comment.commentID}/agree`, { method: 'POST' });
                if (!response.ok) throw new Error('Failed to toggle agree');
                const data = await response.json();
                comment.agreesCount = data.agreesCount; // Sync with server
            } catch (error) {
                console.error(error);
                // Revert on failure
                comment.agreesCount = originalAgreesCount;
                comment.isAgreedByCurrentUser = originalIsAgreed;
            }
        },
        async deleteComment(comment) {
            if (!confirm('確定要刪除這則評論嗎？')) return;

            const originalComments = [...this.comments];
            this.comments = this.comments.filter(c => c.commentID !== comment.commentID);

            try {
                const response = await fetch(`/api/comments/${comment.commentID}`, { method: 'DELETE' });
                if (!response.ok) throw new Error('Failed to delete comment');
            } catch (error) {
                console.error(error);
                this.comments = originalComments; // Revert on failure
            }
        },
        async saveComment(comment) {
            const originalContent = comment.content;
            comment.content = comment.editableContent;
            comment.isEditing = false;

            try {
                const response = await fetch(`/api/comments/${comment.commentID}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ content: comment.editableContent })
                });
                if (!response.ok) throw new Error('Failed to save comment');
            } catch (error) {
                console.error(error);
                comment.content = originalContent; // Revert on failure
            }
            this.editingComment = null;
        },
        editComment(comment) {
            this.editingComment = comment;
            this.showNewCommentModal(comment.content);
        },
        showNewCommentModal(content = '') {
            const modal = new bootstrap.Modal(document.getElementById('commentModal'));
            modal.show();
            
            if (window.CKEDITOR.instances['comment-editor']) {
                window.CKEDITOR.instances['comment-editor'].setData(content);
            } else {
                window.CKEDITOR.replace('comment-editor', {
                    on: {
                        instanceReady: function(evt) {
                            evt.editor.setData(content);
                        }
                    }
                });
            }
        },
        changePage(page) {
            if (page < 1 || page > this.pagination.totalPages) return;
            this.fetchComments(page);
        },
        changeSort() {
            this.fetchComments(1);
        }
    }
}).mount('#comment-app');
