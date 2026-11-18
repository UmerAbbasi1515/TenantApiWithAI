using FABModels.Model;
using FABTenantApi.Classes;
using FABTenantApi.Models;
using FABTenantD2Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FABTenantD2Api.Classes
{
    public class PaymentGatway
    {
        private readonly Settings _settings;
        public PaymentGatway()
        {
            _settings = new Settings();
        }

        public string AddCmsLogTransaction(SqlTransaction tran,SqlConnection conn,int UserId, string Title, string Description)
        {
            string ResponseMessage = "";
            try
            {
                //Temporary comment to check payment - 03-12-2022
                //if (UserId == 0)
                //{
                //    ResponseMessage = "Invalid user device";
                //    return ResponseMessage;
                //}

                var AddReplyQuery = $@"INSERT INTO cms.GatewayLog(Title,Description, CreatedOn, UserId) VALUES (@Title,@Description,'{DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt")}' ,{UserId});";
            var titleParam = new SqlParameter("@Title", Title);
            if (string.IsNullOrWhiteSpace(Title))
            {
                titleParam = new SqlParameter("@Title", DBNull.Value);
            }
            var descriptionParam = new SqlParameter("@Description", Description);
            if (string.IsNullOrWhiteSpace(Description))
            {
                descriptionParam = new SqlParameter("@Description", DBNull.Value);
            }
            SqlParameter[] sqlParameters = new SqlParameter[]
                       {
                       titleParam,
                       descriptionParam
                       };
            var UpdateRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran,conn, AddReplyQuery, sqlParameters);

                ResponseMessage = "Add successfully";
           

            }
            catch (Exception ex)
            {
                throw ex;
            }

                return ResponseMessage;
        }
        public string AddCmsLog(int UserId, string Title, string Description)
        {
            string ResponseMessage = "";

            //Temporary comment to check payment - 03-12-2022
            //if (UserId == 0)
            //{
            //    ResponseMessage = "Invalid user device";
            //    return ResponseMessage;
            //}


            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var AddReplyQuery = $@"INSERT INTO cms.GatewayLog(Title,Description, CreatedOn, UserId) VALUES (@Title,@Description,'{DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt")}' ,{UserId});";
            var titleParam = new SqlParameter("@Title", Title);
            if (string.IsNullOrWhiteSpace(Title))
            {
                titleParam = new SqlParameter("@Title", DBNull.Value);
            }
            var descriptionParam = new SqlParameter("@Description", Description);
            if (string.IsNullOrWhiteSpace(Description))
            {
                descriptionParam = new SqlParameter("@Description", DBNull.Value);
            }
            var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery, titleParam, descriptionParam);

            try
            {

                ResponseMessage = "Add successfully";
                return ResponseMessage;
            }
            catch (Exception ex)
            {
                ResponseMessage = ex.Message;
                return ResponseMessage;
            }

        }


        public decimal RegisterPaymentTransaction(SqlTransaction tran,SqlConnection conn,int UserId, string Title, string Description, int ContractId, string GWRegRefNo, decimal? TotalAmount)
        {
            decimal InsertedRecordId = 0;
            InsertedRecordVM InsertRecord = new InsertedRecordVM();

            

            var AddReplyQuery = $@"INSERT INTO cms.GatewayPaymant(PaymantStartOn,LastUpdateOn, Title, Description,UserId, ContractId,GWRegRefNo,TotalAmount,Status,Reconciled) 
            VALUES ('{DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt")}','{DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt")}',@Title ,@Description,{UserId},{ContractId},'{GWRegRefNo}',{TotalAmount},'Registration',0) SELECT SCOPE_IDENTITY() as InsertRecordId;";
            var titleParam = new SqlParameter("@Title", Title);
            if (string.IsNullOrWhiteSpace(Title))
            {
                titleParam = new SqlParameter("@Title", DBNull.Value);
            }
            var descriptionParam = new SqlParameter("@Description", Description);
            if (string.IsNullOrWhiteSpace(Description))
            {
                descriptionParam = new SqlParameter("@Description", DBNull.Value);
            }
            SqlParameter[] sqlParameters = new SqlParameter[]
                       {
                        titleParam,
                        descriptionParam
                       };
            var UpdateRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran,conn, AddReplyQuery, sqlParameters);
            if (UpdateRecord.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<InsertedRecordVM>(UpdateRecord);
                InsertRecord = ListToBeSend.FirstOrDefault();
                InsertedRecordId = InsertRecord.InsertRecordId;
            }
            return InsertedRecordId;
        }
        public decimal RegisterPayment(int UserId, string Title, string Description, int ContractId, string GWRegRefNo, decimal? TotalAmount)
        {
            decimal InsertedRecordId = 0;
            InsertedRecordVM InsertRecord = new InsertedRecordVM();

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var AddReplyQuery = $@"INSERT INTO cms.GatewayPaymant(PaymantStartOn,LastUpdateOn, Title, Description,UserId, ContractId,GWRegRefNo,TotalAmount,Status,Reconciled) 
            VALUES ('{DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt")}','{DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt")}',@Title ,@Description,{UserId},{ContractId},'{GWRegRefNo}',{TotalAmount},'Registration',0) SELECT SCOPE_IDENTITY() as InsertRecordId;";
            var titleParam = new SqlParameter("@Title", Title);
            if (string.IsNullOrWhiteSpace(Title))
            {
                titleParam = new SqlParameter("@Title", DBNull.Value);
            }
            var descriptionParam = new SqlParameter("@Description", Description);
            if (string.IsNullOrWhiteSpace(Description))
            {
                descriptionParam = new SqlParameter("@Description", DBNull.Value);
            }
            var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery, titleParam, descriptionParam);
            if (UpdateRecord.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<InsertedRecordVM>(UpdateRecord);
                InsertRecord = ListToBeSend.FirstOrDefault();
                InsertedRecordId = InsertRecord.InsertRecordId;
            }
            return InsertedRecordId;
        }

        public string AddPaymentDetailItemTransaction(SqlTransaction tran,SqlConnection conn,decimal PaymentId, List<PaymentDetailVm> modal)
        {
            string ResponseMessage = "";
            if (PaymentId == 0)
            {
                ResponseMessage = "Invalid Paymant Id";
                return ResponseMessage;
            }

            
            var sqlCommand = new SqlCommand
            {
                Connection = conn,
                Transaction= tran
            };

            bool amountMisMatch = false;

            try
            {

                foreach (var item in modal)
                {
                    //commendted 14/10/2022
                    //if (CrossSiteScriptingValidation.IsDangerousParameter(item.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(item.Title, out _))
                    //{
                    //    var dataBadReq = new
                    //    {
                    //        Status = "BadRequest",
                    //        Message = "Invalid Request.",
                    //        StatusCode = "500"
                    //    };
                    //    return "BadRequest";
                    //}
                    sqlCommand.CommandText = $"SELECT ISNULL(VATAmount,0) FROM Pms_ContractCharges WHERE ContractChargeId = {item.ContractchargeID}";

                    var isVATPayment = Convert.ToDecimal(sqlCommand.ExecuteScalar()) == item.Amount;
                    var col = isVATPayment ? "ContractchargeID" : "VATOnChargeID";

                    sqlCommand.CommandText = $@"select Amount from cms.PaymentSetting WHERE PaymentSettingId = {item.PaymentSettingId}";

                    var amount = Convert.ToDecimal(sqlCommand.ExecuteScalar());

                    if (amount != item.Amount)
                    {
                        amountMisMatch = true;
                        break;
                    }
                    if (!string.IsNullOrWhiteSpace(item.Description))
                    {
                        item.Description = _settings.RemoveSpecialChar(item.Description);
                    }

                    var AddReplyQuery = $@"INSERT INTO cms.GatewayPaymantDetail(PaymentId,Title, Description, CreatedOn,Amount,ContractchargeID,ContractPaymentID,VatOnRentContractId,VATOnChargeID,PaymentSettingId) VALUES ('{PaymentId}',@Title,@Description ,'{DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt")}',{item.Amount},{item.ContractchargeID},{item.PaymentID},{item.VatOnRentContractId},{item.VATOnChargeID},{item.PaymentSettingId});";
                    var DescriptionSqlParam = new SqlParameter("@Description", item.Description);
                    if (string.IsNullOrWhiteSpace(item.Description))
                    {
                        DescriptionSqlParam = new SqlParameter("@Description", DBNull.Value);
                    }
                    var TitleSqlParam = new SqlParameter("@Title", item.Title);
                    if (string.IsNullOrWhiteSpace(item.Title))
                    {
                        TitleSqlParam = new SqlParameter("@Title", DBNull.Value);
                    }
                    //sqlCommand.CommandText = AddReplyQuery;
                    //sqlCommand.ExecuteNonQuery();
                    SqlParameter[] sqlParameters = new SqlParameter[]
                       {
                        DescriptionSqlParam,
                        TitleSqlParam
                       };

                    var UpdateRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran,conn, AddReplyQuery, sqlParameters);
                }

                ResponseMessage = "Add successfully";
            }
            catch (Exception ex)
            {
                throw ex;
            }
            

            return amountMisMatch ? "" : ResponseMessage;
        }

        public string AddPaymentDetailItem(decimal PaymentId, List<PaymentDetailVm> modal)
        {
            string ResponseMessage = "";
            if (PaymentId == 0)
            {
                ResponseMessage = "Invalid Paymant Id";
                return ResponseMessage;
            }

            var constring = _settings.GetConnectionString();
            //constring = constring.ToString();
            var conn = new SqlConnection(constring);
            conn.Open();
            var sqlCommand = new SqlCommand
            {
                Connection = conn
            };

            bool amountMisMatch = false;

            try
            {

                foreach (var item in modal)
                {
                    //commendted 14/10/2022
                    //if (CrossSiteScriptingValidation.IsDangerousParameter(item.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(item.Title, out _))
                    //{
                    //    var dataBadReq = new
                    //    {
                    //        Status = "BadRequest",
                    //        Message = "Invalid Request.",
                    //        StatusCode = "500"
                    //    };
                    //    return "BadRequest";
                    //}
                    sqlCommand.CommandText = $"SELECT ISNULL(VATAmount,0) FROM Pms_ContractCharges WHERE ContractChargeId = {item.ContractchargeID}";

                    var isVATPayment = Convert.ToDecimal(sqlCommand.ExecuteScalar()) == item.Amount;
                    var col = isVATPayment ? "ContractchargeID" : "VATOnChargeID";

                    sqlCommand.CommandText = $@"select Amount from cms.PaymentSetting WHERE PaymentSettingId = {item.PaymentSettingId}";

                    var amount = Convert.ToDecimal(sqlCommand.ExecuteScalar());

                    if (amount != item.Amount)
                    {
                        amountMisMatch = true;
                        break;
                    }
                    if (!string.IsNullOrWhiteSpace(item.Description))
                    {
                        item.Description = _settings.RemoveSpecialChar(item.Description);
                    }

                    var AddReplyQuery = $@"INSERT INTO cms.GatewayPaymantDetail(PaymentId,Title, Description, CreatedOn,Amount,ContractchargeID,ContractPaymentID,VatOnRentContractId,VATOnChargeID,PaymentSettingId) VALUES ('{PaymentId}',@Title,@Description ,'{DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt")}',{item.Amount},{item.ContractchargeID},{item.PaymentID},{item.VatOnRentContractId},{item.VATOnChargeID},{item.PaymentSettingId});";
                    var DescriptionSqlParam = new SqlParameter("@Description", item.Description);
                    if (string.IsNullOrWhiteSpace(item.Description))
                    {
                        DescriptionSqlParam = new SqlParameter("@Description", DBNull.Value);
                    }
                    var TitleSqlParam = new SqlParameter("@Title", item.Title);
                    if (string.IsNullOrWhiteSpace(item.Title))
                    {
                        TitleSqlParam = new SqlParameter("@Title", DBNull.Value);
                    }
                    //sqlCommand.CommandText = AddReplyQuery;
                    //sqlCommand.ExecuteNonQuery();
                    var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery, DescriptionSqlParam, TitleSqlParam);
                }

                ResponseMessage = "Add successfully";
            }
            catch (Exception ex)
            {
                ResponseMessage = ex.Message;
            }
            finally
            {
                conn.Close();
            }

            return amountMisMatch ? "" : ResponseMessage;
        }

         public string UpdateTransactionIdWithSqlTransaction(SqlTransaction tran,SqlConnection conn,decimal orderId, string TransactionId)
        {
            string ResponseMessage = "";
            try
            {
                if (orderId == 0)
            {
                ResponseMessage = "Invalid order";
                return ResponseMessage;
            }

            

            var AddReplyQuery = $@"update cms.GatewayPaymant set GWRegRefNo='{TransactionId}',LastUpdateOn='{DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt")}' where PaymentId='{orderId}'";
            var UpdateRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran,conn, AddReplyQuery);
            
                ResponseMessage = "updated successfully";
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return ResponseMessage;
        }
        public string UpdateTransactionId(decimal orderId, string TransactionId)
        {
            string ResponseMessage = "";
            if (orderId == 0)
            {
                ResponseMessage = "Invalid order";
                return ResponseMessage;
            }

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var AddReplyQuery = $@"update cms.GatewayPaymant set GWRegRefNo='{TransactionId}',LastUpdateOn='{DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt")}' where PaymentId='{orderId}'";
            var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery);
            try
            {
                ResponseMessage = "updated successfully";
                return ResponseMessage;
            }
            catch (Exception ex)
            {
                ResponseMessage = ex.Message;
                return ResponseMessage;
            }
        }

        public List<PaymentSettingListVm> GetPaymentSetingIdListTransaction(SqlTransaction tran,SqlConnection conn,string GWRegRefNo)
        {
            List<PaymentSettingListVm> record = new List<PaymentSettingListVm>();
            
            var query = $@"select PaymentSettingId from cms.GatewayPaymant gp join cms.GatewayPaymantDetail gpd on gp.PaymentId=gpd.PaymentId where GWRegRefNo = @GWRegRefNo";
            var GWRegRefNoParam = new SqlParameter("@GWRegRefNo", GWRegRefNo);
            if (string.IsNullOrWhiteSpace(GWRegRefNo))
            {
                GWRegRefNoParam = new SqlParameter("@GWRegRefNo", DBNull.Value);
            }
            SqlParameter[] sqlParameter = new SqlParameter[]
            {
               GWRegRefNoParam
            };
            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran,conn, query, sqlParameter);
            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<PaymentSettingListVm>(Properties);
                record = ListToBeSend;
            }
            return record;
        }
        public List<PaymentSettingListVm> GetPaymentSetingIdList(string GWRegRefNo)
        {
            List<PaymentSettingListVm> record = new List<PaymentSettingListVm>();
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var query = $@"select PaymentSettingId from cms.GatewayPaymant gp join cms.GatewayPaymantDetail gpd on gp.PaymentId=gpd.PaymentId where GWRegRefNo = @GWRegRefNo";
            var GWRegRefNoParam = new SqlParameter("@GWRegRefNo", GWRegRefNo);
            if (string.IsNullOrWhiteSpace(GWRegRefNo))
            {
                GWRegRefNoParam = new SqlParameter("@GWRegRefNo", DBNull.Value);
            }
            var Properties = Conn.ExecuteDataTableSqlDA(conn, query, GWRegRefNoParam);
            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<PaymentSettingListVm>(Properties);
                record = ListToBeSend;
            }
            return record;
        }


        public string FinalizedPaymentTransaction(SqlTransaction tran,SqlConnection conn,string GWFinalRefNo, string GWRegRefNo, string Status, string ResponseCode, string ApprovalCode)
        {
            //log
            string ResponseMessage = "";
           
            try
            {
               
                   
                    Conn.ExecuteDataTableSqlDAWithTrnSaction(tran,conn, "SELECT 'FinalizedPayment' as t");

                    var AddReplyQuery = $@"update cms.GatewayPaymant set GWFinalRefNo='{GWFinalRefNo}',LastUpdateOn='{DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt")}', Status='{Status}',ResponseCode='{ResponseCode}' where GWRegRefNo='{GWRegRefNo}'";
                    var UpdateRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran,conn, AddReplyQuery);

                    var PaymentSettingId = GetPaymentSetingIdListTransaction(tran,conn,GWRegRefNo);
                    if (ResponseCode == "000" || ResponseCode == "200" || ResponseCode == "0")
                    {
                        foreach (var item in PaymentSettingId)
                        {

                            var UpdateQuery = $@"update cms.PaymentSetting set Paid=1, PaidOn=getdate() where PaymentSettingId='{item.PaymentSettingId}'";
                            var UpdateSettingRecord = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran,conn, UpdateQuery);
                            //    }
                            //}
                            //try
                            //{
                            //ResponseMessage = "updated successfully";
                            //try
                            //{
                            //    var subject = "Payment From Tenant";
                            //    var tittle = "Payment From Tenant";
                            //    var sedemailnotification = AddNewEmailNotification(subject, tittle, GWRegRefNo);
                            //}
                            //catch (Exception ex)
                            //{
                            //    var errorMessage = InsertErrorLog(ex.Message);
                            //}
                            //return ResponseMessage;
                        }

                        ResponseMessage = "updated successfully";
                        try
                        {
                            var subject = "Payment From Tenant";
                            var tittle = "Payment From Tenant";
                            var sedemailnotification = AddNewEmailNotificationTransaction(tran,conn,subject, tittle, GWRegRefNo);
                        }
                        catch (Exception ex)
                        {
                            var errorMessage = InsertErrorLog(ex.Message);
                        throw ex;
                        }
                    }
                
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ResponseMessage;
        }
        public string FinalizedPayment(string GWFinalRefNo, string GWRegRefNo, string Status, string ResponseCode, string ApprovalCode)
        {
            //log
            string ResponseMessage = "";

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            Conn.ExecuteDataTableSqlDA(conn, "SELECT 'FinalizedPayment' as t");

            var AddReplyQuery = $@"update cms.GatewayPaymant set GWFinalRefNo='{GWFinalRefNo}',LastUpdateOn='{DateTime.UtcNow.AddHours(4).ToString("dd-MM-yyyy hh:mm tt")}', Status='{Status}',ResponseCode='{ResponseCode}' where GWRegRefNo='{GWRegRefNo}'";
            var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery);

            var PaymentSettingId = GetPaymentSetingIdList(GWRegRefNo);
            if (ResponseCode == "000" || ResponseCode == "200" || ResponseCode == "0")
            {
                foreach (var item in PaymentSettingId)
                {

                    var UpdateQuery = $@"update cms.PaymentSetting set Paid=1, PaidOn=getdate() where PaymentSettingId='{item.PaymentSettingId}'";
                    var UpdateSettingRecord = Conn.ExecuteDataTableSqlDA(conn, UpdateQuery);
                    //    }
                    //}
                    //try
                    //{
                    //ResponseMessage = "updated successfully";
                    //try
                    //{
                    //    var subject = "Payment From Tenant";
                    //    var tittle = "Payment From Tenant";
                    //    var sedemailnotification = AddNewEmailNotification(subject, tittle, GWRegRefNo);
                    //}
                    //catch (Exception ex)
                    //{
                    //    var errorMessage = InsertErrorLog(ex.Message);
                    //}
                    //return ResponseMessage;
                }

                ResponseMessage = "updated successfully";
                try
                {
                    var subject = "Payment From Tenant";
                    var tittle = "Payment From Tenant";
                    var sedemailnotification = AddNewEmailNotification(subject, tittle, GWRegRefNo);
                }
                catch (Exception ex)
                {
                    var errorMessage = InsertErrorLog(ex.Message);
                }
            }
            return ResponseMessage;
        }

        public bool AddNewEmailNotificationTransaction(SqlTransaction tran,SqlConnection conn,string subject, string Title, string GWRegRefNo)
        {
            var caseinfo = GetCaseDetailsTransaction(tran,conn,GWRegRefNo);
            if (caseinfo == null || caseinfo.CaseNo == 0 || string.IsNullOrWhiteSpace(caseinfo.EmailId))
            {
                return false;

            }
            var email = caseinfo.EmailId;
            decimal amount = 0;
            if (caseinfo.Amount > 0)
            {
                amount = caseinfo.Amount;
            }

            // <br> case no  <a href={url}>{item.CaseId}</a>
            /* var emailbody = $@"Dear Empolyee, <br/>
                          The tenant with case No: {caseinfo.CaseNo} has made the payment with the amount {amount}. Please  proceed with payment verfication. 
                         System generated email";
            */
            // string CMSLocationPath = new ConfigValues().GetCMSBaseURL();
            // string CMSurl = CMSLocationPath + "/Admin/Case/Edit/" + caseinfo.CaseNo;
            var emailbody = $@"Dear Empolyee, <br/>
                        The tenant with Case No: {caseinfo.CaseNo}</a> , Assigned to: {caseinfo.FullName}  has made the payment of amount: {amount}. Please proceed with payment verification.  <br/>
                        System generated email";

            var timeZone = "Arabian Standard Time";

            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            string currentDate = DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd hh:mm:ss tt");
            var batch = email + "-" + DateTime.UtcNow.AddHours(4).ToString("ddMMYY");
            var query = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title, UserId, Description, Batch,CurrentStatus,CreatedOn,Email,UserType,Popup, RecordType, RecordId, ShortDescription) VALUES (0,'2',0,@emailTitle,{caseinfo.UserId},@emailbody,'{batch}','Queue','{currentDate}',@email,'Assignee',0, 'Payment For Contract',{caseinfo.CaseNo},@emailbody) SELECT SCOPE_IDENTITY() as InsertRecordId;";
            bool logged = false;            
            var bodyParam = new SqlParameter("@emailbody", emailbody);
            if (string.IsNullOrWhiteSpace(emailbody))
            {
                bodyParam = new SqlParameter("@emailbody", DBNull.Value);
            }
            var subjectParam = new SqlParameter("@emailsubject", subject);
            if (string.IsNullOrWhiteSpace(subject))
            {
                subjectParam = new SqlParameter("@emailsubject", DBNull.Value);
            }
            var titleParam = new SqlParameter("@emailTitle", Title);
            if (string.IsNullOrWhiteSpace(Title))
            {
                titleParam = new SqlParameter("@emailTitle", DBNull.Value);
            }
            var emailParam = new SqlParameter("@email", email);
            if (string.IsNullOrWhiteSpace(email))
            {
                emailParam = new SqlParameter("@email", DBNull.Value);
            }
            SqlParameter[] sqlParameters = new SqlParameter[]
                       {
                      bodyParam,
                      subjectParam,
                      titleParam,
                      emailParam
                       };
            var excutequery = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran,conn, query,sqlParameters);
            if (excutequery != null)
            {
                logged = true;
            }
            return logged;
        }
        public bool AddNewEmailNotification(string subject, string Title, string GWRegRefNo)
        {
            var caseinfo = GetCaseDetails(GWRegRefNo);
            if (caseinfo == null || caseinfo.CaseNo == 0 || string.IsNullOrWhiteSpace(caseinfo.EmailId))
            {
                return false;

            }
            var email = caseinfo.EmailId;
            decimal amount = 0;
            if (caseinfo.Amount > 0)
            {
                amount = caseinfo.Amount;
            }

            // <br> case no  <a href={url}>{item.CaseId}</a>
            /* var emailbody = $@"Dear Empolyee, <br/>
                          The tenant with case No: {caseinfo.CaseNo} has made the payment with the amount {amount}. Please  proceed with payment verfication. 
                         System generated email";
            */           
           // string CMSLocationPath = new ConfigValues().GetCMSBaseURL();
           // string CMSurl = CMSLocationPath + "/Admin/Case/Edit/" + caseinfo.CaseNo;
            var emailbody = $@"Dear Empolyee, <br/>
                        The tenant with Case No: {caseinfo.CaseNo}</a> , Assigned to: {caseinfo.FullName}  has made the payment of amount: {amount}. Please proceed with payment verification.  <br/>
                        System generated email";

            var timeZone = "Arabian Standard Time";

            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            string currentDate = DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd hh:mm:ss tt");
            var batch = email + "-" + DateTime.UtcNow.AddHours(4).ToString("ddMMYY");
            var query = $@"INSERT INTO app.Notifications(IsRead,NotificationTypeId, Sent, Title, UserId, Description, Batch,CurrentStatus,CreatedOn,Email,UserType,Popup, RecordType, RecordId, ShortDescription) VALUES (0,'2',0,@emailTitle,{caseinfo.UserId},@emailbody,'{batch}','Queue','{currentDate}',@email,'Assignee',0, 'Payment For Contract',{caseinfo.CaseNo},@emailbody) SELECT SCOPE_IDENTITY() as InsertRecordId;";
            bool logged = false;
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var bodyParam = new SqlParameter("@emailbody", emailbody);
            if (string.IsNullOrWhiteSpace(emailbody))
            {
                bodyParam = new SqlParameter("@emailbody", DBNull.Value);
            }
            var subjectParam = new SqlParameter("@emailsubject", subject);
            if (string.IsNullOrWhiteSpace(subject))
            {
                subjectParam = new SqlParameter("@emailsubject", DBNull.Value);
            }
            var titleParam = new SqlParameter("@emailTitle", Title);
            if (string.IsNullOrWhiteSpace(Title))
            {
                titleParam = new SqlParameter("@emailTitle", DBNull.Value);
            }
            var emailParam = new SqlParameter("@email", email);
            if (string.IsNullOrWhiteSpace(email))
            {
                emailParam = new SqlParameter("@email", DBNull.Value);
            }
            var excutequery = Conn.ExecuteDataTableSqlDA(conn, query, bodyParam, subjectParam, titleParam, emailParam);
            if (excutequery != null)
            {
                logged = true;
            }
            return logged;
        }

        public CaseDetailsVm GetCaseDetailsTransaction(SqlTransaction tran,SqlConnection conn,string GWRegRefNo)
        {
            CaseDetailsVm caseDetailsVm = new CaseDetailsVm();
            

            var casedetailquery = $@"Select c.CaseNo, Isnull(c.AssignId, 0) As AssigneeId, u.EmailId,u.FullName, Isnull(cu.UserId, 0) as UserId,  Isnull(cda.RenewalContractid, 0) as ContractId,Isnull(gp.TotalAmount, 0) as Amount From CMS.Cases c Inner Join CMS.ContractDueAction cda on cda.CaseId = c.CaseNo inner join  CMS.GatewayPaymant gp on gp.ContractId = cda.RenewalContractid inner join cms.users u on u.UserId = c.AssignId inner join cms.Customer cu on cu.CustomerId = c.CaseOwnerId where gp.GWRegRefNo =  @pGWRegRefNo and gp.Status = 'Success'";
            var sqlParameter = new SqlParameter("@pGWRegRefNo", GWRegRefNo);
            SqlParameter[] sqlParametersArray = new SqlParameter[]
            {
                sqlParameter
            };
            var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran,conn, casedetailquery, sqlParametersArray);
            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseDetailsVm>(Properties);
                caseDetailsVm = ListToBeSend.FirstOrDefault();
            }
            return caseDetailsVm;
        }
        public CaseDetailsVm GetCaseDetails(string GWRegRefNo)
        {
            CaseDetailsVm caseDetailsVm = new CaseDetailsVm();
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            var casedetailquery = $@"Select c.CaseNo, Isnull(c.AssignId, 0) As AssigneeId, u.EmailId,u.FullName, Isnull(cu.UserId, 0) as UserId,  Isnull(cda.RenewalContractid, 0) as ContractId,Isnull(gp.TotalAmount, 0) as Amount From CMS.Cases c Inner Join CMS.ContractDueAction cda on cda.CaseId = c.CaseNo inner join  CMS.GatewayPaymant gp on gp.ContractId = cda.RenewalContractid inner join cms.users u on u.UserId = c.AssignId inner join cms.Customer cu on cu.CustomerId = c.CaseOwnerId where gp.GWRegRefNo =  @pGWRegRefNo and gp.Status = 'Success'";
            var sqlParameter = new SqlParameter("@pGWRegRefNo", GWRegRefNo);
            var Properties = Conn.ExecuteDataTableSqlDA(conn, casedetailquery, sqlParameter);
            if (Properties.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseDetailsVm>(Properties);
                caseDetailsVm = ListToBeSend.FirstOrDefault();
            }
            return caseDetailsVm;
        }
        public bool InsertErrorLog(string error)
        {

            bool logged = false;
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

    }
}
