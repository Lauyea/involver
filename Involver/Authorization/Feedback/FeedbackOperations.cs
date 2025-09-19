using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Involver.Authorization.Feedback
{
    public class FeedbackOperations
    {
        public static OperationAuthorizationRequirement Create =
          new OperationAuthorizationRequirement { Name = Feedbacks.CreateOperationName };
        public static OperationAuthorizationRequirement Read =
          new OperationAuthorizationRequirement { Name = Feedbacks.ReadOperationName };
        public static OperationAuthorizationRequirement Update =
          new OperationAuthorizationRequirement { Name = Feedbacks.UpdateOperationName };
        public static OperationAuthorizationRequirement Delete =
          new OperationAuthorizationRequirement { Name = Feedbacks.DeleteOperationName };
        public static OperationAuthorizationRequirement Block =
          new OperationAuthorizationRequirement { Name = Feedbacks.BlockOperationName };
    }

    public class Feedbacks
    {
        public static readonly string CreateOperationName = "Create";
        public static readonly string ReadOperationName = "Read";
        public static readonly string UpdateOperationName = "Update";
        public static readonly string DeleteOperationName = "Delete";
        public static readonly string BlockOperationName = "Block";

        public static readonly string FeedbackAdministratorsRole = "FeedbackAdministrators";
        public static readonly string FeedbackManagersRole = "FeedbackManagers";
    }
}