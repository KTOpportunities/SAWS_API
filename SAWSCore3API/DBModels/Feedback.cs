using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.DBModels
{    

    [Table("Feedback")]
    public class Feedback
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string fullname { get; set; }
        [Required]
        public string subscriberEmail { get; set; }
        public string subscriberId { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public bool? isdeleted { get; set; }
        public DateTime? deleted_at { get; set; }

        public virtual List<FeedbackMessage> FeedbackMessages { get; set; } = new List<FeedbackMessage>();
    }
}
