using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Involver.Authorization.ProfitSharing
{
    public class ProfitSharingOperations
    {
        public static OperationAuthorizationRequirement Create =
          new OperationAuthorizationRequirement { Name = ProfitSharings.CreateOperationName };
        public static OperationAuthorizationRequirement Read =
          new OperationAuthorizationRequirement { Name = ProfitSharings.ReadOperationName };
        public static OperationAuthorizationRequirement Update =
          new OperationAuthorizationRequirement { Name = ProfitSharings.UpdateOperationName };
        public static OperationAuthorizationRequirement Delete =
          new OperationAuthorizationRequirement { Name = ProfitSharings.DeleteOperationName };
        public static OperationAuthorizationRequirement Block =
          new OperationAuthorizationRequirement { Name = ProfitSharings.BlockOperationName };
    }

    public class ProfitSharings
    {
        public static readonly string CreateOperationName = "Create";
        public static readonly string ReadOperationName = "Read";
        public static readonly string UpdateOperationName = "Update";
        public static readonly string DeleteOperationName = "Delete";
        public static readonly string BlockOperationName = "Block";

        public static readonly string ProfitSharingAdministratorsRole = "ProfitSharingAdministrators";
        public static readonly string ProfitSharingManagersRole = "ProfitSharingManagers";
    }
}