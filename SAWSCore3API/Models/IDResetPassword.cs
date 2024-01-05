using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.Models
{
    [NotMapped]
    public class IDResetPassword
    {
        public string email { get; set; }
        public string token { get; set; }
        public string newPassword { get; set; }
        public string confirmPassword { get; set; }
    }
}
