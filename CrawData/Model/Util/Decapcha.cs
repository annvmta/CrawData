using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawData.Model.Util
{
    public class Decapcha
    {
        [JsonProperty("captcha-text")]
        public string CapChaText { get; set; }
    }
}
