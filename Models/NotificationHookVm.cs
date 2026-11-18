namespace FABTenantD2Api.Models
{
    public class NotificationHookVm
    {
        public int UserTemplateId { get; set; }
    }
    public class NotificationTemplateVm
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public string SubjectAr { get; set; }
        public string BodyContent { get; set; }
        public string BodyContentAr { get; set; }
        public string ShortDescription { get; set; }
        public int Id { get; set; }
    }
    public class CaseVm
    {
        public string CaseCategory { get; set; }
        public string CaseCategoryAr { get; set; }
        public string CaseType { get; set; }
        public string CaseTypeAr { get; set; }
        public string CaseSubCategory { get; set; }
        public string CaseSubCategoryAr { get; set; }
        public int CaseNo { get; set; }
        public string Mobile { get; set; }
        public string RequesterEmail { get; set; }
        public string CaseDescription { get; set; }
        public string OtherContactPersonName { get; set; }
        public string OtherContactPersonMobile { get; set; }
        public string Status { get; set; }
        public string StatusAr { get; set; }
        public string CaseDate { get; set; }
        public string Assignee { get; set; }
        public string Createdby { get; set; }
        public string PreferredLanguage { get; set; }
        public string Emirate { get; set; }
        public string City { get; set; }
        public string PropertyName { get; set; }
        public string UnitNo { get; set; }
        public string VacatingReason { get; set; }
        public int? PropertyId { get; set; }
        public int? EmirateId { get; set; }
    }
    public class CaseOwnerVM
    {
        public string EmailId { get; set; }
        public string Email { get; set; }
    }
    public class UserInfovm
    {
        public int UserId { get; set; }
    }
    public class OTherUserInfovm
    {
        public int UserId { get; set; }
        public string DeviceToken { get; set; }
    }

    public class EngineerVm
    {
        public int? EngineerCharge { get; set; }
    }

    public class CMSUserVm
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }
    public class DepartmentStage
    {
        public int? DepartmentId { get; set; }
        public int DueActionid { get; set; }
    }
    public class CasesListVM
    {
        public int? UserId { get; set; }
        public int? CaseNo { get; set; }
    }
    public class CategoryInfo
    {
        public string Type { get; set; }
    }

    public class CategoryCodeVm
    {
        public int? code { get; set; }
    }
    public class CaseServiceRequestViewModa
    {
        public int CategoryId { get; set; }
    }
    public class SLAInfoVm
    {
        public int SLAId { get; set; }
    }

    public class CurrentStageVM
    {
        public int StageId { get; set; }
    }
    public class ContractorUserIDVM
    {
        public int UserId { get; set; }
    }
}
