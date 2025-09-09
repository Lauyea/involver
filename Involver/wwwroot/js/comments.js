import { createApp, markRaw } from 'vue';

const app = createApp({
    data() {
        return {
            comments: [],
            pagination: { currentPage: 1, totalPages: 1 },
            isLoading: true,
            sortBy: 'oldest',
            from: '',
            fromID: 0,
            isOrderFixed: false,
            modalInstance: null,
            mainEditor: null, // For the modal
            inlineEditors: {}, // For inline editing, keyed by commentID
            newCommentDice: {
                rollTimes: 0,
                diceSides: 0
            }
        };
    },
    computed: {
        pages() {
            const pages = [];
            let startPage = Math.max(1, this.pagination.currentPage - 2);
            let endPage = Math.min(this.pagination.totalPages, this.pagination.currentPage + 2);

            if (endPage - startPage < 4) {
                if (startPage === 1) {
                    endPage = Math.min(this.pagination.totalPages, 5);
                } else {
                    startPage = Math.max(1, this.pagination.totalPages - 4);
                }
            }

            for (let i = startPage; i <= endPage; i++) {
                pages.push(i);
            }
            return pages;
        }
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

        const modalEl = document.getElementById('commentModal');
        this.modalInstance = new bootstrap.Modal(modalEl);
        // Create editor when modal is shown for the first time
        modalEl.addEventListener('shown.bs.modal', () => {
            if (!this.mainEditor) {
                ClassicEditor
                    .create(document.querySelector('#comment-editor'))
                    .then(editor => {
                        this.mainEditor = markRaw(editor);
                    })
                    .catch(error => {
                        console.error('Error creating main CKEditor instance:', error);
                    });
            } else {
                this.mainEditor.setData('');
            }
        });
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
                    hasNextPage: paginationHeader.HasNextPage,
                    hasPreviousPage: paginationHeader.HasPreviousPage
                };

                const data = await response.json();
                this.comments = data.map(c => ({ ...c, isEditing: false }));
            } catch (error) {
                console.error(error);
            } finally {
                this.isLoading = false;
            }
        },
        showNewCommentModal() {
            this.newCommentDice = { rollTimes: 0, diceSides: 0 };
            this.modalInstance.show();
        },
        async submitComment() {
            if (!this.mainEditor) return;
            const content = this.mainEditor.data.get();
            if (!content) return;

            await this.addNewComment(content);
            this.modalInstance.hide();
        },
        async addNewComment(content) {
            try {
                const response = await fetch('/api/comments', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        content: content,
                        from: this.from,
                        fromID: this.fromID,
                        rollTimes: (this.newCommentDice && this.newCommentDice.rollTimes) || 0,
                        diceSides: (this.newCommentDice && this.newCommentDice.diceSides) || 0
                    })
                });
                if (!response.ok) throw new Error('Failed to add comment');
                await this.fetchComments(this.pagination.totalPages || 1);
            } catch (error) {
                console.error(error);
            }
        },
        async toggleAgree(comment) {
            const originalAgreesCount = comment.agreesCount;
            const originalIsAgreed = comment.isAgreedByCurrentUser;
            comment.isAgreedByCurrentUser = !originalIsAgreed;
            comment.agreesCount += originalIsAgreed ? -1 : 1;

            try {
                const response = await fetch(`/api/comments/${comment.commentID}/agree`, { method: 'POST' });
                if (!response.ok) throw new Error('Failed to toggle agree');
                const data = await response.json();
                comment.agreesCount = data.agreesCount;
            } catch (error) {
                console.error(error);
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
                this.comments = originalComments;
            }
        },
        async toggleBlock(comment) {
            const originalIsBlocked = comment.isBlocked;
            comment.isBlocked = !comment.isBlocked;
            try {
                const response = await fetch(`/api/comments/${comment.commentID}/block`, { method: 'POST' });
                if (!response.ok) throw new Error('Failed to toggle block');
                const data = await response.json();
                comment.isBlocked = data.isBlocked;
            } catch (error) {
                console.error(error);
                comment.isBlocked = originalIsBlocked;
            }
        },
        toggleEdit(comment) {
            comment.isEditing = !comment.isEditing;
            if (comment.isEditing) {
                // Entering edit mode
                this.$nextTick(() => {
                    ClassicEditor
                        .create(document.querySelector(`#editor-${comment.commentID}`))
                        .then(editor => {
                            this.inlineEditors[comment.commentID] = markRaw(editor);
                            editor.setData(comment.content);
                        })
                        .catch(error => {
                            console.error(`Error creating inline editor for comment ${comment.commentID}:`, error);
                        });
                });
            } else {
                // Canceling edit mode
                const editor = this.inlineEditors[comment.commentID];
                if (editor) {
                    editor.destroy();
                    delete this.inlineEditors[comment.commentID];
                }
            }
        },
        async submitEdit(comment) {
            const editor = this.inlineEditors[comment.commentID];
            if (!editor) return;

            const newContent = editor.data.get();
            await this.saveComment(comment, newContent);

            comment.isEditing = false;
            editor.destroy();
            delete this.inlineEditors[comment.commentID];
        },
        async saveComment(comment, newContent) {
            const originalContent = comment.content;
            comment.content = newContent;
            try {
                const response = await fetch(`/api/comments/${comment.commentID}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ content: newContent })
                });
                if (!response.ok) throw new Error('Failed to save comment');
            } catch (error) {
                console.error(error);
                comment.content = originalContent;
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
});

app.mount('#comment-app');