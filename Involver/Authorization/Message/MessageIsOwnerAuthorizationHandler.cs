using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Involver.Authorization.Message
{
    public class MessageIsOwnerAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, DataAccess.Models.Message>
    {
        readonly UserManager<InvolverUser> _userManager;

        public MessageIsOwnerAuthorizationHandler(UserManager<InvolverUser>
            userManager)
        {
            _userManager = userManager;
        }

        protected override Task
            HandleRequirementAsync(AuthorizationHandlerContext context,
                                   OperationAuthorizationRequirement requirement,
                                   DataAccess.Models.Message resource)
        {
            if (context.User == null || resource == null)
            {
                return Task.CompletedTask;
            }

            // If not asking for CRUD permission, return.

            if (requirement.Name != Messages.CreateOperationName &&
                requirement.Name != Messages.ReadOperationName &&
                requirement.Name != Messages.UpdateOperationName &&
                requirement.Name != Messages.DeleteOperationName)
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