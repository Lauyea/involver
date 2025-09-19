using Involver.Authorization.Announcement;
using Involver.Authorization.Article;
using Involver.Authorization.Comment;
using Involver.Authorization.Feedback;
using Involver.Authorization.Message;
using Involver.Authorization.Novel;
using Involver.Authorization.Payment;
using Involver.Authorization.Profile;
using Involver.Authorization.ProfitSharing;
using Involver.Authorization.Voting;
using Involver.Services.NotificationSetterService;

using Microsoft.AspNetCore.Authorization;

namespace Involver.Services
{
    public static class ServiceExtension
    {
        public static void AddAuthorizationHandlers(IServiceCollection services)
        {
            //Feedback Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  FeedbackIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  FeedbackAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  FeedbackManagerAuthorizationHandler>();

            //Comment Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  CommentIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  CommentAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  CommentManagerAuthorizationHandler>();

            //Announcement Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  AnnouncementIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  AnnouncementAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  AnnouncementManagerAuthorizationHandler>();

            //Article Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  ArticleIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  ArticleAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  ArticleManagerAuthorizationHandler>();

            //Novel Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  NovelIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  NovelAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  NovelManagerAuthorizationHandler>();

            //Message Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  MessageIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  MessageAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  MessageManagerAuthorizationHandler>();

            //Voting Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  VotingIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  VotingAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  VotingManagerAuthorizationHandler>();

            //Profile Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  ProfileIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  ProfileAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  ProfileManagerAuthorizationHandler>();

            //Payment Authorization Handlers

            services.AddSingleton<IAuthorizationHandler,
                                  PaymentAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  PaymentManagerAuthorizationHandler>();

            //ProfitSharing Authorization Handlers

            services.AddSingleton<IAuthorizationHandler,
                                  ProfitSharingAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  ProfitSharingManagerAuthorizationHandler>();

            services.AddScoped<INotificationSetter, NotificationSetter>();
        }
    }
}