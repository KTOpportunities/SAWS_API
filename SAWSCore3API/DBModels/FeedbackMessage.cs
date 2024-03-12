using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.DBModels
{    

    [Table("FeedbackMessage")]
    public class FeedbackMessage
    {
        [Key]
        public int feedbackMessageId { get; set; }
        public int feedbackId { get; set; }
        [Required]
        public string senderId { get; set; }
        [Required]
        public string senderEmail { get; set; }
        public string responderId { get; set; }
        public string responderEmail { get; set; }
        public string feedback { get; set; }
        public string response { get; set; }
        public string broadcast { get; set; }
        public string broadcastId { get; set; }
        public string feedbackAttachment { get; set; }
        public string feedbackAttachmentFileName { get; set; }
        public string responseAttachment { get; set; }
        public string responseAttachmentFileName { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public bool? isdeleted { get; set; }
        public DateTime? deleted_at { get; set; }
        public virtual List<DocFeedback> DocFeedbacks { get; set; } = new List<DocFeedback>();
    }
}