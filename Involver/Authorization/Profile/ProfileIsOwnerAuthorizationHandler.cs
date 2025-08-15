using DataAccess.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Involver.Authorization.Profile
{
    public class ProfileIsOwnerAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, DataAccess.Models.Profile>
    {
        UserManager<InvolverUser> _userManager;

        public ProfileIsOwnerAuthorizationHandler(UserManager<InvolverUser>
            userManager)
        {
            _userManager = userManager;
        }

        protected override Task
            HandleRequirementAsync(AuthorizationHandlerContext context,
                                   OperationAuthorizationRequirement requirement,
                                   DataAccess.Models.Profile resource)
        {
            if (context.User == null || resource == null)
            {
                return Task.CompletedTask;
            }

            // If not asking for CRUD permission, return.

            if (requirement.Name != Profiles.CreateOperationName &&
                requirement.Name != Profiles.ReadOperationName &&
                requirement.Name != Profiles.UpdateOperationName &&
                requirement.Name != Profiles.DeleteOperationName)
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
