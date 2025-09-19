using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Involver.Authorization.Novel
{
    public class NovelIsOwnerAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, DataAccess.Models.NovelModel.Novel>
    {
        readonly UserManager<InvolverUser> _userManager;

        public NovelIsOwnerAuthorizationHandler(UserManager<InvolverUser>
            userManager)
        {
            _userManager = userManager;
        }

        protected override Task
            HandleRequirementAsync(AuthorizationHandlerContext context,
                                   OperationAuthorizationRequirement requirement,
                                   DataAccess.Models.NovelModel.Novel resource)
        {
            if (context.User == null || resource == null)
            {
                return Task.CompletedTask;
            }

            // If not asking for CRUD permission, return.

            if (requirement.Name != Novels.CreateOperationName &&
                requirement.Name != Novels.ReadOperationName &&
                requirement.Name != Novels.UpdateOperationName &&
                requirement.Name != Novels.DeleteOperationName)
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