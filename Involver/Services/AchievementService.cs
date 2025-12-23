using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.AchievementModel;
using Involver.Common;
using Microsoft.EntityFrameworkCore;

namespace Involver.Services;

public class AchievementService(ApplicationDbContext context) : IAchievementService
{
    public async Task<List<Toast>> BeBetaInvolverAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.BetaInvolver }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> BeProfessionalAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.Professional }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> FirstTimeEditAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.Editor }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> FirstTimeDeleteAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.Discipline }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> BeAutobiographerAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.Autobiographer }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> FirstTimeCreateVotingAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.Assemblies }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> FirstTimeUseTagsAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.Taxonomist }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> ReadAnnouncementAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.Witness }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> ReadNovelAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.Gilgamesh }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> ReadEpisodeAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.LongTimeAgo }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> ReadArticleAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.Reader }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> ReadFeedbackAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.Brainstorm }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> RollDicesAsync(string profileId)
    {
        List<Toast> list = [new Toast { Header = AchievementNames.Rubicon }];
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> CheckGradeAsync(string profileId, DateTime enrollmentDate)
    {
        List<Toast> list = [];
        var now = DateTime.Now;
        if (now.AddYears(-1) > enrollmentDate) list.Add(new Toast { Header = AchievementNames.FirstGrade });
        if (now.AddYears(-2) > enrollmentDate) list.Add(new Toast { Header = AchievementNames.SecondGrade });
        if (now.AddYears(-3) > enrollmentDate) list.Add(new Toast { Header = AchievementNames.ThirdGrade });
        if (now.AddYears(-4) > enrollmentDate) list.Add(new Toast { Header = AchievementNames.FourthGrade });
        if (now.AddYears(-5) > enrollmentDate) list.Add(new Toast { Header = AchievementNames.FifthGrade });
        if (now.AddYears(-6) > enrollmentDate) list.Add(new Toast { Header = AchievementNames.SixthGrade });
        if (now.AddYears(-7) > enrollmentDate) list.Add(new Toast { Header = AchievementNames.SeventhGrade });
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> VoteCountAsync(string profileId)
    {
        int voteCount = await context.Votes.Where(v => v.OwnerID == profileId).CountAsync();
        List<Toast> list = [];
        if (voteCount > 0) list.Add(new Toast { Header = AchievementNames.Vote1 });
        if (voteCount > 29) list.Add(new Toast { Header = AchievementNames.Vote30 });
        if (voteCount > 299) list.Add(new Toast { Header = AchievementNames.Vote300 });
        if (voteCount > 599) list.Add(new Toast { Header = AchievementNames.Vote600 });
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> NovelCountAsync(string profileId)
    {
        var novelCount = await context.Novels.Where(n => n.ProfileID == profileId).CountAsync();
        List<Toast> list = [];
        if (novelCount > 0) list.Add(new Toast { Header = AchievementNames.Novel1 });
        if (novelCount > 29) list.Add(new Toast { Header = AchievementNames.Novel30 });
        if (novelCount > 99) list.Add(new Toast { Header = AchievementNames.Novel100 });
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> EpisodeCountAsync(string profileId)
    {
        var episodeCount = await context.Episodes.Where(e => e.OwnerID == profileId).CountAsync();
        List<Toast> list = [];
        if (episodeCount > 0) list.Add(new Toast { Header = AchievementNames.Episode1 });
        if (episodeCount > 29) list.Add(new Toast { Header = AchievementNames.Episode30 });
        if (episodeCount > 99) list.Add(new Toast { Header = AchievementNames.Episode100 });
        if (episodeCount > 299) list.Add(new Toast { Header = AchievementNames.Episode300 });
        if (episodeCount > 999) list.Add(new Toast { Header = AchievementNames.Episode1000 });
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> ArticleCountAsync(string profileId)
    {
        var articleCount = await context.Articles.Where(e => e.ProfileID == profileId).CountAsync();
        List<Toast> list = [];
        if (articleCount > 0) list.Add(new Toast { Header = AchievementNames.Article1 });
        if (articleCount > 29) list.Add(new Toast { Header = AchievementNames.Article30 });
        if (articleCount > 99) list.Add(new Toast { Header = AchievementNames.Article100 });
        if (articleCount > 299) list.Add(new Toast { Header = AchievementNames.Article300 });
        if (articleCount > 599) list.Add(new Toast { Header = AchievementNames.Article600 });
        if (articleCount > 999) list.Add(new Toast { Header = AchievementNames.Article1000 });
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> FeedbackCountAsync(string profileId)
    {
        var feedbackCount = await context.Articles.Where(n => n.ProfileID == profileId && n.Type == ArticleType.Feedback).CountAsync();
        List<Toast> list = [];
        if (feedbackCount > 0) list.Add(new Toast { Header = AchievementNames.Feedback1 });
        if (feedbackCount > 29) list.Add(new Toast { Header = AchievementNames.Feedback30 });
        if (feedbackCount > 99) list.Add(new Toast { Header = AchievementNames.Feedback100 });
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> CommentCountAsync(string profileId)
    {
        var commentCount = await context.Comments.Where(n => n.ProfileID == profileId).CountAsync();
        List<Toast> list = [];
        if (commentCount > 0) list.Add(new Toast { Header = AchievementNames.Comment1 });
        if (commentCount > 29) list.Add(new Toast { Header = AchievementNames.Comment30 });
        if (commentCount > 299) list.Add(new Toast { Header = AchievementNames.Comment300 });
        if (commentCount > 599) list.Add(new Toast { Header = AchievementNames.Comment600 });
        if (commentCount > 999) list.Add(new Toast { Header = AchievementNames.Comment1000 });
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> AcceptCountAsync(string profileId)
    {
        var feedbackCount = await context.Articles.Where(n => n.ProfileID == profileId && n.Accept == true).CountAsync();
        List<Toast> list = [];
        if (feedbackCount > 0) list.Add(new Toast { Header = AchievementNames.Accept1 });
        if (feedbackCount > 29) list.Add(new Toast { Header = AchievementNames.Accept30 });
        if (feedbackCount > 99) list.Add(new Toast { Header = AchievementNames.Accept100 });
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> GetCoinsCountAsync(string profileId, decimal coins)
    {
        List<Toast> list = [];
        if (coins > 0) list.Add(new Toast { Header = AchievementNames.GetCoin1 });
        if (coins > 999) list.Add(new Toast { Header = AchievementNames.GetCoin1000 });
        if (coins > 9999) list.Add(new Toast { Header = AchievementNames.GetCoin10000 });
        if (coins > 99999) list.Add(new Toast { Header = AchievementNames.GetCoin100000 });
        if (coins > 666665) list.Add(new Toast { Header = AchievementNames.GetCoin666666 });
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> UseCoinsCountAsync(string profileId, decimal coins)
    {
        List<Toast> list = [];
        if (coins > 0) list.Add(new Toast { Header = AchievementNames.UseCoin1 });
        if (coins > 499) list.Add(new Toast { Header = AchievementNames.UseCoin500 });
        if (coins > 999) list.Add(new Toast { Header = AchievementNames.UseCoin1000 });
        if (coins > 9999) list.Add(new Toast { Header = AchievementNames.UseCoin10000 });
        if (coins > 29999) list.Add(new Toast { Header = AchievementNames.UseCoin30000 });
        if (coins > 77776) list.Add(new Toast { Header = AchievementNames.UseCoin77777 });
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> AgreeCountAsync(string profileId)
    {
        var agreeCount = await context.Agrees.Where(a => a.ProfileID == profileId).CountAsync();
        List<Toast> list = [];
        if (agreeCount > 0) list.Add(new Toast { Header = AchievementNames.Agree1 });
        if (agreeCount > 29) list.Add(new Toast { Header = AchievementNames.Agree30 });
        if (agreeCount > 99) list.Add(new Toast { Header = AchievementNames.Agree100 });
        if (agreeCount > 299) list.Add(new Toast { Header = AchievementNames.Agree300 });
        if (agreeCount > 999) list.Add(new Toast { Header = AchievementNames.Agree1000 });
        return await GetToasts(profileId, list);
    }

    public async Task<List<Toast>> GetAgreeCountAsync(string profileId)
    {
        var totalCount = await context.Agrees
            .Where(a => (a.Comment.ProfileID == profileId || a.Message.ProfileID == profileId) && a.ProfileID != profileId)
            .CountAsync();
        List<Toast> list = [];
        if (totalCount > 0) list.Add(new Toast { Header = AchievementNames.GetAgree1 });
        if (totalCount > 29) list.Add(new Toast { Header = AchievementNames.GetAgree30 });
        if (totalCount > 299) list.Add(new Toast { Header = AchievementNames.GetAgree300 });
        if (totalCount > 599) list.Add(new Toast { Header = AchievementNames.GetAgree600 });
        if (totalCount > 999) list.Add(new Toast { Header = AchievementNames.GetAgree1000 });
        return await GetToasts(profileId, list);
    }

    private async Task<List<Toast>> GetToasts(string profileId, List<Toast> list)
    {
        List<Toast> toasts = [];
        Profile profile = await GetProfileWithAchievements(profileId);
        if (profile == null)
        {
            return toasts;
        }

        foreach (var item in list)
        {
            if (profile.Achievements.FirstOrDefault(a => a.Code == item.Header) == null)
            {
                Achievement achievement = await context.Achievements
                    .FirstOrDefaultAsync(a => a.Code == item.Header);

                if (achievement == null)
                {
                    continue;
                }

                profile.Achievements.Add(achievement);
                toasts.Add(new Toast
                {
                    Header = achievement.Title,
                    Body = achievement.Content,
                    Award = GetAward(achievement.Rank)
                });

                profile.VirtualCoins += item.Award;

                await context.SaveChangesAsync();
            }
        }

        return toasts;
    }

    private static decimal GetAward(int rank)
    {
        return rank switch
        {
            1 => Parameters.BronzeBadgeAward,
            2 => Parameters.SilverBadgeAward,
            _ => Parameters.GoldBadgeAward,
        };
    }

    private async Task<Profile> GetProfileWithAchievements(string profileId)
    {
        return await context.Profiles
            .Where(p => p.ProfileID == profileId)
            .Include(p => p.Achievements)
            .FirstOrDefaultAsync();
    }
}
