using CrawData.BL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrawData
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var bl = new BLETaxIndividual();
            var data = await bl.GetEtaxSubmitteds("01/01/2022", "31/12/2022");
            var option = new JsonSerializerOptions
            {
               WriteIndented = true,
               Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var dataJson = JsonSerializer.Serialize(data, option);
            var filePath = $"{Directory.GetCurrentDirectory()}\\Result\\res.json";
            System.IO.File.WriteAllText(filePath, dataJson);
        }
    }
}
