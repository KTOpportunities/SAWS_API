using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.DBModels
{
    [NotMapped]
    public class SmptSetting
    {
        public string host { get; set; }
        public string userName { get; set; }
        public string Password { get; set; }
        public bool defaultCredentials { get; set; }
        public int Port { get; set; }
        public bool enableSsl { get; set; }
        public string from { get; set; }
        public string regUrl { get; set; }
        public string applicationUrl { get; set; }

    }
}
