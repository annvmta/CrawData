using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawData.Model.Etax
{
    public class EtaxNotification
    {
        public string NotificationID { get; set; }
        public string TransactionID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string SendDate { get; set; }
        public string SendBy { get; set; }
        public string XMLTenTBao { get; set; }
        public string XMLTrangThai { get; set; }
        public string FileName { get; set; }
    }
}
