using Involver.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Involver.Authorization.Article
{
    public class ArticleIsOwnerAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, Models.ArticleModel.Article>
    {
        UserManager<InvolverUser> _userManager;

        public ArticleIsOwnerAuthorizationHandler(UserManager<InvolverUser>
            userManager)
        {
            _userManager = userManager;
        }

        protected override Task
            HandleRequirementAsync(AuthorizationHandlerContext context,
                                   OperationAuthorizationRequirement requirement,
                                   Models.ArticleModel.Article resource)
        {
            if (context.User == null || resource == null)
            {
                return Task.CompletedTask;
            }

            // If not asking for CRUD permission, return.

            if (requirement.Name != Articles.CreateOperationName &&
                requirement.Name != Articles.ReadOperationName &&
                requirement.Name != Articles.UpdateOperationName &&
                requirement.Name != Articles.DeleteOperationName)
            {
                return Task.CompletedTask;
            }

            if (resource.ProfileID == _userManager.GetUserId(context.User))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
