using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.DBModels
{
    

    [Table("UserType")]
    public class UserType
    {
        [Key]
        public int usertypeid { get; set; }
        public string fullname { get; set; }
        public string email { get; set; }
        public string mobilenumber { get; set; }
        public string userrole { get; set; }

        public string aspuid { get; set; }

        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
