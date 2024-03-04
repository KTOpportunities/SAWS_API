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
        public int feedbackId { get; set; }
        [Required]
        public string fullname { get; set; }
        [Required]
        public string senderId { get; set; }
        [Required]
         public string title { get; set; }
        [Required]
        public string senderEmail { get; set; }
        public string responderId { get; set; }
        public string responderEmail { get; set; }
        public string broadcasterId { get; set; }
        public string broadcasterEmail { get; set; }
        public string batchId { get; set; }
        public bool? isresponded { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public bool? isdeleted { get; set; }
        public DateTime? deleted_at { get; set; }

        public virtual List<FeedbackMessage> FeedbackMessages { get; set; } = new List<FeedbackMessage>();
    }
}
