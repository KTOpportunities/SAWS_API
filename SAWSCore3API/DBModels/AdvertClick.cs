using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.DBModels
{
    [Table("AdvertClick")]
    public class AdvertClick
    {
        [Key]
        public int advertClickId { get; set; }
        [Required]
        public int advertId { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public bool? isdeleted { get; set; }
        public DateTime? deleted_at { get; set; }
    }
}
