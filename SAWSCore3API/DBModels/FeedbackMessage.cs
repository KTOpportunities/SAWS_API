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
        public string docTypeName { get; set; }
        [NotMapped]
        public IFormFile file { get; set; }
        public string file_origname { get; set; }
        public string file_seqname { get; set; }
        public string file_url { get; set; }
        public string file_mimetype { get; set; }
        public long? file_size { get; set; }
        public string file_extention { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public bool? isdeleted { get; set; }
        public DateTime? deleted_at { get; set; }
    }
}