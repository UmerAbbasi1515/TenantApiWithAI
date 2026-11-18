using FABModels.Model;
using FABTenantApi.Models;
using FABTenantD2Api.Classes;
using FABTenantD2Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantApi.Classes
{
    public class TenantRepository : ITenantRepository
    {
        private readonly FABAppDbContext _dbContext;
        private readonly Settings _settings;
        private IConfiguration _config;
        private ConfigValues configValues;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationRepository _notificationRepository;
        public TenantRepository(FABAppDbContext dbContext, IConfiguration config, INotificationRepository notificationRepository, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _settings = new Settings();
            _config = config;
            configValues = new ConfigValues();
            _httpContextAccessor = httpContextAccessor;
            _notificationRepository = notificationRepository;
        }
        #region Basic

        public DateTime GetCurrentDateTime()
        {
            //var timeZone = "Arabian Standard Time";
            //TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            //DateTime currentDate = TimeZoneInfo.ConvertTime(DateTime.Now, tz);
            //DateTime CurrentDateTime = DateTime.Now;
            DateTime CurrentDateTime = Convert.ToDateTime(DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd hh:mm tt"));
            return CurrentDateTime;
        }
        public class isconfirmedvm
        {
            public bool IsConfirmed { get; set; }
        }

        public string GetRefNumberByContactUnitId(SqlTransaction tran, SqlConnection conn, int? contractunitId)
        {

            string Data = "";
            try
            {


                var RefNumber = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, conn, $"SELECT STRING_AGG(unitrefno,',') as Unitrefno FROM Pms_ContractUnits cu INNER JOIN Pms_Unit u ON u.UnitID = cu.UnitID WHERE cu.Suspend = 0 AND cu.ContractUnitID = {contractunitId}");
                Cases cases = new Cases();
                if (RefNumber.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<Cases>(RefNumber);

                    cases = ListToBeSend.FirstOrDefault();
                    Data = cases.Unitrefno;

                }

            }

            catch (Exception ex)
            {
                throw ex;
            }
            return Data;
        }


        public Object GetRefNumber(int contractId, SqlTransaction tran, SqlConnection con)
        {

            string data = "";

            var GetunitrefQuery = $"SELECT STRING_AGG(unitrefno,',') as Unitrefno FROM Pms_ContractUnits cu INNER JOIN Pms_Unit u ON u.UnitID = cu.UnitID WHERE cu.Suspend = 0 AND cu.ContractID={contractId}";

            var RefNumber = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, GetunitrefQuery);
            Cases cases = new Cases();
            if (RefNumber.Rows.Count > 0)
            {
                var ListToBeSend1 = Conn.ToListof<Cases>(RefNumber);
                cases = ListToBeSend1.FirstOrDefault();
                data = cases.Unitrefno;

            }
            return data;


        }

        public bool GetPaymentMethodConfirmStatusTransaction(SqlTransaction tran, SqlConnection conn, int PaymentSettingId, string chequeno)
        {
            bool data = false;

            var query = $@"select TOP 1 1 as IsConfirmed from cms.PaymentSetting  ps  where ps.PaymentSettingId = {PaymentSettingId} and ps.Confirmed =1 and ps.ChequeNo = @ChequeNo ";
            var chequenoSqlParam = new SqlParameter("@ChequeNo", chequeno);
            if (string.IsNullOrWhiteSpace(chequeno))
            {
                chequenoSqlParam = new SqlParameter("@ChequeNo", DBNull.Value);
            }
            SqlParameter[] sqlParameters = new SqlParameter[]
            {
                chequenoSqlParam
            };
            var ContractTerm = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, conn, query, sqlParameters);
            isconfirmedvm isconfirmedvm = new isconfirmedvm();
            if (ContractTerm.Rows.Count > 0)
            {
                //var ListToBeSend1 = Conn.ToListof<isconfirmedvm>(ContractTerm);
                //isconfirmedvm = ListToBeSend1.FirstOrDefault();
                data = Convert.ToBoolean(ContractTerm.Rows[0][0]); //isconfirmedvm.IsConfirmed;

            }
            return data;
        }

        public bool GetPaymentMethodConfirmStatus(int PaymentSettingId, string chequeno)
        {
            bool data = false;

            var query = $@"select TOP 1 1 as IsConfirmed from cms.PaymentSetting  ps  where ps.PaymentSettingId = {PaymentSettingId} and ps.Confirmed =1 and ps.ChequeNo = @ChequeNo ";
            var chequenoSqlParam = new SqlParameter("@ChequeNo", chequeno);
            if (string.IsNullOrWhiteSpace(chequeno))
            {
                chequenoSqlParam = new SqlParameter("@ChequeNo", DBNull.Value);
            }
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, query, chequenoSqlParam);
            isconfirmedvm isconfirmedvm = new isconfirmedvm();
            if (ContractTerm.Rows.Count > 0)
            {
                var ListToBeSend1 = Conn.ToListof<isconfirmedvm>(ContractTerm);
                isconfirmedvm = ListToBeSend1.FirstOrDefault();
                data = isconfirmedvm.IsConfirmed;

            }
            return data;
        }
        public List<TenantVm> GetTopTenant()
        {
            var tenant = _dbContext.Pms_Tenant.Where(x => x.Suspend == 0)
                 .Select(p => new TenantVm
                 {
                     Mobile = p.Mobile,
                     Name = p.FullName,
                     TenantId = p.TenantId,
                     Email = p.Email,
                     Nationality = p.Nationality,
                     Address = p.Address1 + " " + p.Address2,
                     Fax = p.Fax,
                     Phone = p.Phone,
                     PhotoUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}{_httpContextAccessor.HttpContext.Request.PathBase}" + "/tenant01/Images/displayimage.jpg",
                     TermsAndConditions = "A tenancy agreement is a contract between you and a landlord."

                 }).Take(10).ToList();

            return tenant;
        }
        public int GetTenantId(int UserId)
        {
            var tenantId = 0;
            if (UserId != 0)
            {
                var userMobile = _dbContext.Users.Where(p => p.UserId == UserId).Select(p => p.Mobile).FirstOrDefault();
                var id = _dbContext.Pms_Tenant.Where(p => p.MobileCountryCode.Replace("00", "+") + p.MobileAreaCode + p.Mobile == userMobile).Select(p => p.TenantId).FirstOrDefault();

                //changed && to || 14/09/2022
                if (id == null || id == 0)
                {
                    id = _dbContext.Pms_Tenant.Where(p => p.Mobile == userMobile).Select(p => p.TenantId).FirstOrDefault();
                }
                tenantId = id;
            }
            return tenantId;
        }
        public List<CaseServiceVm> GetServiceRequests(int TenantId)
        {
            var dateFormat = _settings.DateFormat();
            var services = new List<CaseServiceVm>();

            using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = $"SELECT Top 50 CMS.Cases.CaseOwnerId, CMS.Cases.CaseNo, CMS.Cases.Email, CMS.Cases.WorkNo, CMS.Cases.ResidenceTel, CMS.Cases.MobileTel, Format(CMS.Cases.Dated, '{dateFormat}') as Dated, CMS.Cases.EmirateId, Pms_Property.PropertyName,Pms_Property.PropertyNameAR, Pms_Property.BuildingNumber, Pms_Property.BuildingRefNo, CMS.CaseServiceRequest.Description, CMS.CaseServiceRequest.ProposedRemedy, CMS.CaseServiceRequest.ReasonForVocation FROM CMS.Cases INNER JOIN Pms_Tenant ON CMS.Cases.CaseOwnerId = Pms_Tenant.TenantID INNER JOIN CMS.CaseServiceRequest ON CMS.Cases.CaseNo = CMS.CaseServiceRequest.CaseId RIGHT OUTER JOIN Pms_Property ON CMS.Cases.PropertyId = Pms_Property.PropertyID WHERE CMS.Cases.CaseOwnerId = {TenantId}";
                _dbContext.Database.OpenConnection();

                using (var result = command.ExecuteReader())
                {
                    while (result.Read())
                    {
                        int caseOwnerId = 0;

                        if (result["CaseOwnerId"] != null)
                        {
                            caseOwnerId = (int)result["CaseOwnerId"];
                        }
                        var serviceVm = new CaseServiceVm()
                        {
                            BuildingNumber = result["BuildingNumber"].ToString(),
                            BuildingRefNo = result["BuildingRefNo"].ToString(),
                            CaseNo = result["CaseNo"].ToString(),
                            CaseOwnerId = caseOwnerId,
                            Dated = result["Dated"].ToString(),
                            Description = result["Description"].ToString(),
                            Email = result["Email"].ToString(),
                            EmirateId = result["EmirateId"].ToString(),
                            MobileTel = result["MobileTel"].ToString(),
                            PropertyName = result["PropertyName"].ToString(),
                            ProposedRemedy = result["ProposedRemedy"].ToString(),
                            ReasonForVocation = result["ReasonForVocation"].ToString(),
                            ResidenceTel = result["ResidenceTel"].ToString(),
                            WorkNo = result["WorkNo"].ToString()
                        };
                        services.Add(serviceVm);
                    }
                }
            }
            return services;
        }

        public List<NotificationVm> GetNotifications(string search, string status, int UserId, int pageNo = 1, int pageSize = 100)
        {
            //28 Aug
            var sqlParams = new List<SqlParameter>();
            var page = pageSize * (pageNo - 1);
            int? IsRead = null;

            //var query = $@"Select n.NotificationId,SUBSTRING(n.Description, 1, 100) Description, n.Title,TitleAR,DescriptionAR, Format(n.CreatedOn, 'dd-MM-yyyy hh:mm tt') as CreatedOn, n.IsRead,Format(n.ReadOn, 'dd-MM-yyyy hh:mm tt') as ReadOn, n.Sent, Format(n.SentOn, 'dd-MM-yyyy hh:mm tt') as SentOn, n.UserId, u.FullName as UserName, u.Mobile, n.NotificationTypeId, nt.Title as NotificationType, COUNT(*) OVER() as TotalRecords, n.CurrentStatus, n.Batch From App.Notifications n Left Outer Join App.Users u ON u.UserId = n.UserId  Left Outer Join App.NotificationTypes nt ON nt.NotificationTypeId = n.NotificationTypeId Where n.CurrentStatus != 'Queue' and n.UserId = {UserId} And UserType = 'Tenant' and n.NotificationTypeId=1  ORDER BY NotificationId DESC Offset {page} Rows Fetch Next {pageSize} Rows only";
            //var keySqlParam = new SqlParameter("@key", DBNull.Value);
            //sqlParams.Add(keySqlParam);
            //if (search != null && !string.IsNullOrWhiteSpace(search))
            //{
            //    //query = $@"Select n.NotificationId,SUBSTRING(n.Description, 1, 100) Description, n.Title, n.TitleAR,n.DescriptionAR, Format(n.CreatedOn, 'dd-MM-yyyy hh:mm tt') as CreatedOn, n.IsRead,Format(n.ReadOn, 'dd-MM-yyyy hh:mm tt') as ReadOn, n.Sent, Format(n.SentOn, 'dd-MM-yyyy hh:mm tt') as SentOn, n.UserId, u.FullName as UserName, u.Mobile, n.NotificationTypeId, nt.Title as NotificationType, COUNT(*) OVER() as TotalRecords, n.CurrentStatus, n.Batch From App.Notifications n Left Outer Join App.Users u ON u.UserId = n.UserId  Left Outer Join App.NotificationTypes nt ON nt.NotificationTypeId = n.NotificationTypeId Where n.CurrentStatus != 'Queue' and n.UserId = {UserId} AND u.Mobile Like '%'+@Key+'%' OR u.FullName Like '%'+@Key+'%' OR n.Title like '%'+@Key+'%' And UserType = 'Tenant' and n.NotificationTypeId=1  ORDER BY NotificationId DESC Offset {page} Rows Fetch Next {pageSize} Rows only";
            //    keySqlParam = new SqlParameter("@key", search);
            //    sqlParams.Add(keySqlParam);
            //}

            if (status == "Read")
            {
                IsRead = 1;

                //query = $@"Select n.NotificationId,SUBSTRING(n.Description, 1, 100) Description, n.Title, n.TitleAR,n.DescriptionAR,Format(n.CreatedOn, 'dd-MM-yyyy hh:mm tt') as CreatedOn, n.IsRead,Format(n.ReadOn, 'dd-MM-yyyy hh:mm tt') as ReadOn, n.Sent, Format(n.SentOn, 'dd-MM-yyyy hh:mm tt') as SentOn, n.UserId, u.FullName as UserName, u.Mobile, n.NotificationTypeId, nt.Title as NotificationType, COUNT(*) OVER() as TotalRecords, n.CurrentStatus, n.Batch From App.Notifications n Left Outer Join App.Users u ON u.UserId = n.UserId  Left Outer Join App.NotificationTypes nt ON nt.NotificationTypeId = n.NotificationTypeId Where n.CurrentStatus != 'Queue' and n.UserId = {UserId} AND IsRead = 1 And UserType = 'Tenant' and n.NotificationTypeId=1  ORDER BY NotificationId DESC Offset {page} Rows Fetch Next {pageSize} Rows only";
            }
            else if (status == "Unread")
            {
                IsRead = 0;
                //query = $@"Select n.NotificationId,SUBSTRING(n.Description, 1, 100) Description, n.Title,n.TitleAR,n.DescriptionAR, Format(n.CreatedOn, 'dd-MM-yyyy hh:mm tt') as CreatedOn, n.IsRead,Format(n.ReadOn, 'dd-MM-yyyy hh:mm tt') as ReadOn, n.Sent, Format(n.SentOn, 'dd-MM-yyyy hh:mm tt') as SentOn, n.UserId, u.FullName as UserName, u.Mobile, n.NotificationTypeId, nt.Title as NotificationType, COUNT(*) OVER() as TotalRecords, n.CurrentStatus, n.Batch From App.Notifications n Left Outer Join App.Users u ON u.UserId = n.UserId  Left Outer Join App.NotificationTypes nt ON nt.NotificationTypeId = n.NotificationTypeId Where n.CurrentStatus != 'Queue' and n.UserId = {UserId} AND IsRead = 0 And UserType = 'Tenant' and n.NotificationTypeId=1  ORDER BY NotificationId DESC Offset {page} Rows Fetch Next {pageSize} Rows only";
            }

            else if (status == "Archived")
            {
                IsRead = 3;
                //query = $@"Select n.NotificationId,SUBSTRING(n.Description, 1, 100) Description, n.Title, n.TitleAR, n.DescriptionAR, Format(n.CreatedOn, 'dd-MM-yyyy hh:mm tt') as CreatedOn, n.IsRead,Format(n.ReadOn, 'dd-MM-yyyy hh:mm tt') as ReadOn, n.Sent, Format(n.SentOn, 'dd-MM-yyyy hh:mm tt') as SentOn, n.UserId, u.FullName as UserName, u.Mobile, n.NotificationTypeId, nt.Title as NotificationType, COUNT(*) OVER() as TotalRecords, n.CurrentStatus, n.Batch From App.Notifications n Left Outer Join App.Users u ON u.UserId = n.UserId  Left Outer Join App.NotificationTypes nt ON nt.NotificationTypeId = n.NotificationTypeId Where n.UserId = {UserId} AND CurrentStatus = 'Archived' And UserType = 'Tenant' and n.NotificationTypeId=1 ORDER BY NotificationId DESC Offset {page} Rows Fetch Next {pageSize} Rows only";
            }

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            SqlCommand cmd = new SqlCommand("FAB_PROC_GET_NOTIFICATION", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@page", page);
            cmd.Parameters.AddWithValue("@pageSize", pageSize);
            cmd.Parameters.AddWithValue("@Search", @search);
            cmd.Parameters.AddWithValue("@UserID", UserId);
            cmd.Parameters.AddWithValue("@IsRead", IsRead == null ? DBNull.Value : IsRead);
            cmd.Parameters.AddWithValue("@UserType", "tenant".ToLower());

            SqlDataAdapter adp = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            adp.Fill(ds);
            // var temp = Conn.ExecuteDataTableSqlDA(conn, query, sqlParams.ToArray());
            List<NotificationVm> notificationList = new List<NotificationVm>();
            if (ds.Tables[0].Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<NotificationVm>(ds.Tables[0]);
                notificationList = ListToBeSend;
            }
            return notificationList;
        }

        public NotificationVm GeNotificationDetails(int NotificationId, int UserId)
        {
            var notification = new NotificationVm();
            notification = _dbContext.Notifications.Where(p => p.NotificationId == NotificationId && p.UserId == UserId)
                .Select(p => new NotificationVm
                {
                    NotificationId = p.NotificationId,
                    CreatedOn = p.CreatedOn.ToString("dd-MM-yyyy hh:mm tt"),
                    CurrentStatus = p.CurrentStatus,
                    Description = p.Description,
                    Title = p.Title,
                    TitleAR = p.TitleAR,
                    DescriptionAR = p.DescriptionAR

                }).FirstOrDefault();
            return notification;
        }

        public List<AccessLogVm> GeAccessLogs(string search, int UserId, int pageNo = 1, int pageSize = 100)
        {
            var sqlParams = new List<SqlParameter>();
            var page = pageSize * (pageNo - 1);
            var query = $@"SELECT [UserAccessLogId], [UserId],Format([CreatedOn], 'dd MMM, yyyy hh:mm tt') as CreatedOn, [Details] FROM [App].[UserAccessLogs] Where UserId = {UserId} Order By UserAccessLogId DESC Offset {page} rows fetch next {pageSize} rows only";

            if (search != null && !string.IsNullOrWhiteSpace(search))
            {
                query = $@"SELECT [UserAccessLogId], [UserId], Format([CreatedOn], 'dd MMM, yyyy hh:mm tt') as CreatedOn, [Details] FROM [App].[UserAccessLogs] Where UserId = {UserId} AND Like '%'+@Search+'%' Order By UserAccessLogId DESC Offset {page} rows fetch next {pageSize} rows only";
                var searchParam = new SqlParameter("@Search", search);
                sqlParams.Add(searchParam);
            }

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var temp = Conn.ExecuteDataTableSqlDA(conn, query);
            List<AccessLogVm> accessLogs = new List<AccessLogVm>();
            if (temp.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<AccessLogVm>(temp);
                accessLogs = ListToBeSend;
            }
            return accessLogs;
        }

        public bool ReadNotification(int NotificationId, int? UserId)
        {
            var notification = _dbContext.Notifications.Where(p => p.NotificationId == NotificationId && p.UserId == UserId).FirstOrDefault();
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadOn = GetCurrentDateTime();
                notification.CurrentStatus = "Read";
                _dbContext.SaveChanges();
                return true;
            }
            return false;
        }
        public bool ArchivedNotification(int NotificationId, int? UserId)
        {

            var notification = _dbContext.Notifications.Where(p => p.NotificationId == NotificationId && p.UserId == UserId).FirstOrDefault();
            if (notification != null)
            {
                notification.CurrentStatus = "Archived";
                _dbContext.SaveChanges();
                return true;
            }
            return false;
        }
        public bool AddAccessLog(AddAccessLog addLog)
        {


            UserAccessLog userAccessLog = new UserAccessLog();
            userAccessLog.CreatedOn = GetCurrentDateTime();
            userAccessLog.Details = addLog.Details;
            userAccessLog.UserId = addLog.UserId;
            _dbContext.UserAccessLogs.Add(userAccessLog);
            try
            {
                _dbContext.SaveChanges();
                return true;

            }
            catch
            {

                return false;
            }
        }
        public TenantVm GetProfile(int TenantId)
        {
            var imageurl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}{_httpContextAccessor.HttpContext.Request.PathBase}" + "/tenant01/Images/displayimage.jpg";
            var query = $@"select  Concat(REPLACE(MobileCountryCode, '00', '+'),MobileAreaCode,Mobile) as Mobile, Concat(ISNULL(FirstName,''),' ',+ ISNULL(FullName,'')) as Name,Concat(ISNULL(FirstNameAR,''),' ',+ ISNULL(FullNameAR,'')) as NameAR , TenantId, Email, Nationality, Fax, Phone, Concat(Address1+', ',pe.EmirateName+', UAE') as Address, Concat(TenantAddressAR+', ',pe.EmirateNameAR) as AddressAR ,' ' as TermsAndConditions, '{imageurl}' as PhotoUrl from Pms_Tenant pt
                join Pms_Countries pc on pt.CountryID=pc.CountryID
                join Pms_Emirate pe on pt.EmirateId=pe.EmirateId
                where pt.TenantID={TenantId} and pt.Suspend=0";


            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var temp = Conn.ExecuteDataTableSqlDA(conn, query);
            TenantVm Tenent = new TenantVm();
            if (temp.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<TenantVm>(temp);
                Tenent = ListToBeSend.FirstOrDefault();
            }


            return Tenent;
        }

        public string SendPushNotification(int UserId, string Message, string ShortDescription, string Title, string TitleAr, string MessageAr, string UserType, int RecordId, string RecordType)
        {
            string ResponseMessage = "";
            if (UserId == 0)
            {
                ResponseMessage = "Invalid user device";
                return ResponseMessage;
            }
            var typeId = 1;
            var type = _dbContext.NotificationTypes.Where(p => p.Title.Contains("Push")).Select(p => p.NotificationTypeId).FirstOrDefault();
            if (type != 0)
            {
                typeId = type;
            }
            var timeZone = "Arabian Standard Time";
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            DateTime currentDate = DateTime.UtcNow.AddHours(4);//TimeZoneInfo.ConvertTime(DateTime.Now, tz);
            var usr = _dbContext.Users.Where(p => p.UserId == UserId).FirstOrDefault();

            if (usr != null)
            {
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);

                string Batch = "Push:" + currentDate.ToString("hhmm");

                var AddReplyQuery = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title,UserId,Description,Batch,CurrentStatus,CreatedOn,DeviceToken,UserType, RecordType,RecordId,TitleAr,DescriptionAr) VALUES (0,'{typeId}',0,@pTitle,{UserId},@pMessage,'{Batch}','Queue','{currentDate}',@DeviceToken,'{UserType}','{RecordType}',{RecordId},@pTitleAr,@pMessageAr);";
                var messageSqlParam = new SqlParameter("@pMessage", Message);
                if (string.IsNullOrWhiteSpace(Message))
                {
                    messageSqlParam = new SqlParameter("@pMessage", DBNull.Value);
                }
                var TitleSqlParam = new SqlParameter("@pTitle", Title);
                if (string.IsNullOrWhiteSpace(Title))
                {
                    TitleSqlParam = new SqlParameter("@pTitle", DBNull.Value);
                }
                var messageArSqlParam = new SqlParameter("@pMessageAr", MessageAr);
                if (string.IsNullOrWhiteSpace(Message))
                {
                    messageArSqlParam = new SqlParameter("@pMessagAre", DBNull.Value);
                }
                var TitleArSqlParam = new SqlParameter("@pTitleAr", TitleAr);
                if (string.IsNullOrWhiteSpace(Title))
                {
                    TitleArSqlParam = new SqlParameter("@pTitleAr", DBNull.Value);
                }
                var DeviceTokenSqlParam = new SqlParameter("@DeviceToken", usr.DeviceToken);
                if (string.IsNullOrWhiteSpace(usr.DeviceToken))
                {
                    DeviceTokenSqlParam = new SqlParameter("@DeviceToken", DBNull.Value);
                }
                var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery, messageSqlParam, TitleSqlParam, DeviceTokenSqlParam, TitleArSqlParam, messageArSqlParam);
                try
                {
                    _dbContext.SaveChanges();
                    ResponseMessage = "Notification sent successfully";
                    return ResponseMessage;
                }
                catch (Exception ex)
                {
                    ResponseMessage = ex.Message;
                    return ResponseMessage;
                }
            }
            else
            {
                return "User not Found";
            }

        }

        public string SendEmailNotification(int UserId, string Message, string Title, string TitleAr, string MessageAr, string UserType)
        {
            string ResponseMessage = "";
            if (UserId == 0)
            {
                ResponseMessage = "Invalid user device";
                return ResponseMessage;
            }
            var typeId = 2;
            var type = _dbContext.NotificationTypes.Where(p => p.Title.Contains("Email")).Select(p => p.NotificationTypeId).FirstOrDefault();
            if (type != 0)
            {
                typeId = type;
            }
            var timeZone = "Arabian Standard Time";
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            DateTime currentDate = TimeZoneInfo.ConvertTime(DateTime.Now, tz);
            var usr = _dbContext.Users.Where(p => p.UserId == UserId).FirstOrDefault();

            if (usr != null)
            {
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);

                string Batch = "Push:" + currentDate.ToString("hhmm");

                var AddReplyQuery = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title,UserId,Description,Batch,CurrentStatus,CreatedOn,Email,UserType,TitleAr,DescriptionAr) VALUES (0,'{typeId}',0,@pTitle,{UserId},@pMessage,'{Batch}','Queue','{currentDate}',@Email,'{UserType}',@pTitleAr,@pMessageAr);";
                var messageSqlParam = new SqlParameter("@pMessage", Message);
                if (string.IsNullOrWhiteSpace(Message))
                {
                    messageSqlParam = new SqlParameter("@pMessage", DBNull.Value);
                }
                var TitleSqlParam = new SqlParameter("@pTitle", Title);
                if (string.IsNullOrWhiteSpace(Title))
                {
                    TitleSqlParam = new SqlParameter("@pTitle", DBNull.Value);
                }
                //added 22-12-2022
                var messageArSqlParam = new SqlParameter("@pMessage", Message);
                if (string.IsNullOrWhiteSpace(Message))
                {
                    messageArSqlParam = new SqlParameter("@pMessage", DBNull.Value);
                }
                var TitleArSqlParam = new SqlParameter("@pTitle", Title);
                if (string.IsNullOrWhiteSpace(Title))
                {
                    TitleArSqlParam = new SqlParameter("@pTitle", DBNull.Value);
                }
                var EmailSqlParam = new SqlParameter("@Email", usr.Email);
                if (string.IsNullOrWhiteSpace(usr.Email))
                {
                    EmailSqlParam = new SqlParameter("@Email", DBNull.Value);
                }
                var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery, messageSqlParam, TitleSqlParam, EmailSqlParam, TitleArSqlParam, messageArSqlParam);
                try
                {
                    _dbContext.SaveChanges();
                    ResponseMessage = "Notification sent successfully";
                    return ResponseMessage;
                }
                catch (Exception ex)
                {
                    ResponseMessage = ex.Message;
                    return ResponseMessage;
                }
            }
            else
            {
                return "User not Found";
            }

        }

        public int CaseOwnerIdTransaction(SqlTransaction tran, SqlConnection con, int UserId, string UserName, string MobileNo)
        {
            CMSUserInfoVM cMSUserInfo = new CMSUserInfoVM();
            var CaseOwnerId = 0;

            var UserInfo = _dbContext.Users.Where(p => p.UserId == UserId).FirstOrDefault();
            if (UserInfo != null)
            {
                var CheckCMSCustomer = $@"select CustomerId, CustomerName  from cms.Customer where UserId={UserId}";
                //var phoneParam1 = new SqlParameter("@MobileNo", MobileNo);
                //if (string.IsNullOrWhiteSpace(MobileNo))
                //{
                //    phoneParam1 = new SqlParameter("@MobileNo", DBNull.Value);
                //}
                var RequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, CheckCMSCustomer);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CMSUserInfoVM>(RequestDetail);
                    cMSUserInfo = ListToBeSend.FirstOrDefault();
                    CaseOwnerId = cMSUserInfo.CustomerId;
                }
                else
                {
                    int? Cityid = 0;
                    int? EmirateId = 0;
                    InsertedPhotoVM selected = new InsertedPhotoVM();

                    UserInfo.FullName = UserInfo.FullName.Replace("'", "");
                    var AddCustomer = $@"INSERT INTO CMS.Customer(CustomerName,CustomerEmail,CustomerContact,CityId, EmirateId,UserId) VALUES (@FullName,@Email,@Mobile,{Cityid},{EmirateId},{UserId}) SELECT SCOPE_IDENTITY() as SelectedId";
                    var fullnameSqlParam = new SqlParameter("@FullName", UserInfo.FullName);


                    if (string.IsNullOrWhiteSpace(UserInfo.FullName))
                    {
                        fullnameSqlParam = new SqlParameter("@FullName", DBNull.Value);
                    }
                    var mobileSqlParam = new SqlParameter("@Mobile", UserInfo.Mobile);
                    if (string.IsNullOrWhiteSpace(UserInfo.Mobile))
                    {
                        mobileSqlParam = new SqlParameter("@Mobile", DBNull.Value);
                    }
                    var emailSqlParam = new SqlParameter("@Email", UserInfo.Email);
                    if (string.IsNullOrWhiteSpace(UserInfo.Email))
                    {
                        emailSqlParam = new SqlParameter("@Email", DBNull.Value);
                    }

                    SqlParameter[] parameter = new SqlParameter[]
                    {
                        new SqlParameter("@FullName", UserInfo.FullName),
                        new SqlParameter("@Mobile", UserInfo.Mobile),
                        new SqlParameter("@Email", UserInfo.Email),
                    };

                    var AddRequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, AddCustomer, parameter);
                    if (AddRequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<InsertedPhotoVM>(AddRequestDetail);
                        selected = ListToBeSend.FirstOrDefault();
                        CaseOwnerId = Convert.ToInt32(selected.SelectedId);
                    }
                }
            }

            return CaseOwnerId;
        }



        public int CaseOwnerId(int UserId, string UserName, string MobileNo)
        {
            CMSUserInfoVM cMSUserInfo = new CMSUserInfoVM();
            var CaseOwnerId = 0;
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var UserInfo = _dbContext.Users.Where(p => p.UserId == UserId).FirstOrDefault();
            if (UserInfo != null)
            {
                var CheckCMSCustomer = $@"select CustomerId, CustomerName  from cms.Customer where UserId={UserId}";
                //var phoneParam1 = new SqlParameter("@MobileNo", MobileNo);
                //if (string.IsNullOrWhiteSpace(MobileNo))
                //{
                //    phoneParam1 = new SqlParameter("@MobileNo", DBNull.Value);
                //}
                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, CheckCMSCustomer);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CMSUserInfoVM>(RequestDetail);
                    cMSUserInfo = ListToBeSend.FirstOrDefault();
                    CaseOwnerId = cMSUserInfo.CustomerId;
                }
                else
                {
                    int? Cityid = 0;
                    int? EmirateId = 0;
                    InsertedPhotoVM selected = new InsertedPhotoVM();

                    UserInfo.FullName = UserInfo.FullName.Replace("'", "");
                    var AddCustomer = $@"INSERT INTO CMS.Customer(CustomerName,CustomerEmail,CustomerContact,CityId, EmirateId,UserId) VALUES (@FullName,@Email,@Mobile,{Cityid},{EmirateId},{UserId}) SELECT SCOPE_IDENTITY() as SelectedId";
                    var fullnameSqlParam = new SqlParameter("@FullName", UserInfo.FullName);
                    if (string.IsNullOrWhiteSpace(UserInfo.FullName))
                    {
                        fullnameSqlParam = new SqlParameter("@FullName", DBNull.Value);
                    }
                    var mobileSqlParam = new SqlParameter("@Mobile", UserInfo.Mobile);
                    if (string.IsNullOrWhiteSpace(UserInfo.Mobile))
                    {
                        mobileSqlParam = new SqlParameter("@Mobile", DBNull.Value);
                    }
                    var emailSqlParam = new SqlParameter("@Email", UserInfo.Email);
                    if (string.IsNullOrWhiteSpace(UserInfo.Email))
                    {
                        emailSqlParam = new SqlParameter("@Email", DBNull.Value);
                    }
                    var AddRequestDetail = Conn.ExecuteDataTableSqlDA(conn, AddCustomer, fullnameSqlParam, mobileSqlParam, emailSqlParam);
                    if (AddRequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<InsertedPhotoVM>(AddRequestDetail);
                        selected = ListToBeSend.FirstOrDefault();
                        CaseOwnerId = Convert.ToInt32(selected.SelectedId);
                    }
                }
            }

            return CaseOwnerId;
        }

        //assign contractor on case creation

        public string AutoAssignContractorTransaction(SqlTransaction tran, SqlConnection con, int CaseNo, int UserId)
        {
            string ResponseMessage = "";
            if (CaseNo == 0)
            {
                ResponseMessage = "Contrator not found";
                return ResponseMessage;
            }
            CaseInfoVm caseInfo = new CaseInfoVm();
            GetContractorVm contractorVm = new GetContractorVm();

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var GetcaseInfo = $@" select PropertyId,csr.CaseSubCatagouryId CaseSubCatagouryId  from cms.Cases cca 
                                INNER join cms.CaseServiceRequest csr on cca.CaseNo=csr.CaseId
                                where cca.CaseNo={CaseNo}";

            var AddRequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, GetcaseInfo);
            if (AddRequestDetail.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseInfoVm>(AddRequestDetail);
                caseInfo = ListToBeSend.FirstOrDefault();
            }
            ContractCategoryVm ContractCategory = new ContractCategoryVm();
            var ContractCategoryQuery = $@"select ISNULL(ContractCategoryID,0) ContractCategoryID,ISNULL(ContractCategoryID2,0)ContractCategoryID2, ISNULL(ContractCategoryID3,0)ContractCategoryID3 from cms.CaseSubCategory where id={caseInfo.CaseSubCatagouryId}";

            string ContractCategoryID = "";
            var ContractCategoryQueryExecute = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, ContractCategoryQuery);
            if (ContractCategoryQueryExecute.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ContractCategoryVm>(ContractCategoryQueryExecute);
                ContractCategory = ListToBeSend.FirstOrDefault();
                ContractCategoryID = ContractCategory.ContractCategoryID + "," + ContractCategory.ContractCategoryID2 + "," + ContractCategory.ContractCategoryID3;
            }

            var GetContratorId = $@"--contractor
                SELECT top 1 ServiceContractProperty.PropertyID, ServiceContracts.ContractID as ServiceContractId,Contractors.Company, ServiceContracts.ContractorID, ServiceContracts.ContractStatusID, ServiceContracts.ContractCategoryID
              FROM   ServiceContractProperty 
			  inner JOIN ServiceContracts ON ServiceContractProperty.ServiceContractID = ServiceContracts.ContractID 
			   inner JOIN Contractors ON Contractors.ContractorID = ServiceContracts.ContractorID
             where ServiceContracts.ContractStatusID=3 and ServiceContractProperty.PropertyID={caseInfo.PropertyId}  and  ServiceContracts.ContractCategoryID in ({ContractCategoryID}) and ServiceContracts.Suspend=0  ORDER BY ServiceContracts.ContractID ";
            var GetRequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, GetContratorId);
            if (GetRequestDetail.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<GetContractorVm>(GetRequestDetail);
                contractorVm = ListToBeSend.FirstOrDefault();

                var updateCaseQuery = $@"update cms.Cases set ContractorId={contractorVm.ContractorID}  where CaseNo={CaseNo}";
                var UpdateRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, updateCaseQuery);

                // change status of case if contractor is assigned. 11 means assigned to contractor
                var UpdateStatus = $@"update cms.Cases set StatusId=11 where CaseNo={CaseNo}";
                var UpdateCaseStatus = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, UpdateStatus);

                try
                {
                    ResponseMessage = contractorVm.Company;
                    // Get Notification Content
                    var caseIn = GetCaseInfoTrnsaction(tran, con, CaseNo);
                    var TemplateId = 37;
                    if (TemplateId != 0)
                    {
                        //work
                        var GetAssignContent = GetNotificationTemplateTransaction(tran, con, TemplateId);
                        var ContractorUserId = GetContractorUserIdTRansaction(tran, con, contractorVm.ContractorID.Value);
                        //Generate ticket 

                        foreach (var item in ContractorUserId)
                        {
                            if (item != 0)
                            {
                                string AssignContractorSubject = GetAssignContent.Subject;
                                string AssignContractorMessage = GetAssignContent.BodyContent.Replace("{case}", CaseNo.ToString());
                                string ShortDescription = GetAssignContent.ShortDescription.Replace("{case}", CaseNo.ToString());
                                //added 22-12-2022 ar
                                string AssignContractorMessageAr = GetAssignContent.BodyContentAr.Replace("{case}", CaseNo.ToString());

                                var AssignContractorPushNotification = SendPushNotificationTransaction(tran, con, item, AssignContractorMessage, ShortDescription, AssignContractorSubject, GetAssignContent.SubjectAr, AssignContractorMessageAr, "Vendor", CaseNo, "case");
                                var AssignContractorEmailNotification = SendEmailNotificationTransaction(tran, con, item, AssignContractorMessage, AssignContractorSubject, GetAssignContent.SubjectAr, AssignContractorMessageAr, "Vendor");

                            }
                        }

                    }
                    return ResponseMessage;
                }
                catch (Exception ex)
                {
                    ResponseMessage = "Contrator not found";
                    return ResponseMessage;
                }
            }
            else
            {
                // committed FM assignee funtion and call the PM assignee function after discuss with Mir sb,
                // var UpdateAssignee = AssignFMAssigneeTransaction(tran, con, CaseNo);
                var UpdateAssignee = AssignPMAssineeeTransaction(tran, con, CaseNo,null);
                ResponseMessage = "Contrator not found";
                return ResponseMessage;
            }
        }




        public string SendPushNotificationTransaction(SqlTransaction tran, SqlConnection con, int UserId, string Message, string ShortDescription, string Title, string TitleAr, string MessageAr, string UserType, int RecordId, string RecordType)
        {
            string ResponseMessage = "";
            if (UserId == 0)
            {
                ResponseMessage = "Invalid user device";
                return ResponseMessage;
            }
            var typeId = 1;
            var type = _dbContext.NotificationTypes.Where(p => p.Title.Contains("Push")).Select(p => p.NotificationTypeId).FirstOrDefault();
            if (type != 0)
            {
                typeId = type;
            }
            var timeZone = "Arabian Standard Time";
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            DateTime currentDate = DateTime.UtcNow.AddHours(4);//TimeZoneInfo.ConvertTime(DateTime.Now, tz);
            var usr = _dbContext.Users.Where(p => p.UserId == UserId).FirstOrDefault();

            if (usr != null)
            {
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);

                string Batch = "Push:" + currentDate.ToString("hhmm");

                var AddReplyQuery = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title,UserId,Description,Batch,CurrentStatus,CreatedOn,DeviceToken,UserType, RecordType,RecordId,TitleAr,DescriptionAr) VALUES (0,'{typeId}',0,@pTitle,{UserId},@pMessage,'{Batch}','Queue','{currentDate}',@DeviceToken,'{UserType}','{RecordType}',{RecordId},@pTitleAr,@pMessageAr);";
                var messageSqlParam = new SqlParameter("@pMessage", Message);
                if (string.IsNullOrWhiteSpace(Message))
                {
                    messageSqlParam = new SqlParameter("@pMessage", DBNull.Value);
                }
                var TitleSqlParam = new SqlParameter("@pTitle", Title);
                if (string.IsNullOrWhiteSpace(Title))
                {
                    TitleSqlParam = new SqlParameter("@pTitle", DBNull.Value);
                }
                var messageArSqlParam = new SqlParameter("@pMessageAr", MessageAr);
                if (string.IsNullOrWhiteSpace(Message))
                {
                    messageArSqlParam = new SqlParameter("@pMessagAre", DBNull.Value);
                }
                var TitleArSqlParam = new SqlParameter("@pTitleAr", TitleAr);
                if (string.IsNullOrWhiteSpace(Title))
                {
                    TitleArSqlParam = new SqlParameter("@pTitleAr", DBNull.Value);
                }
                var DeviceTokenSqlParam = new SqlParameter("@DeviceToken", usr.DeviceToken);
                if (string.IsNullOrWhiteSpace(usr.DeviceToken))
                {
                    DeviceTokenSqlParam = new SqlParameter("@DeviceToken", DBNull.Value);
                }

                SqlParameter[] parameters = new SqlParameter[]
                {
                        new SqlParameter("@pMessage", Message),
                        new SqlParameter("@pMessageAr", MessageAr),
                        new SqlParameter("@pTitle", Title),
                        new SqlParameter("@pTitleAr", TitleAr),
                        new SqlParameter("@DeviceToken", usr.DeviceToken),
                };
                var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery, parameters);
                try
                {
                    _dbContext.SaveChanges();
                    ResponseMessage = "Notification sent successfully";
                    return ResponseMessage;
                }
                catch (Exception ex)
                {
                    ResponseMessage = ex.Message;
                    return ResponseMessage;
                }
            }
            else
            {
                return "User not Found";
            }

        }

        public string SendEmailNotificationTransaction(SqlTransaction tran, SqlConnection con, int UserId, string Message, string Title, string TitleAr, string MessageAr, string UserType)
        {
            string ResponseMessage = "";
            if (UserId == 0)
            {
                ResponseMessage = "Invalid user device";
                return ResponseMessage;
            }
            var typeId = 2;
            var type = _dbContext.NotificationTypes.Where(p => p.Title.Contains("Email")).Select(p => p.NotificationTypeId).FirstOrDefault();
            if (type != 0)
            {
                typeId = type;
            }
            var timeZone = "Arabian Standard Time";
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            DateTime currentDate = TimeZoneInfo.ConvertTime(DateTime.Now, tz);
            var usr = _dbContext.Users.Where(p => p.UserId == UserId).FirstOrDefault();

            if (usr != null)
            {
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);

                string Batch = "Push:" + currentDate.ToString("hhmm");

                var AddReplyQuery = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title,UserId,Description,Batch,CurrentStatus,CreatedOn,Email,UserType,TitleAr,DescriptionAr) VALUES (0,'{typeId}',0,@pTitle,{UserId},@pMessage,'{Batch}','Queue','{currentDate}',@Email,'{UserType}',@pTitleAr,@pMessageAr);";


                var messageSqlParam = new SqlParameter("@pMessage", Message);
                if (string.IsNullOrWhiteSpace(Message))
                {
                    messageSqlParam = new SqlParameter("@pMessage", DBNull.Value);
                }
                var TitleSqlParam = new SqlParameter("@pTitle", Title);
                if (string.IsNullOrWhiteSpace(Title))
                {
                    TitleSqlParam = new SqlParameter("@pTitle", DBNull.Value);
                }
                //added 22-12-2022
                var messageArSqlParam = new SqlParameter("@pMessage", Message);
                if (string.IsNullOrWhiteSpace(Message))
                {
                    messageArSqlParam = new SqlParameter("@pMessage", DBNull.Value);
                }
                var TitleArSqlParam = new SqlParameter("@pTitle", Title);
                if (string.IsNullOrWhiteSpace(Title))
                {
                    TitleArSqlParam = new SqlParameter("@pTitle", DBNull.Value);
                }
                var EmailSqlParam = new SqlParameter("@Email", usr.Email);
                if (string.IsNullOrWhiteSpace(usr.Email))
                {
                    EmailSqlParam = new SqlParameter("@Email", DBNull.Value);
                }

                SqlParameter[] sqlParametersname = new SqlParameter[]
                    {
                        new SqlParameter("@pMessage", Message),
                       new SqlParameter("@pTitle", Title),
                       new SqlParameter("@pMessage", Message),
                       new SqlParameter("@pTitle", Title),
                       new SqlParameter("@Email", usr.Email),

                 };




                var UpdateRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, AddReplyQuery, sqlParametersname);
                try
                {
                    _dbContext.SaveChanges();
                    ResponseMessage = "Notification sent successfully";
                    return ResponseMessage;
                }
                catch (Exception ex)
                {
                    ResponseMessage = ex.Message;
                    return ResponseMessage;
                }
            }
            else
            {
                return "User not Found";
            }

        }



        public List<int> GetContractorUserIdTRansaction(SqlTransaction tran, SqlConnection con, int ContractorId)
        {
            List<int> UserId = new List<int>();
            //05122022 commented becuase profileprentid ccontains contractor id
            // var query = $@"select UserId from app.UserRoles where ProfileParentId in (select ContactPersonID from ContactPerson where ContractorID={ContractorId})";

            var query = $@"select UserId from app.UserRoles where ProfileParentId = {ContractorId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ContractorUserIDVM>(Properties);
                UserId = ListToBeSend.Select(p => p.UserId).ToList();
            }
            return UserId;
        }

        public string AutoAssignContractor(int CaseNo, int UserId)
        {
            string ResponseMessage = "";
            if (CaseNo == 0)
            {
                ResponseMessage = "Contrator not found";
                return ResponseMessage;
            }
            CaseInfoVm caseInfo = new CaseInfoVm();
            GetContractorVm contractorVm = new GetContractorVm();

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var GetcaseInfo = $@" select PropertyId,csr.CaseSubCatagouryId CaseSubCatagouryId  from cms.Cases cca 
                                INNER join cms.CaseServiceRequest csr on cca.CaseNo=csr.CaseId
                                where cca.CaseNo={CaseNo}";

            var AddRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetcaseInfo);
            if (AddRequestDetail.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseInfoVm>(AddRequestDetail);
                caseInfo = ListToBeSend.FirstOrDefault();
            }
            ContractCategoryVm ContractCategory = new ContractCategoryVm();
            var ContractCategoryQuery = $@"select ISNULL(ContractCategoryID,0) ContractCategoryID,ISNULL(ContractCategoryID2,0)ContractCategoryID2, ISNULL(ContractCategoryID3,0)ContractCategoryID3 from cms.CaseSubCategory where id={caseInfo.CaseSubCatagouryId}";

            string ContractCategoryID = "";
            var ContractCategoryQueryExecute = Conn.ExecuteDataTableSqlDA(conn, ContractCategoryQuery);
            if (ContractCategoryQueryExecute.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ContractCategoryVm>(ContractCategoryQueryExecute);
                ContractCategory = ListToBeSend.FirstOrDefault();
                ContractCategoryID = ContractCategory.ContractCategoryID + "," + ContractCategory.ContractCategoryID2 + "," + ContractCategory.ContractCategoryID3;
            }

            var GetContratorId = $@"--contractor
                SELECT top 1 ServiceContractProperty.PropertyID, ServiceContracts.ContractID as ServiceContractId,Contractors.Company, ServiceContracts.ContractorID, ServiceContracts.ContractStatusID, ServiceContracts.ContractCategoryID
              FROM   ServiceContractProperty 
			  inner JOIN ServiceContracts ON ServiceContractProperty.ServiceContractID = ServiceContracts.ContractID 
			   inner JOIN Contractors ON Contractors.ContractorID = ServiceContracts.ContractorID
             where ServiceContracts.ContractStatusID=3 and ServiceContractProperty.PropertyID={caseInfo.PropertyId}  and  ServiceContracts.ContractCategoryID in ({ContractCategoryID}) and ServiceContracts.Suspend=0  ORDER BY ServiceContracts.ContractID ";
            var GetRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetContratorId);
            if (GetRequestDetail.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<GetContractorVm>(GetRequestDetail);
                contractorVm = ListToBeSend.FirstOrDefault();

                var updateCaseQuery = $@"update cms.Cases set ContractorId={contractorVm.ContractorID}  where CaseNo={CaseNo}";
                var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, updateCaseQuery);

                // change status of case if contractor is assigned. 11 means assigned to contractor
                var UpdateStatus = $@"update cms.Cases set StatusId=11 where CaseNo={CaseNo}";
                var UpdateCaseStatus = Conn.ExecuteDataTableSqlDA(conn, UpdateStatus);

                try
                {
                    ResponseMessage = contractorVm.Company;
                    // Get Notification Content
                    var caseIn = GetCaseInfo(CaseNo);
                    var TemplateId = 37;
                    if (TemplateId != 0)
                    {
                        var GetAssignContent = GetNotificationTemplate(TemplateId);
                        var ContractorUserId = GetContractorUserId(contractorVm.ContractorID.Value);
                        //Generate ticket 

                        foreach (var item in ContractorUserId)
                        {
                            if (item != 0)
                            {
                                string AssignContractorSubject = GetAssignContent.Subject;
                                string AssignContractorMessage = GetAssignContent.BodyContent.Replace("{case}", CaseNo.ToString());
                                string ShortDescription = GetAssignContent.ShortDescription.Replace("{case}", CaseNo.ToString());
                                //added 22-12-2022 ar
                                string AssignContractorMessageAr = GetAssignContent.BodyContentAr.Replace("{case}", CaseNo.ToString());

                                var AssignContractorPushNotification = SendPushNotification(item, AssignContractorMessage, ShortDescription, AssignContractorSubject, GetAssignContent.SubjectAr, AssignContractorMessageAr, "Vendor", CaseNo, "case");
                                var AssignContractorEmailNotification = SendEmailNotification(item, AssignContractorMessage, AssignContractorSubject, GetAssignContent.SubjectAr, AssignContractorMessageAr, "Vendor");

                            }
                        }

                    }
                    return ResponseMessage;
                }
                catch (Exception ex)
                {
                    ResponseMessage = "Contrator not found";
                    return ResponseMessage;
                }
            }
            else
            {
                var UpdateAssignee = AssignFMAssignee(CaseNo);
                ResponseMessage = "Contrator not found";
                return ResponseMessage;
            }
        }

        public List<DocumentTypeVm> GetDocumentType(string Type)
        {
            List<DocumentTypeVm> documentType = new List<DocumentTypeVm>();

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var GetcaseInfo = $@"--get document types
                                select dt.Name,dt.DocumentTypeId from cms.DocumentContentType dct
                                join cms.ContentType ct on  ct.ContentId=dct.ContentId
                                join cms.DocumentType dt on  dt.DocumentTypeId=dct.DocumentTypeId
                                where ct.name=@Type";
            var typeParam = new SqlParameter("@Type", Type);
            if (string.IsNullOrWhiteSpace(Type))
            {
                typeParam = new SqlParameter("@Type", DBNull.Value);
            }

            var AddRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetcaseInfo, typeParam);
            if (AddRequestDetail.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<DocumentTypeVm>(AddRequestDetail);
                documentType = ListToBeSend.ToList();
            }
            return documentType;
        }

        public string GetContractInfo(int ContractId)
        {
            string date = "";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var GetRewalInfo = $@"select FORMAT(ContractDate, 'dd-MM-yyyy') as ContractDate  from Pms_Contract where ContractID={ContractId}";

            var AddRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetRewalInfo);
            if (AddRequestDetail.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<GetContractInfoVm>(AddRequestDetail);
                date = ListToBeSend.Select(p => p.ContractDate).FirstOrDefault();
            }
            return date;
        }

        public OtherRenewalContractInfoVm GetRenewalContractInfo(int ContractId)
        {
            OtherRenewalContractInfoVm renewalContract = new OtherRenewalContractInfoVm();

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var GetRewalInfo = $@"select 
                                '' as fromDate,
                                ''as EndDate,
                                '' as AddNewDate, 
                                '' as EndNewDate from Pms_Contract";

            var AddRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetRewalInfo);
            if (AddRequestDetail.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<OtherRenewalContractInfoVm>(AddRequestDetail);
                renewalContract = ListToBeSend.FirstOrDefault();
            }
            return renewalContract;
        }

        public int CountUnreadNotifications(int UserId)
        {
            var query = $@"Select n.NotificationId,n.Description, n.Title, Format(n.CreatedOn, 'dd-MM-yyyy hh:mm tt') as CreatedOn, n.IsRead,Format(n.ReadOn, 'dd-MM-yyyy hh:mm tt') as ReadOn, n.Sent, Format(n.SentOn, 'dd-MM-yyyy hh:mm tt') as SentOn, n.UserId, u.FullName as UserName, u.Mobile, n.NotificationTypeId, nt.Title as NotificationType, COUNT(*) OVER() as TotalRecords, n.CurrentStatus, n.Batch From App.Notifications n Left Outer Join App.Users u ON u.UserId = n.UserId  Left Outer Join App.NotificationTypes nt ON nt.NotificationTypeId = n.NotificationTypeId Where n.UserId = {UserId} AND IsRead = 0 AND currentstatus = 'Sent' And UserType = 'Tenant' and nt.NotificationTypeId=1 ORDER BY NotificationId DESC";

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var temp = Conn.ExecuteDataTableSqlDA(conn, query);
            List<NotificationVm> notificationList = new List<NotificationVm>();
            if (temp.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<NotificationVm>(temp);
                notificationList = ListToBeSend;
            }
            return notificationList.Count();
        }


        public NotificationContentVm GetNotificationTemplateTransaction(SqlTransaction tran, SqlConnection con, int TemplateId)
        {

            var query = $@"select Subject,SubjectAr,ShortDescription, BodyContent, BodyContentAr  from cms.Template where id={TemplateId}";

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var temp = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);
            NotificationContentVm notificationContent = new NotificationContentVm();
            if (temp.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<NotificationContentVm>(temp);
                notificationContent = ListToBeSend.FirstOrDefault();
            }
            return notificationContent;
        }





        //added 22-12-2022 ar
        public NotificationContentVm GetNotificationTemplate(int TemplateId)
        {

            var query = $@"select Subject,SubjectAr,ShortDescription, BodyContent, BodyContentAr  from cms.Template where id={TemplateId}";

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var temp = Conn.ExecuteDataTableSqlDA(conn, query);
            NotificationContentVm notificationContent = new NotificationContentVm();
            if (temp.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<NotificationContentVm>(temp);
                notificationContent = ListToBeSend.FirstOrDefault();
            }
            return notificationContent;
        }


        public string AddTicketNewSerivceRequestCaseTransaction(SqlTransaction tran, SqlConnection con, int CaseNo, int? UserId, int CaseOwnerId)
        {


            // Get Notification Content
            var caseIn = GetCaseInfoTrnsaction(tran, con, CaseNo);

            //try
            //{
            var AddTicketQuery = $@"INSERT INTO CMS.Ticket(TicketNo,Subject, Message, PerorityId, LastUpdated, IsAnswered, UserId, StatusId, DateTime)VALUES ({CaseNo},'New Ticket Is Created','New service request is created',2, '{DateTime.UtcNow.AddHours(4)}',0,(select top 1 UserId from cms.Users),(select Id from cms.TicketStatus where Name='Open'), '{DateTime.UtcNow.AddHours(4)}');";
            var RequestDetail4 = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, AddTicketQuery);
            //}
            //catch (Exception ex)
            //{

            //    var looged = InsertErrorLog(ex.Message);
            //}

            return "OK";
        }


        public string AddTicketNewSerivceRequestCase(int CaseNo, int? UserId, int CaseOwnerId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            // Get Notification Content
            var caseIn = GetCaseInfo(CaseNo);

            try
            {
                var AddTicketQuery = $@"INSERT INTO CMS.Ticket(TicketNo,Subject, Message, PerorityId, LastUpdated, IsAnswered, UserId, StatusId, DateTime)VALUES ({CaseNo},'New Ticket Is Created','New service request is created',2, '{DateTime.UtcNow.AddHours(4)}',0,(select top 1 UserId from cms.Users),(select Id from cms.TicketStatus where Name='Open'), '{DateTime.UtcNow.AddHours(4)}');";
                var RequestDetail4 = Conn.ExecuteDataTableSqlDA(conn, AddTicketQuery);
            }
            catch (Exception ex)
            {

                var looged = InsertErrorLog(ex.Message);
            }

            return "OK";
        }

        public string NotificationServiceRequestAssignedToContractor(string Contractor, int? UserId, int CaseNo)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            // Get Notification Content
            var caseIn = GetCaseInfo(CaseNo);
            var TemplateId = GetNotificationTemplateId(caseIn.CaseCategouryId, caseIn.CaseTypeId, caseIn.EmirateId);
            if (TemplateId != null)
            {
                var GetNotificationContent = GetNotificationTemplate(TemplateId.Value);

                //Generate ticket 
                string Subject = GetNotificationContent.Subject;
                string Message = GetNotificationContent.BodyContent.Replace("{Name}", Contractor);
                string ShortDescription = GetNotificationContent.ShortDescription.Replace("{Name}", Contractor);

                //added 22-12-2022 ar
                string SubjectAr = GetNotificationContent.SubjectAr;
                string MessageAr = GetNotificationContent.BodyContentAr.Replace("{Name}", Contractor);

                var AddPushNotification = SendPushNotification(UserId.Value, Message, ShortDescription, Subject, SubjectAr, MessageAr, "Tenant", 0, "case");
                var AddEmailNotification = SendEmailNotification(UserId.Value, Message, Subject, SubjectAr, MessageAr, "Tenant");

            }
            return "OK";
        }

        public string NotificationServiceRequestResolved(int CaseNo, int? UserId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            // Get Notification Content
            var caseIn = GetCaseInfo(CaseNo);
            var TemplateId = GetNotificationTemplateId(caseIn.CaseCategouryId, caseIn.CaseTypeId, caseIn.EmirateId);
            if (TemplateId != null)
            {
                var GetNotificationContent = GetNotificationTemplate(TemplateId.Value);

                //Generate ticket 
                string Subject = GetNotificationContent.Subject;
                string Message = GetNotificationContent.BodyContent.Replace("{CaseNo}", CaseNo.ToString());
                string ShortDescription = GetNotificationContent.ShortDescription.Replace("{CaseNo}", CaseNo.ToString());
                //added 22-12-2022 ar
                string MessageAr = GetNotificationContent.BodyContentAr.Replace("{CaseNo}", CaseNo.ToString());


                var AddPushNotification = SendPushNotification(UserId.Value, Message, ShortDescription, Subject, GetNotificationContent.SubjectAr, MessageAr, "Tenant", CaseNo, "case");
                var AddEmailNotification = SendEmailNotification(UserId.Value, Message, Subject, GetNotificationContent.SubjectAr, MessageAr, "Tenant");

            }

            return "OK";
        }
        public string NotificationInvitetosharefeedback(int? UserId, int CaseNo)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            // Get Notification Content
            var caseIn = GetCaseInfo(CaseNo);
            var TemplateId = GetNotificationTemplateId(caseIn.CaseCategouryId, caseIn.CaseTypeId, caseIn.EmirateId);
            if (TemplateId != null)
            {
                var GetNotificationContent = GetNotificationTemplate(TemplateId.Value);

                //Generate ticket 
                string Subject = GetNotificationContent.Subject;
                string Message = GetNotificationContent.BodyContent;
                string ShortDescription = GetNotificationContent.ShortDescription;

                //added 22-12-2022 ar

                var AddPushNotification = SendPushNotification(UserId.Value, Message, ShortDescription, Subject, GetNotificationContent.SubjectAr, GetNotificationContent.BodyContentAr, "Tenant", 0, "case");
                var AddEmailNotification = SendEmailNotification(UserId.Value, Message, Subject, GetNotificationContent.SubjectAr, GetNotificationContent.BodyContentAr, "Tenant");

            }

            return "OK";
        }

        public string NotificationSharingfeedback(int? UserId, int CaseNo)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var caseIn = GetCaseInfo(CaseNo);
            var TemplateId = GetNotificationTemplateId(caseIn.CaseCategouryId, caseIn.CaseTypeId, caseIn.EmirateId);
            if (TemplateId != null)
            {
                // Get Notification Content
                var GetNotificationContent = GetNotificationTemplate(TemplateId.Value);

                //Generate ticket 
                string Subject = GetNotificationContent.Subject;
                string Message = GetNotificationContent.BodyContent;
                string ShortDescription = GetNotificationContent.ShortDescription;
                var AddPushNotification = SendPushNotification(UserId.Value, Message, ShortDescription, Subject, GetNotificationContent.SubjectAr, GetNotificationContent.BodyContentAr, "Tenant", 0, "case");
                var AddEmailNotification = SendEmailNotification(UserId.Value, Message, Subject, GetNotificationContent.SubjectAr, GetNotificationContent.BodyContentAr, "Tenant");
            }






            return "OK";
        }

        public string NotificationNewTicketAssigned(int? UserId, int caseNo)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var caseIn = GetCaseInfo(caseNo);
            var TemplateId = GetNotificationTemplateId(caseIn.CaseCategouryId, caseIn.CaseTypeId, caseIn.EmirateId);
            if (TemplateId != null)
            {
                // Get Notification Content
                var GetNotificationContent = GetNotificationTemplate(TemplateId.Value);

                //Generate ticket 
                string Subject = GetNotificationContent.Subject;
                string Message = GetNotificationContent.BodyContent.Replace("{case}", caseNo.ToString());
                string ShortDescription = GetNotificationContent.ShortDescription.Replace("{case}", caseNo.ToString());

                var AddPushNotification = SendPushNotification(UserId.Value, Message, ShortDescription, Subject, GetNotificationContent.SubjectAr, GetNotificationContent.BodyContentAr, "Tenant", caseNo, "case");
                var AddEmailNotification = SendEmailNotification(UserId.Value, Message, Subject, GetNotificationContent.SubjectAr, GetNotificationContent.BodyContentAr, "Tenant");

            }

            return "OK";
        }


        public bool CanCheckin(int ContractId)
        {
            int ContractPositionId = 0;
            int ChecKin = 0;
            int RenewalCase = 0;
            bool IscheckIn = false;
            //contract postition = 1
            var query = $@"select ContractPosition from Pms_Contract where ContractID={ContractId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var temp = Conn.ExecuteDataTableSqlDA(conn, query);
            CheckInContract checkInContract = new CheckInContract();
            if (temp.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CheckInContract>(temp);
                checkInContract = ListToBeSend.FirstOrDefault();
                ContractPositionId = 1;
            }

            //checkin case doesnt exist or case status is not cancelled 
            var caseInfo = $@"select CaseContractDetailId from cms.CaseContractDetail ccd
                            left join cms.CaseServiceRequest csr on  ccd.CaseNo=csr.CaseId
                            left join cms.cases cas on  cas.CaseNo=csr.CaseId
                            where ContractId={ContractId} and csr.CaseSubCatagouryId=317 and cas.StatusId not in (20)";
            var CheckincaseInfoQuery = Conn.ExecuteDataTableSqlDA(conn, caseInfo);
            CaseContractVm CaseContractVm = new CaseContractVm();
            if (CheckincaseInfoQuery.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseContractVm>(CheckincaseInfoQuery);
                CaseContractVm = ListToBeSend.FirstOrDefault();
                ChecKin = 1;// mean record exist
            }

            //contract renewal case doesnt exit
            var RenewalcaseInfoQuery = $@"select CaseContractDetailId from cms.CaseContractDetail ccd
                            left join cms.CaseServiceRequest csr on  ccd.CaseNo=csr.CaseId
                            left join cms.cases cas on  cas.CaseNo=csr.CaseId
                            where ContractId=107890 and csr.CaseSubCatagouryId=103 ";
            var caseInfoQuery = Conn.ExecuteDataTableSqlDA(conn, RenewalcaseInfoQuery);
            CaseContractVm RenewalVm = new CaseContractVm();
            if (caseInfoQuery.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseContractVm>(caseInfoQuery);
                CaseContractVm = ListToBeSend.FirstOrDefault();
                RenewalCase = 1;
            }

            //contract postition =1 & checkin case doesnt exist or case status is not cancelled & contract renewal case doesnt exit

            if (ContractPositionId == 1 && ChecKin == 0 && RenewalCase == 0)
            {
                IscheckIn = true;
            }
            return IscheckIn;
        }


        public int CanRewewContract(int ContractId)
        {
            int id = 0;
            return id;
        }

        public List<PaymentMethodVm> GetPaymentMethod()
        {
            List<PaymentMethodVm> Data = new List<PaymentMethodVm>();
            var ChargeQuery = $@"select PaymentMethodId,Name from cms.PaymentMethod";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery);

            if (Charges.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<PaymentMethodVm>(Charges);
                Data = ListToBeSend;
            }
            return Data;
        }

        public string UpdateContractCaseIdNew(int? DueActionid, int CaseNo, SqlTransaction tran, SqlConnection conn)
        {
            var ContractQuery = $@"update cms.ContractDueAction set CaseId={CaseNo} where DueActionid={DueActionid}";



            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, conn, ContractQuery);
            return "Ok";


        }

        public string UpdateContractCaseId(int? DueActionid, int CaseNo)
        {
            var ContractQuery = $@"update cms.ContractDueAction set CaseId={CaseNo} where DueActionid={DueActionid}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);
            return "Ok";
        }
        public string UpdateStageIdTransaction(int? DueActionid, int StageId, SqlTransaction tran, SqlConnection conn)
        {
            var ContractQuery = $@"update cms.ContractDueAction set StageId=(select StageId from cms.Stages where code={StageId}) where DueActionid={DueActionid}";

            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, conn, ContractQuery);

            var ContractStage = ContractStageInfoTransaction(DueActionid, tran, conn);

            InsertContractStageTransaction(tran, conn, StageId, DueActionid, ContractStage.UserId);

            //  string CMSLocationPath = new ConfigValues().GetCMSBaseURL() + "/Admin/Case/Edit/";


            if (StageId == 3)
            {
                var logged = InsertErrorLog("When Stage Id is 3 trying to send email");
                //try
                //{
                var CaseStageInfo = ContractStageInfoTransaction(DueActionid, tran, conn);
                var CaseInfo = GetCaseInfoTransaction(CaseStageInfo.CaseNo, tran, conn);
                InsertErrorLog("When Stage Id is 3 trying to send email after geting case info.");
                if (CaseInfo != null && CaseInfo.UserId != null)
                {
                    logged = InsertErrorLog("workgroup email before ");
                    var workgroupemail = GetWorkGroupEmailTransaction(CaseInfo.WorkGroupId, CaseInfo.EmirateId, tran, conn);
                    logged = InsertErrorLog("workgroup email: " + workgroupemail);
                    if (!string.IsNullOrWhiteSpace(workgroupemail))
                    {
                        //  string CMSurl = CMSLocationPath + CaseStageInfo.CaseNo;
                        //<a href={CMSurl}></a>
                        var body = $@"Dear Employee, <br/>
                                        Tenant with Case No:  {CaseStageInfo.CaseNo} , Assigned to: {CaseStageInfo.FullName}            has submitted the required documents.<br/>
                                        System generated email
                                        ";
                        var subject = "Documents Submitted By Tenant";
                        var title = "Documents Submitted By Tenant";
                        var sendemailnotification = AddNewEmailNotificationTransaction(tran, conn, body, CaseInfo.UserId.Value, subject, workgroupemail, title, CaseStageInfo.CaseNo);
                        logged = InsertErrorLog("When Stage Id is 3 email added to queue");
                    }
                }
                //}
                //catch (Exception ex)
                //{
                //    logged = InsertErrorLog(ex.Message);
                //}
            }
            return "Ok";
        }

        public string UpdateStageId(int? DueActionid, int StageId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var ContractQuery = $@"update cms.ContractDueAction set StageId=(select StageId from cms.Stages where code={StageId}) where DueActionid={DueActionid}";

            var Properties = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);

            var ContractStage = ContractStageInfo(DueActionid);

            InsertContractStage(StageId, DueActionid, ContractStage.UserId);

            //  string CMSLocationPath = new ConfigValues().GetCMSBaseURL() + "/Admin/Case/Edit/";


            if (StageId == 3)
            {
                var logged = InsertErrorLog("When Stage Id is 3 trying to send email");
                try
                {
                    var CaseStageInfo = ContractStageInfo(DueActionid);
                    var CaseInfo = GetCaseInfo(CaseStageInfo.CaseNo);
                    InsertErrorLog("When Stage Id is 3 trying to send email after geting case info.");
                    if (CaseInfo != null && CaseInfo.UserId != null)
                    {
                        logged = InsertErrorLog("workgroup email before ");
                        var workgroupemail = GetWorkGroupEmail(CaseInfo.WorkGroupId, CaseInfo.EmirateId);
                        logged = InsertErrorLog("workgroup email: " + workgroupemail);
                        if (!string.IsNullOrWhiteSpace(workgroupemail))
                        {
                            //  string CMSurl = CMSLocationPath + CaseStageInfo.CaseNo;
                            //<a href={CMSurl}></a>
                            var body = $@"Dear Employee, <br/>
                                        Tenant with Case No:  {CaseStageInfo.CaseNo} , Assigned to: {CaseStageInfo.FullName}            has submitted the required documents.<br/>
                                        System generated email
                                        ";
                            var subject = "Documents Submitted By Tenant";
                            var title = "Documents Submitted By Tenant";
                            var sendemailnotification = AddNewEmailNotification(body, CaseInfo.UserId.Value, subject, workgroupemail, title, CaseStageInfo.CaseNo);
                            logged = InsertErrorLog("When Stage Id is 3 email added to queue");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logged = InsertErrorLog(ex.Message);
                }
            }
            return "Ok";
        }

        public string InsertContractStageTransaction(SqlTransaction tran, SqlConnection con, int StageId, int? DueActionid, int UserId)
        {
            string description = "Stage updated";
            var datecurrent = DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt");
            var ContractQuery = $@"INSERT INTO CMS.CaseContractStages(UpdatedOn,UpdatedBy,StageId, DueActionId, Description)
                                    VALUES ('{datecurrent}',0,{StageId},{DueActionid}, '{description}');";

            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, ContractQuery);


            return "Ok";
        }

        public string InsertContractStage(int StageId, int? DueActionid, int UserId)
        {
            string description = "Stage updated";
            var datecurrent = DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt");
            var ContractQuery = $@"INSERT INTO CMS.CaseContractStages(UpdatedOn,UpdatedBy,StageId, DueActionId, Description)
                                    VALUES ('{datecurrent}',0,{StageId},{DueActionid}, '{description}');";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);


            return "Ok";
        }

        //Saving final payment
        public string RegisterContractPayment(List<NewPaymentDetailVm> modal)
        {
            string ResponseMessage = "";
            var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
            string Filename = null;
            var constring = _settings.GetConnectionString();
            //constring = constring.ToString();
            var conn = new SqlConnection(constring);
            conn.Open();
            var sqlCommand = new SqlCommand
            {
                Connection = conn
            };

            try
            {
                foreach (var item in modal)
                {
                    //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };//point no 10

                    if (item.Cheque != null)
                    {
                        var ext = Path.GetExtension(item.Cheque.FileName);

                        //28 Aug
                        var bytes = item.Cheque.GetBytes();
                        if (MimeTypes.IsValidMimeType(bytes, item.Cheque.ContentType, ext))
                        {
                            Filename = DateTime.Now.ToString("yyyyMMddHHmmssffff") + item.Cheque.FileName;
                            var path = GetFilePath + Filename;
                            using (var stream = new FileStream(path, FileMode.Create))
                            {
                                item.Cheque.CopyToAsync(stream);
                            }
                        }
                        else
                        {
                            return ResponseMessage = "Invalid File Type";
                        }

                    }
                    sqlCommand.CommandText = $"SELECT ISNULL(VATAmount,0) FROM Pms_ContractCharges WHERE ContractChargeId = {item.ContractchargeID}";

                    var isVATPayment = Convert.ToDecimal(sqlCommand.ExecuteScalar()) == item.Amount;
                    var col = isVATPayment ? "ContractchargeID" : "VATOnChargeID";


                    var AddReplyQuery = $@"

delete from cms.PaymentSetting where ContractId = {item.ContractId} and Title = @Title and ContractchargeID={item.ContractchargeID} and ContractPaymentID={item.PaymentID} and VatOnRentContractId={item.VatOnRentContractId} and VATOnChargeID={item.VATOnChargeID})

BEGIN
               IF NOT EXISTS (SELECT  ContractId FROM  cms.PaymentSetting  WHERE ContractId = {item.ContractId} and Title = @Title and ContractchargeID={item.ContractchargeID} and ContractPaymentID={item.PaymentID} and VatOnRentContractId={item.VatOnRentContractId} and VATOnChargeID={item.VATOnChargeID})
               BEGIN
                   INSERT INTO cms.PaymentSetting(Type,Title, ContractId,PaymentMethodId,AramexAddress,SelfDelivery, Description,Amount,ContractchargeID,ContractPaymentID,VatOnRentContractId,VATOnChargeID, Cheque) VALUES (@Type,@Title,{item.ContractId},{item.PaymentMethodId},@AramexAddress,@SelfDelivery,@Description,{item.Amount},{item.ContractchargeID},{item.PaymentID},{item.VatOnRentContractId},{item.VATOnChargeID},'{Filename}')
               END
            END";
                    //sqlCommand.CommandText = AddReplyQuery;
                    //sqlCommand.ExecuteNonQuery();
                    var titleParam = new SqlParameter("@Title", item.Title);
                    if (string.IsNullOrWhiteSpace(item.Title))
                    {
                        titleParam = new SqlParameter("@Title", DBNull.Value);
                    }
                    var descriptionParam = new SqlParameter("@Description", item.Description);
                    if (string.IsNullOrWhiteSpace(item.Description))
                    {
                        descriptionParam = new SqlParameter("@Description", DBNull.Value);
                    }
                    var TypeParam = new SqlParameter("@Type", item.Type);
                    if (string.IsNullOrWhiteSpace(item.Type))
                    {
                        TypeParam = new SqlParameter("@Type", DBNull.Value);
                    }
                    var AramexAddressParam = new SqlParameter(parameterName: "@AramexAddress", item.AramexAddress);
                    if (string.IsNullOrWhiteSpace(item.AramexAddress))
                    {
                        AramexAddressParam = new SqlParameter("@AramexAddress", DBNull.Value);
                    }
                    var SelfDeliveryParam = new SqlParameter(parameterName: "@SelfDelivery", item.SelfDelivery);
                    if (string.IsNullOrWhiteSpace(item.SelfDelivery))
                    {
                        SelfDeliveryParam = new SqlParameter("@SelfDelivery", DBNull.Value);
                    }
                    var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery, titleParam, descriptionParam, TypeParam, AramexAddressParam, SelfDeliveryParam);
                }

                ResponseMessage = "Added successfully";
            }
            catch (Exception ex)
            {
                ResponseMessage = ex.Message;
            }
            finally
            {
                conn.Close();
            }

            return ResponseMessage;
        }

        public int? GetNotificationTemplateId(int CategoryId, int typeid, int? emirateid)
        {
            int? UserTemplateId = null;
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var query = $@"select UserTemplateId from cms.NotificationTypeDetials where CategoryId='{CategoryId}' and  typeid='{typeid}' and  emirateid='{emirateid}'";
            var Properties = Conn.ExecuteDataTableSqlDA(conn, query);
            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<NotificationTemplate>(Properties);
                UserTemplateId = ListToBeSend.Select(p => p.UserTemplateId).FirstOrDefault();
            }
            return UserTemplateId;
        }


        public CaseINfoVm GetCaseInfoTransaction(int caseNo, SqlTransaction tran, SqlConnection con)
        {
            CaseINfoVm record = new CaseINfoVm();

            var query = $@"select CaseTypeId, sqr.CaseCategouryId,sqr.CaseSubCatagouryId,cas.EmirateId, Isnull(cas.UserId, 0) as WorkGroupId, cu.UserId as UserId, u.FullName   from cms.Cases cas
                           inner join cms.CaseServiceRequest sqr on sqr.CaseId=cas.CaseNo
						   inner join cms.Customer  cu on cu.CustomerId = cas.CaseOwnerId
                           left join cms.Users u on u.UserId = cas.AssignId
                           where CaseNo={caseNo}";

            InsertErrorLog("GetCaseInfo" + query);

            ///try
            //{
            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);
            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseINfoVm>(Properties);
                record = ListToBeSend.FirstOrDefault();
            }
            //}
            //catch (Exception ex)
            //{

            //var error = InsertErrorLog("GetCaseInfo Exception " + ex.Message);
            //}

            return record;
        }

        public CaseINfoVm GetCaseInfo(int caseNo)
        {
            CaseINfoVm record = new CaseINfoVm();
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var query = $@"select CaseTypeId, sqr.CaseCategouryId,cas.EmirateId, Isnull(cas.UserId, 0) as WorkGroupId, cu.UserId as UserId, u.FullName   from cms.Cases cas
                           inner join cms.CaseServiceRequest sqr on sqr.CaseId=cas.CaseNo
						   inner join cms.Customer  cu on cu.CustomerId = cas.CaseOwnerId
                           inner join cms.Users u on u.UserId = cas.AssignId
                           where CaseNo={caseNo}";

            InsertErrorLog("GetCaseInfo" + query);

            try
            {
                var Properties = Conn.ExecuteDataTableSqlDA(conn, query);
                if (Properties.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseINfoVm>(Properties);
                    record = ListToBeSend.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {

                var error = InsertErrorLog("GetCaseInfo Exception " + ex.Message);
            }

            return record;
        }

        public CaseInfoModel GetCaseInfoTrnsaction(SqlTransaction tran, SqlConnection con, int caseNo)
        {
            CaseInfoModel record = new CaseInfoModel();

            var query = $@"select CaseTypeId, sqr.CaseCategouryId,cas.EmirateId, Isnull(cas.UserId, 0) as WorkGroupId, cu.UserId as UserId, u.FullName   from cms.Cases cas
                           inner join cms.CaseServiceRequest sqr on sqr.CaseId=cas.CaseNo
						   inner join cms.Customer  cu on cu.CustomerId = cas.CaseOwnerId
                           inner join cms.Users u on u.UserId = cas.AssignId
                           where CaseNo={caseNo}";

            //InsertErrorLog("GetCaseInfoTrnsaction" + Properties.Rows.Count);
            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);
            InsertErrorLog("GetCaseInfoTrnsaction Count" + query);
            if (Properties.Rows.Count > 0)
            {
                //InsertErrorLog("GetCaseInfo1 Exists");
                var ListToBeSend = Conn.ToListof<CaseInfoModel>(Properties);
                record = ListToBeSend.FirstOrDefault();
                //InsertErrorLog("CaseCategouryId" + record.CaseCategouryId);
            }
            else
            {
                InsertErrorLog("GetCaseInfo1 Count" + Properties.Rows.Count);
            }
            return record;
        }


        public CaseInfoModel GetCaseInfo1(int caseNo)
        {
            CaseInfoModel record = new CaseInfoModel();
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var query = $@"select CaseTypeId, sqr.CaseCategouryId,cas.EmirateId, Isnull(cas.UserId, 0) as WorkGroupId, cu.UserId as UserId, u.FullName   from cms.Cases cas
                           inner join cms.CaseServiceRequest sqr on sqr.CaseId=cas.CaseNo
						   inner join cms.Customer  cu on cu.CustomerId = cas.CaseOwnerId
                           inner join cms.Users u on u.UserId = cas.AssignId
                           where CaseNo={caseNo}";

            //InsertErrorLog("GetCaseInfo" + query);

            try
            {
                var Properties = Conn.ExecuteDataTableSqlDA(conn, query);
                InsertErrorLog("GetCaseInfo1 Count" + Properties.Rows.Count);
                if (Properties.Rows.Count > 0)
                {
                    //InsertErrorLog("GetCaseInfo1 Exists");
                    var ListToBeSend = Conn.ToListof<CaseInfoModel>(Properties);
                    record = ListToBeSend.FirstOrDefault();
                    //InsertErrorLog("CaseCategouryId" + record.CaseCategouryId);
                }

            }
            catch (Exception ex)
            {

                var error = InsertErrorLog("GetCaseInfo Exception " + ex.Message);
            }

            return record;
        }


        public CaseDetailINfoVm GetCaseDetailInfoTransaction(SqlTransaction tran, SqlConnection con, int caseNo)
        {
            CaseDetailINfoVm record = new CaseDetailINfoVm();
            var query = $@"select cas.CaseTypeId, sqr.CaseCategouryId,cas.EmirateId, cas.UserId as WorkGroupId, cas.AssignId, u.FullName as Assignee, ug.GroupName as WorkGroup, ug.Email as WorkGroupEmail, cc.Name as CategoryName   from cms.Cases cas
                           inner join cms.CaseServiceRequest sqr on sqr.CaseId=cas.CaseNo
						   inner join cms.Users u on u.UserId = cas.AssignId
						   inner join cms.UserGroups ug on ug.UserGroupId = cas.UserId
						   inner join cms.CaseCategory cc on  cc.Id = sqr.CaseCategouryId
                           where CaseNo={caseNo}";
            try
            {
                var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);
                if (Properties.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseDetailINfoVm>(Properties);
                    record = ListToBeSend.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {

                var error = InsertErrorLog("GetCaseDetailInfo" + ex.Message);
                throw ex;
            }

            return record;
        }


        public int GetCaseNoPaymentSettingid(SqlTransaction t, SqlConnection con, int paymentsettingid)
        {
            CaseDetailINfoVm record = new CaseDetailINfoVm();
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var query = $@"Select top 1 isnull(cda.CaseId, 0) as CaseNo from CMS.PaymentSetting pst
             JOIN CMS.ContractDueAction cda on pst.ContractId = cda.RenewalContractid
            Where pst.PaymentSettingId = {paymentsettingid}";
            try
            {
                var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(t, con, query);
                if (Properties.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseDetailINfoVm>(Properties);
                    record = ListToBeSend.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {

                var error = InsertErrorLog("GetCaseDetailInfo" + ex.Message);
            }

            return record.CaseNo;



        }

        public int GetCaseNobyAttachmentid(int attachmentid)
        {
            CaseDetailINfoVm record = new CaseDetailINfoVm();
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var query = $@"Select UploadedCaseId AS CaseNo from CMS.Attachment Where AttachmentId={attachmentid}";
            try
            {
                var Properties = Conn.ExecuteDataTableSqlDA(conn, query);
                if (Properties.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseDetailINfoVm>(Properties);
                    record = ListToBeSend.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {

                var error = InsertErrorLog("GetCaseDetailInfo" + ex.Message);
            }

            return record.CaseNo;
        }



        public CaseDetailINfoVm GetCaseDetailInfo(int caseNo)
        {
            CaseDetailINfoVm record = new CaseDetailINfoVm();
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var query = $@"select cas.CaseNo,cas.CaseTypeId, sqr.CaseCategouryId,cas.EmirateId, cas.UserId as WorkGroupId, cas.AssignId, u.FullName as Assignee, ug.GroupName as WorkGroup, ug.Email as WorkGroupEmail, cc.Name as CategoryName from cms.Cases cas
                           inner join cms.CaseServiceRequest sqr on sqr.CaseId=cas.CaseNo
						   inner join cms.Users u on u.UserId = cas.AssignId
						   inner join cms.UserGroups ug on ug.UserGroupId = cas.UserId
						   inner join cms.CaseCategory cc on  cc.Id = sqr.CaseCategouryId
                           where CaseNo={caseNo}";
            try
            {
                var Properties = Conn.ExecuteDataTableSqlDA(conn, query);
                if (Properties.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseDetailINfoVm>(Properties);
                    record = ListToBeSend.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {

                var error = InsertErrorLog("GetCaseDetailInfo" + ex.Message);
            }

            return record;
        }
        //Adding payment settings 
        public string GetContractPayablePayments(int ContractId)
        {
            try
            {
                List<ContractPayableVM> contractPayables = new List<ContractPayableVM>();
                List<ContractPayableVM> Charges = new List<ContractPayableVM>();
                List<ContractPayableVM> VatOnRent = new List<ContractPayableVM>();
                List<ContractPayableVM> VatCharges = new List<ContractPayableVM>();


                VATOnRentVM vATOnRent = new VATOnRentVM();
                VatonRentVm VatonRent1 = new VatonRentVm();
                VatonRentVm VatonRent2 = new VatonRentVm();
                VatRecordVm VatRecord = new VatRecordVm();

                // this is the mendatory function this delete the confirm payment by contract id
                DeletePaymentRecord(ContractId);
                string DateFormat = _settings.DateFormat();

                var query = $@"SELECT PaymentID
	                                ,cp.ContractID
	                                ,FORMAT(cp.PaymentDate, 'dd-MM-yyyy') AS PaymentDate
	                                ,pc.Contractno
	                                ,ISNULL(cp.PaymentLeft,0) as PaymentValue
	                                ,ISNULL(cp.PaymentLeft,0) Balance
	                                ,cp.PaymentNo
	                                ,cp.PaidAmount
	                                ,pc.Installments
	                                ,'Rental' AS Type
                                    ,'تأجير' as TypeAR
                                FROM Pms_ContractPayments AS cp
                                LEFT JOIN Pms_Contract pc ON pc.ContractID = cp.ContractID
                                WHERE cp.Suspend = 0
	                                AND pc.Suspend = 0
	                                AND cp.ContractID = {ContractId}
									AND PaymentLeft > 0
	                                AND cp.PaymentID NOT IN (
		                                SELECT gpd.ContractPaymentID
		                                FROM cms.GatewayPaymant gp
		                                JOIN cms.GatewayPaymantDetail gpd ON gp.PaymentId = gpd.PaymentId
		                                WHERE gp.ContractId = {ContractId} AND 
										gpd.ContractPaymentID IS NOT NULL and  gp.responseCode in(0, 000, 200) and gp.Reconciled=0  
                                    )
                                order by cp.PaymentDate
		                     ";

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var dashboardData = Conn.ExecuteDataTableSqlDA(conn, query);
                if (dashboardData.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ContractPayableVM>(dashboardData);
                    contractPayables = ListToBeSend;
                }

                var ChargesQuery = $@"
                --Contract Charges
                SELECT ChargesTypeID
	                ,ContractID
	                ,ISNULL(PaymentValue,0) PaymentValue
	                ,Type
                    ,TypeAR
	                ,TxnPaymentAmount
	                ,ISNULL(ChequeAmount,0) ChequeAmount
	                ,ISNULL(PaymentValue - TxnPaymentAmount - ChequeAmount,0) AS Balance
	                ,ContractchargeID
                    ,ISNULL(TxnPaymentAmount + ChequeAmount,0)  as PaidAmount
                    ,PaymentDate
                FROM (
	                SELECT cc.ChargesTypeID
	                ,cc.ContractchargeID
		                ,cc.ContractID
		                ,ISNULL(cc.Amount,0) as PaymentValue
		                ,rop.PaymentFor as Type
                        ,rop.PaymentForAR as TypeAR
		                ,FORMAT(CreationDate, 'dd-MM-yyyy') AS PaymentDate
		                ,isnull((
				                SELECT SUM(PaymentAmount)
				                FROM pms_transactions
				                WHERE suspend = 0
					                AND STATUS = 'Confirmed'
					                AND ContractID = cc.ContractID --CONTRACT_ID
					                AND transactiontypeid = 1
					                AND paymentkind IN (1)
					                AND PaymentFor =  cc.ChargesTypeID
				                ), 0) AS TxnPaymentAmount
		                ,ISNULL((
				                SELECT SUM(sqta.ChequeAmount) AS Expr1
				                FROM Pms_Cheques AS sqta
				                INNER JOIN Pms_Transactions AS sqtb ON sqta.TransactionID = sqtb.TransactionID
				                WHERE (sqta.Suspend = 0)
					                AND (sqtb.Suspend = 0)
					                AND (
						                sqta.ChequeStatusID NOT IN (
							                5
							                ,6
							                ,7
							                ,11
							                ,12
							                )
						                )
					                AND (sqtb.ContractID = cc.ContractID)
					                AND (sqtb.STATUS = 'Confirmed')
					                AND (sqtb.TransactionTypeID = 1)
					                AND (sqtb.PaymentFor = cc.ChargesTypeID)
				                ), 0) AS ChequeAmount
	                FROM Pms_ContractCharges AS cc
	                INNER JOIN Pms_ReasonOfPayment AS rop ON cc.ChargesTypeID = rop.ReasonofPaymentID
	                left join Pms_Contract pc on pc.ContractID=cc.ContractID
	                WHERE (cc.Suspend = 0)
		                AND (rop.Suspend = 0) AND (cc.ContractID = {ContractId})
		                AND cc.ContractchargeID NOT IN (
		                                        SELECT gpd.ContractchargeID
		                                        FROM cms.GatewayPaymant gp
		                                        JOIN cms.GatewayPaymantDetail gpd ON gp.PaymentId = gpd.PaymentId
		                                        WHERE gp.ContractId = {ContractId}
			                                        AND gpd.ContractchargeID IS NOT NULL and  gp.responseCode in(0, 000, 200)
		                                        )
                and (ChargesTypeID * IsSDPaid <> 2 )
	                ) l

                WHERE PaymentValue - TxnPaymentAmount - ChequeAmount > 0

--select * from Pms_ContractCharges";

                var chargesData = Conn.ExecuteDataTableSqlDA(conn, ChargesQuery);
                if (chargesData.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ContractPayableVM>(chargesData);
                    Charges = ListToBeSend;
                }

                var VATOnRENTQuery = $@"
                                    --Total VAT on Rent
                                    select ISNULL(sum(VATCharges),0) VATOnRent from Pms_Contract where ContractID = {ContractId}";
                var VatOnRentData = Conn.ExecuteDataTableSqlDA(conn, VATOnRENTQuery);
                if (VatOnRentData.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<VATOnRentVM>(VatOnRentData);
                    vATOnRent = ListToBeSend.FirstOrDefault();
                }

                var RentReciveable1Query = $@"
                --Cash Payment for VAT on Rent
                SELECT ISNULL(SUM(PaymentAmount),0) as PaymentAmount
				FROM pms_transactions
				WHERE suspend = 0
					AND STATUS = 'Confirmed'
					AND ContractID = {ContractId} 
					AND transactiontypeid = 1
					AND paymentkind IN (1) --CASH
					AND PaymentFor = 42
					AND VATROPID=1";
                var RentReciveable1 = Conn.ExecuteDataTableSqlDA(conn, RentReciveable1Query);
                if (RentReciveable1.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<VatonRentVm>(RentReciveable1);
                    VatonRent1 = ListToBeSend.FirstOrDefault();
                }

                var RentReciveable2Query = $@"
                --Cheque Payment for VAT on Rent
                SELECT SUM(sqta.ChequeAmount) AS ChequeAmount
				FROM Pms_Cheques AS sqta
				INNER JOIN Pms_Transactions AS sqtb ON sqta.TransactionID = sqtb.TransactionID
				WHERE (sqta.Suspend = 0)
					AND (sqtb.Suspend = 0)
					AND (
						sqta.ChequeStatusID NOT IN (
							5
							,6
							,7
							,11
							,12
							)
						)
					AND (sqtb.ContractID ={ContractId})
					AND (sqtb.STATUS = 'Confirmed')
					AND (sqtb.TransactionTypeID = 1)
					AND (sqtb.PaymentFor = 42)
					AND VATROPID=1";

                var RentReciveable2 = Conn.ExecuteDataTableSqlDA(conn, RentReciveable2Query);
                if (RentReciveable2.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<VatonRentVm>(RentReciveable2);
                    VatonRent2 = ListToBeSend.FirstOrDefault();
                }
                var SumVatOnRent = vATOnRent.VATOnRent ?? 0 - (VatonRent1.PaymentAmount ?? 0 + VatonRent2.ChequeAmount ?? 0);
                var CHeckRentValueExistQuery = $@"select gpd.PaymentId from cms.GatewayPaymantDetail gpd
                                                  join cms.GatewayPaymant gp on gp.ContractId=gp.ContractId where                      gp.ContractId={ContractId} and gpd.VatOnRentContractId=                             {ContractId} and gp.ResponseCode in (0,000,200)";
                var CHeckRentValueExist = Conn.ExecuteDataTableSqlDA(conn, CHeckRentValueExistQuery);
                if (CHeckRentValueExist.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<VatRecordVm>(RentReciveable2);
                    VatRecord = ListToBeSend.FirstOrDefault();
                    SumVatOnRent = 0;
                }
                var VatChargesQuery = $@"SELECT ChargesTypeID
	            ,ContractID
                ,ContractchargeID
	            ,ISNULL(VATAmount,0) as PaymentValue
	            ,'VAT On ' + PaymentFor  As Type
                ,'ضريبة القيمة المضافة على ' + PaymentForAR  As TypeAR
	            ,TxnPaymentAmount
	            ,ISNULL(ChequeAmount,0) as ChequeAmount	
	            ,ISNULL(VATAmount - TxnPaymentAmount - ChequeAmount,0) AS Balance
                ,ISNULL(TxnPaymentAmount + ChequeAmount,0) AS PaidAmount
                ,PaymentDate
            FROM (
	            SELECT cc.ChargesTypeID
                ,ContractchargeID
		            ,cc.ContractID
		            ,ISNULL(cc.VATAmount,0) VATAmount
		            ,rop.PaymentFor
                    ,rop.PaymentForAR
                    ,FORMAT(CreationDate, 'dd-MM-yyyy') AS PaymentDate
		            ,isnull((
				            SELECT SUM(PaymentAmount)
				            FROM pms_transactions
				            WHERE suspend = 0
					            AND STATUS = 'Confirmed'
					            AND ContractID = cc.ContractID --CONTRACT_ID
					            AND transactiontypeid = 1
					            AND paymentkind IN (1)
					            AND PaymentFor = 42
					            AND VATROPID= cc.ChargesTypeID
				            ), 0) AS TxnPaymentAmount
		            ,ISNULL((
				            SELECT SUM(sqta.ChequeAmount) AS Expr1
				            FROM Pms_Cheques AS sqta
				            INNER JOIN Pms_Transactions AS sqtb ON sqta.TransactionID = sqtb.TransactionID
				            WHERE (sqta.Suspend = 0)
					            AND (sqtb.Suspend = 0)
					            AND (
						            sqta.ChequeStatusID NOT IN (
							            5
							            ,6
							            ,7
							            ,11
							            ,12
							            )
						            )
					            AND (sqtb.ContractID = cc.ContractID)
					            AND (sqtb.STATUS = 'Confirmed')
					            AND (sqtb.TransactionTypeID = 1)
					            AND PaymentFor = 42
					            AND VATROPID= cc.ChargesTypeID
				            ), 0) AS ChequeAmount
	            FROM Pms_ContractCharges AS cc
	            INNER JOIN Pms_ReasonOfPayment AS rop ON cc.ChargesTypeID = rop.ReasonofPaymentID
	            WHERE (cc.Suspend = 0)
		            AND (rop.Suspend = 0)
		            AND (cc.ContractID = {ContractId})
		            AND cc.ContractchargeID NOT IN (
		                                    SELECT gpd.VATOnChargeId
		                                    FROM cms.GatewayPaymant gp
		                                    JOIN cms.GatewayPaymantDetail gpd ON gp.PaymentId = gpd.PaymentId
		                                    WHERE gp.ContractId = {ContractId}
			                                AND gpd.VATOnChargeId IS NOT NULL and  gp.responseCode in(0, 000, 200)
		                                    )
	            ) l

                WHERE VATAmount - TxnPaymentAmount - ChequeAmount > 0";
                var VatChargesList = Conn.ExecuteDataTableSqlDA(conn, VatChargesQuery);
                if (VatChargesList.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ContractPayableVM>(VatChargesList);
                    VatCharges = ListToBeSend.ToList();
                }

                if (SumVatOnRent != 0)//changed from > 0
                {
                    ContractPayableVM contractPayableVM = new ContractPayableVM();
                    contractPayableVM.Type = "VAT on Rent.";
                    contractPayableVM.TypeAR = "ضريبة القيمة المضافة على الإيجار";
                    contractPayableVM.PaidAmount = SumVatOnRent;
                    contractPayableVM.PaymentValue = SumVatOnRent;
                    contractPayableVM.Balance = SumVatOnRent;
                    contractPayableVM.ContractchargeID = ContractId;
                    contractPayableVM.ContractID = ContractId;
                    contractPayableVM.PaymentDate = GetContractInfo(ContractId);
                    VatOnRent.Add(contractPayableVM);
                }

                if ((contractPayables != null && contractPayables.Count() > 0) || (Charges != null && Charges.Count > 0))
                {
                    // 1 is for contract payments

                    // CONTRACT paybale
                    conn.Open();
                    int acceptedpaymenttype = 0;
                    int defaultpaymentmethodtype = 0; //0 = No default selection, 1=Card, 2=cheque

                    foreach (var item in contractPayables)
                    {
                        var isallowed = IsCardAllowed((int)item.ContractID, 1, 0, conn);

                        if (isallowed == 1 && item.PaymentNo == "1")
                        {
                            acceptedpaymenttype = (int)AcceptPaymentType.CardPaymentCheque;
                            defaultpaymentmethodtype = 1;
                        }
                        else
                        {
                            acceptedpaymenttype = (int)AcceptPaymentType.Cheque;
                            defaultpaymentmethodtype = 2;//Cheque
                        }
                        //bank transfer disable
                        //var isallowed = IsCardAllowed((int)item.ContractID, 1, 0, conn);
                        //if (isallowed == 1)
                        //{
                        //    acceptedpaymenttype = (int)AcceptPaymentType.All;
                        //    defaultpaymentmethodtype = 1;
                        //}
                        //else
                        //{
                        //    acceptedpaymenttype = (int)AcceptPaymentType.BankTransferCheque;
                        //    defaultpaymentmethodtype = 3;
                        //}

                        AddREcord("Contract Payable", "Installment No: " + item.PaymentNo, item.PaymentNo + " رقم القسط:  ", item.ContractID, defaultpaymentmethodtype, "", "", item.Type, item.Balance, 0, item.PaymentID, 0, 0, item.PaymentDate, ReasonofPaymentId: 1, 0, false, acceptedpaymenttype);

                        //UpdatePaymentSettingAcceptType("Contract Payable", "Installment No: " + item.PaymentNo, item.ContractID, 1, "", "", item.Type, item.Balance, 0, item.PaymentID, 0, 0, item.PaymentDate);
                    }


                    //additionalCharges
                    foreach (var item in Charges)
                    {
                        var isallowed = IsCardAllowed((int)item.ContractID, item.ChargesTypeID.Value, 0, conn);

                        acceptedpaymenttype = (int)AcceptPaymentType.CardPayment;
                        defaultpaymentmethodtype = 1;


                        //diable bank transfer
                        //if (isallowed == 1)
                        //{
                        //    acceptedpaymenttype = (int)AcceptPaymentType.CardPaymentBankTransfer;
                        //    defaultpaymentmethodtype = 1;

                        //}
                        //else
                        //{
                        //    acceptedpaymenttype = (int)AcceptPaymentType.BankTranfser;
                        //    defaultpaymentmethodtype = 3;

                        //}

                        // pass accept  payment type must be integer
                        AddREcord("Additional Charges", item.Type, item.TypeAR, item.ContractID, defaultpaymentmethodtype, "", "", item.Type, item.Balance, item.ContractchargeID, 0, 0, 0, item.PaymentDate, item.ChargesTypeID.Value, 0, false, acceptedpaymenttype);

                        //UpdatePaymentSettingAcceptType("Additional Charges", item.Type, item.ContractID, 1, "", "", item.Type, item.Balance, item.ContractchargeID, 0, 0, 0, item.PaymentDate);
                    }
                    // VatOnRent
                    foreach (var item in VatOnRent)
                    {
                        var isallowed = IsCardAllowed((int)item.ContractID, 42, 1, conn);

                        acceptedpaymenttype = (int)AcceptPaymentType.CardPayment;
                        defaultpaymentmethodtype = 1;


                        //if (isallowed == 1)
                        //{
                        //     defaultpaymentmethodtype = 1;

                        //}
                        //else
                        //{
                        //    acceptedpaymenttype = (int)AcceptPaymentType.BankTranfser;
                        //    defaultpaymentmethodtype = 3;

                        //}
                        AddREcord("VAT On Rent", item.Type, item.TypeAR, item.ContractID, defaultpaymentmethodtype, "", "", item.Type, item.Balance, 0, 0, item.ContractID, 0, item.PaymentDate, 42, 1, false, acceptedpaymenttype);

                        //UpdatePaymentSetting("Vat On Rent", item.Type, item.ContractID, 1, "", "", item.Type, item.Balance, 0, 0, item.ContractID, 0, item.PaymentDate);
                    }
                    // VatOnCharges
                    foreach (var item in VatCharges)
                    {
                        var isallowed = IsCardAllowed((int)item.ContractID, 42, item.ChargesTypeID.Value, conn);
                        acceptedpaymenttype = (int)AcceptPaymentType.CardPayment;
                        defaultpaymentmethodtype = 1;


                        //if (isallowed == 1)
                        //{
                        //    acceptedpaymenttype = (int)AcceptPaymentType.CardPaymentBankTransfer;
                        //    defaultpaymentmethodtype = 1;

                        //}
                        //else
                        //{
                        //    acceptedpaymenttype = (int)AcceptPaymentType.BankTranfser;
                        //    defaultpaymentmethodtype = 3;
                        //}
                        AddREcord("VAT On Charges", item.Type, item.TypeAR, item.ContractID, defaultpaymentmethodtype, "", "", item.Type, item.Balance, 0, 0, 0, item.ContractchargeID, item.PaymentDate, ReasonofPaymentId: 42, item.ChargesTypeID.Value, false, acceptedpaymenttype);

                        //UpdatePaymentSetting("Vat On Charges", item.Type, item.ContractID, 1, "", "", item.Type, item.Balance, 0, 0, 0, item.ContractchargeID, item.PaymentDate);
                    }

                    conn.Close();

                    return "Ok";
                }
                else
                {
                    var dataError = new
                    {
                        statusCode = "404",
                        Status = "NotFound",
                        Message = "No Record Found.",
                        contractPayable = contractPayables,
                        additionalCharges = Charges
                    };
                    return "not";
                }
            }


            catch (Exception ex)
            {
                var data = new
                {
                    Status = "Processing Error",
                    StatusCode = "501",
                    Message = "Internal Server Error",

                };
                return "Not";
            }
        }


        //for checking card allowed or not
        public int IsCardAllowed(int ContractId, int RopId, int ChargesTypeId, SqlConnection con)
        {
            try
            {
                int isallowed = 0;
                SqlCommand cmd = new SqlCommand("pms_cardapproved", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@contractID", ContractId);
                cmd.Parameters.AddWithValue("@ropID", RopId);
                cmd.Parameters.AddWithValue("@vatonropID", ChargesTypeId);
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                adp.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<IsCardAllowedVM>(ds.Tables[0]);
                    isallowed = ListToBeSend.FirstOrDefault().CardApproved;
                }
                return isallowed;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        //For adding payment settings for first time
        public string AddREcord(string Type, string Title, string TitleAR, int? ContractId, int? PaymentMethodId, string AramexAddress, string SelfDelivery, string Description, decimal Amount, int? ContractchargeID, int? PaymentID, int? VatOnRentContractId, int? VATOnChargeID, string PaymentDate, int ReasonofPaymentId, int VatTypeId, bool IsRejected, int AcceptPaymentType)
        {
            var ChargeQuery = $@"

            BEGIN
               IF NOT EXISTS (SELECT  ContractId FROM  cms.PaymentSetting  WHERE ContractId = {ContractId} and Title='{Title}' and ContractchargeID={ContractchargeID} and ContractPaymentID={PaymentID} and VatOnRentContractId={VatOnRentContractId} and VATOnChargeID={VATOnChargeID})
               BEGIN
                   INSERT INTO cms.PaymentSetting(Type,Title,TitleAR, ContractId,PaymentMethodId,AramexAddress,SelfDelivery, Description,Amount,ContractchargeID,ContractPaymentID,VatOnRentContractId,VATOnChargeID, PaymentDate, ReasonofPaymentId,VatOnChargeTypeId,IsRejected,AcceptPaymentType) VALUES (@Type,@Title, @TitleAR,{ContractId},{PaymentMethodId},@AramexAddress,@SelfDelivery,@Description,{Amount},{ContractchargeID},{PaymentID},{VatOnRentContractId},{VATOnChargeID},'{PaymentDate}',{ReasonofPaymentId},{VatTypeId},0,{AcceptPaymentType})
               END
            END";
            var titleParam = new SqlParameter("@Title", Title);
            if (string.IsNullOrWhiteSpace(Title))
            {
                titleParam = new SqlParameter("@Title", DBNull.Value);
            }
            var titleArParam = new SqlParameter("@TitleAR", TitleAR);
            if (string.IsNullOrWhiteSpace(TitleAR))
            {
                titleArParam = new SqlParameter("@TitleAR", DBNull.Value);
            }
            var typeParam = new SqlParameter("@Type", Type);
            if (string.IsNullOrWhiteSpace(Type))
            {
                typeParam = new SqlParameter("@Type", DBNull.Value);
            }
            var AramexAddressParam = new SqlParameter("@AramexAddress", AramexAddress);
            if (string.IsNullOrWhiteSpace(AramexAddress))
            {
                AramexAddressParam = new SqlParameter("@AramexAddress", DBNull.Value);
            }
            var SelfDeliveryParam = new SqlParameter("@SelfDelivery", SelfDelivery);
            if (string.IsNullOrWhiteSpace(SelfDelivery))
            {
                SelfDeliveryParam = new SqlParameter("@SelfDelivery", DBNull.Value);
            }
            var DescriptionParam = new SqlParameter("@Description", Description);
            if (string.IsNullOrWhiteSpace(Description))
            {
                DescriptionParam = new SqlParameter("@Description", DBNull.Value);
            }
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery, titleParam, titleArParam, typeParam, AramexAddressParam, SelfDeliveryParam, DescriptionParam);


            return "ok";
        }

        public string UpdatePaymentSetting(string Type, string Title, int? ContractId, int? PaymentMethodId, string AramexAddress, string SelfDelivery, string Description, decimal Amount, int? ContractchargeID, int? PaymentID, int? VatOnRentContractId, int? VATOnChargeID, string PaymentDate)
        {
            var ChargeQuery = $@"update cms.PaymentSetting set Amount={Amount} where Title=@Title and Type=@Type and ContractId='{ContractId}' and ContractchargeID='{ContractchargeID}' and ContractPaymentID='{PaymentID}' and VatOnRentContractId='{VatOnRentContractId}' and VATOnChargeID='{VATOnChargeID}' and PaymentMethodId=1";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var titleParam = new SqlParameter("@Title", Title);
            if (string.IsNullOrWhiteSpace(Title))
            {
                titleParam = new SqlParameter("@Title", DBNull.Value);
            }

            var typeParam = new SqlParameter("@Type", Type);
            if (string.IsNullOrWhiteSpace(Type))
            {
                typeParam = new SqlParameter("@Type", DBNull.Value);
            }
            var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery, titleParam, typeParam);
            return "ok";
        }
        public string UpdatePaymentSettingAcceptType(string Type, string Title, int? ContractId, int? PaymentMethodId, string AramexAddress, string SelfDelivery, string Description, decimal Amount, int? ContractchargeID, int? PaymentID, int? VatOnRentContractId, int? VATOnChargeID, string PaymentDate)
        {
            var ChargeQuery = $@"UPDATE ps
                                SET ps.AcceptPaymentType = CASE 
		                                WHEN rop.PaymentTo = 2 -- Company Charges
			                                THEN 1 --Online Payment Only
		                                WHEN rop.PaymentTo = 3 -- Landlord
			                                THEN CASE 
					                                WHEN rop.CardType = 1 -- Restricted 
						                                THEN 2 --Cheque only
					                                WHEN rop.CardType = 2 -- unrestricted 
						                                THEN 0 -- both
					                                WHEN rop.CardType = 3 -- Based on landlord approval in property table
						                                THEN CASE 
								                                WHEN prp.ApprovedForCardPayment = 1 -- Approved
									                                THEN 0 -- both
								                                ELSE 2 -- only cheque
								                                END
					                                END
		                                ELSE 0
		                                END
                                FROM cms.PaymentSetting ps
                                LEFT JOIN Pms_ReasonOfPayment rop ON rop.ReasonofPaymentID = ps.ReasonofPaymentId
                                LEFT JOIN Pms_Contract con ON con.ContractID = ps.ContractId
                                LEFT JOIN Pms_Property prp ON prp.PropertyID = con.PropertyID
                                WHERE ps.Title = '{Title}'
	                                AND ps.Type = '{Type}'
	                                AND ps.ContractId = '{ContractId}'
	                                AND ps.ContractchargeID = '{ContractchargeID}'
	                                AND ps.ContractPaymentID = '{PaymentID}'
	                                AND ps.VatOnRentContractId = '{VatOnRentContractId}'
	                                AND ps.VATOnChargeID = '{VATOnChargeID}'
	                                AND ps.PaymentMethodId = {PaymentMethodId}";

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery);
            return "ok";
        }

        public CaseStageVm CaseStageInfo(int CaseId)
        {
            CaseStageVm Data = new CaseStageVm();

            var Query = $@"select DueActionid, contractid, CaseId, StageId from cms.ContractDueAction where CaseId={CaseId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, Query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseStageVm>(Properties);
                Data = ListToBeSend.FirstOrDefault();
            }
            return Data;


        }


        // get record by Due Action Id
        public ContractStageVm ContractStageInfoTransaction(int? DueActionid, SqlTransaction tran, SqlConnection conn)
        {
            ContractStageVm Data = new ContractStageVm();

            var Query = $@"select cus.UserId, CaseNo, MobileTel, sta.StageId,sta.Name as Stage, cas.CaseOwnerId ,u.FullName
                from cms.ContractDueAction  cda
                join cms.cases cas on cas.CaseNo =  cda.CaseId
                join cms.Customer cus on cus.CustomerId =  cas.CaseOwnerId
                join cms.Stages sta on sta.StageId =  cda.StageId
                join cms.Users u on u.UserId = cas.AssignId
                where cda.DueActionid={DueActionid}";

            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, conn, Query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ContractStageVm>(Properties);
                Data = ListToBeSend.FirstOrDefault();
            }
            return Data;


        }

        public ContractStageVm ContractStageInfo(int? DueActionid)
        {
            ContractStageVm Data = new ContractStageVm();

            var Query = $@"select cus.UserId, CaseNo, MobileTel, sta.StageId,sta.Name as Stage, cas.CaseOwnerId ,u.FullName
                from cms.ContractDueAction  cda
                join cms.cases cas on cas.CaseNo =  cda.CaseId
                join cms.Customer cus on cus.CustomerId =  cas.CaseOwnerId
                join cms.Stages sta on sta.StageId =  cda.StageId
                join cms.Users u on u.UserId = cas.AssignId
                where cda.DueActionid={DueActionid}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, Query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ContractStageVm>(Properties);
                Data = ListToBeSend.FirstOrDefault();
            }
            return Data;


        }
        public CaseContractStageVM GetContractcaseIInfo(int? ContractId)
        {
            CaseContractStageVM Data = new CaseContractStageVM();

            var Query = $@"select caseid, StageId, DueActionid from cms.ContractDueAction where RenewalContractid={ContractId} or Contractid={ContractId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, Query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseContractStageVM>(Properties);
                Data = ListToBeSend.FirstOrDefault();
            }
            return Data;


        }


        public int? GetWorkGroupIdTransaction(SqlTransaction tran, SqlConnection con, int? CategoryId,int? SubCategoryId, int typeid, int? emirateid,int? PropertyId, int? CaseNo)
        {
            int? WorkGroupId = null;
            //Committed old query and implemant store procedure with new logic
            //var constring = _settings.GetConnectionString();
            //constring = constring.ToString();
            //SqlConnection conn = new SqlConnection(constring);
            //var query = $@"--Case owner id 
            //                select OwnerId from cms.NotificationTypeDetials where CategoryId='{CategoryId}' and  typeid='{typeid}' and  emirateid='{emirateid}' and UserTemplateId is not null";

            //var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);
            //if (Properties.Rows.Count > 0)
            //{
            //    var ListToBeSend = Conn.ToListof<NotificationUserVm>(Properties);
            //    OwnerId = ListToBeSend.Select(p => p.OwnerId).FirstOrDefault();

            //}
            using (SqlCommand cmd = new SqlCommand("GetWorkGroupIdForCase", con, tran))
            {
                cmd.Parameters.AddWithValue("@CategoryId", CategoryId == null ? DBNull.Value : CategoryId);
                cmd.Parameters.AddWithValue("@SubCategory", SubCategoryId == null ? DBNull.Value : SubCategoryId);
                cmd.Parameters.AddWithValue("@CaseTypeId", typeid);
                cmd.Parameters.AddWithValue("@PropertyId", PropertyId == null ? DBNull.Value : PropertyId);
                cmd.Parameters.AddWithValue("@EmirateId", emirateid == null ? DBNull.Value : emirateid);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter oDataAdaptor = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                oDataAdaptor.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    WorkGroupId = string.IsNullOrEmpty(dt.Rows[0]["WorkGroupId"].ToString()) ? 0 : Convert.ToInt32(dt.Rows[0]["WorkGroupId"]);
                }
            }

            return WorkGroupId;
        }





        public int? UpdateCaseUserIdTransaction(SqlTransaction tran, SqlConnection con, int? CategoryId, int? SubCategoryId, int typeid, int? emirateid, int? PropertyId, int? CaseNo)
        {
            int? WorkGroupId = null;
            //var query = $@"--Case owner id 
            //                select top 1 OwnerId from cms.NotificationTypeDetials where CategoryId='{CategoryId}' and  typeid='{typeid}' and  emirateid='{emirateid}' and UserTemplateId is not null and UserTemplateId <> 0";

            //var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);
            //if (Properties.Rows.Count > 0)
            //{
            //    var ListToBeSend = Conn.ToListof<NotificationUserVm>(Properties);
            //    OwnerId = ListToBeSend.Select(p => p.OwnerId).FirstOrDefault();

            //    if (OwnerId != null && OwnerId.Value != 0)
            //    {
            using (SqlCommand cmd = new SqlCommand("GetWorkGroupIdForCase", con, tran))
            {
                cmd.Parameters.AddWithValue("@CategoryId", CategoryId == null ? DBNull.Value : CategoryId);
                cmd.Parameters.AddWithValue("@SubCategory", SubCategoryId == null ? DBNull.Value : SubCategoryId);
                cmd.Parameters.AddWithValue("@CaseTypeId", typeid);
                cmd.Parameters.AddWithValue("@PropertyId", PropertyId == null ? DBNull.Value : PropertyId);
                cmd.Parameters.AddWithValue("@EmirateId", emirateid == null ? DBNull.Value : emirateid);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter oDataAdaptor = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                oDataAdaptor.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    WorkGroupId = string.IsNullOrEmpty(dt.Rows[0]["WorkGroupId"].ToString()) ? 0 : Convert.ToInt32(dt.Rows[0]["WorkGroupId"]);
                }
            }

            if (WorkGroupId != 0)
            {

                var update = $@"--update workgroup id
                                            update cms.Cases set UserId='{WorkGroupId}' where CaseNo={CaseNo}";
                Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, update);

            }

            return WorkGroupId;
        }
        //public int? UpdateCaseUserIdTransaction(int? CategoryId, int typeid, int? emirateid, int? CaseNo, SqlTransaction tran, SqlConnection con)
        //{
        //    int? OwnerId = null;

        //    var query = $@"--Case owner id 
        //                    select top 1 OwnerId from cms.NotificationTypeDetials where CategoryId='{CategoryId}' and  typeid='{typeid}' and  emirateid='{emirateid}' and UserTemplateId is not null and UserTemplateId <> 0";

        //    var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);
        //    if (Properties.Rows.Count > 0)
        //    {
        //        var ListToBeSend = Conn.ToListof<NotificationUserVm>(Properties);
        //        OwnerId = ListToBeSend.Select(p => p.OwnerId).FirstOrDefault();
        //        InsertErrorLog("OwnerId1 " + OwnerId);
        //        if (OwnerId != null && OwnerId.Value != 0)
        //        {
        //            InsertErrorLog("Wor GroupId: " + OwnerId.Value);

        //            var update = $@"--update workgroup id
        //                                    update cms.Cases set UserId='{OwnerId.Value}' where CaseNo={CaseNo}";
        //            Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, update);
        //            InsertErrorLog("OwnerId updated " + OwnerId.Value);


        //        }
        //    }

        //    // var error = InsertErrorLog(ex.Message);

        //    return OwnerId;
        //}
        public int? UpdateCaseUserId(SqlConnection con,int? CategoryId, int? SubCategoryId, int typeid, int? emirateid, int? PropertyId, int? CaseNo)
        {
            int? WorkGroupId = null;
            using (SqlCommand cmd = new SqlCommand("GetWorkGroupIdForCase", con, null))
            {
                cmd.Parameters.AddWithValue("@CategoryId", CategoryId == null ? DBNull.Value : CategoryId);
                cmd.Parameters.AddWithValue("@SubCategory", SubCategoryId == null ? DBNull.Value : SubCategoryId);
                cmd.Parameters.AddWithValue("@CaseTypeId", typeid);
                cmd.Parameters.AddWithValue("@PropertyId", PropertyId == null ? DBNull.Value : PropertyId);
                cmd.Parameters.AddWithValue("@EmirateId", emirateid == null ? DBNull.Value : emirateid);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataAdapter oDataAdaptor = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                oDataAdaptor.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    WorkGroupId = string.IsNullOrEmpty(dt.Rows[0]["WorkGroupId"].ToString()) ? 0 : Convert.ToInt32(dt.Rows[0]["WorkGroupId"]);
                }
            }

            if (WorkGroupId != 0)
            {

                var update = $@"--update workgroup id
                                            update cms.Cases set UserId='{WorkGroupId}' where CaseNo={CaseNo}";
                Conn.ExecuteDataTableSqlDAWithTrnSaction(null, con, update);

            }

            return WorkGroupId;
        }
        //public int? UpdateCaseUserId(int? CategoryId, int typeid, int? emirateid, int? CaseNo)
        //{
        //    int? OwnerId = null;
        //    var constring = _settings.GetConnectionString();
        //    constring = constring.ToString();
        //    SqlConnection conn = new SqlConnection(constring);
        //    var query = $@"--Case owner id 
        //                    select top 1 OwnerId from cms.NotificationTypeDetials where CategoryId='{CategoryId}' and  typeid='{typeid}' and  emirateid='{emirateid}' and UserTemplateId is not null and UserTemplateId <> 0";
        //    try
        //    {
        //        var Properties = Conn.ExecuteDataTableSqlDA(conn, query);
        //        if (Properties.Rows.Count > 0)
        //        {
        //            var ListToBeSend = Conn.ToListof<NotificationUserVm>(Properties);
        //            OwnerId = ListToBeSend.Select(p => p.OwnerId).FirstOrDefault();
        //            InsertErrorLog("OwnerId1 " + OwnerId);
        //            if (OwnerId != null && OwnerId.Value != 0)
        //            {
        //                InsertErrorLog("Wor GroupId: " + OwnerId.Value);
        //                try
        //                {
        //                    var update = $@"--update workgroup id
        //                                    update cms.Cases set UserId='{OwnerId.Value}' where CaseNo={CaseNo}";
        //                    Properties = Conn.ExecuteDataTableSqlDA(conn, update);
        //                    InsertErrorLog("OwnerId updated " + OwnerId.Value);

        //                }
        //                catch (Exception ex)
        //                {

        //                    var error = InsertErrorLog(ex.Message);
        //                }
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        var error = InsertErrorLog(ex.Message);
        //    }

        //    return OwnerId;
        //}
        //public int? GetWorkGroupId(int? CategoryId, int typeid, int? emirateid, int? CaseNo)
        //{
        //    int? OwnerId = null;
        //    var constring = _settings.GetConnectionString();
        //    constring = constring.ToString();
        //    SqlConnection conn = new SqlConnection(constring);
        //    var query = $@"--Case owner id 
        //                    select OwnerId from cms.NotificationTypeDetials where CategoryId='{CategoryId}' and  typeid='{typeid}' and  emirateid='{emirateid}' and UserTemplateId is not null";
        //    try
        //    {
        //        var Properties = Conn.ExecuteDataTableSqlDA(conn, query);
        //        if (Properties.Rows.Count > 0)
        //        {
        //            var ListToBeSend = Conn.ToListof<NotificationUserVm>(Properties);
        //            OwnerId = ListToBeSend.Select(p => p.OwnerId).FirstOrDefault();

        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        var error = InsertErrorLog(ex.Message);
        //    }

        //    return OwnerId;
       // }

        public string AddErrorLog(string Description)
        {
            //var ChargeQuery = $@"insert into cms.ErrorLogs (Description, Date) values (@Description, GETDATE())";
            //var descriptionSqlParam = new SqlParameter("@Description", Description);
            //if (string.IsNullOrWhiteSpace(Description))
            //{
            //    descriptionSqlParam = new SqlParameter("@descriptionSqlParam", DBNull.Value);
            //}
            //var constring = _settings.GetConnectionString();
            //constring = constring.ToString();
            //SqlConnection conn = new SqlConnection(constring);
            //var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery, descriptionSqlParam);


            return "ok";
        }

        public string AddOtherNotificationNewSerivceRequestCase(int CaseNo, int? UserId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            // Get Notification Content
            var caseIn = GetCaseInfo(CaseNo);






            var TemplateId = GetNotificationTemplateId(caseIn.CaseCategouryId, caseIn.CaseTypeId, caseIn.EmirateId);
            if (TemplateId != null)
            {

                var GetNotificationContent = GetNotificationTemplate(TemplateId.Value);
                string Subject = GetNotificationContent.Subject.Replace("{CaseNo}", CaseNo.ToString());
                string Message = GetNotificationContent.BodyContent.Replace("{CaseNo}", CaseNo.ToString());
                string ShortDescription = GetNotificationContent.ShortDescription.Replace("{CaseNo}", CaseNo.ToString());
                //Generate ticket 
                //added 22/12/2022
                string SubjectAr = GetNotificationContent.SubjectAr.Replace("{CaseNo}", CaseNo.ToString());
                string MessageAr = GetNotificationContent.BodyContentAr.Replace("{CaseNo}", CaseNo.ToString());

                var AddPushNotification = SendPushNotification(UserId.Value, Message, ShortDescription, Subject, SubjectAr, MessageAr, "Tenant", CaseNo, "case");
                var AddEmailNotification = SendEmailNotification(UserId.Value, Message, Subject, SubjectAr, MessageAr, "Tenant");

            }

            return "OK";
        }

        public string DeletePaymentRecord(int ContractId)
        {
            //this function delete all the payments which are not confirmed, confirmation is done by user from app side
            var ChargeQuery = $@"delete from cms.PaymentSetting  where ContractId={ContractId} and (Confirmed is null or Confirmed=0) ";

            //AND cc.ContractchargeID NOT IN (
            //SELECT gpd.VATOnChargeId
            //FROM cms.GatewayPaymant gp
            //                    JOIN cms.GatewayPaymantDetail gpd ON gp.PaymentId = gpd.PaymentId
            //                    WHERE gp.ContractId = { ContractId}
            //AND gpd.VATOnChargeId IS NOT NULL and gp.responseCode in(0, 000, 200)
            //              )

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery);


            return "ok";
        }

        //Get the Unmarked CHeque payments
        public string UpdateChequePayments(int ContractId)
        {
            var record = new List<UpdateChequeVM>();
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            var conn = new SqlConnection(constring);
            var query = $@"
                SELECT ContractID
	                , PaymentFor AS ReasonOfPayment
	                , tra.PaymentKind AS ChequeCash
	                , ChequeAmount
	                , ChequeID
	                , chq.TransactionID
	                , tra.PaymentAmount
	                , VATROPID
                FROM Pms_Cheques chq
                INNER JOIN Pms_Transactions tra ON tra.TransactionID = chq.TransactionID
                WHERE ContractID = '{ContractId}'
	                AND chq.ChequeID NOT IN (
		                SELECT ChequeId
		                FROM cms.PaymentSetting ps
		                WHERE ContractId = '{ContractId}'
			                AND (
				                ps.paid = 0
				                OR ps.paid = null
				                )
		                )
	                AND tra.Suspend         = 0
	                AND chq.Suspend         = 0
	                AND TransactionTypeID   = 1
	                AND tra.[Status]        = 'Confirmed'
	                AND tra.PaymentKind     = 2
                ";

            var Properties = Conn.ExecuteDataTableSqlDA(conn, query);
            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<UpdateChequeVM>(Properties);
                record = ListToBeSend;
                // update matching record in payment setting
                foreach (var item in record)
                {

                    var updateQuery = $@"
                        --Contract app payments 
                        --update ONE row in the contract payment table matching with a cheque
                        UPDATE TOP (1) CMS.PaymentMethod 
                        SET Paid = 1, TransactionId = '{item.TransactionID}', ChequeId = '{item.ChequeID}' 
                        WHERE   Amount            = '{item.ChequeAmount}' 
                            AND ReasonofPaymentId = '{item.ReasonOfPayment}' 
                            AND PaymentMethodId   = 2 
                            AND VatOnChargeTypeId = '{item.VATROPID}' 
                            AND (Paid = 0 OR Paid IS NULL)";

                }
            }
            return "";
        }

        #endregion Basic



        public NotificationTemplateVm GetTemplateInfoTransaction(SqlTransaction tran, SqlConnection con, int TemplateId)
        {

            NotificationTemplateVm NotificationTemplate = new NotificationTemplateVm();
            var GetNotificationTemplate = $@"select Name, Subject,SubjectAr,BodyContent,BodyContentAR AS BodyContentAr, ShortDescription, Id from cms.Template where id={TemplateId}";
            var NotificationTemplateExecute = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, GetNotificationTemplate);

            if (NotificationTemplateExecute.Rows.Count > 0)
            {
                var BindNotificationData = Conn.ToListof<NotificationTemplateVm>(NotificationTemplateExecute);
                NotificationTemplate = BindNotificationData.FirstOrDefault();

            }
            return NotificationTemplate;
        }


        //added 22-12-2022 ar
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
        //added 26-12-2022 ar


        public CaseVm GetCaseInformationTRansaction(SqlTransaction tran, SqlConnection con, int CaseId)
        {
            //try
            //{
            CaseVm caseVm = new CaseVm();
            var query = $@"select PropertyId,EmirateId,cate.Name as CaseCategory,cate.NameAR as CaseCategoryAr,typ.Name as CaseType,typ.NameAR as CaseTypeAr,Scate.Name as CaseSubCategory,Scate.NameAr as CaseSubCategoryAr,cas.CaseNo, req.Description,cas.OtherContactPersonName, cas.OtherContactPersonMobile,status.Name as Status,status.NameAR as StatusAr, FORMAT(cas.Dated,'dd-MM-yyyy') as casedate  from cms.Cases cas
                join cms.CaseServiceRequest req on req.CaseId=cas.CaseNo
                join cms.CaseCategory cate on cate.Id=req.CaseCategouryId
                join cms.CaseTypes typ on typ.Id=cas.CaseTypeId
                join cms.CaseSubCategory Scate on Scate.Id=req.CaseSubCatagouryId
                join cms.CaseStatuses status on status.Id=cas.StatusId
                where CaseNo={CaseId}";

            //InsertErrorLog("GetCaseInformationkd" + query.ToString());


            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseVm>(Properties);
                caseVm = ListToBeSend.FirstOrDefault();
            }
            return caseVm;
            //}
            //catch (Exception ex)
            //{
            //    AddErrorLog(ex.Message + " " + ex.InnerException);
            //    throw ex.Message;
            //}
        }
        public CaseVm GetCaseInformation(int CaseId)
        {
            try
            {
                CaseVm caseVm = new CaseVm();
                var query = $@"select PropertyId,EmirateId,cate.Name as CaseCategory,cate.NameAR as CaseCategoryAr,typ.Name as CaseType,typ.NameAR as CaseTypeAr,Scate.Name as CaseSubCategory,Scate.NameAr as CaseSubCategoryAr,cas.CaseNo, req.Description,cas.OtherContactPersonName, cas.OtherContactPersonMobile,status.Name as Status,status.NameAR as StatusAr, FORMAT(cas.Dated,'dd-MM-yyyy') as casedate  from cms.Cases cas
                join cms.CaseServiceRequest req on req.CaseId=cas.CaseNo
                join cms.CaseCategory cate on cate.Id=req.CaseCategouryId
                join cms.CaseTypes typ on typ.Id=cas.CaseTypeId
                join cms.CaseSubCategory Scate on Scate.Id=req.CaseSubCatagouryId
                join cms.CaseStatuses status on status.Id=cas.StatusId
                where CaseNo={CaseId}";

                InsertErrorLog("GetCaseInformationkd" + query.ToString());

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Properties = Conn.ExecuteDataTableSqlDA(conn, query);

                if (Properties.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseVm>(Properties);
                    caseVm = ListToBeSend.FirstOrDefault();
                }
                return caseVm;
            }
            catch (Exception ex)
            {
                AddErrorLog(ex.Message + " " + ex.InnerException);
                throw;
            }
        }




        public NotificationTemplateVm ReplacementTrnsaction(SqlTransaction tran, SqlConnection con, NotificationTemplateVm model, int CaseId)
        {
            try
            {
                var CaseInfo = GetCaseInformationTRansaction(tran, con, CaseId);
                if (model.Subject != null)
                {
                    model.Subject = model.Subject.Replace("{CaseNo}", CaseInfo.CaseNo.ToString()).Replace("{CaseType}", CaseInfo.CaseType).Replace("{Description}", CaseInfo.CaseDescription).Replace("{CaseCategory}", CaseInfo.CaseCategory).Replace("{CaseSubCategory}", CaseInfo.CaseSubCategory).Replace("{ContactPersonName}", CaseInfo.OtherContactPersonName).Replace("{ContactPersonMobile}", CaseInfo.OtherContactPersonMobile).Replace("{Status}", CaseInfo.Status).Replace("{casedate}", CaseInfo.CaseDate);

                    model.SubjectAr = model.SubjectAr?.Replace("{CaseNo}", CaseInfo?.CaseNo.ToString()).Replace("{CaseType}", CaseInfo?.CaseTypeAr).Replace("{Description}", CaseInfo.CaseDescription).Replace("{CaseCategory}", CaseInfo?.CaseCategoryAr).Replace("{CaseSubCategory}", CaseInfo?.CaseSubCategoryAr).Replace("{ContactPersonName}", CaseInfo.OtherContactPersonName).Replace("{ContactPersonMobile}", CaseInfo.OtherContactPersonMobile).Replace("{Status}", CaseInfo?.StatusAr).Replace("{casedate}", CaseInfo.CaseDate);

                }
                if (model.BodyContent != null)
                {
                    model.BodyContent = model.BodyContent.Replace("{CaseNo}", CaseInfo.CaseNo.ToString()).Replace("{CaseType}", CaseInfo.CaseType).Replace("{Description}", CaseInfo.CaseDescription).Replace("{CaseCategory}", CaseInfo.CaseCategory).Replace("{CaseSubCategory}", CaseInfo.CaseSubCategory).Replace("{ContactPersonName}", CaseInfo.OtherContactPersonName).Replace("{ContactPersonMobile}", CaseInfo.OtherContactPersonMobile).Replace("{Status}", CaseInfo.Status).Replace("{casedate}", CaseInfo.CaseDate);
                    model.BodyContentAr = model.BodyContentAr?.Replace("{CaseNo}", CaseInfo.CaseNo.ToString()).Replace("{CaseType}", CaseInfo?.CaseTypeAr).Replace("{Description}", CaseInfo.CaseDescription).Replace("{CaseCategory}", CaseInfo?.CaseCategoryAr).Replace("{CaseSubCategory}", CaseInfo?.CaseSubCategoryAr).Replace("{ContactPersonName}", CaseInfo.OtherContactPersonName).Replace("{ContactPersonMobile}", CaseInfo.OtherContactPersonMobile).Replace("{Status}", CaseInfo?.StatusAr).Replace("{casedate}", CaseInfo.CaseDate);

                }
                if (model.ShortDescription != null)
                {
                    model.ShortDescription = model.ShortDescription.Replace("{CaseNo}", CaseInfo.CaseNo.ToString()).Replace("{CaseType}", CaseInfo.CaseType).Replace("{Description}", CaseInfo.CaseDescription).Replace("{CaseCategory}", CaseInfo.CaseCategory).Replace("{CaseSubCategory}", CaseInfo.CaseSubCategory).Replace("{ContactPersonName}", CaseInfo.OtherContactPersonName).Replace("{ContactPersonMobile}", CaseInfo.OtherContactPersonMobile).Replace("{Status}", CaseInfo.Status).Replace("{casedate}", CaseInfo.CaseDate);

                }

                return model;
            }
            catch (Exception ex)
            {
                AddErrorLog(ex.Message);
                AddErrorLog(ex.InnerException.ToString());
                throw;
            }
        }




        //added 26-12-2022 ar
        public NotificationTemplateVm Replacement(NotificationTemplateVm model, int CaseId)
        {
            try
            {
                var CaseInfo = GetCaseInformation(CaseId);
                if (model.Subject != null)
                {
                    model.Subject = model.Subject.Replace("{CaseNo}", CaseInfo.CaseNo.ToString()).Replace("{CaseType}", CaseInfo.CaseType).Replace("{Description}", CaseInfo.CaseDescription).Replace("{CaseCategory}", CaseInfo.CaseCategory).Replace("{CaseSubCategory}", CaseInfo.CaseSubCategory).Replace("{ContactPersonName}", CaseInfo.OtherContactPersonName).Replace("{ContactPersonMobile}", CaseInfo.OtherContactPersonMobile).Replace("{Status}", CaseInfo.Status).Replace("{casedate}", CaseInfo.CaseDate);

                    model.SubjectAr = model.SubjectAr?.Replace("{CaseNo}", CaseInfo?.CaseNo.ToString()).Replace("{CaseType}", CaseInfo?.CaseTypeAr).Replace("{Description}", CaseInfo.CaseDescription).Replace("{CaseCategory}", CaseInfo?.CaseCategoryAr).Replace("{CaseSubCategory}", CaseInfo?.CaseSubCategoryAr).Replace("{ContactPersonName}", CaseInfo.OtherContactPersonName).Replace("{ContactPersonMobile}", CaseInfo.OtherContactPersonMobile).Replace("{Status}", CaseInfo?.StatusAr).Replace("{casedate}", CaseInfo.CaseDate);

                }
                if (model.BodyContent != null)
                {
                    model.BodyContent = model.BodyContent.Replace("{CaseNo}", CaseInfo.CaseNo.ToString()).Replace("{CaseType}", CaseInfo.CaseType).Replace("{Description}", CaseInfo.CaseDescription).Replace("{CaseCategory}", CaseInfo.CaseCategory).Replace("{CaseSubCategory}", CaseInfo.CaseSubCategory).Replace("{ContactPersonName}", CaseInfo.OtherContactPersonName).Replace("{ContactPersonMobile}", CaseInfo.OtherContactPersonMobile).Replace("{Status}", CaseInfo.Status).Replace("{casedate}", CaseInfo.CaseDate);
                    model.BodyContentAr = model.BodyContentAr?.Replace("{CaseNo}", CaseInfo.CaseNo.ToString()).Replace("{CaseType}", CaseInfo?.CaseTypeAr).Replace("{Description}", CaseInfo.CaseDescription).Replace("{CaseCategory}", CaseInfo?.CaseCategoryAr).Replace("{CaseSubCategory}", CaseInfo?.CaseSubCategoryAr).Replace("{ContactPersonName}", CaseInfo.OtherContactPersonName).Replace("{ContactPersonMobile}", CaseInfo.OtherContactPersonMobile).Replace("{Status}", CaseInfo?.StatusAr).Replace("{casedate}", CaseInfo.CaseDate);

                }
                if (model.ShortDescription != null)
                {
                    model.ShortDescription = model.ShortDescription.Replace("{CaseNo}", CaseInfo.CaseNo.ToString()).Replace("{CaseType}", CaseInfo.CaseType).Replace("{Description}", CaseInfo.CaseDescription).Replace("{CaseCategory}", CaseInfo.CaseCategory).Replace("{CaseSubCategory}", CaseInfo.CaseSubCategory).Replace("{ContactPersonName}", CaseInfo.OtherContactPersonName).Replace("{ContactPersonMobile}", CaseInfo.OtherContactPersonMobile).Replace("{Status}", CaseInfo.Status).Replace("{casedate}", CaseInfo.CaseDate);

                }

                return model;
            }
            catch (Exception ex)
            {
                AddErrorLog(ex.Message);
                AddErrorLog(ex.InnerException.ToString());
                throw;
            }
        }


        public string prepareAndAddEmailNotificationTransaction(SqlTransaction tran, SqlConnection con, int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, string Email)
        {
            int TemplateId = 0;
            NotificationHookVm NotificationHook = new NotificationHookVm();

            // get User template Id based on parameter
            var GetTemplateId = $@"select Top 1 UserTemplateId from cms.NotificationTypeDetials where TypeId={CaseTypeId} and EmirateId={Emirate} and CategoryId={CaseCategory} and UserTemplateId !=0 and NotificationTypeId=(select Id from cms.NotificationTypes where Code={HookCode})";

            // get User template Id based on parameter
            if (HookCode == 20)
            {
                GetTemplateId = $@"select Top 1 UserTemplateId from cms.NotificationTypeDetials where EmirateId={Emirate} and UserTemplateId !=0 and NotificationTypeId=(select Id from cms.NotificationTypes where Code={HookCode})";

            }

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, GetTemplateId);


            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<NotificationHookVm>(Properties);
                NotificationHook = ListToBeSend.FirstOrDefault();
                TemplateId = NotificationHook.UserTemplateId;

                var TemplateInfo = GetTemplateInfoTransaction(tran, con, TemplateId);
                // Replace {} with Origional text
                var template = ReplacementTrnsaction(tran, con, TemplateInfo, CaseId);

                AddEmailNotificationTransaction(tran, con, Email, template.BodyContent, template.ShortDescription, template.Subject, TemplateInfo.SubjectAr, TemplateInfo.BodyContentAr, "Tenant", "Case", CaseId);

            }
            return "";
        }

        public string prepareAndAddEmailNotificationTransaction(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, string Email, SqlTransaction tran, SqlConnection conn)
        {
            int TemplateId = 0;
            NotificationHookVm NotificationHook = new NotificationHookVm();

            // get User template Id based on parameter
            var GetTemplateId = $@"select Top 1 UserTemplateId from cms.NotificationTypeDetials where TypeId={CaseTypeId} and EmirateId={Emirate} and CategoryId={CaseCategory} and UserTemplateId !=0 and NotificationTypeId=(select Id from cms.NotificationTypes where Code={HookCode})";

            // get User template Id based on parameter
            if (HookCode == 20)
            {
                GetTemplateId = $@"select Top 1 UserTemplateId from cms.NotificationTypeDetials where EmirateId={Emirate} and UserTemplateId !=0 and NotificationTypeId=(select Id from cms.NotificationTypes where Code={HookCode})";

            }
            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, conn, GetTemplateId);
            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<NotificationHookVm>(Properties);
                NotificationHook = ListToBeSend.FirstOrDefault();
                TemplateId = NotificationHook.UserTemplateId;

                var TemplateInfo = GetTemplateInfoTransaction(tran, conn, TemplateId);
                // Replace {} with Origional text
                var template = ReplacementTrnsaction(tran, conn, TemplateInfo, CaseId);

                AddEmailNotificationTransaction(tran, conn, Email, template.BodyContent, template.ShortDescription, template.Subject, TemplateInfo.SubjectAr, TemplateInfo.BodyContentAr, "Tenant", "Case", CaseId);

            }
            return "";
        }
        //This will prepareth notification and to notification table
        public string prepareAndAddEmailNotification(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, string Email)
        {
            int TemplateId = 0;
            NotificationHookVm NotificationHook = new NotificationHookVm();

            // get User template Id based on parameter
            var GetTemplateId = $@"select Top 1 UserTemplateId from cms.NotificationTypeDetials where TypeId={CaseTypeId} and EmirateId={Emirate} and CategoryId={CaseCategory} and UserTemplateId !=0 and NotificationTypeId=(select Id from cms.NotificationTypes where Code={HookCode})";

            // get User template Id based on parameter
            if (HookCode == 20)
            {
                GetTemplateId = $@"select Top 1 UserTemplateId from cms.NotificationTypeDetials where EmirateId={Emirate} and UserTemplateId !=0 and NotificationTypeId=(select Id from cms.NotificationTypes where Code={HookCode})";

            }

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

                AddEmailNotification(Email, template.BodyContent, template.ShortDescription, template.Subject, TemplateInfo.SubjectAr, TemplateInfo.BodyContentAr, "Tenant", "Case", CaseId);

            }
            return "";
        }

        public string AddEmailNotificationTransaction(SqlTransaction tran, SqlConnection con, string Email, string Message, string ShortDescription, string Title, string TitleAr, string MessageAr, string UserType, string RecordType, int RecordId)
        {
            // in  app.NotificationTypes  table  must have Push and Email Notification Type
            string ResponseMessage = "0";

            var typeId = 2;
            var timeZone = "Arabian Standard Time";
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            string currentDate = DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd hh:mm tt");
            string Batch = "Push:" + DateTime.UtcNow.AddHours(4).ToString("hhmm");
            InsertedRecordVM selected = new InsertedRecordVM();
            //added 22-12-2022
            var AddReplyQuery = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title,UserId,Description,Batch,CurrentStatus,CreatedOn,Email,UserType,Popup, RecordType, RecordId, ShortDescription,TitleAr,DescriptionAr) VALUES (0,'{typeId}',0,'{Title}',0,'{Message}','{Batch}','Queue','{currentDate}','{Email}','{UserType}',0, '{RecordType}',{RecordId},N'{ShortDescription}',N'{TitleAr}',N'{MessageAr}') SELECT SCOPE_IDENTITY() as InsertRecordId;";
            var UpdateRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, AddReplyQuery);
            if (UpdateRecord.Rows.Count > 0)
            {
                var InsertListToBeSend = Conn.ToListof<InsertedRecordVM>(UpdateRecord);
                selected = InsertListToBeSend.FirstOrDefault();
            }

            ResponseMessage = selected.InsertRecordId.ToString();
            return ResponseMessage;

        }

        //added 22-12-2022 ar
        public string AddEmailNotification(string Email, string Message, string ShortDescription, string Title, string TitleAr, string MessageAr, string UserType, string RecordType, int RecordId)
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
            //added 22-12-2022

            var AddReplyQuery = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title,UserId,Description,Batch,CurrentStatus,CreatedOn,Email,UserType,Popup, RecordType, RecordId, ShortDescription,TitleAr,DescriptionAr) VALUES (0,'{typeId}',0,'{Title}',0,'{Message}','{Batch}','Queue','{currentDate}','{Email}','{UserType}',0, '{RecordType}',{RecordId},N'{ShortDescription}',N'{TitleAr}',N'{MessageAr}') SELECT SCOPE_IDENTITY() as InsertRecordId;";
            var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery);
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

        public string CaseOwnerEmailTransaction(SqlTransaction tran, SqlConnection con, int? Id)
        {
            string Email = "";
            var query = $@"select top 1 EmailId from cms.Users where UserId={Id}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseOwnerVM>(Properties);
                Email = ListToBeSend.Select(p => p.EmailId).FirstOrDefault();
            }
            return Email;
        }


        public string CaseOwnerEmailTransaction(int? Id, SqlTransaction tran, SqlConnection conn)
        {
            string Email = "";
            var query = $@"select top 1 EmailId from cms.Users where UserId={Id}";

            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, conn, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseOwnerVM>(Properties);
                Email = ListToBeSend.Select(p => p.EmailId).FirstOrDefault();
            }
            return Email;
        }

        public string CaseOwnerEmail(int? Id)
        {
            string Email = "";
            var query = $@"select EmailId from cms.Users where UserId={Id}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseOwnerVM>(Properties);
                Email = ListToBeSend.Select(p => p.EmailId).FirstOrDefault();
            }
            return Email;
        }

        public string GetWorkGroupEmailTransaction(int? Id, int? EmirateId, SqlTransaction tran, SqlConnection conn)
        {
            string Email = "";
            var query = $@"select Email, Email as EmailId from cms.UserGroups where UserGroupId={Id} and EmirateId = {EmirateId}";

            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, conn, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseOwnerVM>(Properties);
                Email = ListToBeSend.Select(p => p.EmailId).FirstOrDefault();
            }
            return Email;
        }

        public string GetWorkGroupEmail(int? Id, int? EmirateId)
        {
            string Email = "";
            var query = $@"select Email, Email as EmailId from cms.UserGroups where UserGroupId={Id} and EmirateId = {EmirateId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseOwnerVM>(Properties);
                Email = ListToBeSend.Select(p => p.EmailId).FirstOrDefault();
            }
            return Email;
        }

        public string AddPushNotification(int UserId, string Message, string ShortDescription, string Title, string TitleAr, string MessageAr, string UserType, string RecordType, int RecordId)
        {
            string ResponseMessage = "0";

            var typeId = 1;
            var timeZone = "Arabian Standard Time";
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            string currentDate = DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd hh:mm tt");

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var Query2 = $@"select UserId, DeviceToken from app.Users where UserId={UserId}";
            var Properties2 = Conn.ExecuteDataTableSqlDA(conn, Query2);
            if (Properties2.Rows.Count > 0)
            {
                OTherUserInfovm oTherUserInfovm = new OTherUserInfovm();

                var ListToBeSend2 = Conn.ToListof<OTherUserInfovm>(Properties2);
                oTherUserInfovm = ListToBeSend2.FirstOrDefault();

                string Batch = "Push:" + DateTime.UtcNow.AddHours(4).ToString("hhmm");
                InsertedRecordVM selected = new InsertedRecordVM();
                var AddReplyQuery = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title,UserId,Description,Batch,CurrentStatus,CreatedOn,DeviceToken,UserType,Popup, RecordType, RecordId, ShortDescription,TitleAr,DescriptionAr) VALUES (0,'{typeId}',0,'{Title}',{UserId},'{Message}','{Batch}','Queue','{currentDate}','{oTherUserInfovm.DeviceToken}','{UserType}',1, '{RecordType}',{RecordId},N'{ShortDescription}',N'{TitleAr}',N'{MessageAr}') SELECT SCOPE_IDENTITY() as InsertRecordId;";
                var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery);

                InsertErrorLog("AddReplyQuery" + AddReplyQuery.ToString());
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


        public string AddPushNotificationTransaction(SqlTransaction tran, SqlConnection con, int UserId, string Message, string ShortDescription, string Title, string TitleAr, string MessageAr, string UserType, string RecordType, int RecordId)
        {
            string ResponseMessage = "0";

            var typeId = 1;
            var timeZone = "Arabian Standard Time";
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            string currentDate = DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd hh:mm tt");



            var Query2 = $@"select UserId, DeviceToken from app.Users where UserId={UserId}";
            var Properties2 = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, Query2);
            if (Properties2.Rows.Count > 0)
            {
                OTherUserInfovm oTherUserInfovm = new OTherUserInfovm();

                var ListToBeSend2 = Conn.ToListof<OTherUserInfovm>(Properties2);
                oTherUserInfovm = ListToBeSend2.FirstOrDefault();

                string Batch = "Push:" + DateTime.UtcNow.AddHours(4).ToString("hhmm");
                InsertedRecordVM selected = new InsertedRecordVM();
                var AddReplyQuery = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title,UserId,Description,Batch,CurrentStatus,CreatedOn,DeviceToken,UserType,Popup, RecordType, RecordId, ShortDescription,TitleAr,DescriptionAr) VALUES (0,'{typeId}',0,'{Title}',{UserId},'{Message}','{Batch}','Queue','{currentDate}','{oTherUserInfovm.DeviceToken}','{UserType}',1, '{RecordType}',{RecordId},N'{ShortDescription}',N'{TitleAr}',N'{MessageAr}') SELECT SCOPE_IDENTITY() as InsertRecordId;";
                var UpdateRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, AddReplyQuery);

                InsertErrorLog("AddReplyQuery" + AddReplyQuery.ToString());
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

        public string prepareAndAddPushNotificationTransaction(SqlTransaction tran, SqlConnection con, int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, int UserId, string Role)
        {
            InsertErrorLog("prepareAndAddPushNotification HookCode " + HookCode.ToString());

            int TemplateId = 0;
            NotificationHookVm NotificationHook = new NotificationHookVm();

            // get User template Id based on parameter
            var GetTemplateId = $@"select top 1 UserTemplateId from cms.NotificationTypeDetials where TypeId={CaseTypeId} and EmirateId='{Emirate}' and CategoryId={CaseCategory} and NotificationTypeId=(select top 1 Id from cms.NotificationTypes where Code={HookCode})";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, GetTemplateId);

            InsertErrorLog("prepareAndAddPushNotification HookCode Query " + GetTemplateId.ToString());


            if (Properties.Rows.Count > 0)
            {


                var ListToBeSend = Conn.ToListof<NotificationHookVm>(Properties);
                NotificationHook = ListToBeSend.FirstOrDefault();
                TemplateId = NotificationHook.UserTemplateId;

                var TemplateInfo = GetTemplateInfoTransaction(tran, con, TemplateId);
                // Replace {} with Origional text
                var template = ReplacementTrnsaction(tran, con, TemplateInfo, CaseId);

                if (TemplateId > 0)
                {

                    AddPushNotificationTransaction(tran, con, UserId, template.BodyContent, template.ShortDescription, template.Subject, template.SubjectAr, template.BodyContentAr, Role, "Case", CaseId);
                }
            }
            //else
            //{
            //    GetTemplateId = $@"select top 1 UserTemplateId from cms.NotificationTypeDetials";
            //    InsertErrorLog("prepareAndAddPushNotification GetTemplateId Query " + GetTemplateId.ToString());
            //    Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, GetTemplateId);
            //    if (Properties.Rows.Count > 0)
            //    {
            //        var ListToBeSend = Conn.ToListof<NotificationHookVm>(Properties);
            //        NotificationHook = ListToBeSend.FirstOrDefault();
            //        TemplateId = NotificationHook.UserTemplateId;

            //        var TemplateInfo = GetTemplateInfoTransaction(tran, con, TemplateId);
            //        var template = ReplacementTrnsaction(tran, con, TemplateInfo, CaseId);
            //        // InsertErrorLog("prepareAndAddPushNotification GetTemplateId Query " + GetTemplateId.ToString());
            //        AddPushNotificationTransaction(tran, con, UserId, template.BodyContent, template.ShortDescription, template.Subject, template.SubjectAr, template.BodyContentAr, Role, "Case", CaseId);
            //    }
            //}
            return "OK";
        }

        public string prepareAndAddPushNotificationTransaction(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, int UserId, string Role, SqlTransaction tran, SqlConnection conn)
        {
            InsertErrorLog("prepareAndAddPushNotification HookCode " + HookCode.ToString());

            int TemplateId = 0;
            NotificationHookVm NotificationHook = new NotificationHookVm();

            // get User template Id based on parameter
            var GetTemplateId = $@"select top 1 UserTemplateId from cms.NotificationTypeDetials where TypeId={CaseTypeId} and EmirateId='{Emirate}' and CategoryId={CaseCategory} and NotificationTypeId=(select top 1 Id from cms.NotificationTypes where Code={HookCode})";

            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, conn, GetTemplateId);

            InsertErrorLog("prepareAndAddPushNotification HookCode Query " + GetTemplateId.ToString());


            if (Properties.Rows.Count > 0)
            {


                var ListToBeSend = Conn.ToListof<NotificationHookVm>(Properties);
                NotificationHook = ListToBeSend.FirstOrDefault();
                TemplateId = NotificationHook.UserTemplateId;

                var TemplateInfo = GetTemplateInfoTransaction(tran, conn, TemplateId);
                // Replace {} with Origional text
                var template = ReplacementTrnsaction(tran, conn, TemplateInfo, CaseId);

                AddPushNotificationTransaction(tran, conn, UserId, template.BodyContent, template.ShortDescription, template.Subject, template.SubjectAr, template.BodyContentAr, Role, "Case", CaseId);
            }
            //else
            //{
            //    GetTemplateId = $@"select top 1 UserTemplateId from cms.NotificationTypeDetials";
            //    InsertErrorLog("prepareAndAddPushNotification GetTemplateId Query " + GetTemplateId.ToString());
            //    Properties = Conn.ExecuteDataTableSqlDA(conn, GetTemplateId);
            //    if (Properties.Rows.Count > 0)
            //    {
            //        var ListToBeSend = Conn.ToListof<NotificationHookVm>(Properties);
            //        NotificationHook = ListToBeSend.FirstOrDefault();
            //        TemplateId = NotificationHook.UserTemplateId;

            //        var TemplateInfo = GetTemplateInfo(TemplateId);
            //        var template = Replacement(TemplateInfo, CaseId);
            //        // InsertErrorLog("prepareAndAddPushNotification GetTemplateId Query " + GetTemplateId.ToString());
            //        AddPushNotification(UserId, template.BodyContent, template.ShortDescription, template.Subject, template.SubjectAr, template.BodyContentAr, Role, "Case", CaseId);
            //    }
            //}
            return "OK";
        }

        public string prepareAndAddPushNotification(int CaseTypeId, int CaseCategory, int? Emirate, int HookCode, int CaseId, int UserId, string Role)
        {
            InsertErrorLog("prepareAndAddPushNotification HookCode " + HookCode.ToString());

            int TemplateId = 0;
            NotificationHookVm NotificationHook = new NotificationHookVm();

            // get User template Id based on parameter
            var GetTemplateId = $@"select top 1 UserTemplateId from cms.NotificationTypeDetials where TypeId={CaseTypeId} and EmirateId='{Emirate}' and CategoryId={CaseCategory} and NotificationTypeId=(select top 1 Id from cms.NotificationTypes where Code={HookCode})";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, GetTemplateId);

            InsertErrorLog("prepareAndAddPushNotification HookCode Query " + GetTemplateId.ToString());


            if (Properties.Rows.Count > 0)
            {


                var ListToBeSend = Conn.ToListof<NotificationHookVm>(Properties);
                NotificationHook = ListToBeSend.FirstOrDefault();
                TemplateId = NotificationHook.UserTemplateId;

                var TemplateInfo = GetTemplateInfo(TemplateId);
                // Replace {} with Origional text
                var template = Replacement(TemplateInfo, CaseId);

                AddPushNotification(UserId, template.BodyContent, template.ShortDescription, template.Subject, template.SubjectAr, template.BodyContentAr, Role, "Case", CaseId);
            }
            //else
            //{
            //    GetTemplateId = $@"select top 1 UserTemplateId from cms.NotificationTypeDetials";
            //    InsertErrorLog("prepareAndAddPushNotification GetTemplateId Query " + GetTemplateId.ToString());
            //    Properties = Conn.ExecuteDataTableSqlDA(conn, GetTemplateId);
            //    if (Properties.Rows.Count > 0)
            //    {
            //        var ListToBeSend = Conn.ToListof<NotificationHookVm>(Properties);
            //        NotificationHook = ListToBeSend.FirstOrDefault();
            //        TemplateId = NotificationHook.UserTemplateId;

            //        var TemplateInfo = GetTemplateInfo(TemplateId);
            //        var template = Replacement(TemplateInfo, CaseId);
            //        // InsertErrorLog("prepareAndAddPushNotification GetTemplateId Query " + GetTemplateId.ToString());
            //        AddPushNotification(UserId, template.BodyContent, template.ShortDescription, template.Subject, template.SubjectAr, template.BodyContentAr, Role, "Case", CaseId);
            //    }
            //}
            return "OK";
        }
        
        public int ContractRenewalActions(int TenantId)
        {
            List<ContractRenewalActionVm> Data = new List<ContractRenewalActionVm>();
            string DateFormat = _settings.DateFormat();
            int TotalRecord = 0;
            var ChargeQuery = $@"select DueActionid,CaseId,ExpiringingDays,cda.StageId,sta.Name as StageName,con.Contractno as PreviousContractNo,
		                COALESCE (RenewalContractid,cda.Contractid) as 'Contractid', 
		                COALESCE (cont.Contractno,con.Contractno) as 'Contractno',
		                COALESCE (propt.PropertyName,prop.PropertyName) as 'PropertyName',
						COALESCE (propt.PropertyNameAr,prop.PropertyNameAr) as 'PropertyNameAr',
		                COALESCE (format(cont.fromDate,'dd-MM-yyyy'),format(con.fromDate,'dd-MM-yyyy')) as 'Fromdate',
		                COALESCE (format(cont.ToDate,'dd-MM-yyyy'),format(con.ToDate,'dd-MM-yyyy')) as 'ToDate',
						COALESCE (case WHEN ttt.ContractType='Posted' THEN 'Active' ELSE  ttt.ContractType END,
                                case WHEN tt.ContractType='Posted' THEN 'Active' ELSE  tt.ContractType END ) as 'Status',
						 COALESCE (case WHEN ttt.ContractType='Posted' THEN N'فعال' ELSE  ttt.ContractTypeAr END,
                        case WHEN tt.ContractType='Posted' THEN N'فعال' ELSE  tt.ContractTypeAr END ) as 'StatusAr'
		                    from cms.ContractDueAction cda
                        join cms.stages  sta on sta.stageId= cda.StageId
                        left join Pms_Contract con on con.ContractID= cda.Contractid
						left join Pms_Property prop on prop.PropertyID= con.PropertyID
						left join Pms_ContractType tt on tt.ContractTypeID= con.ContractType
						--For renewal 
						left join Pms_Contract cont on cont.ContractID= cda.RenewalContractid
						left join Pms_Property propt on propt.PropertyID= cont.PropertyID
						left join Pms_ContractType ttt on ttt.ContractTypeID= cont.ContractType
                        where cda.TennantId={TenantId} and cda.StageId < 9 
                        AND COALESCE(tt.ContractTypeID, ttt.ContractTypeID) IN (1)
                        order by COALESCE (cont.Contractno,con.Contractno) desc";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery);

            if (Charges.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ContractRenewalActionVm>(Charges);
                Data = ListToBeSend;
                TotalRecord = Data.Count();
            }
            return TotalRecord;
        }
        public int NewContractActions(int TenantId)
        {
            string query = $@"
        SELECT COUNT(cda.DueActionId)
        FROM cms.ContractDueAction cda
        INNER JOIN cms.Stages sta 
            ON sta.StageId = cda.StageId
        LEFT JOIN Pms_Contract con 
            ON con.ContractID = cda.ContractId
        LEFT JOIN Pms_Property prop 
            ON prop.PropertyID = con.PropertyID
        LEFT JOIN Pms_ContractType tt 
            ON tt.ContractTypeID = con.ContractType
        WHERE cda.TennantId = {TenantId}
          AND cda.StageId < 9
          AND tt.ContractTypeID = 3";

            using (SqlConnection conn = new SqlConnection(_settings.GetConnectionString()))
            {
                conn.Open();
                return Convert.ToInt32(
                    new SqlCommand(query, conn).ExecuteScalar()
                );
            }
        }

        public string AssignFMAssigneeTransaction(SqlTransaction tran, SqlConnection con, int CaseId)
        {
            var CaseInfo = GetCaseInformationTRansaction(tran, con, CaseId);
            if (CaseInfo != null)
            {
                EngineerVm model = new EngineerVm();
                if (CaseInfo.PropertyId != null)
                {
                    var engineerQuery = $@"select EngineerCharge from Pms_Property where PropertyID={CaseInfo.PropertyId};";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, engineerQuery);

                    if (Properties.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<EngineerVm>(Properties);
                        model = ListToBeSend.FirstOrDefault();
                        if (model.EngineerCharge != null)
                        {
                            CMSUserVm user = new CMSUserVm();
                            var UserInfo = $@"select UserId,FullName, EmailId as Email from cms.users where EngineerId={model.EngineerCharge}";
                            var User = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, UserInfo);
                            if (User.Rows.Count > 0)
                            {
                                var UserRecord = Conn.ToListof<CMSUserVm>(User);
                                user = UserRecord.FirstOrDefault();
                                var UpdateCase = $@"update cms.Cases set AssignId={user.UserId} where CaseNo={CaseId}";
                                var ExecuteQuery = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, UpdateCase);

                                var AssigneeTemplate = _notificationRepository.GetTemplateInfo(37);
                                var body = _notificationRepository.NotificationBodyReplacement(tran, con, AssigneeTemplate.BodyContent, CaseId);
                                var sendworkGroupEmail = AddNewEmailNotificationTransaction(null, con, body, user.UserId, AssigneeTemplate.Subject, user.Email, AssigneeTemplate.Subject, CaseId);

                                AssigneeLog(tran, con, CaseId, user.UserId);
                            }
                        }
                    }
                }
            }
            return "ok";
        }


        //engineer assignment
        public string AssignFMAssignee(int CaseId)
        {
            var CaseInfo = GetCaseInformation(CaseId);
            if (CaseInfo != null)
            {
                EngineerVm model = new EngineerVm();
                if (CaseInfo.PropertyId != null)
                {
                    var engineerQuery = $@"select EngineerCharge from Pms_Property where PropertyID={CaseInfo.PropertyId};";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Properties = Conn.ExecuteDataTableSqlDA(conn, engineerQuery);

                    if (Properties.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<EngineerVm>(Properties);
                        model = ListToBeSend.FirstOrDefault();
                        if (model.EngineerCharge != null)
                        {
                            CMSUserVm user = new CMSUserVm();
                            var UserInfo = $@"select UserId,FullName from cms.users where EngineerId={model.EngineerCharge}";
                            var User = Conn.ExecuteDataTableSqlDA(conn, UserInfo);
                            if (User.Rows.Count > 0)
                            {
                                var UserRecord = Conn.ToListof<CMSUserVm>(User);
                                user = UserRecord.FirstOrDefault();
                                var UpdateCase = $@"update cms.Cases set AssignId={user.UserId} where CaseNo={CaseId}";
                                var ExecuteQuery = Conn.ExecuteDataTableSqlDA(conn, UpdateCase);
                                AssigneeLogWithOutTransaction(CaseId, user.UserId);

                            }
                        }
                    }
                }
            }
            return "ok";
        }
        public bool AssignPMAssineeeTransaction(SqlTransaction tran, SqlConnection con, int CaseId, string type, bool? IsRenewal = false)
        {
            var IsUpdated = false;
            int? assigneeId = null;
            if (CaseId == 0)
            {
                return IsUpdated;
            }
            var getCaseInfo = GetCaseDetailInfoTransaction(tran, con, CaseId);
            if (getCaseInfo == null)
            {
                return IsUpdated;
            }

            try
            {
                using (SqlCommand cmd = new SqlCommand("GetAssigneeByCase", con, tran))
                {
                    cmd.Parameters.AddWithValue("@CaseId", CaseId);

                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    SqlDataAdapter oDataAdaptor = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();

                    oDataAdaptor.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        assigneeId = string.IsNullOrEmpty(dt.Rows[0]["UserId"].ToString()) ? 0 : Convert.ToInt32(dt.Rows[0]["UserId"]);
                    }
                }
                var UpdateCase = string.Empty;
                if (!string.IsNullOrEmpty(assigneeId.ToString()))
                {
                    if (GetCurrentStageIdTransaction(tran, con, CaseId) == 2)
                        UpdateCase = $@"update cms.Cases set AssignId={assigneeId}, InitialAssigneeId={assigneeId} where CaseNo={CaseId}";
                    else
                        UpdateCase = $@"update cms.Cases set AssignId={assigneeId} where CaseNo={CaseId}";

                    var ExecuteQuery = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, UpdateCase);
                    IsUpdated = true;
                }
            }
            catch (Exception ex)
            {
                InsertErrorLog(ex.Message);
                throw ex;
            }

            return IsUpdated;
        }
        // old function committed and above function is written with new logic and storeprocedure
        //public string AssignPMAssineeeTransaction(SqlTransaction tran, SqlConnection con, int CaseId, string type, bool? IsRenewal = false)
        //{
        //    var logged = InsertErrorLog("Assignee Started");


        //    // in case of isrenewal false 
        //    if (IsRenewal == false)
        //    {
        //        //logged = InsertErrorLog("Assignee IsRenewal False Part Started");
        //        var caseInfo = GetCaseInformationTRansaction(tran, con, CaseId);
        //        if (caseInfo != null)
        //        {
        //            if (caseInfo.EmirateId != null)
        //            {
        //                //get User List against department and emirate, workgroup
        //                List<CMSUserVm> User = new List<CMSUserVm>();
        //                //                        var UserList = $@"--get Assignee 
        //                //select UserId, FullName, EmailId as Email from cms.Users where DepartmentId={department.DepartmentId} and EmirateId={caseInfo.EmirateId}";
        //                var UserList = $@"--get Assignee list
        //                          select u.UserId, u.FullName, u.EmailId as Email from cms.Users u inner join cms.UserMultipleGroups umg on umg.UserId = u.UserId inner join cms.UserGroups ug on ug.UserGroupId = umg.UserGroupId where umg.UserId = u.UserId
        //                       -- and  u.EmirateId = {caseInfo.EmirateId} 
        //                        and umg.UserGroupId = (Select Top 1 c.UserId from cms.Cases c where c.CaseNo = {CaseId})";


        //                //                        var UserList = $@"--get Assignee 
        //                //select u.UserId, u.FullName, u.EmailId as Email from cms.Users u inner join CMS.UserGroups ug on ug.EmirateId = u.EmirateId where  u.EmirateId= {caseInfo.EmirateId} and ug.UserGroupId = IsNull((Select Top 1 c.UserId from cms.Cases c where c.CaseNo = {CaseId}),0)";

        //                var UserRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, UserList);
        //                if (UserRecord.Rows.Count > 0)
        //                {
        //                    var UserRecordlIST = Conn.ToListof<CMSUserVm>(UserRecord);
        //                    User = UserRecordlIST.ToList();
        //                    // get user id who has leave active case and who are present
        //                    CasesListVM CaseList = new CasesListVM();
        //                    //              var UserCase = $@"SELECT cas.AssignId
        //                    //                               ,count(CaseNo) AS CaseNo
        //                    //                              FROM cms.Cases cas
        //                    //                              WHERE cas.AssignId not IN (
        //                    //                                SELECT usr.UserId
        //                    //                                FROM cms.Users usr
        //                    //                                --left join cms.LeaveRecord leave on  leave.UserId= usr.UserId
        //                    //                                      inner join CMS.UserGroups ug on ug.EmirateId = usr.EmirateId
        //                    //                                WHERE usr.EmirateId = {caseInfo.EmirateId}
        //                    //and ug.UserGroupId = cas.UserId
        //                    //                                 --and NOT (getdate() BETWEEN CAST(FromDate AS DATE) AND CAST(ToDate AS DATE))
        //                    //                                )
        //                    //                               AND StatusId NOT IN (17,20 )
        //                    //                                      and cas.AssignId is not null
        //                    //                              GROUP BY cas.AssignId
        //                    //                              ORDER BY count(CaseNo)";

        //                    var UserCase = $@"Select u.UserId, (select Count(*) From cms.cases c where c.AssignId = u.UserId  and c.StatusId Not In(17, 20)) as CaseNo  From CMS.users u inner join cms.UserMultipleGroups umg on umg.UserId = u.UserId inner join cms.UserGroups ug on ug.UserGroupId = umg.UserGroupId where  
        //                        --u.EmirateId = (select c.EmirateId From cms.cases c where c.CaseNo = {CaseId}) and 
        //                         ug.UserGroupId = (select c.UserId From cms.cases c where c.CaseNo = {CaseId})";


        //                    var UserCaseRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, UserCase);
        //                    if (UserCaseRecord.Rows.Count > 0)
        //                    {
        //                        var CaseRecordlIST = Conn.ToListof<CasesListVM>(UserCaseRecord);
        //                        CaseList = CaseRecordlIST.OrderBy(p => p.CaseNo).FirstOrDefault();
        //                        var UpdateCase = $@"update cms.Cases set AssignId={CaseList.UserId},    InitialAssigneeId={CaseList.UserId} where CaseNo={CaseId}";


        //                        if (type == "Update" && GetCurrentStageIdTransaction(tran, con, CaseId) == 2)
        //                        {

        //                            UpdateCase = $@"update cms.Cases set AssignId={CaseList.UserId},            InitialAssigneeId={CaseList.UserId}  where CaseNo={CaseId}";

        //                        }

        //                        var ExecuteQuery = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, UpdateCase);

        //                        AssigneeLog(tran, con, CaseId, CaseList.UserId);

        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else //renewal case
        //    {
        //        logged = InsertErrorLog("Assignee Else Part Started");
        //        //get department id which assign to current stage of the case
        //        DepartmentStage department = new DepartmentStage();
        //        var query = $@"--get department
        //                    select DepartmentId, DueActionid from cms.ContractDueAction con join cms.Stages sta on con.StageId=sta.StageId where CaseId={CaseId}";
        //        //try
        //        //{
        //        var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);

        //        if (Properties.Rows.Count > 0)
        //        {
        //            var ListToBeSend = Conn.ToListof<DepartmentStage>(Properties);
        //            department = ListToBeSend.FirstOrDefault();
        //            if (department == null || department.DepartmentId == null || department.DepartmentId.Value == 0)
        //            {
        //                logged = InsertErrorLog("Department Null");
        //                return "No Change In Assignee ";

        //            }
        //            var caseInfo = GetCaseInformationTRansaction(tran, con, CaseId);
        //            if (caseInfo != null)
        //            {
        //                if (caseInfo.EmirateId != null)
        //                {
        //                    //get User List against department and emirate, workgroup
        //                    List<CMSUserVm> User = new List<CMSUserVm>();
        //                    //                        var UserList = $@"--get Assignee 
        //                    //select UserId, FullName, EmailId as Email from cms.Users where DepartmentId={department.DepartmentId} and EmirateId={caseInfo.EmirateId}";
        //                    //var UserList = $@"--get Assignee 
        //                    //                select u.UserId, u.FullName, u.EmailId as Email from cms.Users u inner join CMS.UserGroups ug on ug.EmirateId = u.EmirateId where u.DepartmentId= {department.DepartmentId.Value} and u.EmirateId= {caseInfo.EmirateId} and ug.UserGroupId = (Select Top 1 c.UserId from cms.Cases c where c.CaseNo = {CaseId})";
        //                    var UserList = $@"--get Assignee list
        //                        select u.UserId, u.FullName, u.EmailId as Email from cms.Users u inner join cms.UserMultipleGroups umg on umg.UserId = u.UserId inner join cms.UserGroups ug on ug.UserGroupId = umg.UserGroupId where umg.UserId = u.UserId and u.DepartmentId = {department.DepartmentId.Value} 
        //                       -- and u.EmirateId = {caseInfo.EmirateId} 
        //                         and umg.UserGroupId = (Select Top 1 c.UserId from cms.Cases c where c.CaseNo = {CaseId})";

        //                    var UserRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, UserList);

        //                    if (UserRecord.Rows.Count > 0)
        //                    {
        //                        var UserRecordlIST = Conn.ToListof<CMSUserVm>(UserRecord);
        //                        User = UserRecordlIST.ToList();

        //                        CasesListVM CaseList = new CasesListVM();


        //                        //check existing open cases with users
        //                        //                      var UserCase = $@"SELECT cas.AssignId
        //                        //                               ,count(CaseNo) AS CaseNo
        //                        //                              FROM cms.Cases cas
        //                        //                              WHERE cas.AssignId not IN (
        //                        //                                SELECT usr.UserId
        //                        //                                FROM cms.Users usr
        //                        //                                --left join cms.LeaveRecord leave on  leave.UserId=cas.UserId
        //                        //                                      inner join CMS.UserGroups ug on ug.EmirateId = usr.EmirateId
        //                        //                                WHERE DepartmentId = {department.DepartmentId.Value}
        //                        //                                 AND usr.EmirateId = {caseInfo.EmirateId}
        //                        //and ug.UserGroupId = cas.UserId
        //                        //                                 --and NOT (getdate() BETWEEN CAST(FromDate AS DATE) AND CAST(ToDate AS DATE))
        //                        //                                )
        //                        //                               AND StatusId NOT IN (17,20 ) 
        //                        //                                      and cas.AssignId is not null
        //                        //                              GROUP BY cas.AssignId
        //                        //                              ORDER BY count(CaseNo)";
        //                        //total open case of user


        //                        var UserCase = $@"Select u.UserId, (select Count(*) From cms.cases c where c.AssignId = u.UserId  and c.StatusId Not In(17, 20)) as CaseNo  From CMS.users u inner join cms.UserMultipleGroups umg on umg.UserId = u.UserId inner join cms.UserGroups ug on ug.UserGroupId = umg.UserGroupId where u.DepartmentId = {department.DepartmentId.Value} 
        //                        --and u.EmirateId = (select c.EmirateId From cms.cases c where c.CaseNo = {CaseId}) 
        //                        and ug.UserGroupId = (select c.UserId From cms.cases c where c.CaseNo = {CaseId})";

        //                        var UserCaseRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, UserCase);

        //                        if (UserCaseRecord.Rows.Count > 0)
        //                        {
        //                            var CaseRecordlIST = Conn.ToListof<CasesListVM>(UserCaseRecord);

        //                            //  var CaseRecordlIST = Conn.ToListof<CasesListVM>(UserCaseRecord);
        //                            CaseList = CaseRecordlIST.OrderBy(p => p.CaseNo).FirstOrDefault();
        //                            var UpdateCase = $@"update cms.Cases set AssignId={CaseList.UserId}, InitialAssigneeId={CaseList.UserId} where CaseNo={CaseId}";
        //                            if (type == "Update")
        //                            {
        //                                UpdateCase = $@"update cms.Cases set AssignId={CaseList.UserId} , InitialAssigneeId={CaseList.UserId} where CaseNo={CaseId}";
        //                                // var currentStageid = GetCurrentStageId(CaseId);
        //                                //if (currentStageid > .0 && currentStageid == 6)
        //                                //{
        //                                //    UpdateCase = $@"update cms.Cases set AssignId=InitialAssigneeId where CaseNo={CaseId} and InitialAssigneeId IS NOT NULL";
        //                                //}
        //                            }
        //                            try
        //                            {
        //                                var ExecuteQuery = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, UpdateCase);


        //                                AssigneeLog(tran, con, CaseId, CaseList.UserId);


        //                            }
        //                            catch (Exception ex)
        //                            {

        //                                logged = InsertErrorLog(ex.Message);
        //                            }

        //                            var UpdateContractAsignee = $@"update cms.ContractDueAction set AssigneeUpdated=1 where DueActionid={department.DueActionid}";
        //                            var ExecuteContractAsigneeQuery = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, UpdateContractAsignee);
        //                            try
        //                            {
        //                                if (User != null && User.Count() > 0)
        //                                {
        //                                    var getuseremail = User.Where(p => p.UserId == CaseList.UserId).Select
        //                                        (p => p.Email).FirstOrDefault();
        //                                    if (!string.IsNullOrWhiteSpace(getuseremail))
        //                                    {

        //                                        //try
        //                                        //{
        //                                        //    logged = InsertErrorLog("Email Function Called");
        //                                        //    var AddEmailNotification = prepareAndAddEmailNotification(6, 8, caseInfo.EmirateId, 20, CaseId, getuseremail);
        //                                        //}
        //                                        //catch (Exception ex)
        //                                        //{

        //                                        //    var message = ex.Message + " " + ex.InnerException;
        //                                        //    logged = InsertErrorLog(message);
        //                                        //}

        //                                    }
        //                                }

        //                            }
        //                            catch (Exception ex)
        //                            {

        //                                var message = ex.Message + " " + ex.InnerException;
        //                                logged = InsertErrorLog(message);
        //                            }


        //                        }
        //                        else
        //                        {
        //                            logged = InsertErrorLog("UserCaseRecord Null");
        //                        }
        //                    }
        //                    else
        //                    {
        //                        logged = InsertErrorLog("Users Null");
        //                    }
        //                }
        //                else
        //                {

        //                    logged = InsertErrorLog("case Info .EmirateId Null");

        //                }
        //            }
        //            else
        //            {
        //                logged = InsertErrorLog("Cse Info Null");
        //            }
        //        }
        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    logged = InsertErrorLog(ex.Message);
        //        //}

        //    }
        //    logged = InsertErrorLog("Call at the end of assignee id");
        //    return "ok";
        //}

        public bool AssigneeLogWithOutTransaction(int caseid, int? userid = null)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            string Notes = "System";
            if (userid == null)
            {
                userid = 0;
            }
            try
            {
                var caseLogQuery = $@"INSERT INTO CMS.CaseUser(
                                              CaseId
                                             ,AssignedDate
                                             ,UserId
                                             ,Notes
                                             )
                                            VALUES (
                                              '{caseid}'
                                             ,'{DateTime.UtcNow.AddHours(4)}'
                                             ,'{userid}'
                                             ,'{Notes}'
                                             );";

                Conn.ExecuteDataTableSqlDA(conn, caseLogQuery);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return true;
        }
        public bool AssigneeLog(SqlTransaction tran, SqlConnection con, int caseid, int? userid = null)
        {
            string Notes = "System";
            if (userid == null)
            {
                userid = 0;
            }
            try
            {
                var caseLogQuery = $@"INSERT INTO CMS.CaseUser(
                                              CaseId
                                             ,AssignedDate
                                             ,UserId
                                             ,Notes
                                             )
                                            VALUES (
                                              '{caseid}'
                                             ,'{DateTime.UtcNow.AddHours(4)}'
                                             ,'{userid}'
                                             ,'{Notes}'
                                             );";

                Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, caseLogQuery);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return true;
        }

        public string AssignPMAssineee(int CaseId, string type, bool? IsRenewal = false)
        {
            var logged = InsertErrorLog("Assignee Started");
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            // in case of isrenewal false 
            if (IsRenewal == false)
            {
                logged = InsertErrorLog("Assignee IsRenewal False Part Started");
                var caseInfo = GetCaseInformation(CaseId);
                if (caseInfo != null)
                {
                    if (caseInfo.EmirateId != null)
                    {
                        //get User List against department and emirate, workgroup
                        List<CMSUserVm> User = new List<CMSUserVm>();
                        //                        var UserList = $@"--get Assignee 
                        //select UserId, FullName, EmailId as Email from cms.Users where DepartmentId={department.DepartmentId} and EmirateId={caseInfo.EmirateId}";
                        var UserList = $@"--get Assignee list
                                  select u.UserId, u.FullName, u.EmailId as Email from cms.Users u inner join cms.UserMultipleGroups umg on umg.UserId = u.UserId inner join cms.UserGroups ug on ug.UserGroupId = umg.UserGroupId where umg.UserId = u.UserId
                               -- and  u.EmirateId = {caseInfo.EmirateId} 
                                and umg.UserGroupId = (Select Top 1 c.UserId from cms.Cases c where c.CaseNo = {CaseId})";



                        //                        var UserList = $@"--get Assignee 
                        //select u.UserId, u.FullName, u.EmailId as Email from cms.Users u inner join CMS.UserGroups ug on ug.EmirateId = u.EmirateId where  u.EmirateId= {caseInfo.EmirateId} and ug.UserGroupId = IsNull((Select Top 1 c.UserId from cms.Cases c where c.CaseNo = {CaseId}),0)";
                        var UserRecord = Conn.ExecuteDataTableSqlDA(conn, UserList);
                        if (UserRecord.Rows.Count > 0)
                        {
                            var UserRecordlIST = Conn.ToListof<CMSUserVm>(UserRecord);
                            User = UserRecordlIST.ToList();
                            // get user id who has leave active case and who are present
                            CasesListVM CaseList = new CasesListVM();
                            //              var UserCase = $@"SELECT cas.AssignId
                            //                               ,count(CaseNo) AS CaseNo
                            //                              FROM cms.Cases cas
                            //                              WHERE cas.AssignId not IN (
                            //                                SELECT usr.UserId
                            //                                FROM cms.Users usr
                            //                                --left join cms.LeaveRecord leave on  leave.UserId= usr.UserId
                            //                                      inner join CMS.UserGroups ug on ug.EmirateId = usr.EmirateId
                            //                                WHERE usr.EmirateId = {caseInfo.EmirateId}
                            //and ug.UserGroupId = cas.UserId
                            //                                 --and NOT (getdate() BETWEEN CAST(FromDate AS DATE) AND CAST(ToDate AS DATE))
                            //                                )
                            //                               AND StatusId NOT IN (17,20 )
                            //                                      and cas.AssignId is not null
                            //                              GROUP BY cas.AssignId
                            //                              ORDER BY count(CaseNo)";

                            var UserCase = $@"Select u.UserId, (select Count(*) From cms.cases c where c.AssignId = u.UserId  and c.StatusId Not In(17, 20)) as CaseNo  From CMS.users u inner join cms.UserMultipleGroups umg on umg.UserId = u.UserId inner join cms.UserGroups ug on ug.UserGroupId = umg.UserGroupId where  
                                --u.EmirateId = (select c.EmirateId From cms.cases c where c.CaseNo = {CaseId}) and 
                                 ug.UserGroupId = (select c.UserId From cms.cases c where c.CaseNo = {CaseId})";


                            var UserCaseRecord = Conn.ExecuteDataTableSqlDA(conn, UserCase);
                            if (UserCaseRecord.Rows.Count > 0)
                            {
                                var CaseRecordlIST = Conn.ToListof<CasesListVM>(UserCaseRecord);
                                CaseList = CaseRecordlIST.OrderBy(p => p.CaseNo).FirstOrDefault();
                                var UpdateCase = $@"update cms.Cases set AssignId={CaseList.UserId}, InitialAssigneeId={CaseList.UserId} where CaseNo={CaseId}";
                                if (type == "Update" && GetCurrentStageId(CaseId) == 2)
                                {

                                    UpdateCase = $@"update cms.Cases set AssignId={CaseList.UserId}, InitialAssigneeId={CaseList.UserId}  where CaseNo={CaseId}";

                                }

                                var ExecuteQuery = Conn.ExecuteDataTableSqlDA(conn, UpdateCase);

                                AssigneeLogWithOutTransaction(CaseId, CaseList.UserId);




                            }
                        }
                    }
                }
            }
            else //renewal case
            {
                logged = InsertErrorLog("Assignee Else Part Started");
                //get department id which assign to current stage of the case
                DepartmentStage department = new DepartmentStage();
                var query = $@"--get department
                            select DepartmentId, DueActionid from cms.ContractDueAction con join cms.Stages sta on con.StageId=sta.StageId where CaseId={CaseId}";
                try
                {
                    var Properties = Conn.ExecuteDataTableSqlDA(conn, query);

                    if (Properties.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<DepartmentStage>(Properties);
                        department = ListToBeSend.FirstOrDefault();
                        if (department == null || department.DepartmentId == null || department.DepartmentId.Value == 0)
                        {
                            logged = InsertErrorLog("Department Null");
                            return "No Change In Assignee ";

                        }
                        var caseInfo = GetCaseInformation(CaseId);
                        if (caseInfo != null)
                        {
                            if (caseInfo.EmirateId != null)
                            {
                                //get User List against department and emirate, workgroup
                                List<CMSUserVm> User = new List<CMSUserVm>();
                                //                        var UserList = $@"--get Assignee 
                                //select UserId, FullName, EmailId as Email from cms.Users where DepartmentId={department.DepartmentId} and EmirateId={caseInfo.EmirateId}";
                                //var UserList = $@"--get Assignee 
                                //                select u.UserId, u.FullName, u.EmailId as Email from cms.Users u inner join CMS.UserGroups ug on ug.EmirateId = u.EmirateId where u.DepartmentId= {department.DepartmentId.Value} and u.EmirateId= {caseInfo.EmirateId} and ug.UserGroupId = (Select Top 1 c.UserId from cms.Cases c where c.CaseNo = {CaseId})";
                                var UserList = $@"--get Assignee list
                                select u.UserId, u.FullName, u.EmailId as Email from cms.Users u inner join cms.UserMultipleGroups umg on umg.UserId = u.UserId inner join cms.UserGroups ug on ug.UserGroupId = umg.UserGroupId where umg.UserId = u.UserId and u.DepartmentId = {department.DepartmentId.Value} 
                               -- and u.EmirateId = {caseInfo.EmirateId} 
                                 and umg.UserGroupId = (Select Top 1 c.UserId from cms.Cases c where c.CaseNo = {CaseId})";

                                var UserRecord = Conn.ExecuteDataTableSqlDA(conn, UserList);

                                if (UserRecord.Rows.Count > 0)
                                {
                                    var UserRecordlIST = Conn.ToListof<CMSUserVm>(UserRecord);
                                    User = UserRecordlIST.ToList();

                                    CasesListVM CaseList = new CasesListVM();


                                    //check existing open cases with users
                                    //                      var UserCase = $@"SELECT cas.AssignId
                                    //                               ,count(CaseNo) AS CaseNo
                                    //                              FROM cms.Cases cas
                                    //                              WHERE cas.AssignId not IN (
                                    //                                SELECT usr.UserId
                                    //                                FROM cms.Users usr
                                    //                                --left join cms.LeaveRecord leave on  leave.UserId=cas.UserId
                                    //                                      inner join CMS.UserGroups ug on ug.EmirateId = usr.EmirateId
                                    //                                WHERE DepartmentId = {department.DepartmentId.Value}
                                    //                                 AND usr.EmirateId = {caseInfo.EmirateId}
                                    //and ug.UserGroupId = cas.UserId
                                    //                                 --and NOT (getdate() BETWEEN CAST(FromDate AS DATE) AND CAST(ToDate AS DATE))
                                    //                                )
                                    //                               AND StatusId NOT IN (17,20 ) 
                                    //                                      and cas.AssignId is not null
                                    //                              GROUP BY cas.AssignId
                                    //                              ORDER BY count(CaseNo)";
                                    //total open case of user


                                    var UserCase = $@"Select u.UserId, (select Count(*) From cms.cases c where c.AssignId = u.UserId  and c.StatusId Not In(17, 20)) as CaseNo  From CMS.users u inner join cms.UserMultipleGroups umg on umg.UserId = u.UserId inner join cms.UserGroups ug on ug.UserGroupId = umg.UserGroupId where u.DepartmentId = {department.DepartmentId.Value} 
                                --and u.EmirateId = (select c.EmirateId From cms.cases c where c.CaseNo = {CaseId}) 
                                and ug.UserGroupId = (select c.UserId From cms.cases c where c.CaseNo = {CaseId})";

                                    var UserCaseRecord = Conn.ExecuteDataTableSqlDA(conn, UserCase);

                                    if (UserCaseRecord.Rows.Count > 0)
                                    {
                                        var CaseRecordlIST = Conn.ToListof<CasesListVM>(UserCaseRecord);

                                        //  var CaseRecordlIST = Conn.ToListof<CasesListVM>(UserCaseRecord);
                                        CaseList = CaseRecordlIST.OrderBy(p => p.CaseNo).FirstOrDefault();
                                        var UpdateCase = $@"update cms.Cases set AssignId={CaseList.UserId}, InitialAssigneeId={CaseList.UserId} where CaseNo={CaseId}";
                                        if (type == "Update")
                                        {
                                            UpdateCase = $@"update cms.Cases set AssignId={CaseList.UserId} , InitialAssigneeId={CaseList.UserId} where CaseNo={CaseId}";

                                        }
                                        try
                                        {
                                            var ExecuteQuery = Conn.ExecuteDataTableSqlDA(conn, UpdateCase);
                                            AssigneeLogWithOutTransaction(CaseId, CaseList.UserId);
                                        }
                                        catch (Exception ex)
                                        {

                                            logged = InsertErrorLog(ex.Message);
                                        }

                                        var UpdateContractAsignee = $@"update cms.ContractDueAction set AssigneeUpdated=1 where DueActionid={department.DueActionid}";
                                        var ExecuteContractAsigneeQuery = Conn.ExecuteDataTableSqlDA(conn, UpdateContractAsignee);
                                        try
                                        {
                                            if (User != null && User.Count() > 0)
                                            {
                                                var getuseremail = User.Where(p => p.UserId == CaseList.UserId).Select
                                                    (p => p.Email).FirstOrDefault();
                                                if (!string.IsNullOrWhiteSpace(getuseremail))
                                                {



                                                }
                                            }

                                        }
                                        catch (Exception ex)
                                        {

                                            var message = ex.Message + " " + ex.InnerException;
                                            logged = InsertErrorLog(message);
                                        }


                                    }
                                    else
                                    {
                                        logged = InsertErrorLog("UserCaseRecord Null");
                                    }
                                }
                                else
                                {
                                    logged = InsertErrorLog("Users Null");
                                }
                            }
                            else
                            {

                                logged = InsertErrorLog("case Info .EmirateId Null");

                            }
                        }
                        else
                        {
                            logged = InsertErrorLog("Cse Info Null");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logged = InsertErrorLog(ex.Message);
                }

            }
            logged = InsertErrorLog("Call at the end of assignee id");
            return "ok";
        }

        public int GetCurrentStageIdTransaction(SqlTransaction tran, SqlConnection con, int CaseId)
        {
            int stageid = 0;
            var query = $@"Select Isnull(StageId,0) as StageId from cms.ContractDueAction cda where CaseId ={CaseId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CurrentStageVM>(Properties);
                stageid = ListToBeSend.Select(p => p.StageId).FirstOrDefault();
            }
            return stageid;
        }

        public int GetCurrentStageId(int CaseId)
        {
            int stageid = 0;
            var query = $@"Select Isnull(StageId,0) as StageId from cms.ContractDueAction cda where CaseId ={CaseId})";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CurrentStageVM>(Properties);
                stageid = ListToBeSend.Select(p => p.StageId).FirstOrDefault();
            }
            return stageid;
        }

        public string CategoryInfoTransaction(SqlTransaction tran, SqlConnection con, int CategoryId)
        {
            string Type = "";
            var query = $@"select Type from cms.CaseCategory where Id={CategoryId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CategoryInfo>(Properties);
                Type = ListToBeSend.Select(p => p.Type).FirstOrDefault();
            }
            return Type;
        }
        public string CategoryInfo(int CategoryId)
        {
            string Type = "";
            var query = $@"select Type from cms.CaseCategory where Id={CategoryId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CategoryInfo>(Properties);
                Type = ListToBeSend.Select(p => p.Type).FirstOrDefault();
            }
            return Type;
        }

        public int? GetCategoryCode(int CaseId)
        {
            int? Code = 0;
            var query = $@"select code from cms.CaseCategory where Id=(select CaseCategouryId from cms.CaseServiceRequest where CaseId={CaseId})";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CategoryCodeVm>(Properties);
                Code = ListToBeSend.Select(p => p.code).FirstOrDefault();
            }
            return Code;
        }

        public List<int> GetContractorUserId(int ContractorId)
        {
            List<int> UserId = new List<int>();
            //05122022 commented becuase profileprentid ccontains contractor id
            // var query = $@"select UserId from app.UserRoles where ProfileParentId in (select ContactPersonID from ContactPerson where ContractorID={ContractorId})";

            var query = $@"select UserId from app.UserRoles where ProfileParentId = {ContractorId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ContractorUserIDVM>(Properties);
                UserId = ListToBeSend.Select(p => p.UserId).ToList();
            }
            return UserId;
        }
        public bool InsertErrorLog(string error)
        {

            bool logged = true;
            var datecurrent = DateTime.UtcNow.AddHours(4);
            var query = $@"INSERT INTO [CMS].[ErrorLogs]
                       ([Date]
                       ,[Description])
                        VALUES
                       ('{datecurrent}'
                       ,@Error)

                        SELECT SCOPE_IDENTITY() AS SelectedId
            ";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var errorParam = new SqlParameter("@Error", error);

            var excutequery = Conn.ExecuteDataTableSqlDA(conn, query, errorParam);
            if (excutequery != null)
            {
                logged = true;
            }
            return logged;
        }

        public int GetCaseServiceRequestCategoryIdTransaction(SqlTransaction tran, SqlConnection con, int CaseId)
        {
            int categoryid = 0;
            var query = $@"Select IsNull(CaseCategouryId, 0) as CategoryId From CMS.CaseServiceRequest Where CaseId = {CaseId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseServiceRequestViewModa>(Properties);
                categoryid = ListToBeSend.Select(p => p.CategoryId).FirstOrDefault();
            }
            return categoryid;
        }
        public int GetCaseServiceRequestCategoryId(int CaseId)
        {
            int categoryid = 0;
            var query = $@"Select IsNull(CaseCategouryId, 0) as CategoryId From CMS.CaseServiceRequest Where CaseId = {CaseId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, query);

            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseServiceRequestViewModa>(Properties);
                categoryid = ListToBeSend.Select(p => p.CategoryId).FirstOrDefault();
            }
            return categoryid;
        }

        public bool AddNewEmailNotificationTransaction(SqlTransaction tran, SqlConnection con, string emailbody, int UserId, string subject, string email, string Title, int CaseId)
        {
            bool logged = false;
            var timeZone = "Arabian Standard Time";
            if (string.IsNullOrWhiteSpace(email))
            {
                var errorlog = InsertErrorLog("No Email Address");
                return logged;
            }
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            string currentDate = DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd hh:mm:ss tt");
            var batch = email + "-" + DateTime.UtcNow.AddHours(4).ToString("ddMM");
            var query = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title, UserId, Description, Batch,CurrentStatus,CreatedOn,Email,UserType,Popup, 
                         RecordType, RecordId, ShortDescription) VALUES (0,'2',0,@emailTitle,{UserId},@emailbody, '{batch}','Queue','{currentDate}',@email,'Work Goup',0, 
                        'Assign Case',{CaseId},@emailbody) SELECT SCOPE_IDENTITY() as InsertRecordId;";

            var bodyParam = new SqlParameter("@emailbody", emailbody);
            var subjectParam = new SqlParameter("@emailsubject", subject);
            var titleParam = new SqlParameter("@emailTitle", Title);
            var emailParam = new SqlParameter("@email", email);

            SqlParameter[] paramter = new SqlParameter[]
            {
                  new SqlParameter("@emailbody",           emailbody),
                  new SqlParameter("@emailsubject",        subject),
                  new SqlParameter("@emailTitle",          Title),
                  new SqlParameter("@email",               email),
            };
            var excutequery = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, query, paramter);
            if (excutequery != null)
            {
                logged = true;
            }
            return logged;
        }

        //Adding New Email Log for workGroup on new service re
        public bool AddNewEmailNotification(string emailbody, int UserId, string subject, string email, string Title, int CaseId)
        {
            bool logged = false;
            var timeZone = "Arabian Standard Time";
            if (string.IsNullOrWhiteSpace(email))
            {
                var errorlog = InsertErrorLog("No Email Address");
                return logged;
            }
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            string currentDate = DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd hh:mm:ss tt");
            var batch = email + "-" + DateTime.UtcNow.AddHours(4).ToString("ddMM");
            var query = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title, UserId, Description, Batch,CurrentStatus,CreatedOn,Email,UserType,Popup, RecordType, RecordId, ShortDescription) VALUES (0,'2',0,@emailTitle,{UserId},@emailbody, '{batch}','Queue','{currentDate}',@email,'Work Goup',0, 'Assign Case',{CaseId},@emailbody) SELECT SCOPE_IDENTITY() as InsertRecordId;";

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var bodyParam = new SqlParameter("@emailbody", emailbody);
            var subjectParam = new SqlParameter("@emailsubject", subject);
            var titleParam = new SqlParameter("@emailTitle", Title);
            var emailParam = new SqlParameter("@email", email);
            var excutequery = Conn.ExecuteDataTableSqlDA(conn, query, bodyParam, subjectParam, titleParam, emailParam);
            if (excutequery != null)
            {
                logged = true;
            }
            return logged;
        }
        public bool SetSLA(int TypeId, int CategoryId, int SubCategoryId, int EmirateId, int CaseNo)
        {
            int slaid = 0;
            //Remove Emirate Id from filter on 05/13/2022
            var slaquery = $@"Select Top 1 SLAid as SLAId From CMS.SLARulesSet Where TypeId = {TypeId} and CategoryId = {CategoryId} and SubCategoryId = {SubCategoryId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var excutequery = Conn.ExecuteDataTableSqlDA(conn, slaquery);
            if (excutequery.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<SLAInfoVm>(excutequery);
                slaid = ListToBeSend.Select(p => p.SLAId).FirstOrDefault();
            }
            if (slaid > 0)
            {
                var updateQuery = $@"Update cms.cases set ServiceLevelId = {slaid} Where CaseNo = {CaseNo}";
                var excuteupdatequery = Conn.ExecuteDataTableSqlDA(conn, updateQuery);
                return true;
            }
            return false;
        }
    }
}