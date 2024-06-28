
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace SAWSCore3API.Models
{
    public class PaymentModel
    {
        [JsonIgnore]
        public int merchant_id { get; set; }

        [JsonIgnore]
        public string merchant_key { get; set; }

        public string return_url { get; set; }

        public string cancel_url { get; set; }

        public string notify_url { get; set; }

        //public int fica_idnumber { get; set; }

        public string name_first { get; set; }

        public string name_last { get; set; }

        public string email_address { get; set; }

        public string cell_number { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        public decimal amount { get; set; }

        [Required(ErrorMessage = "Item name is required")]
        public string item_name { get; set; }

        public string item_description { get; set; }
        public string m_payment_id { get; set; }

        public int custom_int1 { get; set; }

        public string custom_str1 { get; set; }

        public bool email_confirmation { get; set; }

        public string confirmation_address { get; set; }
        public string payment_method { get; set; }

    }

    public class PaymentModel2
    {
        public string returnUrl { get; set; }
        public int userId { get; set; }
        public string CancelUrl { get; set; }
        public string NotifyUrl { get; set; }
        public string name_first { get; set; }
        public string name_last { get; set; }
        public string email_address { get; set; }
        public string m_payment_id { get; set; }
        public string item_name { get; set; }
        public string item_description { get; set; }
        public bool email_confirmation { get; set; }
        public string confirmation_email { get; set; }
        public double amount { get; set; }
        public double recurring_amount { get; set; }
        public string frequency { get; set; }


    }

    public class CancelSubscriptionRequest
    {
        public string token{ get; set; }

        public bool isTesting { get; set;}

    }

    public class subscriptionresponse
    {
       public string url { get; set; }
    }
}
