using Involver.Common;

namespace Involver.Services.NotificationSetterService
{
    public interface INotificationSetter
    {
        /// <summary>
        /// 設定留言通知
        /// </summary>
        /// <param name="commentId">評論ID</param>
        /// <param name="userId">留言者ID</param>
        /// <param name="message">留言內容</param>
        /// <param name="url">評論URL</param>
        Task ForMessageAsync(int commentId, string userId, string message, string url);

        /// <summary>
        /// 設定評論通知
        /// </summary>
        /// <param name="from">註記從哪個功能來的</param>
        /// <param name="fromId">功能ID</param>
        /// <param name="url">功能URL</param>
        /// <param name="userId">評論者ID</param>
        /// <param name="comment">評論內容</param>
        /// <returns></returns>
        Task ForCommentAsync(string from, int fromId, string url, string userId, string comment);

        /// <summary>
        /// 設定意見被接受通知
        /// </summary>
        /// <param name="feedbackTitle">意見標題</param>
        /// <param name="userId">User ID</param>
        /// <param name="url"></param>
        /// <param name="toasts"></param>
        /// <returns></returns>
        Task ForFeedbackAcceptAsync(string feedbackTitle, string userId, string url, List<Toast> toasts);

        /// <summary>
        /// 設定留言被讚時的通知
        /// </summary>
        /// <param name="messageContent">留言內容</param>
        /// <param name="commenterId"></param>
        /// <param name="url"></param>
        /// <param name="toasts"></param>
        /// <returns></returns>
        Task ForMessageBeAgreedAsync(string messageContent, string commenterId, string url, List<Toast> toasts);

        /// <summary>
        /// 設定評論被讚時的通知
        /// </summary>
        /// <param name="commentContent">評論內容</param>
        /// <param name="commenterId"></param>
        /// <param name="url"></param>
        /// <param name="toasts"></param>
        /// <returns></returns>
        Task ForCommentBeAgreedAsync(string commentContent, string commenterId, string url, List<Toast> toasts);
    }
}