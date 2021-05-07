using Involver.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Involver.Authorization.Announcement
{
    public class AnnouncementIsOwnerAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, Models.AnnouncementModel.Announcement>
    {
        UserManager<InvolverUser> _userManager;

        public AnnouncementIsOwnerAuthorizationHandler(UserManager<InvolverUser>
            userManager)
        {
            _userManager = userManager;
        }

        protected override Task
            HandleRequirementAsync(AuthorizationHandlerContext context,
                                   OperationAuthorizationRequirement requirement,
                                   Models.AnnouncementModel.Announcement resource)
        {
            if (context.User == null || resource == null)
            {
                return Task.CompletedTask;
            }

            // If not asking for CRUD permission, return.

            if (requirement.Name != Announcements.CreateOperationName &&
                requirement.Name != Announcements.ReadOperationName &&
                requirement.Name != Announcements.UpdateOperationName &&
                requirement.Name != Announcements.DeleteOperationName)
            {
                return Task.CompletedTask;
            }

            if (resource.OwnerID == _userManager.GetUserId(context.User))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
