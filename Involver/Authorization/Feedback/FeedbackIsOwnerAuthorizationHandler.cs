﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Involver.Data;

namespace Involver.Authorization.Feedback
{
    public class FeedbackIsOwnerAuthorizationHandler
        : AuthorizationHandler<OperationAuthorizationRequirement, Models.FeedbackModel.Feedback>
    {
        UserManager<InvolverUser> _userManager;

        public FeedbackIsOwnerAuthorizationHandler(UserManager<InvolverUser>
            userManager)
        {
            _userManager = userManager;
        }

        protected override Task
            HandleRequirementAsync(AuthorizationHandlerContext context,
                                   OperationAuthorizationRequirement requirement,
                                   Models.FeedbackModel.Feedback resource)
        {
            if (context.User == null || resource == null)
            {
                return Task.CompletedTask;
            }

            // If not asking for CRUD permission, return.

            if (requirement.Name != Feedbacks.CreateOperationName &&
                requirement.Name != Feedbacks.ReadOperationName &&
                requirement.Name != Feedbacks.UpdateOperationName &&
                requirement.Name != Feedbacks.DeleteOperationName)
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
