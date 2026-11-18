using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantApi.Models
{
    public class DashboardVm
    {
        public string FullName { get; set; }
        public string FullNameAR { get; set; }
        public string PropertyImage { get; set; }
        public int ContractsExpiring { get; set; }
        public decimal ToBePaidIn30Days { get; set; }
        public PaymentVm Payment { get; set; }
        public List<PaymentBalanceVm> PaymentBalanceChart { get; set; }
    }
    public class ProVm
    {
        public string FullName { get; set; }

    }
    public class ImageVm
    {
        public string PropertyImage { get; set; }

    }
    public class PayVm
    {
        public decimal ThiryDayPayment { get; set; }
        public decimal RentalValues { get; set; }

    }
    public class CheckDueVm
    {
        public int CheckDueInThirtyDays { get; set; }

    }
    public class ThirtyDaysPayments
    {
        public string ContractID { get; set; }
        public decimal Amount { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string TransactionNo { get; set; }

    }
    public class ContExpiringVm
    {
        public int ContractsExpiring { get; set; }

    }
    public class PaymentBalanceVm
    {
        public decimal AmountToBeReceived { get; set; }
        public decimal Received { get; set; }
        public decimal Balance { get; set; }
        public decimal RentOutstanding { get; set; }
    }
    public class PaymentVm
    {
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
    }

    public class TenantDashboardVm
    {
        public int TenantId { get; set; }
        public string TenantName { get; set; }
        public string TenantNameAR { get; set; }
        public decimal PaymentBalance { get; set; }
        public int ContractExpiringIn30Days { get; set; }
        public int CheckDueIn30Days { get; set; }
        public decimal RentalValues { get; set; }
        public decimal RentOutstanding { get; set; }
        public decimal ActiveContracts { get; set; }
    }

    public class TenantDashboardVmV1
    {
        public int TenantId { get; set; }
        public string TenantName { get; set; }
        public string TenantNameAR { get; set; }
        public decimal PaymentBalance { get; set; }
        public int ContractExpiringIn30Days { get; set; }
        public int CheckDueIn30Days { get; set; }
        public decimal RentalValues { get; set; }
        public decimal RentOutstanding { get; set; }
        public decimal ActiveContracts { get; set; } = 0;
        public int UnreadNotificationCount { get; set; }
        public int ContractRenewalActionCount { get; set; }
    }
    public class GetOccupantOutstandingVM
    {
        public decimal ActiveTcRent { get; set; }
        public decimal ActiveTcRentBalance { get; set; }
        public decimal TotalLiability { get; set; }
    }
        public class ContActiveVm
    {
        public int ActiveContracts { get; set; }

    }
    public class DashboardNotificationVm
    {
        public string Contractno { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public int? StageId { get; set; }
        public int NotificationId { get; set; }
        public int RecordId { get; set; }
        public string Title { get; set; }
        public string TitleAr { get; set; }
        public string RecordType { get; set; }
        public string Description { get; set; }
        public string DescriptionAr { get; set; }
        public string CreatedOn { get; set; }
        public int? DueActionid { get; set; }
        public int? CaseId { get; set; }
    }
}
