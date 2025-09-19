using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Involver.Authorization.Payment
{
    public class PaymentOperations
    {
        public static OperationAuthorizationRequirement Create =
          new OperationAuthorizationRequirement { Name = Payments.CreateOperationName };
        public static OperationAuthorizationRequirement Read =
          new OperationAuthorizationRequirement { Name = Payments.ReadOperationName };
        public static OperationAuthorizationRequirement Update =
          new OperationAuthorizationRequirement { Name = Payments.UpdateOperationName };
        public static OperationAuthorizationRequirement Delete =
          new OperationAuthorizationRequirement { Name = Payments.DeleteOperationName };
        public static OperationAuthorizationRequirement Block =
          new OperationAuthorizationRequirement { Name = Payments.BlockOperationName };
    }

    public class Payments
    {
        public static readonly string CreateOperationName = "Create";
        public static readonly string ReadOperationName = "Read";
        public static readonly string UpdateOperationName = "Update";
        public static readonly string DeleteOperationName = "Delete";
        public static readonly string BlockOperationName = "Block";

        public static readonly string PaymentAdministratorsRole = "PaymentAdministrators";
        public static readonly string PaymentManagersRole = "PaymentManagers";
    }
}