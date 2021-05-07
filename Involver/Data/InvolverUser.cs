using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Involver.Data
{
    public class InvolverUser : IdentityUser
    {
        [PersonalData]
        public bool Prime { get; set; }
        [PersonalData]
        public string BankAccount { get; set; }
        public bool Banned { get; set; }
    }
}
