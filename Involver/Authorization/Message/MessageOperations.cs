using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Involver.Authorization.Message
{
    public class MessageOperations
    {
        public static OperationAuthorizationRequirement Create =
          new OperationAuthorizationRequirement { Name = Messages.CreateOperationName };
        public static OperationAuthorizationRequirement Read =
          new OperationAuthorizationRequirement { Name = Messages.ReadOperationName };
        public static OperationAuthorizationRequirement Update =
          new OperationAuthorizationRequirement { Name = Messages.UpdateOperationName };
        public static OperationAuthorizationRequirement Delete =
          new OperationAuthorizationRequirement { Name = Messages.DeleteOperationName };
        public static OperationAuthorizationRequirement Block =
          new OperationAuthorizationRequirement { Name = Messages.BlockOperationName };
    }

    public class Messages
    {
        public static readonly string CreateOperationName = "Create";
        public static readonly string ReadOperationName = "Read";
        public static readonly string UpdateOperationName = "Update";
        public static readonly string DeleteOperationName = "Delete";
        public static readonly string BlockOperationName = "Block";

        public static readonly string MessageAdministratorsRole = "MessageAdministrators";
        public static readonly string MessageManagersRole = "MessageManagers";
    }
}