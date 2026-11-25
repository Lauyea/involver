using DataAccess.Models;

using Involver.Common;

namespace Involver.Services.NotificationSetterService
{
    /// <summary>
    /// 建立站內通知。通知的網址由這裡依評論所在的頁面組出來，呼叫端不用自己組網址。
    /// </summary>
    public interface INotificationSetter
    {
        /// <summary>
        /// 有人評論作品時，通知作品作者
        /// </summary>
        /// <param name="comment">剛建立的評論（評論者為 <see cref="Comment.ProfileID"/>）</param>
        /// <param name="cancellationToken">取消權杖</param>
        Task ForCommentAsync(Comment comment, CancellationToken cancellationToken = default);

        /// <summary>
        /// 有人在評論底下留言時，通知評論者，以及之前在同一則評論留言過的人
        /// </summary>
        /// <param name="message">剛建立的留言（留言者為 <see cref="Message.ProfileID"/>）</param>
        /// <param name="cancellationToken">取消權杖</param>
        Task ForReplyAsync(Message message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 評論被讚時，通知評論者；同一則評論還有未讀的按讚通知時不重複建立
        /// </summary>
        /// <param name="comment">被讚的評論</param>
        /// <param name="agreerId">按讚的人</param>
        /// <param name="toasts">評論者因此獲得的成就</param>
        /// <param name="cancellationToken">取消權杖</param>
        Task ForCommentAgreedAsync(Comment comment, string agreerId, List<Toast> toasts, CancellationToken cancellationToken = default);

        /// <summary>
        /// 留言被讚時，通知留言者；同一則留言還有未讀的按讚通知時不重複建立
        /// </summary>
        /// <param name="message">被讚的留言</param>
        /// <param name="agreerId">按讚的人</param>
        /// <param name="toasts">留言者因此獲得的成就</param>
        /// <param name="cancellationToken">取消權杖</param>
        Task ForReplyAgreedAsync(Message message, string agreerId, List<Toast> toasts, CancellationToken cancellationToken = default);

        /// <summary>
        /// 意見回饋被接受時，通知提出意見的人
        /// </summary>
        /// <param name="feedbackId">意見回饋的 ArticleID</param>
        /// <param name="feedbackTitle">意見回饋的標題</param>
        /// <param name="userId">提出意見的人</param>
        /// <param name="toasts">因此獲得的成就</param>
        /// <param name="cancellationToken">取消權杖</param>
        Task ForFeedbackAcceptAsync(int feedbackId, string feedbackTitle, string userId, List<Toast> toasts, CancellationToken cancellationToken = default);
    }
}
