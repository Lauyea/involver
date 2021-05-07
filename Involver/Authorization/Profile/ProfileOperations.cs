using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Involver.Authorization.Profile
{
    public class ProfileOperations
    {
        public static OperationAuthorizationRequirement Create =
          new OperationAuthorizationRequirement { Name = Profiles.CreateOperationName };
        public static OperationAuthorizationRequirement Read =
          new OperationAuthorizationRequirement { Name = Profiles.ReadOperationName };
        public static OperationAuthorizationRequirement Update =
          new OperationAuthorizationRequirement { Name = Profiles.UpdateOperationName };
        public static OperationAuthorizationRequirement Delete =
          new OperationAuthorizationRequirement { Name = Profiles.DeleteOperationName };
        public static OperationAuthorizationRequirement Ban =
          new OperationAuthorizationRequirement { Name = Profiles.BanOperationName };
    }

    public class Profiles
    {
        public static readonly string CreateOperationName = "Create";
        public static readonly string ReadOperationName = "Read";
        public static readonly string UpdateOperationName = "Update";
        public static readonly string DeleteOperationName = "Delete";
        public static readonly string BanOperationName = "Ban";

        public static readonly string ProfileAdministratorsRole = "ProfileAdministrators";
        public static readonly string ProfileManagersRole = "ProfileManagers";
    }
}
