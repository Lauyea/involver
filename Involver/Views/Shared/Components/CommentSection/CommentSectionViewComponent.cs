using DataAccess.Common;
using DataAccess.Data;

using Microsoft.AspNetCore.Mvc;

namespace Involver.Views.Shared.Components.CommentSection
{
    public class CommentSectionViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CommentSectionViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string from, int fromID, string ownerId = null)
        {
            bool isCommentOrderFixed = false;
            switch (from.ToLower())
            {
                case "articles":
                    var article = await _context.Articles.FindAsync(fromID);
                    if (article != null) isCommentOrderFixed = article.IsCommentOrderFixed;
                    break;
                case "novels":
                    var novel = await _context.Novels.FindAsync(fromID);
                    if (novel != null) isCommentOrderFixed = novel.IsCommentOrderFixed;
                    break;
                case "episodes":
                    var episode = await _context.Episodes.FindAsync(fromID);
                    if (episode != null) isCommentOrderFixed = episode.IsCommentOrderFixed;
                    break;
            }

            var model = new CommentSectionViewModel
            {
                From = from,
                FromID = fromID,
                IsCommentOrderFixed = isCommentOrderFixed,
                MaxLength = Parameters.CommentLength,
                OwnerId = ownerId
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
        public string OwnerId { get; set; }
    }
}