using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.DBModels
{    

    [Table("Service")]
    public class Service
    {
        [Key]
        public int serviceId { get; set; }
        [Required]
        public string name { get; set; }
        [Required]
        public int packageId { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public bool? isdeleted { get; set; }
        public DateTime? deleted_at { get; set; }

        public virtual List<ServiceProduct> Products { get; set; } = new List<ServiceProduct>();

    }
}
