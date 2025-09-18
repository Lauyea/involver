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
        this.maxLength = parseInt(el.dataset.maxLength, 10) || 10000; // Read maxLength from data attribute

        if (this.isOrderFixed) {
            this.sortBy = 'oldest';
        }
        this.fetchComments(1);

        $('#commentModal').on('shown.bs.modal', () => {
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
    },
    methods: {
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
                this.comments = data.map(c => ({ ...c, isEditing: false, isBlockedContentVisible: false }));
            } catch (error) {
                console.error(error);
            } finally {
                this.isLoading = false;
            }
        },
        showNewCommentModal() {
            this.newCommentDice = { rollTimes: 0, diceSides: 0 };
            $('#commentModal').modal('show');
        },
        async submitComment() {
            if (!this.mainEditor) return;
            const content = this.mainEditor.data.get();
            if (!content) return;

            await this.addNewComment(content);
            $('#commentModal').modal('hide');
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
                await this.fetchComments(data.newTotalPages || 1);

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
         * Opens the message modal for a specific comment.
         * @param {Object} comment - The comment object to show messages for.
         */
        async openMessageModal(comment) {
            this.currentCommentForMessages = comment;
            this.isLoadingMessages = true;
            this.messages = [];
            $('#messageModal').modal('show');
            try {
                const response = await fetch(`/api/MessagesApi/ByComment/${comment.commentID}`);
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
         * Submits the new message form.
         */
        async submitNewMessage() {
            if (!this.newMessageContent.trim()) {
                alert('訊息內容不能為空');
                return;
            }
            try {
                const response = await fetch('/api/MessagesApi', {
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
        async toggleMessageAgree(message) {
            // Optimistic update
            const originalAgreed = message.isAgreedByCurrentUser;
            const originalCount = message.agrees.length;

            message.isAgreedByCurrentUser = !originalAgreed;
            message.agrees.length += originalAgreed ? -1 : 1;

            try {
                const response = await fetch(`/api/MessagesApi/${message.messageID}/agree`, { method: 'POST' });
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
        async deleteMessage(message) {
            if (!confirm('確定要刪除這則訊息嗎？')) return;

            const originalMessages = [...this.messages];
            const messageIndex = this.messages.findIndex(m => m.messageID === message.messageID);
            if (messageIndex === -1) return; // Should not happen

            // Optimistic update
            this.messages.splice(messageIndex, 1);
            this.currentCommentForMessages.messagesCount--;

            try {
                const response = await fetch(`/api/MessagesApi/${message.messageID}`, { method: 'DELETE' });
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
        async saveMessageEdit(message) {
            if (!message.editableContent.trim()) {
                alert('訊息內容不能為空');
                return;
            }

            const originalContent = message.content;
            message.content = message.editableContent; // Optimistic update

            try {
                const response = await fetch(`/api/MessagesApi/${message.messageID}`, {
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
        async toggleAgree(comment) {
            const originalAgreesCount = comment.agreesCount;
            const originalIsAgreed = comment.isAgreedByCurrentUser;
            comment.isAgreedByCurrentUser = !originalIsAgreed;
            comment.agreesCount += originalIsAgreed ? -1 : 1;

            try {
                const response = await fetch(`/api/comments/${comment.commentID}/agree`, { method: 'POST' });
                if (response.status === 401) {
                    alert('請先登入才能按讚。');
                    throw new Error('Unauthorized');
                }
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
        async toggleBlock(comment) {
            const originalIsBlocked = comment.isBlocked;
            comment.isBlocked = !comment.isBlocked;
            try {
                const response = await fetch(`/api/comments/${comment.commentID}/block`, { method: 'POST' });
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
        toggleEdit(comment) {
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
                if (response.status === 401) {
                    alert('請先登入才能編輯評論。');
                    throw new Error('Unauthorized');
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
        changePage(page) {
            if (page < 1 || page > this.pagination.totalPages) return;
            this.fetchComments(page);
        },
        changeSort() {
            this.fetchComments(1);
        },
        showToasts(toasts) {
            const toastContainer = document.getElementById('toast-container');
            if (!toastContainer) return;

            toasts.forEach(toast => {
                let badgeClass = '';
                switch (toast.award) {
                    case 10: badgeClass = 'bronze'; break; // 設定於 DataAccess\Common\Parameters.cs 的 `BronzeBadgeAward`
                    case 30: badgeClass = 'silver'; break;
                    default: badgeClass = 'gold'; break;
                }

                const toastId = `toast-${Date.now()}-${Math.random()}`;
                const toastHtml = `
                    <div id="${toastId}" class="toast" data-autohide="false">
                        <div class="toast-header">
                            ${badgeClass ? `<span class="dot mr-2 ${badgeClass}"></span>` : ''}
                            <strong class="mr-auto">${toast.header}</strong>
                            <button type="button" class="ml-2 mb-1 close" data-dismiss="toast" aria-label="Close">
                                <span aria-hidden="true">&times;</span>
                            </button>
                        </div>
                        <div class="toast-body">
                            ${toast.body}
                        </div>
                    </div>
                `;
                toastContainer.insertAdjacentHTML('beforeend', toastHtml);
                const toastElement = document.getElementById(toastId);
                $(toastElement).toast('show');
                // Remove the element from the DOM after it has been hidden
                $(toastElement).on('hidden.bs.toast', function () {
                    $(this).remove();
                });
            });
        },
    }
});

app.mount('#comment-app');
