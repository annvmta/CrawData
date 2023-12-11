using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawData.Model.Etax
{
    public class EtaxSubmitted
    {
        public string SortOrder { get; set; }
        public string TaxCode { get; set; }
        public string TransactionID { get; set; }
        public string ParentTransactionID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string TaxPeriod { get; set; }
        public string DeClarationType { get; set; }
        public string SubmitTimes { get; set; }
        public string Additiontimes { get; set; }
        public string SubmitDate { get; set; }
        public string TaxAgencyCode { get; set; }
        public string TaxAgencyName { get; set; }
        public string DebitAmount { get; set; }
        public string CreditAmount { get; set; }
        public string FileName { get; set; }
        public string StateTitle { get; set; }
        public List<EtaxNotification> Notifications { get; set; }
    }
}
