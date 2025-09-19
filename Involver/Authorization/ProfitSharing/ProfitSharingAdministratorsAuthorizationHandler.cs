using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Involver.Authorization.ProfitSharing
{
    public class ProfitSharingAdministratorsAuthorizationHandler
                    : AuthorizationHandler<OperationAuthorizationRequirement, DataAccess.Models.StatisticalData.ProfitSharing>
    {
        protected override Task HandleRequirementAsync(
                                              AuthorizationHandlerContext context,
                                    OperationAuthorizationRequirement requirement,
                                     DataAccess.Models.StatisticalData.ProfitSharing resource)
        {
            if (context.User == null)
            {
                return Task.CompletedTask;
            }

            // Administrators can do anything.
            if (context.User.IsInRole(ProfitSharings.ProfitSharingAdministratorsRole))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}