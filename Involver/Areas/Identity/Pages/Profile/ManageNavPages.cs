using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Involver.Areas.Identity.Pages.Profile
{
    public static class ManageNavPages
    {
        public static string Index => "Index";

        public static string Creations => "Creations";

        public static string Articles => "Articles";

        public static string Follow => "Follow";

        public static string Interaction => "Interaction";

        public static string Involving => "Involving";

        public static string Missions => "Missions";

        public static string Achievements => "Achievements";

        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

        public static string CreationsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Creations);

        public static string ArticlesNavClass(ViewContext viewContext) => PageNavClass(viewContext, Articles);

        public static string FollowNavClass(ViewContext viewContext) => PageNavClass(viewContext, Follow);

        public static string InteractionNavClass(ViewContext viewContext) => PageNavClass(viewContext, Interaction);

        public static string InvolvingNavClass(ViewContext viewContext) => PageNavClass(viewContext, Involving);

        public static string MissionsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Missions);

        public static string AchievementsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Achievements);

        private static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }

        public static string GetProfileID(ViewContext viewContext)
        {
            string ProfileID = viewContext.ViewData["ProfileID"] as string;
            return ProfileID;
        }
    }
}
