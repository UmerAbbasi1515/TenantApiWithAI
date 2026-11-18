using FABTenantApi.Classes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantD2Api.Models
{
    public class ServiceRequest
    {
        public int RequestNo { get; set; }
        public int? code { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string SubCategoryAR { get; set; }
        public string CaseType { get; set; }
        public string CategoryAR { get; set; }
        public string Detail { get; set; }
        public string Date { get; set; }
        public string PropertyName { get; set; }
        public string UnitRefNo { get; set; }
        public string PropertyNameAR { get; set; }
        public string Status { get; set; }
        public string StatusAR { get; set; }
        public int Units { get; set; }
        public int? stageId { get; set; }
        public string StageName { get; set; }
        public int? DueActionid { get; set; }
    }

    public class CaseTypes
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameAR { get; set; }
    }
    public class UnitCategories
    {
        public int UnitCategoryID { get; set; }
        public string UnitCategoryName { get; set; }
        public string UnitCategoryNameAR { get; set; }
    }
    public class CitiesVm
    {
        public int CityID { get; set; }
        public string CityName { get; set; }
        public string CityNameAR { get; set; }
    }
    public class TenantPropertiesVm
    {
        public int PropertyID { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
        public string PropertyAddressAR { get; set; }
    }

    public class ContractUnitVm
    {
        public int ContractUnitID { get; set; }
        public string UnitName { get; set; }
        public string UnitNameAR { get; set; }
        public string Contractno { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
        public string UnitNo { get; set; }
    }
    public class CaseOwnerViewModal
    {
        public int CaseOwnerId { get; set; }
    }
    public class PropertyUnitVm
    {
        public string Contractno { get; set; }
        public int UnitID { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
    }
    public class ServiceRequestInfoVm
    {
        public int CaseNo { get; set; }
        public int CaseTypeId { get; set; }
        public int ContactTimeId { get; set; }
        public int InnitialAssesmentId { get; set; }
        public int ServiceLevelId { get; set; }
        public string Description { get; set; }
        public int? PropertyId { get; set; }
        public int? PreferredLanguageId { get; set; }
        public DateTime Dated { get; set; }
        public int? Units { get; set; }
        public int StatusId { get; set; }
    }
    public class GetContactTiming
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class AddServiceRequest
    {
        public int? CaseNo { get; set; }
        public int? CaseTypeId { get; set; }
        public int? ContractUnitId { get; set; }

        public int? ContactTimeId { get; set; }
        public int? CityId { get; set; }

        public int? LanguagesId { get; set; }

        public int? CaseCategoryId { get; set; }
        public int? CaseSubCategoryId { get; set; }
        public string Description { get; set; }
       
        //[RegularExpression(@"^[a-zA-Z0-9 ,_-]*$", ErrorMessage = "Special characters are not allowed.")]
        public string OtherContactPersonName { get; set; }

        //commented on 18/10/2022
     //   [RegularExpression(@"^(\+[0-9])$", ErrorMessage = "Invalid Mobile Number")]
        public string OtherContactPersonMobile { get; set; }

        //[RegularExpression(@"^(\[0-9])$", ErrorMessage = "Invalid Mobile Number")]
      //  public string OtherContactPersonMobile { get; set; }


    }

    public class InitialAssesments
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameAR { get; set; }
    }
    public class ServiceLevels
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameAR { get; set; }
    }
    public class PreferredLanguages
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class UserInfo
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string FullName { get; set; }
    }
    public class AddServiceRequestDetailVm
    {
        public int CaseNo { get; set; }
    }
    public class UpdateServiceRequest
    {
        public int CaseTypeId { get; set; }
        public int CaseNo { get; set; }
        public int StatusId { get; set; }
        public int CaseCategoryId { get; set; }
        public int PropertyId { get; set; }
        public int ContactTimeId { get; set; }
        public int InnitialAssesmentId { get; set; }
        public int ServiceLevelId { get; set; }
        public int PreferredLanguageId { get; set; }
        public string Description { get; set; }
        public DateTime Dated { get; set; }
    }
    public class ServiceRequestDetailVm
    {
        public string RequestType { get; set; }
        public int CaseFeedback { get; set; }
        public int CaseSurvey { get; set; }
        public int Code { get; set; }
        public int CaseNo { get; set; }
        public int CaseCategouryId { get; set; }
        public int CaseSubCatagouryId { get; set; }
        public string CaseType { get; set; }
        public string CaseTypeAR { get; set; }
        public string PropertyName { get; set; }
        public string VacatingReason { get; set; }
        public string VacatingDate { get; set; }
        public string PropertyNameAR { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string ContactTiming { get; set; }
        public string CategoryAR { get; set; }
        public string SubCategoryAR { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string UnitRefNo { get; set; }
        public string UnitRefNoOtherContactPersonName { get; set; }
        public string OtherContactPersonMobile { get; set; }
        public string InitialAssesment { get; set; }
        public string InitialAssesmentAR { get; set; }
        public string CaseServiceLevel { get; set; }
        public string CaseServiceLevelAR { get; set; }
        public string Description { get; set; }
        public string statusAR { get; set; }
        public string status { get; set; }
        public string language { get; set; }
        public int Units { get; set; }

    }
    public class CaseType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameAR { get; set; }
        public string Type { get; set; }
    }

    public class TicketReplyVm
    {
        public int TicketReplyId { get; set; }
        public string Reply { get; set; }
        public string DateTime { get; set; }
        public string FileName { get; set; }
        public int? UserId { get; set; }
    }

    public class AddTicketVm
    {
        public string CaseId { get; set; }
       // [RegularExpression(@"^[a-zA-Z0-9 ,'-_.]*$", ErrorMessage = "Special characters are not allowed.")]
        public string Reply { get; set; }
        [AllowedExtensions(new string[] { ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".doc", ".gif", ".apng", ".bmp", ".pjpeg" })]
        public IFormFile File { get; set; }
    }
    public class FaqCategoriesVm
    {
        public int CategoryId { get; set; }
        //[RegularExpression(@"^[a-zA-Z0-9''-'\s]{1,400}$", ErrorMessage = "Special characters are not allowed.")]
        public string Title { get; set; }
        public string TitleAR { get; set; }
    }
    public class InsertedPhotoVM
    {
        public decimal SelectedId { get; set; }
    }
    public class FaqVm
    {
        public string Title { get; set; }
        public string TitleAR { get; set; }
        public string Description { get; set; }
        public string DescriptionAR { get; set; }
    }
    public class FaqQuestionVm
    {
        public int QuestionId { get; set; }
        public string Title { get; set; }
        public int AnswerId { get; set; }
        public string TitleAR { get; set; }
        public string Description { get; set; }
        public string DescriptionAR { get; set; }
    }
    public class ServeryOptions
    {
        public int OptionId { get; set; }
        public int? QuestionId { get; set; }
        public string OptionTitle { get; set; }
        public string OptionTitleAR { get; set; }
        public bool? DescriptionRequired { get; set; }
    }
    public class ServiceRequestPhotosVm
    {
       // [RegularExpression(@"^[a-zA-Z0-9 ,_-]*$", ErrorMessage = "Special characters are not allowed.")]
        public string AttachmentType { get; set; }
        public string CaseNo { get; set; }
        public string DocumentTypeId { get; set; }
        public string ExpireDate { get; set; }
        [AllowedExtensions(new string[] { ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".doc", ".gif", ".apng", ".bmp", ".pjpeg" })]
        public List<IFormFile> File { get; set; }

        //addeed on 09-12-2022
        public string EmirateIdNumber { get; set; }
        public string Nationality { get; set; }
        public string NameEng { get; set; }
        public string NameAr { get; set; }
        public string IssueDate { get; set; }
        public string DOB { get; set; }
    }
    public class RemoveServiceRequestVm
    {
        public int PhotoId { get; set; }
    }
    public class TicketFilesVm
    {
        public int CaseId { get; set; }
        [AllowedExtensions(new string[] { ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".doc", ".gif", ".apng", ".bmp", ".pjpeg" })]
        public List<IFormFile> File { get; set; }
    }

    public class ServiceRequestFiles
    {
        public int photoId { get; set; }
        public string PhotoSource { get; set; }
        public string AttachmentType { get; set; }
    }
    public class SaveSRFeedbackVm
    {
        public int CaseId { get; set; }
        public string Description { get; set; }
        public decimal? Rating
        {
            get; set;
        }
    }
    public class GetSRFeedbackVm
    {
        public int CaseNo { get; set; }
        public string Description { get; set; }
        public decimal? Rating { get; set; }
    }

    public class ServiceRequestFileResponse
    {
        public ServiceRequestFileResponse()
        {
            photoId = new List<int>();
            AttachmentType = new List<string>();
            imageByteArray = new List<byte[]>();
        }
        public List<int> photoId { get; set; }
        public List<byte[]> imageByteArray { get; set; }
        public List<string> AttachmentType { get; set; }
    }

    public class CMSUserInfoVM
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
    }
    public class RequestDetailFeedbackVM
    {
        public bool CanCancel { get; set; }
        public bool CanReopen { get; set; }
        public bool CanAddFeedback { get; set; }
        public bool RequestClosed { get; set; }
        public bool CanTakeSurvey { get; set; }
        public bool CanUploadDocuments { get; set; }
        public bool CanUploadPhotos { get; set; }
        public bool CanChat { get; set; }
    }
    public class MaxNumber
    {
        public int MaxCase { get; set; }
    }

    public class UpdateProfileServiceRequest
    {
        public int? CaseNo { get; set; }
        public string Description { get; set; }
        public string PersonName { get; set; }
        public string PersonMobile { get; set; }
        [DataType("Email")]
        public string PersonEmail { get; set; }
        public string Address { get; set; }
    }
    public class CaseInfoVm
    {
        public int PropertyId { get; set; }
        public int CaseSubCatagouryId { get; set; }
        public string categoryName { get; set; }
    }
    public class GetContractorVm
    {
        public int? ContractorID { get; set; }
        public string Company { get; set; }
    }

    public class DocumentTypeVm
    {
        public string Name { get; set; }
        public int DocumentTypeId { get; set; }
    }

    public class OtherRenewalContractInfoVm
    {
        public int ContractID { get; set; }
        public int Installments { get; set; }
        public int? EmirateID { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? Amount { get; set; }
        public string contractno { get; set; }
        public string fromDate { get; set; }
        public string EndDate { get; set; }
        public string AddNewDate { get; set; }
        public string EndNewDate { get; set; }
        public string EmirateName { get; set; }
    }
    public class InsertedRecordVM
    {
        public decimal InsertRecordId { get; set; }
    }

    public class AttachmentFileVm
    {
        public string Name { get; set; }
        public string NameAr { get; set; }
        public string AttachmentUrl { get; set; }
        public int DocumentTypeId { get; set; }
        public string Extentsion { get; set; }
        public string AttachmentDate { get; set; }
        public string ExpireDate { get; set; }
        public int? AttachmentId { get; set; }
        public bool? IsRejected { get; set; }
    }

    public class AttachmentOfferFileVm
    {
        public int DocumentID { get; set; }
        public string Title { get; set; }
        public string OriginalFiles { get; set; }
        public string PhysicalFolder { get; set; }
        public DateTime AddedDate { get; set; }
        public string DocumentType { get; set; }
    }

    public class VacatingVm
    {
        public int VacatingId { get; set; }
        public string Title { get; set; }
        public string TitleAr { get; set; }
        public string Description { get; set; }
        public string DescriptionAR { get; set; }
    }

    public class NotificationContentVm
    {
        public string Subject { get; set; }
        public string SubjectAr { get; set; }
        public string BodyContent { get; set; }
        public string BodyContentAr { get; set; }
        public string ShortDescription { get; set; }
    }
    public class CaseServiceRequestVM
    {
        public int CaseCategouryId { get; set; }
    }

    public class CheckInContract
    {
        public int? ContractPosition { get; set; }
    }
    public class CaseContractVm
    {
        public int? CaseContractDetailId { get; set; }
    }

    public class ContractInfoVm
    {
        public string Contractno { get; set; }
        public int ContractID { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
    }
    public class ContractPhotosVm
    {
        public int? DueActionId { get; set; }
        public string AttachmentType { get; set; }
        public int CaseNo { get; set; }
        public int DocumentTypeId { get; set; }
        public string ExpireDate { get; set; }
        public List<IFormFile> File { get; set; }
    }

    public class CaseEmirateVm
    {
        public int? EmirateID { get; set; }
        public int? PropertyID { get; set; }
    }

    public class GetContractInfoVm
    {
        public string ContractDate { get; set; }
    }
    public class UpdateCaseVm
    {
        public string AttachmentId { get; set; }
        public string ExpireDate { get; set; }
        [AllowedExtensions(new string[] { ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".doc", ".gif", ".apng", ".bmp",".pjpeg" })]
        public List<IFormFile> File { get; set; }
    }

    public class ContractCategoryVm
    {
        public int? ContractCategoryID { get; set; }
        public int? ContractCategoryID2 { get; set; }
        public int? ContractCategoryID3 { get; set; }
    }
}
