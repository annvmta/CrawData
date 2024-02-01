using CrawData.Model.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CrawData.Utils.Clients
{
    public class DecapchaUtil
    {
        private HttpClient _client { get; set; }

        public DecapchaUtil()
        {
            _client = new HttpClient() { BaseAddress = new Uri("https://aiservice.misa.vn/v1/captcha-decode/") };
            _client.DefaultRequestHeaders.Add("x-api-key", "");
            _client.DefaultRequestHeaders.Add("project", "");
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36");
        }

        public async Task<string> DeCapcha(Stream imgstream)
        {
            var capcha = "";
            try
            {
                using (var mutilpartfm = new MultipartFormDataContent())
                {
                    var fileStream = new StreamContent(imgstream);
                    mutilpartfm.Add(fileStream, name: "image", fileName: $"image_{DateTime.Now.Ticks}.jpg");
                    var mes = await _client.PostAsync($"image?vendor=thuecanhan", mutilpartfm);
                    mes.EnsureSuccessStatusCode();
                    string responBody = await mes.Content.ReadAsStringAsync();
                    var res = JsonConvert.DeserializeObject<Decapcha>(responBody);
                    capcha = res.CapChaText;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return capcha; 
        }
    }
}
