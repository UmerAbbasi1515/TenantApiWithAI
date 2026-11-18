using System.Collections.Generic;

namespace FABTenantD2Api.Models
{
    public class GatewatLogVm
    {
    }
    public class RegisterPaymentVm
    {
        public decimal? TotalAmount { get; set; }
        public int ContractId { get; set; }
        public string ContractNo { get; set; }
        public int UserId { get; set; }
        public List<PaymentDetailVm> DetailList { get; set; }
    }
    public class PaymentDetailVm
    {
        public int PaymentID { get; set; }
        public int? VatOnRentContractId { get; set; }
        public int? VATOnChargeID { get; set; }
        public int ContractchargeID { get; set; }
        public int? PaymentSettingId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
    public class FinalizedPaymentVM
    {
        public int UserId { get; set; }
        public int ContractId { get; set; }
        public int OrderId { get; set; }
        public string TransactionId { get; set; }
        public string UniqueId { get; set; }
        public string ContractNo { get; set; }
    }
   
}
