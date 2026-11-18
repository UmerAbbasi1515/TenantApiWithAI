using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantApi.Models
{

    public class TransactionVm
    {
        public int TransactionId { get; set; }
        public int ContractID { get; set; }

        public string PaymentDate { get; set; }

        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
        public string Status { get; set; }
        public string StatusAr { get; set; }

        public string PropertyNumber { get; set; }

        public string ReceiptNo { get; set; }

        public string AccountNo { get; set; }

        public string AccountTitle { get; set; }
        public decimal Amount { get; set; }

        public string PaymentType { get; set; }
        public string PaymentTypeAR { get; set; }

        public string ContractDate { get; set; }

        public string Tenant { get; set; }
        public string TenantAR { get; set; }

        public string UnitNo { get; set; }

        public string ContractNo { get; set; }

        public string City { get; set; }
        public string CityAR { get; set; }

        public string PaymentFor { get; set; }
        public string PaymentForAR { get; set; }

        public string LandLord { get; set; }

       

    }
    public class InvoiceVm
    {

        public InvoiceTransactionVm Transaction { get; set; }
        public InvoiceContractDate ContractVM { get; set; }
        public PaymentForVm PaymentFor { get; set; }
        public List<ChequesVM> Cheques { get; set; }
        public byte[] logo { get;set; }

    }
    public class InvoiceTransactionVm
    {
        public string TransactionNo { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string TransactionDate { get; set; }
        public string Description { get; set; }
        public string PayeeName { get; set; }
        public string PaymentKind { get; set; }
        public string PreparedBy { get; set; }
        public int TransactionTypeID { get; set; }
        public int PaymentFor { get; set; }
        public int ActionID { get; set; }
        public int ContractID { get; set; }
        public string PrintStatus { get; set; }
        public string PropertyName { get; set; }
        public string PropertyAddress { get; set; }
    }

    public class InvoiceContractDate
    {
        public string Name { get; set; }
        public string PropertyName { get; set; }
        public string BuildingNumber { get; set; }
        public string fromDate { get; set; }
        public string ToDate { get; set; }
        public string Contractno { get; set; }
    }

    public class PaymentForVm
    {
        public string PaymentFor { get; set; }
    }
    public class ChequesVM
    {
        public string ChequeNo { get; set; }
        public string ChequeDate { get; set; }
        public string BankName { get; set; }
        public decimal ChequeAmount { get; set; }
    }

    public class ContractsVM
    {
        public string Contractno { get; set; }
        public string UserDefinedNo { get; set; }
        public string ContractDate { get; set; }
        public string PropertyName { get; set; }
        public string Property { get; set; }
        public string PropertyArea { get; set; }
        public string Sector { get; set; }
        public string PlotNo { get; set; }
        public string fromDate { get; set; }
        public int? SDReceivedContractiD { get; set; }
        public string SecurtiyDeposits { get; set; }
        public string ToDate { get; set; }
        public string GraceStartDate { get; set; }
        public string GraceEndDate { get; set; }
        public string AnnualMinRent { get; set; }
        public string Rentforstay { get; set; }
        public string Subject { get; set; }
        public string AdditionalTerms { get; set; }
        public string OccupiedBy { get; set; }
        public string TenantShare { get; set; }
        public string OccupantShare { get; set; }
        public int? TenantID { get; set; }
        public int? Occupant { get; set; }
        public int? ContractPosition { get; set; }
    }

    

    public class TanentVm
    {
        public string Name { get; set; }
        public string NameAR { get; set; }
        public string PostBoxno { get; set; }
        public string TelNo { get; set; }
        public string FaxNo { get; set; }
        public string MobileNo { get; set; }
        public string EmirateName { get; set; }
        public string Email { get; set; }
        public int TenantTypeID { get; set; }
    }

    public class CompanyDetails
    {

        public string CompanyShortName { get; set; }
        public string CompanyName { get; set; }
        public string CompanyNameAR { get; set; }
    }

    public class LandLordDetails
    {
        public string LandlordName { get; set; }
        public string LandlordNameAR { get; set; }
    }

    public class LesseDetails
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string FullName { get; set; }
        public int MasterTenantTypeID { get; set; }
        public string FirstNameAR { get; set; }
        public string MiddleNameAR { get; set; }
        public string FullNameAR { get; set; }
    }


    public class DownloadContractVm
    {
        public string Position { get; set; }
        public string TransactionDate { get; set; }
        public string PreparedBy { get; set; }
        public string UnitNo { get; set; }
        public string Bedrooms { get; set; }
        public string Area { get; set; }
        public string UnitStore { get; set; }
        public string UnitParking { get; set; }
        public byte[] TenantSignImageByte { get; set; }
        public byte[] ApproverSignImageByte { get; set; }


        public int ContractTypeID { get; set; }
        public string ContractTypeName { get; set; }
        public string ContractTypeAR { get; set; }


        public ContractsVM ContractsVM { get; set; }
        public TanentVm TanentVm { get; set; }
        public List<ParkingRefVm> parking { get; set; }
        public List<UnitRefVm> UnitRefVm { get; set; }
        public List<UnitRefVm> UnitStoreVm { get; set; }
        public CompanyDetails CompanyDetail { get; set; }
        public LandLordDetails landLordDetails { get; set; }
        public LesseDetails LesseDetails { get;set; }


        public byte[] logo { get; set; }
        public byte[] ImageByte { get; set; }
        public byte[] Res2 { get; set; }
        public byte[] Res3 { get; set; }
        public byte[] Res4 { get; set; }
        public byte[] Res5 { get; set; }

    }
    public class UnitRefVm
    {
        public string UnitRefNo { get; set; }
        public int? BedRooms { get; set; }
        public decimal? AreaSizeSqm { get; set; }
        public string MeasurementType { get; set; }
        public string Areasize { get; set; }
    }
    public class ParkingRefVm
    {
        public string ParkingRefNo { get; set; }
    }
    public class TransactionDateVm
    {
        public string TransactionDate { get; set; }
    }
    public class PreparedByVm
    {
        public string Name { get; set; }
    }

    public class ContactIdVm
    {
        public int ContractID { get; set; }
    }
    public class RequestSignatureVM
    {
        public int photoId { get; set; }
        public string PhotoSource { get; set; }
        public string AttachmentType { get; set; }
        public int CaseNo { get; set; }
    }

    public class TermConditionVM
    {
        public int Id { get; set; }
        public string Residential { get; set; }
        public string Commercial { get; set; }
        public string Type { get; set; }
    }

    public class ContractTermsVm
    {
        public int? TenantTypeID { get; set; }
        public string Contractno { get; set; }
        public int ContractType { get; set; }
        public string ContractTypeName { get; set; }
        public string ContractTypeAR { get; set; }
    }
    public class ContractExpireVm
    {
        public int ContractID { get; set; }
        public string ContractDate { get; set; }
        public string Fromdate { get; set; }
        public string Todate { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
        public string Contractno { get; set; }
        public string Status { get; set; }
        public string StatusAR { get; set; }
        public int? Installments { get; set; }
        public decimal? RentAmount { get; set; }
        public decimal? Rentforstay { get; set; }
    }
    public class ChequeDue30DaysVm
    {
        public string AccountTitle { get; set;}
        public string ChequeNo { get; set;}
        public string check_bank { get; set;}
        public decimal? ChequeAmount { get; set;}
        public int? TransactionID { get; set;}
    }
    public class PaymentMethodVm
    {
        public int PaymentMethodId { get; set; }
        public string Name { get; set; }
    }
    public class ContractPaymentVM
    {
        public int PaymentSettingId { get; set; }
        public int Confirmed { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string PaymentDate { get; set; }
        public string TitleAr { get; set; }
        public string PaymentMode { get; set; }
        public string AramexAddress { get; set; }
        public string Cheque { get; set; }
        public string ChequeNo { get; set; }
        public decimal? Amount { get; set; }
        public string Description { get; set; }
        public int ContractId { get; set; }
        public int PaymentMethodId { get; set; }
        public int? ContractchargeID { get; set; }
        public int? ContractPaymentID { get; set; }
        public int? VatOnRentContractId { get; set; }
        public int? VatOnChargeId { get; set; }
        public string SelfDelivery { get; set; }
        public bool? IsRejected { get; set; }
        public AcceptPaymentType AcceptPaymentType { get; set; }
    }
    public enum AcceptPaymentType
    {
        All,
        CardPayment,
        BankTranfser,
        Cheque,
        CardPaymentBankTransfer,
        CardPaymentCheque,
        BankTransferCheque
    }
    //public enum AcceptPaymentType
    //{
    //    Both,
    //    Online,
    //    Cheque
    //}
    public class NotificationTemplate
    {
        public int UserTemplateId { get; set; }
    }
    public class NotificationUserVm
    {
        public int OwnerId { get; set; }
    }

    public class CaseINfoVm
    {
        public int CaseTypeId { get; set;}
        public int CaseCategouryId { get; set;}
        public int? CaseSubCatagouryId { get; set;}
        public int? EmirateId { get; set;}
        public int? UserId { get; set;}
        public string? FullName { get; set; }
        public int WorkGroupId { get; set; }
    }
    public class CaseDetailINfoVm
    {
        public int CaseTypeId { get; set; }
        public int CaseCategouryId { get; set; }
        public int? EmirateId { get; set; }
        public int? WorkGroupId { get; set; }
        public int? AssignId { get; set; }
        public string Assignee { get; set; }
        public string WorkGroup { get; set; }
        public string WorkGroupEmail { get; set; }
        public string CategoryName { get; set; }
        public int CaseNo { get; set; }
    }
    public class CaseStageVm
    {
        public int DueActionid { get; set; }
        public int StageId { get; set; }
    }
    public class ContractStageVm
    {
        public int UserId { get; set; }
        public int CaseNo { get; set; }
        public string MobileTel { get; set; }
        public int StageId { get; set; }
        public int CaseOwnerId { get; set; }
        public string Stage { get; set; }
        public string FullName { get; set; }
    }
    public class CaseContractStageVM
    {
        public int? caseid { get; set; }
        public int? StageId { get; set; }
        public int DueActionid { get; set; }
       
    }
    public class ContractDueActionStageVM
    {
        public int DueActionid { get; set; }
    }
    public class ApproveMunicipalVM
    {
        public string Description { get; set; }
    }
    public class PaymentSettingListVm
    {
        public int PaymentSettingId { get; set; }
    }
    public class CaseDetailsVm
    {
        public int CaseNo { get; set; }
        public string EmailId { get; set; }

        public int AssigneeId { get; set; }
        public string FullName { get; set; }
        public int UserId { get; set; }
        public int ContractId { get; set; }
        public decimal Amount { get; set; }
    }
    public class UpdateChequeVM
    {
        public int ReasonOfPayment { get; set; }
        public int ChequeCash { get; set; }
        public decimal ChequeAmount { get; set; }
        public int ChequeID { get; set; }
        public int TransactionID { get; set; }
        public int? VATROPID { get; set; }
        public decimal PaymentAmount { get; set; }
    }

    public class OfferLetterVm
    {
        public decimal? TotalPayable { get; set; }
        public string UnitName { get; set; }
        public string Contractno { get; set; }
        public string fromDate { get; set; }
        public string TenantName { get; set; }
        public string ToDate { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
        public string UnitRefNo { get; set; }
        public string PropertyImage { get; set; }
        public string PropertyType { get; set; }
        public string PropertyTypeAR { get; set; }
        public string UnitType { get; set; }
        public string UnitTypeAR { get; set; }
        public int UnitID { get; set; }
        public int BedRooms { get; set; }
        public int RentPaidByEntity { get; set; }
    }
}
