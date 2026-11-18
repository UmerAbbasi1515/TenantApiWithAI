using FABModels.Model;
using FABTenantApi.Classes;
using FABTenantD2Api.Classes;
using FABTenantD2Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Linq;

namespace FABTenantApi.Classes
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly FABAppDbContext _dbContext;
        private readonly Settings _settings;
        private IConfiguration _config;
        private ConfigValues configValues;
        public NotificationRepository(FABAppDbContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            _settings = new Settings();
            _config = config;
            configValues = new ConfigValues();
        }
        #region Send Email
        public NotificationTemplateVm GetTemplateInfo(int TemplateId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            NotificationTemplateVm NotificationTemplate = new NotificationTemplateVm();
            var GetNotificationTemplate = $@"select Name, Subject, BodyContent, ShortDescription, Id from cms.Template where id={TemplateId}";
            var NotificationTemplateExecute = Conn.ExecuteDataTableSqlDA(conn, GetNotificationTemplate);

            if (NotificationTemplateExecute.Rows.Count > 0)
            {
                var BindNotificationData = Conn.ToListof<NotificationTemplateVm>(NotificationTemplateExecute);
                NotificationTemplate = BindNotificationData.FirstOrDefault();

            }
            return NotificationTemplate;
        }

        public CaseVm GetCaseInformation(SqlTransaction tran, SqlConnection con, int CaseId)
        {
            CaseVm caseVm = new CaseVm();


            using (SqlCommand cmd = new SqlCommand("SP_GetCaseInfo", con, tran))
            {
                cmd.Parameters.AddWithValue("CaseNo", SqlDbType.Int).Value = CaseId;
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter oDataAdaptor = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                oDataAdaptor.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseVm>(dt);
                    caseVm = ListToBeSend.FirstOrDefault();
                }
            }
            return caseVm;
        }

        public NotificationTemplateVm Replacement(NotificationTemplateVm model, int CaseId)
        {
            var CaseInfo = GetCaseInformation(null, null, CaseId);
            if (model.Subject != null)
            {
                model.Subject = model.Subject.Replace("{CaseNo}", CaseInfo.CaseNo.ToString()).Replace("{CaseType}", CaseInfo.CaseType).Replace("{Description}", CaseInfo.CaseDescription).Replace("{CaseCategory}", CaseInfo.CaseCategory).Replace("{CaseSubCategory}", CaseInfo.CaseSubCategory).Replace("{ContactPersonName}", CaseInfo.OtherContactPersonName).Replace("{ContactPersonMobile}", CaseInfo.OtherContactPersonMobile).Replace("{Status}", CaseInfo.Status).Replace("{Casedate}", CaseInfo.CaseDate);
            }
            return model;
        }
        public string NotificationBodyReplacement(SqlTransaction tran, SqlConnection con, string BodyContent, int CaseId)
        {
            var CaseInfo = GetCaseInformation(tran, con, CaseId);
            if (BodyContent != null)
            {
                BodyContent = BodyContent.Replace("{CaseNo}", CaseInfo.CaseNo.ToString())
                                                     .Replace("{CaseType}", CaseInfo.CaseType)
                                                     .Replace("{CaseDescription}", CaseInfo.CaseDescription)
                                                     .Replace("{Description}", CaseInfo.CaseDescription)
                                                     .Replace("{CaseCategory}", CaseInfo.CaseCategory)
                                                     .Replace("{CaseSubCategory}", CaseInfo.CaseSubCategory)
                                                     .Replace("{OtherContactPersonName}", CaseInfo.OtherContactPersonName)
                                                     .Replace("{OtherContactPersonMobile}", CaseInfo.OtherContactPersonMobile)
                                                     .Replace("{Status}", CaseInfo.Status)
                                                     .Replace("{CaseDate}", CaseInfo.CaseDate)
                                                     .Replace("{Assignee}", CaseInfo.Assignee)
                                                     .Replace("{Createdby}", CaseInfo.Createdby)
                                                     .Replace("{PreferredLanguage}", CaseInfo.PreferredLanguage)
                                                     .Replace("{Emirate}", CaseInfo.Emirate)
                                                     .Replace("{City}", CaseInfo.City)
                                                     .Replace("{PropertyName}", CaseInfo.PropertyName)
                                                     .Replace("{UnitNo}", CaseInfo.UnitNo)
                                                     .Replace("{Mobile}", CaseInfo.Mobile)
                                                     .Replace("{RequesterEmail}", CaseInfo.RequesterEmail)
                                                     .Replace("{VacatingReason}", CaseInfo.VacatingReason);
            }
            return BodyContent;
        }
        //This will prepare notification and to notification table
        //public string prepareAndAddEmailNotification(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, string Email)
        //{
        //    int TemplateId = 0;
        //    NotificationHookVm NotificationHook = new NotificationHookVm();


        //    // get User template Id based on parameter
        //    var GetTemplateId = $@"select UserTemplateId from cms.NotificationTypeDetials where TypeId={CaseTypeId} and EmirateId={Emirate} and CategoryId={CaseCategory} and NotificationTypeId=(select Id from cms.NotificationTypes where Code={HookCode})";
        //    var constring = _settings.GetConnectionString();
        //    constring = constring.ToString();
        //    SqlConnection conn = new SqlConnection(constring);
        //    var Properties = Conn.ExecuteDataTableSqlDA(conn, GetTemplateId);


        //    if (Properties.Rows.Count > 0)
        //    {
        //        var ListToBeSend = Conn.ToListof<NotificationHookVm>(Properties);
        //        NotificationHook = ListToBeSend.FirstOrDefault();
        //        TemplateId = NotificationHook.UserTemplateId;

        //        var TemplateInfo = GetTemplateInfo(TemplateId);
        //        // Replace {} with Origional text
        //        var template = Replacement(TemplateInfo, CaseId);

        //        AddEmailNotification(Email, template.BodyContent, template.ShortDescription, template.Subject, "Owner", "Case", CaseId);


        //    }
        //    return "";
        //}

        public string AddEmailNotification(string Email, string Message, string ShortDescription, string Title, string UserType, string RecordType, int RecordId)
        {
            // in  app.NotificationTypes  table  must have Push and Email Notification Type
            string ResponseMessage = "0";

            var typeId = 2;
            var timeZone = "Arabian Standard Time";
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            string currentDate = DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd hh:mm tt");

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            string Batch = "Push:" + DateTime.UtcNow.AddHours(4).ToString("hhmm");
            InsertedRecordVM selected = new InsertedRecordVM();
            //noneed
            var AddReplyQuery = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title,UserId,Description,Batch,CurrentStatus,CreatedOn,Email,UserType,Popup, RecordType, RecordId, ShortDescription) VALUES (0,'{typeId}',0,@pTitle,0,@pMessage,'{Batch}','Queue','{currentDate}',@Email,@UserType,0, @RecordType,{RecordId},@ShortDescription) SELECT SCOPE_IDENTITY() as InsertRecordId;";
            var messageSqlParam = new SqlParameter("@pMessage", Message);
            if (string.IsNullOrWhiteSpace(Message))
            {
                messageSqlParam = new SqlParameter("@pMessage", DBNull.Value);
            }
            var ShortDescriptionSqlParam = new SqlParameter("@ShortDescription", ShortDescription);
            if (string.IsNullOrWhiteSpace(ShortDescription))
            {
                ShortDescriptionSqlParam = new SqlParameter("@ShortDescription", DBNull.Value);
            }
            var TitleSqlParam = new SqlParameter("@pTitle", Title);
            if (string.IsNullOrWhiteSpace(Title))
            {
                TitleSqlParam = new SqlParameter("@pTitle", DBNull.Value);
            }
            var emailparam = new SqlParameter("@Email", Email);
            if (string.IsNullOrWhiteSpace(Email))
            {
                emailparam = new SqlParameter("@Email", DBNull.Value);
            }
            var RecordTypeparam = new SqlParameter("@RecordType", RecordType);
            if (string.IsNullOrWhiteSpace(RecordType))
            {
                RecordTypeparam = new SqlParameter("@RecordType", DBNull.Value);
            }
            var UserTypeParam = new SqlParameter("@UserType", UserType);
            if (string.IsNullOrWhiteSpace(UserType))
            {
                RecordTypeparam = new SqlParameter("@UserType", DBNull.Value);
            }
            var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery, messageSqlParam, TitleSqlParam, emailparam, RecordTypeparam, UserTypeParam, ShortDescriptionSqlParam);
            if (UpdateRecord.Rows.Count > 0)
            {
                var InsertListToBeSend = Conn.ToListof<InsertedRecordVM>(UpdateRecord);
                selected = InsertListToBeSend.FirstOrDefault();
            }
            try
            {
                ResponseMessage = selected.InsertRecordId.ToString();
                return ResponseMessage;
            }
            catch (Exception ex)
            {
                return ResponseMessage;
            }


        }
        #endregion Send Email

        //public string CaseOwnerEmail(int? Id)
        //{
        //    string Email = "";
        //    var query = $@"select EmailId from cms.Users where UserId={Id}";
        //    var constring = _settings.GetConnectionString();
        //    constring = constring.ToString();
        //    SqlConnection conn = new SqlConnection(constring);
        //    var Properties = Conn.ExecuteDataTableSqlDA(conn, query);

        //    if (Properties.Rows.Count > 0)
        //    {
        //        var ListToBeSend = Conn.ToListof<CaseOwnerVM>(Properties);
        //        Email = ListToBeSend.Select(p=>p.EmailId).FirstOrDefault();
        //    }
        //    return Email;
        //}

        public string AddPushNotification(int tenantId, string Message, string ShortDescription, string Title, string UserType, string RecordType, int RecordId)
        {
            string ResponseMessage = "0";

            var typeId = 1;
            UserInfovm userInfovm = new UserInfovm();
            var timeZone = "Arabian Standard Time";
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            string currentDate = DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd hh:mm tt");

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var Query = $@"select UserId from app.UserRoles where RoleId=1 and ProfileId={tenantId}";
            var Properties = Conn.ExecuteDataTableSqlDA(conn, Query);
            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<UserInfovm>(Properties);
                userInfovm = ListToBeSend.FirstOrDefault();

                var Query2 = $@"select UserId, DeviceToken from app.Users where UserId={userInfovm.UserId}";
                var Properties2 = Conn.ExecuteDataTableSqlDA(conn, Query2);
                if (Properties2.Rows.Count > 0)
                {
                    OTherUserInfovm oTherUserInfovm = new OTherUserInfovm();

                    var ListToBeSend2 = Conn.ToListof<OTherUserInfovm>(Properties2);
                    oTherUserInfovm = ListToBeSend2.FirstOrDefault();

                    string Batch = "Push:" + DateTime.UtcNow.AddHours(4).ToString("hhmm");
                    InsertedRecordVM selected = new InsertedRecordVM();
                    var AddReplyQuery = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title,UserId,Description,Batch,CurrentStatus,CreatedOn,DeviceToken,UserType,Popup, RecordType, RecordId, ShortDescription) VALUES (0,'{typeId}',0,@pTitle,{userInfovm.UserId},@pMessage,'{Batch}','Queue','{currentDate}',@DeviceToken,@UserType,1, @@DeviceToken,{RecordId},@ShortDescription) SELECT SCOPE_IDENTITY() as InsertRecordId;";
                    var messageSqlParam = new SqlParameter("@pMessage", Message);
                    if (string.IsNullOrWhiteSpace(Message))
                    {
                        messageSqlParam = new SqlParameter("@pMessage", DBNull.Value);
                    }
                    var ShortDescriptionSqlParam = new SqlParameter("@ShortDescription", ShortDescription);
                    if (string.IsNullOrWhiteSpace(ShortDescription))
                    {
                        ShortDescriptionSqlParam = new SqlParameter("@ShortDescription", DBNull.Value);
                    }
                    var TitleSqlParam = new SqlParameter("@pTitle", Title);
                    if (string.IsNullOrWhiteSpace(Title))
                    {
                        TitleSqlParam = new SqlParameter("@pTitle", DBNull.Value);
                    }
                    var DeviceTokenparam = new SqlParameter("@DeviceToken", oTherUserInfovm.DeviceToken);
                    if (string.IsNullOrWhiteSpace(oTherUserInfovm.DeviceToken))
                    {
                        DeviceTokenparam = new SqlParameter("@DeviceToken", DBNull.Value);
                    }
                    var RecordTypeparam = new SqlParameter("@RecordType", RecordType);
                    if (string.IsNullOrWhiteSpace(RecordType))
                    {
                        RecordTypeparam = new SqlParameter("@RecordType", DBNull.Value);
                    }
                    var UserTypeParam = new SqlParameter("@UserType", UserType);
                    if (string.IsNullOrWhiteSpace(UserType))
                    {
                        RecordTypeparam = new SqlParameter("@UserType", DBNull.Value);
                    }
                    var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery, messageSqlParam, TitleSqlParam, DeviceTokenparam, RecordTypeparam, UserTypeParam, ShortDescriptionSqlParam);

                    if (UpdateRecord.Rows.Count > 0)
                    {
                        var InsertListToBeSend = Conn.ToListof<InsertedRecordVM>(UpdateRecord);
                        selected = InsertListToBeSend.FirstOrDefault();
                    }
                    try
                    {
                        ResponseMessage = selected.InsertRecordId.ToString();
                        return ResponseMessage;
                    }
                    catch (Exception ex)
                    {
                        return ResponseMessage;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public string prepareAndAddPushNotification(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, int ProfileId, string Role)
        {
            int TemplateId = 0;
            NotificationHookVm NotificationHook = new NotificationHookVm();

            // get User template Id based on parameter
            var GetTemplateId = $@"select UserTemplateId from cms.NotificationTypeDetials where TypeId={CaseTypeId} and EmirateId={Emirate} and CategoryId={CaseCategory} and NotificationTypeId=(select Id from cms.NotificationTypes where Code={HookCode})";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, GetTemplateId);


            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<NotificationHookVm>(Properties);
                NotificationHook = ListToBeSend.FirstOrDefault();
                TemplateId = NotificationHook.UserTemplateId;

                var TemplateInfo = GetTemplateInfo(TemplateId);
                // Replace {} with Origional text
                var template = Replacement(TemplateInfo, CaseId);

                AddPushNotification(ProfileId, template.BodyContent, template.ShortDescription, template.Subject, Role, "Case", CaseId);
            }
            return "OK";
        }

        public string prepareAndAddEmailNotification(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, string Email)
        {
            throw new NotImplementedException();
        }


    }
}
