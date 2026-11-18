using FABTenantApi.Models;
using FABTenantD2Api.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantApi.Classes
{
    public interface ITenantRepository
    {
        public bool GetPaymentMethodConfirmStatusTransaction(SqlTransaction tran, SqlConnection conn, int PaymentSettingId, string chequeno);
        public string UpdateStageIdTransaction(int? DueActionid, int StageId, SqlTransaction tran, SqlConnection conn);
        public string CaseOwnerEmailTransaction(int? Id, SqlTransaction tran, SqlConnection conn);
        public string prepareAndAddEmailNotificationTransaction(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, string Email, SqlTransaction tran, SqlConnection conn);

        public string prepareAndAddPushNotificationTransaction(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, int UserId, string Role, SqlTransaction tran, SqlConnection conn);
        public string GetRefNumberByContactUnitId(SqlTransaction tran, SqlConnection conn, int? contractunitId);
       public object GetRefNumber(int contractId,SqlTransaction tran,SqlConnection con);
        List<TenantVm> GetTopTenant();
        int GetTenantId(int UserId);
        bool GetPaymentMethodConfirmStatus(int PaymentSettingId, string chequeno);
        List<CaseServiceVm> GetServiceRequests(int TenantId);
        List<NotificationVm> GetNotifications(string search, string status, int UserId, int pageNo = 1, int pageSize = 100);
        bool ReadNotification(int NotificationId, int? UserId);
        bool ArchivedNotification(int NotificationId, int? UserId);
        bool AddAccessLog(AddAccessLog addLog);
        NotificationVm GeNotificationDetails(int NotificationId, int UserId);
        TenantVm GetProfile(int TenantId);
        List<AccessLogVm> GeAccessLogs(string search, int UserId, int pageNo = 1, int pageSize = 100);
        public DateTime GetCurrentDateTime();
        public string SendPushNotification(int UserId, string Message, string ShortDescription, string Title, string TitleAr, string MessageAr, string UserType, int RecordId, string RecordType);
        public string SendEmailNotification(int UserId, string Message, string Title, string TitleAr, string MessageAr, string UserType);
        public int CaseOwnerId(int UserId, string UserName, string MobileNo);


        public int CaseOwnerIdTransaction(SqlTransaction tran, SqlConnection con, int UserId, string UserName, string MobileNo);



        public string AutoAssignContractor(int CaseNo, int UserId);

        public string AutoAssignContractorTransaction(SqlTransaction tran, SqlConnection con, int CaseNo, int UserId);

        public List<DocumentTypeVm> GetDocumentType(string Type);
        public OtherRenewalContractInfoVm GetRenewalContractInfo(int ContractId);
        public int CountUnreadNotifications(int UserId);
        public NotificationContentVm GetNotificationTemplate(int TemplateId);
        public string AddTicketNewSerivceRequestCase(int CaseNo, int? UserId, int CaseOwnerId);
        public string AddTicketNewSerivceRequestCaseTransaction(SqlTransaction tran,SqlConnection con,int CaseNo, int? UserId, int CaseOwnerId);
        public string NotificationServiceRequestAssignedToContractor(string Contractor, int? UserId, int CaseNo);
        public string NotificationServiceRequestResolved(int CaseNo, int? UserId);
        public string NotificationInvitetosharefeedback(int? UserId, int CaseNo);
        public string NotificationSharingfeedback(int? UserId, int CaseNo);
        public string NotificationNewTicketAssigned(int? UserId, int caseNo);
        public bool CanCheckin(int ContractId);
        //public int? UpdateCaseUserIdTransaction(int? CategoryId, int typeid, int? emirateid, int? CaseNo, SqlTransaction tran, SqlConnection conn);
        public List<PaymentMethodVm> GetPaymentMethod();
        public string UpdateContractCaseIdNew(int? DueActionid, int CaseNo,SqlTransaction tran,SqlConnection conn);
        public string UpdateContractCaseId(int? DueActionid, int CaseNo);
        public string UpdateStageId(int? DueActionid, int StageId);
        public string RegisterContractPayment(List<NewPaymentDetailVm> modal);
        public CaseINfoVm GetCaseInfo(int caseNo);
        public CaseInfoModel GetCaseInfo1(int caseNo);
        public CaseINfoVm GetCaseInfoTransaction(int caseNo, SqlTransaction tran, SqlConnection con);
        public CaseInfoModel GetCaseInfoTrnsaction(SqlTransaction tran,SqlConnection con,int caseNo);

        public CaseDetailINfoVm GetCaseDetailInfo(int caseNo);
        public CaseDetailINfoVm GetCaseDetailInfoTransaction(SqlTransaction tran, SqlConnection con, int caseNo);
        public int? GetNotificationTemplateId(int CategoryId, int typeid, int? emirateid);
        public string GetContractPayablePayments(int ContractId);
        public CaseStageVm CaseStageInfo(int CaseId);
        public CaseContractStageVM GetContractcaseIInfo(int? ContractId);
        public int? UpdateCaseUserId(SqlConnection con, int? CategoryId, int? SubCategoryId, int typeid, int? emirateid, int? PropertyId, int? CaseNo);

        public int? UpdateCaseUserIdTransaction(SqlTransaction tran, SqlConnection con, int? CategoryId, int? SubCategoryId, int typeid, int? emirateid, int? PropertyId, int? CaseNo);


        public int? GetWorkGroupIdTransaction(SqlTransaction tran, SqlConnection con, int? CategoryId, int? SubCategoryId, int typeid, int? emirateid, int? PropertyId, int? CaseNo);

        //public int? GetWorkGroupId(int? CategoryId, int typeid, int? emirateid, int? CaseNo);

        public string AddErrorLog(string Description);
        public string AddOtherNotificationNewSerivceRequestCase(int CaseNo, int? UserId);
        public string UpdateChequePayments(int ContractId);
        public ContractStageVm ContractStageInfo(int? DueActionid);

        public string prepareAndAddEmailNotification(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, string Email);

        public string prepareAndAddEmailNotificationTransaction(SqlTransaction tran, SqlConnection con,int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, string Email);

        public string CaseOwnerEmail(int? Id);
        public string CaseOwnerEmailTransaction(SqlTransaction tran, SqlConnection con, int? Id);
        public string GetWorkGroupEmail(int? Id, int? EmrateId);
        public string prepareAndAddPushNotification(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, int UserId, string Role);


        public string prepareAndAddPushNotificationTransaction(SqlTransaction tran, SqlConnection con, int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, int UserId, string Role);

        public string CategoryInfoTransaction(SqlTransaction tran, SqlConnection con, int CategoryId);
        public int ContractRenewalActions(int TenantId);
        public int NewContractActions(int TenantId);
        public string CategoryInfo(int CategoryId);
        public string AssignFMAssignee(int CaseId);
        public string AssignPMAssineee(int CaseId, string type, bool? IsRenewal = false);


        public bool AssignPMAssineeeTransaction(SqlTransaction tran, SqlConnection con, int CaseId, string type, bool? IsRenewal = false);



        public int? GetCategoryCode(int CaseId);
        public int GetCurrentStageId(int CaseId);
        public bool InsertErrorLog(string error);
        public int GetCaseServiceRequestCategoryId(int CaseId);

        public int GetCaseServiceRequestCategoryIdTransaction(SqlTransaction tran,SqlConnection con,int CaseId);


        public bool AddNewEmailNotification(string emailbody, int UserId, string subject, string email, string Title, int CaseId);

        public bool AddNewEmailNotificationTransaction(SqlTransaction tran, SqlConnection con, string emailbody, int UserId, string subject, string email, string Title, int CaseId);


        //public bool ValidateUserFile(int FileId, int UserId);
        public bool SetSLA(int TypeId, int CategoryId, int SubCategoryId, int EmirateId, int CaseNo);


        public bool AssigneeLog(SqlTransaction tran, SqlConnection con, int caseid, int? userid = null);

        public int GetCaseNobyAttachmentid(int attachmentid);

        public int GetCaseNoPaymentSettingid(SqlTransaction t,SqlConnection con,int paymentsettingid);

        //        Select top 1 isnull(cda.CaseId,0) as CaseNo from CMS.PaymentSetting pst
        //JOIN CMS.ContractDueAction cda on pst.ContractId= cda.RenewalContractid
        //Where pst.PaymentSettingId= 79
    }
}
