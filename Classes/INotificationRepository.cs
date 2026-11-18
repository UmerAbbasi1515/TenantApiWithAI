using FABTenantD2Api.Models;
using Microsoft.Data.SqlClient;

namespace FABTenantApi.Classes
{
    public interface INotificationRepository
    {
        public string prepareAndAddEmailNotification(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, string Email);
        //public string CaseOwnerEmail(int? Id);
        public string prepareAndAddPushNotification(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, int ProfileId, string Role);
        public string NotificationBodyReplacement(SqlTransaction tran, SqlConnection con, string BodyContent, int CaseId);
        public NotificationTemplateVm GetTemplateInfo(int TemplateId);
    }
}
