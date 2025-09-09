using Microsoft.AspNetCore.Mvc;
using DataAccess.Data;
using DataAccess.Common;

namespace Involver.Views.Shared.Components.CommentSection
{
    public class CommentSectionViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CommentSectionViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string from, int fromID)
        {
            bool isCommentOrderFixed = false;
            switch (from.ToLower())
            {
                case "article":
                    var article = await _context.Articles.FindAsync(fromID);
                    if (article != null) isCommentOrderFixed = article.IsCommentOrderFixed;
                    break;
                case "novel":
                    var novel = await _context.Novels.FindAsync(fromID);
                    if (novel != null) isCommentOrderFixed = novel.IsCommentOrderFixed;
                    break;
                case "episode":
                    var episode = await _context.Episodes.FindAsync(fromID);
                    if (episode != null) isCommentOrderFixed = episode.IsCommentOrderFixed;
                    break;
            }

            var model = new CommentSectionViewModel
            {
                From = from,
                FromID = fromID,
                IsCommentOrderFixed = isCommentOrderFixed,
                MaxLength = Parameters.CommentLength
            };

            return View(model);
        }
    }

    public class CommentSectionViewModel
    {
        public string From { get; set; }
        public int FromID { get; set; }
        public bool IsCommentOrderFixed { get; set; }
        public int MaxLength { get; set; }
    }
}
