using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Involver.Authorization.Comment
{
    public class CommentOperations
    {
        public static OperationAuthorizationRequirement Create =
          new OperationAuthorizationRequirement { Name = Comments.CreateOperationName };
        public static OperationAuthorizationRequirement Read =
          new OperationAuthorizationRequirement { Name = Comments.ReadOperationName };
        public static OperationAuthorizationRequirement Update =
          new OperationAuthorizationRequirement { Name = Comments.UpdateOperationName };
        public static OperationAuthorizationRequirement Delete =
          new OperationAuthorizationRequirement { Name = Comments.DeleteOperationName };
        public static OperationAuthorizationRequirement Block =
          new OperationAuthorizationRequirement { Name = Comments.BlockOperationName };
    }

    public class Comments
    {
        public static readonly string CreateOperationName = "Create";
        public static readonly string ReadOperationName = "Read";
        public static readonly string UpdateOperationName = "Update";
        public static readonly string DeleteOperationName = "Delete";
        public static readonly string BlockOperationName = "Block";

        public static readonly string CommentAdministratorsRole = "CommentAdministrators";
        public static readonly string CommentManagersRole = "CommentManagers";
    }
}