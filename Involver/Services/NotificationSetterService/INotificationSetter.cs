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
    }
}