using DataAccess.Common;

namespace Involver.Extensions;

public static class ArticleTypeExtensions
{
    public static string GetBadgeClass(this ArticleType type)
    {
        return type switch
        {
            ArticleType.General => "bg-success",
            ArticleType.Announcement => "bg-warning",
            ArticleType.Feedback => "bg-info",
            _ => "bg-secondary"
        };
    }
}