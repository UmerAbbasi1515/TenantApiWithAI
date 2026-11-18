using System;

namespace FABTenantD2Api.Models
{
    public class OfferVm
    {
        public int Offerid { get; set; }
        public string Title { get; set; }
        public string TitleAr { get; set; }
        public string ValidateFrom { get; set; }
        public string ValidateTo { get; set; }
    }
    public class OfferDetailVm
    {
        public int Offerid { get; set; }
        public string Title { get; set; }
        public string TitleAr { get; set; }
        public string ValidateFrom { get; set; }
        public string ValidateTo { get; set; }
        public string Description { get; set; }
        public string DesriptionAR { get; set; }
    }
    public class OfferModelNVM
    {
        public string OfferNo { get; set; }    
        public int OfferID { get; set; }
        public string OfferName { get; set; }
        public string IssueDate { get; set; }
        public string ExpiryDate { get; set; }
        public int Period { get; set; }
        public string RentStartDate { get; set; }
        public string RentEndDate { get; set; }
        public decimal OfferAmt { get; set; }
        public decimal SecurityAmount { get; set; }
        public decimal ReservationFee { get; set; }
        public int PaymentInstallments { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
        public string BuildingRefNo { get; set; }
        public string OfferStatus { get; set; }
    }
    public class OfferProperties
    {
        public int PropertyID { get; set;}
        public string PropertyName { get; set;}
        public string PropertyNameAR { get; set;}
    }
    public class GetOfferVM
    {
        public string pageNo { get; set; } = "1";
        public string pageSize { get; set; } = "20";
    }
    public class Renewal
    {
        public string fromDate1 { get; }
        public string EndDate1 { get; }
        public string AddNewDate1 { get; }
        public string EndNewDate1 { get; }
    }

    public class ContractPaymantVm
    {
        public int PaymentDetailId { get; set;}
        public string Title { get; set;}
        public string TitleAR { get; set;}
        public string ReferenceNo { get; set;}
        public string Description { get; set;}
        public string Status { get; set;}
        public string Contractno { get; set;}
        public string CreatedOn { get; set;}
        public string Cheque { get; set;}
        public string PaymentType { get; set;}
        public string PaymentTypeAR { get; set;}
        public string ChequeNo { get; set;}
        public decimal Amount { get; set;}
    }

}
