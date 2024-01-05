using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.Models
{
    [NotMapped]
    public class Response
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public IdentityResult Detail { get; set; }
        public object DetailDescription { get; set; }
    }
}
