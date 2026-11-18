using FABTenantApi.Classes;
using FABTenantD2Api.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantApi.Models
{
    public class ContractVm
    {
        public int ContractID { get; set; }
        public int ContractTypeID { get; set; }
        public int ContractPosition { get; set; }
        public string ContractPositionName { get; set; }
        public string previousContactNo { get; set; }
        public int ContractStatusID { get; set; }
        public int PropertyTypeID { get; set; }
        public int PropertyStatusID { get; set; }
        public string Contractno { get; set; }
        public string address { get; set; }
        public string addressAR { get; set; }
        public string EmirateName { get; set; }
        public string ContractDate { get; set; }
        public string ContractStartDate { get; set; }
        public string ContractStatusAR { get; set; }
        public string ContractEndDate { get; set; }
        public decimal Rentforstay { get; set; }
        public int NoOfDays { get; set; }
        public int GracePeriod { get; set; }
        public int NoOfContractYears { get; set; }
        public int Installments { get; set; }
        public string Retention { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal VATCharges { get; set; }
        public decimal VATAmount { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
        public string PropertyImage { get; set; }
        public string UnitType { get; set; }
        public string UnitTypeAR { get; set; }
        public string UnitNo { get; set; }
        public string UnitRefNo { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public string ContractStatus { get; set; }
        public int IsExpire { get; set; }       
        public int IsreadyToExpire { get; set; }
    }
    public class ContractDetailVm
    {
        public int ContractPosition { get; set; }
        public string ContractPositionName { get; set; }
        public int PropertyTypeID { get; set; }
        public int PropertyStatusID { get; set; }

        public int IsExpire { get; set; }
        public int IsreadyToExpire { get; set; }
        public int ContractID { get; set; }
        public string Contractno { get; set; }
        public string address { get; set; }
        public string addressAR { get; set; }
        public string ContractDate { get; set; }
        public string ContractStartDate { get; set; }
        public string ContractEndDate { get; set; }
        public decimal Rentforstay { get; set; }
        public int NoOfDays { get; set; }
        public int GracePeriod { get; set; }
        public int NoOfContractYears { get; set; }
        public int Installments { get; set; }
        public string Retention { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal VATCharges { get; set; }
        public decimal VATAmount { get; set; }
        public string UnitName { get; set; }
        public string PropertyName { get; set; }
        public string UnitNameAR { get; set; }
        public string UnitPicture { get; set; }
        public string UnitType { get; set; }
        public string UnitTypeAR { get; set; }
        public string ContractStatus { get; set; }
        public int ContractTypeID { get; set; }
        public string ContractStatusAR { get; set; }
        public string UnitNo { get; set; }
        public int IsCanceled { get; set; }
        public PaymentVm payment { get; set; }
    }
    public class SingleUnitVm
    {
        public string UnitName { get; set; }
        public string UnitPicture { get; set; }
        public string UnitType { get; set; }
        public int ContractID { get; set; }
    }
    public class ContractUnitVm
    {
        public string MeasurementType { get; set; }
        public decimal? AreaSizeSqm { get; set; }
        public string Areasize { get; set; }
        public int UnitID { get; set; }
        public byte[] PropertyImageInByte { get; set; }
        public string PropertyImage { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
        public string UnitName { get; set; }
        public string UnitTypeAR { get; set; }
        public string UnitViewAR { get; set; }
        public string UnitNo { get; set; }
        public string UnitCategory { get; set; }
        public string UnitCategoryAR { get; set; }
        public string Landlord { get; set; }
        public string LandlordAR { get; set; }
        public string UnitView { get; set; }
        public string UnitType { get; set; }
        public decimal CurrentRent { get; set; }
        public string FloorNo { get; set; }
        public int BedRooms { get; set; }
        public int Balconies { get; set; }
        public int Kitchens { get; set; }
        public int LivingRooms { get; set; }
        public int Washrooms { get; set; }
        public int MaidRooms { get; set; }
        public int DriverRooms { get; set; }
        public int ContractID { get; set; }
    }
    public class ContractChargesVm
    {
        public int ChargeNo { get; set; }
        public int? ChargesTypeID { get; set; }
        public string ChargeName { get; set; }
        public int TenancyCodeNumber { get; set; }
        public decimal Amount { get; set; }
        public int ContractID { get; set; }
        public decimal VATAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string CreatedOn { get; set; }
        public string ChargesType { get; set; }
        public string ChargesTypeAR { get; set; }
    }
    public class PaidCharges
    {
        public decimal? Paid { get; set; }
    }

    public class TransactionCheque
    {
        public int TransactionID { get; set; }
        public string ChequeNo { get; set; }
        public string ChequeDate { get; set; }
        public string BankName { get; set; }
        public decimal? ChequeAmount { get; set; }
        public string ChequeStatus { get; set; }
        public string ChequeStatusAR { get; set; }
        public string BankNameAR { get; set; }
    }
    public class ContractTypesVm
    {
        public string PropertyType { get; set; }
        public int PropertyTypeID { get; set; }
        public string PropertyTypeAR { get; set; }
    }
    public class ContractStatusVm
    {
        public int ContractTypeID { get; set; }
        public string ContractType { get; set; }
        public string ContractTypeAR { get; set; }
    }
    public class UnitAdditionalInfo
    {
        public string FacilityDescription { get; set; }
        public string FacilityDescriptionAR { get; set; }
    }
    public class ContractChargeReceipts
    {
        public decimal? PaymentAmount { get; set; }
        public string ReceiptNo { get; set; }
        public string TransactionDate { get; set; }
        public string PaymentType { get; set; }
    }

    public class PropertyImageVm
    {
        public string PropertyImage { get; set; }
        public string PropertyName { get; set; }
        public int PropertyID { get; set; }
    }
    public class UnitImageVm
    {
        public string UnitImage { get; set; }
        public string UnitName { get; set; }
        public int UnitID { get; set; }
    }
    public class IsCardAllowedVM
    {
        public int CardApproved { get; set; }

    }
    public class ContractPayableVM
    {
        public int? ContractID { get; set;}
        public int? ChargesTypeID { get; set;}
        public int? ContractchargeID { get; set;}
        public int? PaymentID { get; set;}
        public string Contractno { get; set;}
        public string PaymentDate { get; set;}
        public decimal PaymentValue { get; set;}
        public decimal? PaidAmount { get; set;}
        public decimal Balance { get; set;}
        public string PaymentNo { get; set;}
        public string Type { get; set;}
        public string TypeAR { get; set;}
        public int Installments { get; set;}
    }
    public class CaseExistVm
    {
        public string Description { get; set; }
        public int? CaseNo { get; set; }
        public decimal? Amount { get; set; }
    }

    public class OtherCheckInContract
    {
        public int ContractPosition { get; set; }
        public int ContractDays { get; set; }
        public DateTime? ContractDate { get; set; }
    }
    public class VATOnRentVM
    {
        public decimal? VATOnRent { get; set; }
    }
    public class VatonRentVm
    {
        public decimal? PaymentAmount { get; set; }
        public decimal? ChequeAmount { get; set; }
    }
    public class VatRecordVm
    {
        public int? PaymentId { get; set; }
    }
    public class RenewalCaseVm
    {
        public int IsExpire { get; set; }
        public int IsreadyToExpire { get; set; }
        public int ContractTypeID { get; set; }
    }
    public class ContractRenewalActionVm
    {
        public int ShowOfferLetter { get; set;}
        public int DueActionid { get; set;}
        public int Contractid { get; set;}
        public int CaseId { get; set;}
        public int ExpiringingDays { get; set;}
        public int RenewalContractid { get; set;}
        public int StageId { get; set;}
        public string StageName { get; set;}
        public string PreviousContractNo { get; set;}
        public string PropertyName { get; set;}
        public string Fromdate { get; set;}
        public string ToDate { get; set;}
        public string Contractno { get; set;}
        public string Status { get; set;}
        public string StatusAr { get; set;}
        public string PropertyNameAr { get; set;}
        public string EmirateName { get; set;}
        public int IsAllPaid { get; set;}
        public int IsCanceled { get; set;}
        public int IsClosed { get; set; }

    }
    public class RegisterContractPayment
    {
        public List<NewPaymentDetailVm> DetailList { get; set; }
    }
    public class NewPaymentDetailVm
    {
        [AllowedExtensions(new string[] { ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".doc", ".gif", ".apng", ".bmp", ".pjpeg" })]
        public IFormFile Cheque { get; set; }
        public int PaymentID { get; set; }
        public int? VatOnRentContractId { get; set; }
        public int? VATOnChargeID { get; set; }
        public int? ContractId { get; set; }
        public int? PaymentMethodId { get; set; }
        public int ContractchargeID { get; set; }
        [RegularExpression(@"^[a-zA-Z0-9 ,_-]*$", ErrorMessage = "Special characters are not allowed.")]
        public string Title { get; set; }
        [RegularExpression(@"^[a-zA-Z0-9 ,_-]*$", ErrorMessage = "Special characters are not allowed.")]
        public string AramexAddress { get; set; }
        [RegularExpression(@"^[a-zA-Z0-9 ,_-]*$", ErrorMessage = "Special characters are not allowed.")]
        public string SelfDelivery { get; set; }
        [RegularExpression(@"^[a-zA-Z0-9 ,_-]*$", ErrorMessage = "Special characters are not allowed.")]
        public string Description { get; set; }
        [RegularExpression(@"^[a-zA-Z0-9 ,_-]*$", ErrorMessage = "Special characters are not allowed.")]
        public string Type { get; set; }
        public decimal Amount { get; set; }
    }

    public class ContractPay
    {
        [AllowedExtensions(new string[] { ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".doc", ".gif", ".apng", ".bmp", ".pjpeg" })]
        public IFormFile File { get; set; }
        public string PaymentModeId { get; set; }
        public string PaymentSettingId { get; set; }
        //remove validation becase of encipyted data
       // [RegularExpression(@"^[a-zA-Z0-9 ,_-]*$", ErrorMessage = "Special characters are not allowed.")]
        public string Address { get; set; }
        //[RegularExpression(@"^[a-zA-Z0-9 ,_-]*$", ErrorMessage = "Special characters are not allowed.")]
        public string SelfDelivery { get; set; }
       // [RegularExpression(@"^[a-zA-Z0-9 ,_-]*$", ErrorMessage = "Special characters are not allowed.")]
        public string ChequeNo { get; set; }
    }
    public class ContractAddress
    {
        
        public int ContractId { get; set; }
        public string Address { get; set; }
        public string SelfDelivery { get; set; }
    }
    public class CaseCheckInVM
    {
        public int? CaseNo { get; set; }
        public int? ActionDueId { get; set; }
        public int? ContractId { get; set; }
    }
    public class PaymentSettingVm
    {
        public int PaymentSettingId { get; set; }
        public string Cheque { get; set; }
    }
   
}
