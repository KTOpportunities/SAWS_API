using System;
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
        public int Id { get; set; }
        public int parentMessage_Id { get; set; }
        public int feedbackId { get; set; }
        public string subcriberId { get; set; }
        public string adminId { get; set; }
        public string message { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public bool? isdeleted { get; set; }
        public DateTime? deleted_at { get; set; }
    }
}