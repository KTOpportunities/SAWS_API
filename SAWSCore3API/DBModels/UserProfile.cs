using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.DBModels
{
    [Table("UserProfile")]
    public class UserProfile
    {
        [Key]
        public int UserProfileId { get; set; }
        //public string 
    }
}
