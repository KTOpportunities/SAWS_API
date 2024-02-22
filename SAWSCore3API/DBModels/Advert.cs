using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.DBModels
{
    [Table("Advert")]
    public class Advert
    {
        [Key]
        public int advertId { get; set; }
        [Required]
        public string advert_caption { get; set; }
        [Required]
        [DataType(DataType.EmailAddress)]
        public string uploaded_by { get; set; }
        public string advert_url { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public bool? isdeleted { get; set; }
        public DateTime? deleted_at { get; set; }
        public virtual List<DocAdvert> DocAdverts { get; set; } = new List<DocAdvert>();
    }
}
