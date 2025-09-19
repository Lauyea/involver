using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Involver.Authorization.Comment
{
    public class CommentIsOwnerAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, DataAccess.Models.Comment>
    {
        readonly UserManager<InvolverUser> _userManager;

        public CommentIsOwnerAuthorizationHandler(UserManager<InvolverUser>
            userManager)
        {
            _userManager = userManager;
        }

        protected override Task
            HandleRequirementAsync(AuthorizationHandlerContext context,
                                   OperationAuthorizationRequirement requirement,
                                   DataAccess.Models.Comment resource)
        {
            if (context.User == null || resource == null)
            {
                return Task.CompletedTask;
            }

            // If not asking for CRUD permission, return.

            if (requirement.Name != Comments.CreateOperationName &&
                requirement.Name != Comments.ReadOperationName &&
                requirement.Name != Comments.UpdateOperationName &&
                requirement.Name != Comments.DeleteOperationName)
            {
                return Task.CompletedTask;
            }

            string UserID = _userManager.GetUserId(context.User);
            if (resource.ProfileID == UserID)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}