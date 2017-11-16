using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentifyApp.Models
{
    public class Person
    {
        public string faceId { get; set; }
        public string personId { get; set; }
        public string personGroupId { get; set; } = "persongroup1";
        public string blobUrl { get; set; }

        public double confidence { get; set; } = 0.5;
    }
}
