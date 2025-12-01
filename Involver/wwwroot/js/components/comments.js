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
            ownerId: null,
            authorOnly: false,
            isOrderFixed: false,
            mainEditor: null, // For the modal
            inlineEditors: {}, // For inline editing, keyed by commentID
            newCommentDice: {
                rollTimes: 0,
                diceSides: 0
            },
            maxLength: 10000, // Add maxLength with a default value
            /** @type {Array<Object>} - The list of messages for the currently active comment. */
            messages: [],
            /** @type {Object|null} - The comment object whose messages are currently being viewed or added to. */
            currentCommentForMessages: null,
            /** @type {boolean} - Flag to indicate if messages are currently being loaded from the API. */
            isLoadingMessages: false,
            /** @type {string} - The content of the new message being typed in the modal form. */
            newMessageContent: ''
        };
    },
    watch: {
        authorOnly(newValue, oldValue) {
            if (newValue !== oldValue) {
                this.getCommentsAsync(1);
            }
        }
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
        this.ownerId = el.dataset.ownerId;
        this.isOrderFixed = el.dataset.isOrderFixed === 'true';
        this.maxLength = parseInt(el.dataset.maxLength, 10) || 10000; // Read maxLength from data attribute

        if (this.isOrderFixed) {
            this.sortBy = 'oldest';
        }
        this.getCommentsAsync(1);

        const commentModalEl = document.getElementById('commentModal');
        if (commentModalEl) {
            commentModalEl.addEventListener('shown.bs.modal', () => {
                if (!this.mainEditor) {
                    ClassicEditor
                    .create(document.querySelector('#comment-editor'), this.getEditorConfig()) // Use shared config
                        .then(editor => {
                            this.mainEditor = markRaw(editor);
                            const wordCountPlugin = editor.plugins.get('WordCount');
                            const wordCountWrapper = document.getElementById('comment-word-count');
                            if (wordCountPlugin && wordCountWrapper) {
                                wordCountWrapper.appendChild(wordCountPlugin.wordCountContainer);
                            }
                        })
                        .catch(error => {
                            console.error('Error creating main CKEditor instance:', error);
                        });
                } else {
                    this.mainEditor.setData('');
                }
            });
        }
    },
    methods: {
        /**
         * Initializes the correct CKEditor configuration.
         * @returns {Object} CKEditor configuration object.
         */
        getEditorConfig() {
            return {
                toolbar: {
                    items: [
                        'heading', '|', 'bold', 'italic', 'underline', 'strikethrough',
                        'fontBackgroundColor', 'fontColor', 'removeFormat', 'findAndReplace', '|',
                        'alignment', 'bulletedList', 'numberedList', 'outdent', 'indent',
                        'horizontalLine', '|', 'link', 'imageInsert', 'blockQuote', 'insertTable',
                        'mediaEmbed', 'sourceEditing', 'undo', 'redo'
                    ],
                    shouldNotGroupWhenFull: false
                },
                link: { addTargetToExternalLinks: true },
                image: { toolbar: ['imageTextAlternative', 'imageStyle:inline', 'imageStyle:block', 'imageStyle:side'] },
                table: { contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells'] },
                licenseKey: '',
                mediaEmbed: { previewsInData: true },
                wordCount: {
                    displayWords: false,
                    onUpdate: stats => {
                        if (stats.characters > this.maxLength) {
                            console.warn(`Character limit exceeded: ${stats.characters}/${this.maxLength}`);
                        }
                    }
                }
            };
        },
        /**
         * Fetches comments from the server for a given page and sorting preference.
         * @param {number} [page=1] - The page number to fetch.
         */
        async getCommentsAsync(page = 1) {
            this.isLoading = true;
            try {
                const url = `/api/v1/comments?from=${this.from}&fromID=${this.fromID}&page=${page}&sortBy=${this.sortBy}&authorOnly=${this.authorOnly}&ownerId=${this.ownerId}`;
                const response = await fetch(url);
                if (!response.ok) throw new Error('Failed to fetch comments');

                const paginationHeader = JSON.parse(response.headers.get('X-Pagination'));
                this.pagination = {
                    currentPage: paginationHeader.PageIndex,
                    totalPages: paginationHeader.TotalPages,
                    hasNextPage: paginationHeader.HasNextPage,
                    hasPreviousPage: paginationHeader.HasPreviousPage
                };

                const data = await response.json();
                this.comments = data.map(c => ({ ...c, isEditing: false, isBlockedContentVisible: false }));
            } catch (error) {
                console.error(error);
            } finally {
                this.isLoading = false;
            }
        },
        /**
         * Shows the modal for creating a new comment.
         */
        showNewCommentModal() {
            this.newCommentDice = { rollTimes: 0, diceSides: 0 };

            const modalEl = document.getElementById('commentModal');
            const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.show();
        },
        /**
         * Handles the submission from the main comment editor modal.
         */
        async createCommentFromModalAsync() {
            if (!this.mainEditor) return;
            const content = this.mainEditor.data.get();
            if (!content) {
                alert("內容不能為空。"); 
                return;
            }

            await this.createCommentAsync(content);

            const modalEl = document.getElementById('commentModal');
            const modal = bootstrap.Modal.getInstance(modalEl); // 這裡可以用 getInstance 因為已經打開了
            if (modal) {
                modal.hide();
            }
        },
        /**
         * Creates a new comment and adds it to the list.
         * @param {string} content - The HTML content of the comment.
         */
        async createCommentAsync(content) {
            try {
                const response = await fetch('/api/v1/comments', {
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
                if (response.status === 401) {
                    alert('請先登入才能發表評論。');
                    return;
                }
                if (!response.ok) throw new Error('Failed to add comment');

                const data = await response.json();

                // Show toasts
                if (data.toasts && data.toasts.length > 0) {
                    this.showToasts(data.toasts);
                }

                // Use the new total pages from the API response
                await this.getCommentsAsync(data.newTotalPages || 1);

                // After the DOM updates, scroll to and highlight the new comment
                this.$nextTick(() => {
                    const newCommentId = data.comment.commentID;
                    // The comment container must have an id like 'comment-123'
                    const newCommentElement = document.getElementById(`comment-${newCommentId}`);

                    if (newCommentElement) {
                        // Scroll to the element
                        newCommentElement.scrollIntoView({ behavior: 'smooth', block: 'center' });

                        // Add highlight class
                        newCommentElement.classList.add('new-comment-highlight');

                        // Remove the class after 3 seconds (matching the CSS animation)
                        setTimeout(() => {
                            newCommentElement.classList.remove('new-comment-highlight');
                        }, 3000);
                    }
                });
            } catch (error) {
                console.error(error);
            }
        },
        /**
         * Opens the message modal for a specific comment and fetches its messages.
         * @param {Object} comment - The comment object to show messages for.
         */
        async getMessagesAsync(comment) {
            this.currentCommentForMessages = comment;
            this.isLoadingMessages = true;
            this.messages = [];

            const modalEl = document.getElementById('messageModal');
            const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.show();

            try {
                const response = await fetch(`/api/v1/messages/ByComment/${comment.commentID}`);
                if (!response.ok) throw new Error('Failed to fetch messages');
                const messagesData = await response.json();
                // Add client-side state properties to each message
                this.messages = messagesData.map(m => ({ ...m, isEditing: false, editableContent: '' }));
            } catch (error) {
                console.error(error);
                alert('無法載入訊息');
            } finally {
                this.isLoadingMessages = false;
            }
        },
        /**
         * Submits a new message for the current comment.
         */
        async createNewMessageAsync() {
            if (!this.newMessageContent.trim()) {
                alert('訊息內容不能為空');
                return;
            }
            try {
                const response = await fetch('/api/v1/messages', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        commentId: this.currentCommentForMessages.commentID,
                        content: this.newMessageContent
                    })
                });

                if (response.status === 401 || response.status === 403) {
                    alert('請先登入或確認您有權限發言');
                    return;
                }
                if (!response.ok) throw new Error('Failed to submit message');

                const createdMessage = await response.json();
                // Add client-side state to the new message
                this.messages.push({ ...createdMessage, isEditing: false, editableContent: '' });
                this.newMessageContent = '';
                // Update the count on the comment object
                this.currentCommentForMessages.messagesCount++;
            } catch (error) {
                console.error(error);
                alert('無法新增訊息');
            }
        },
        /**
         * Toggles the agree status for a message.
         * @param {Object} message - The message object to agree/disagree with.
         */
        async updateMessageAgreeAsync(message) {
            // Optimistic update
            const originalAgreed = message.isAgreedByCurrentUser;
            const originalCount = message.agrees.length;

            message.isAgreedByCurrentUser = !originalAgreed;
            message.agrees.length += originalAgreed ? -1 : 1;

            try {
                const response = await fetch(`/api/v1/messages/${message.messageID}/agree`, { method: 'POST' });
                if (response.status === 401 || response.status === 403) {
                    alert('請先登入');
                    // Revert UI
                    message.isAgreedByCurrentUser = originalAgreed;
                    message.agrees.length = originalCount;
                    return;
                }
                if (!response.ok) throw new Error('Failed to toggle agree');
                const data = await response.json();
                // Update count with server's response
                message.agrees.length = data.agreesCount;

                // Show toasts for achievements
                if (data.toasts && data.toasts.length > 0) {
                    this.showToasts(data.toasts);
                }
            } catch (error) {
                console.error(error);
                // Revert UI on error
                message.isAgreedByCurrentUser = originalAgreed;
                message.agrees.length = originalCount;
            }
        },
        /**
         * Deletes a message after confirmation.
         * @param {Object} message - The message object to delete.
         */
        async deleteMessageAsync(message) {
            if (!confirm('確定要刪除這則訊息嗎？')) return;

            const originalMessages = [...this.messages];
            const messageIndex = this.messages.findIndex(m => m.messageID === message.messageID);
            if (messageIndex === -1) return; // Should not happen

            // Optimistic update
            this.messages.splice(messageIndex, 1);
            this.currentCommentForMessages.messagesCount--;

            try {
                const response = await fetch(`/api/v1/messages/${message.messageID}`, { method: 'DELETE' });
                if (response.status === 401 || response.status === 403) {
                    alert('您沒有權限刪除此訊息');
                    this.messages.splice(messageIndex, 0, message); // Re-add the message
                    this.currentCommentForMessages.messagesCount++;
                    return;
                }
                if (!response.ok) throw new Error('Failed to delete message');
            } catch (error) {
                console.error(error);
                alert('刪除失敗，請重試');
                this.messages.splice(messageIndex, 0, message); // Re-add the message
                this.currentCommentForMessages.messagesCount++;
            }
        },
        /**
         * Toggles the editing state for a message.
         * @param {Object} message - The message object to edit.
         */
        toggleMessageEdit(message) {
            if (!message.isEditing) {
                message.editableContent = message.content;
            }
            message.isEditing = !message.isEditing;
        },
        /**
         * Cancels editing a message.
         * @param {Object} message - The message object being edited.
         */
        cancelMessageEdit(message) {
            message.isEditing = false;
            message.editableContent = '';
        },
        /**
         * Saves the edited message content.
         * @param {Object} message - The message object to save.
         */
        async updateMessageAsync(message) {
            if (!message.editableContent.trim()) {
                alert('訊息內容不能為空');
                return;
            }

            const originalContent = message.content;
            message.content = message.editableContent; // Optimistic update

            try {
                const response = await fetch(`/api/v1/messages/${message.messageID}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ content: message.editableContent })
                });

                if (response.status === 401 || response.status === 403) {
                    alert('您沒有權限編輯此訊息');
                    message.content = originalContent; // Revert
                    return;
                }

                if (!response.ok) throw new Error('Failed to save message');

                const updatedMessage = await response.json();
                // Update local message with server response
                const index = this.messages.findIndex(m => m.messageID === updatedMessage.messageID);
                if (index !== -1) {
                    this.messages[index] = { ...this.messages[index], ...updatedMessage, isEditing: false, editableContent: '' };
                }
            } catch (error) {
                console.error(error);
                alert('儲存失敗，請重試');
                message.content = originalContent; // Revert
            } finally {
                message.isEditing = false;
            }
        },
        /**
         * Toggles the agree status for a comment.
         * @param {Object} comment - The comment object to agree/disagree with.
         */
        async updateCommentAgreeAsync(comment) {
            const originalAgreesCount = comment.agreesCount;
            const originalIsAgreed = comment.isAgreedByCurrentUser;
            comment.isAgreedByCurrentUser = !originalIsAgreed;
            comment.agreesCount += originalIsAgreed ? -1 : 1;

            try {
                const response = await fetch(`/api/v1/comments/${comment.commentID}/agree`, { method: 'POST' });
                if (response.status === 401) {
                    alert('請先登入才能按讚。');
                    throw new Error('Unauthorized');
                }
                if (!response.ok) throw new Error('Failed to toggle agree');
                const data = await response.json();
                comment.agreesCount = data.agreesCount;

                // Show toasts for achievements
                if (data.toasts && data.toasts.length > 0) {
                    this.showToasts(data.toasts);
                }
            } catch (error) {
                console.error(error);
                comment.agreesCount = originalAgreesCount;
                comment.isAgreedByCurrentUser = originalIsAgreed;
            }
        },
        /**
         * Deletes a comment after confirmation.
         * @param {Object} comment - The comment object to delete.
         */
        async deleteCommentAsync(comment) {
            if (!confirm('確定要刪除這則評論嗎？')) return;
            const originalComments = [...this.comments];
            this.comments = this.comments.filter(c => c.commentID !== comment.commentID);
            try {
                const response = await fetch(`/api/v1/comments/${comment.commentID}`, { method: 'DELETE' });
                if (response.status === 401) {
                    alert('請先登入才能刪除評論。');
                    throw new Error('Unauthorized');
                }
                if (!response.ok) throw new Error('Failed to delete comment');
            } catch (error) {
                console.error(error);
                this.comments = originalComments;
            }
        },
        /**
         * Toggles the blocked status of a comment.
         * @param {Object} comment - The comment object to block/unblock.
         */
        async updateCommentBlockAsync(comment) {
            const originalIsBlocked = comment.isBlocked;
            comment.isBlocked = !comment.isBlocked;
            try {
                const response = await fetch(`/api/v1/comments/${comment.commentID}/block`, { method: 'POST' });
                if (response.status === 401 || response.status === 403) {
                    alert('您沒有權限執行此操作。');
                    throw new Error('Unauthorized');
                }
                if (!response.ok) throw new Error('Failed to toggle block');
                const data = await response.json();
                comment.isBlocked = data.isBlocked;
            } catch (error) {
                console.error(error);
                comment.isBlocked = originalIsBlocked;
            }
        },
        /**
         * Toggles the editing state for a comment.
         * @param {Object} comment - The comment object to edit.
         */
        toggleCommentEdit(comment) {
            comment.isEditing = !comment.isEditing;
            if (comment.isEditing) {
                // Entering edit mode
                this.$nextTick(() => {
                    ClassicEditor
                        .create(document.querySelector(`#editor-${comment.commentID}`), this.getEditorConfig()) // Use shared config
                        .then(editor => {
                            this.inlineEditors[comment.commentID] = markRaw(editor);
                            editor.setData(comment.content);
                            const wordCountPlugin = editor.plugins.get('WordCount');
                            const wordCountWrapper = document.getElementById(`word-count-${comment.commentID}`);
                            if (wordCountPlugin && wordCountWrapper) {
                                wordCountWrapper.appendChild(wordCountPlugin.wordCountContainer);
                            }
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
        /**
         * Handles the submission of an inline comment edit.
         * @param {Object} comment - The comment being edited.
         */
        async updateCommentFromInlineEditorAsync(comment) {
            const editor = this.inlineEditors[comment.commentID];
            if (!editor) return;

            const newContent = editor.data.get();
            await this.updateCommentAsync(comment, newContent);

            comment.isEditing = false;
            editor.destroy();
            delete this.inlineEditors[comment.commentID];
        },
        /**
         * Saves the updated content of a comment to the server.
         * @param {Object} comment - The comment object.
         * @param {string} newContent - The new HTML content.
         */
        async updateCommentAsync(comment, newContent) {
            const originalContent = comment.content;
            comment.content = newContent;
            try {
                const response = await fetch(`/api/v1/comments/${comment.commentID}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ content: newContent })
                });
                if (response.status === 401) {
                    alert('請先登入才能編輯評論。');
                    throw new Error('Unauthorized');
                }
                if (response.status === 403) {
                    alert('編輯失敗。');
                    throw new Error('Forbid');
                }
                if (!response.ok) throw new Error('Failed to save comment');

                const data = await response.json();

                // Show toasts for achievements
                if (data.toasts && data.toasts.length > 0) {
                    this.showToasts(data.toasts);
                }
            } catch (error) {
                console.error(error);
                comment.content = originalContent;
            }
        },
        /**
         * Changes the current page of comments.
         * @param {number} page - The page number to navigate to.
         */
        async changePage(page) {
            if (page < 1 || page > this.pagination.totalPages) return;
            await this.getCommentsAsync(page);

            this.$nextTick(() => {
                const el = document.getElementById('CommentHead');
                if (!el) return;
                el.scrollIntoView({ behavior: 'smooth', block: 'center' });
            });
        },

        /**
         * Changes the sorting order and re-fetches the comments.
         */
        changeSort() {
            this.getCommentsAsync(1);
        },
        /**
         * Displays global toast notifications.
         * @param {Array<Object>} toasts - An array of toast objects to display.
         */
        showToasts(toasts) {
            showGlobalToasts(toasts);
        },
    }
});

app.mount('#comment-app');
