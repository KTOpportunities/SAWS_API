using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.DBModels
{    

    [Table("ServiceProduct")]
    public class ServiceProduct
    {
        [Key]
        public int serviceProductId { get; set; }
        [Required]
        public string name { get; set; }
        [Required]
        public int serviceId { get; set; }      
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public bool? isdeleted { get; set; }
        public DateTime? deleted_at { get; set; }
    }
}
