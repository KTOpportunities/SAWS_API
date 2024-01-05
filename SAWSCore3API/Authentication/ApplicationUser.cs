using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.Authentication
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; }
        public bool IsAdminUser { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string PhotoUrl { get; set; }
    }
}
