using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.DBModels
{    

    [Table("Package")]
    public class Package
    {
        [Key]
        public int packageId { get; set; }
        [Required]
        public string name { get; set; }    
        public string price { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public bool? isdeleted { get; set; }
        public DateTime? deleted_at { get; set; }

        public virtual List<Service> Services { get; set; } = new List<Service>();

    }
}
