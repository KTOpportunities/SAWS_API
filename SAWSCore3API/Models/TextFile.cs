using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAWSCore3API.Models
{
    [NotMapped]
    public class TextFile
    {
        public string foldername { get; set; }
        public string filename { get; set; }
        public DateTime? lastmodified { get; set; }

        public string filetextcontent{ get; set; }
    }
}
