using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantApi.Models
{
    public class ChargeVm
    {
        public string ChargesFor { get; set; }
        public string ChargeName { get; set; }
        public int ChargesNumber { get; set; }
        public decimal ChargesAmount { get; set; }
        public int ContractId { get; set; }
        public decimal VATAmount { get; set; }
        public string Date { get; set; }
    }
    public class PaymentInfoVm
    {
        public string TransactionNo { get; set; }
        public string DATE { get; set; }
        public string ReasonofPayment { get; set; }
        public string ChequeNo { get; set; }
        public string ChequeDate { get; set; }
        public string Bank { get; set; }
        public decimal ChequeAmount { get; set; }
        public string ChequeStatus { get; set; }
        public string Description { get; set; }
    }
}
