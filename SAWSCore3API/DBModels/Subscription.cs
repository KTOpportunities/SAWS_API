using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.DBModels
{    

    [Table("Subscription")]
    public class Subscription
    {
        [Key]
        public int subscriptionId { get; set; }
        public int userprofileid { get; set; }
        [Required]
        public string package_name { get; set; }
        public int package_id { get; set; }
        public Decimal package_price { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public int subscription_duration { get; set; }

        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public bool? isdeleted { get; set; }
        public DateTime? deleted_at { get; set; }

    }
}
