 using FABTenantApi.Classes;
using FABTenantD2Api.Classes;
using FABTenantD2Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FABTenantD2Api.Controllers
{
    [Route("api/Tenant/[controller]")]
    [ApiController]
    public class CaseController : ControllerBase
    {
        private IConfiguration _config;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        private readonly ConfigValues configValues;
        private readonly INotificationRepository _notificationRepository;
        public CaseController(IConfiguration config, ITokenService tokenService, ITenantRepository tenantRepository, INotificationRepository notificationRepository)
        {
            _config = config;
            _tenantRepository = tenantRepository;
            _notificationRepository = notificationRepository;
            _tokenService = tokenService;
            _settings = new Settings();
            configValues = new ConfigValues();
        }

        [Route("AddRenewalRequest")]
        [HttpPost]
        public ActionResult<string> AddRenewalRequest(AddCaseRequest model)
        {
            //try
            //{
            ConfigValues configValues = new ConfigValues();
            UserInfo UserData = new UserInfo();
            if (User.Identity.IsAuthenticated)
            {
                if (CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.StartDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.EndDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.Duration, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.VacatingDate, out _))
                {
                    var dataBadReq = new
                    {
                        Status = "BadRequest",
                        Message = "Invalid Request.",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
                SqlTransaction sqlTran = null;
                try
                {
                    using (var con = new SqlConnection(_settings.GetConnectionString()))
                    {
                        con.Open();
                        sqlTran = con.BeginTransaction();
                        var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                        int? UserId = _settings.GetUserIdByToken(_bearer_token);
                        if (UserId == null)
                        {
                            var data = new
                            {
                                Status = "Invalid",
                                Message = "InvalidSession."
                            };
                            return Unauthorized(data);
                        }
                        // var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                        var TenantID = GetTenantId(UserId.Value);

                        var contractTenantId = ContractInTenantIdTransaction(model.ContractId, TenantID, sqlTran, con);
                        var RefNumber = _tenantRepository.GetRefNumber(model.ContractId, sqlTran, con);
                        if (!contractTenantId)
                        {
                            var data1 = new
                            {
                                Status = "Invalid",
                                Message = "Contract not found for this user, please try with valid user."
                            };
                            return Ok(data1);
                        }

                        // Get User record
                        var GetUserValueQuery = $@"select UserId, Email,Mobile from app.Users where UserId={UserId}";

                        var RequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetUserValueQuery);

                        if (RequestDetail.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<UserInfo>(RequestDetail);
                            UserData = ListToBeSend.FirstOrDefault();
                        }

                        // insert record to cms.customer data
                        //insert data
                        MaxNumber maxNumber = new MaxNumber();
                        var GetMaxValueQuery = $@"SELECT ISNULL(MAX(CaseNo),0) as MaxCase FROM cms.Cases";
                        var MaxRequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetMaxValueQuery);

                        if (MaxRequestDetail.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<MaxNumber>(MaxRequestDetail);
                            maxNumber = ListToBeSend.FirstOrDefault();
                        }
                        int GernateCaseNumber = (maxNumber.MaxCase) + 1;

                        int? CaseEmirate = 0;
                        int? CasePropertyId = 0;
                        var GetEmirateId = $@"select EmirateID, con.PropertyID from Pms_Contract con
                                        join Pms_Property prop on prop.PropertyID= con.PropertyID
                                        where ContractID={model.ContractId}";
                        var EmirateRequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetEmirateId);
                        if (EmirateRequestDetail.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<CaseEmirateVm>(EmirateRequestDetail);
                            CaseEmirate = ListToBeSend.Select(p => p.EmirateID).FirstOrDefault();
                            CasePropertyId = ListToBeSend.Select(p => p.PropertyID).FirstOrDefault();
                        }

                        // Add and check user exist in cms.customer or not if not exist we add the user
                        var CaseOwnerId = _tenantRepository.CaseOwnerIdTransaction(sqlTran, con, UserId.Value, UserData.Email, UserData.Mobile);
                        // Insert data to cms.cases
                        var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo, PropertyId,EmirateId, CaseTypeId,  Dated, ContactTimeId,  ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId, OtherContactPersonName,OtherContactPersonMobile, RoleId,Unitrefno)

                    VALUES ({GernateCaseNumber},{CasePropertyId},{CaseEmirate} ,(select id from cms.CaseTypes where code={configValues.RSCaseType}), '{DateTime.UtcNow.AddHours(4)}',(select top 1  Id from cms.ContactTimings),

    ISNULL((Select top 1 SLAid from cms.SLARulesSet where CategoryId = (select top 1 id from cms.CaseCategory where code={configValues.RSCaseCategory}) and SubCategoryId = (select top 1 id from cms.CaseSubCategory where code={configValues.RSCaseSubCategory}) and TypeId= 1),0),


@CaseOwnerId,@Email,@Mobile, @Mobile,@Mobile, (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus},@Email,@Mobile,1,'{RefNumber}')";

                        // ISNULL((Select SLAid from cms.SLARulesSet where CategoryId = {brCaseCaregory} and SubCategoryId = {brCaseSubCategory} and TypeId= 1 and EmirateId = {CaseEmirate}),0),
                        //27-Aug-2022
                        var CaseOwnerIdParam = new SqlParameter("@CaseOwnerId", CaseOwnerId);//add paramter
                        var EmailParam = new SqlParameter("@Email", UserData.Email);//add paramter
                        if (string.IsNullOrWhiteSpace(UserData.Email))
                        {
                            EmailParam = new SqlParameter("@Email", DBNull.Value);

                        }
                        var MobileParam = new SqlParameter("@Mobile", UserData.Mobile);//add paramter
                        if (string.IsNullOrWhiteSpace(UserData.Mobile))
                        {
                            MobileParam = new SqlParameter("@Mobile", DBNull.Value);

                        }
                        SqlParameter[] sqlParameter = new SqlParameter[]
                       {
                         new SqlParameter("@CaseOwnerId", CaseOwnerId),
                         new SqlParameter("@Email", UserData.Email),
                         new SqlParameter("@Mobile", UserData.Mobile)
                       };

                        var RequestDetail2 = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, AddRequestQuery, sqlParameter);

                        // INSERT data CMS.CaseServiceRequest
                        var SaveRequestQuery = $@"INSERT INTO CMS.CaseServiceRequest(Description, ProposedRemedy, CircumstanceId,  CaseCategouryId, CaseSubCatagouryId, ReasonForVocation, CaseId)
                    VALUES ('Renewal Service Request','Renewal Service Request',(select top 1 Id  from cms.Circumstances), (select id from cms.CaseCategory where code={configValues.RSCaseCategory}),(select id from cms.CaseSubCategory where code={configValues.RSCaseSubCategory}),'Renewal Service Request' ,{GernateCaseNumber});";
                        var RequestDetail3 = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, SaveRequestQuery);

                        var casetype = 4;
                        var casetypequery = $@"select Top 1 id as CaseTypeId from cms.CaseTypes where code={configValues.RSCaseType}";
                        var casetypeget = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, casetypequery);
                        if (casetypeget != null && casetypeget.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<CaseTypeCodeVm>(casetypeget);
                            casetype = ListToBeSend.Select(p => p.CaseTypeId).FirstOrDefault();
                        }

                        //update contract Action Due
                        if (model.DueActionId != null)
                        {
                            var UpdateContractCase = _tenantRepository.UpdateContractCaseIdNew(model.DueActionId, GernateCaseNumber, sqlTran, con);
                            var UpdateStage = _tenantRepository.UpdateStageIdTransaction(model.DueActionId, 2, sqlTran, con);
                        }

                        // Insert data to cms.CaseContractDetail
                        //var CaseServiceRequestQuery = $@"INSERT INTO CMS.CaseContractDetail(ContractId, StartDate,EndDate, CaseNo,CreatedOn,Type, Duration)
                        //    VALUES ({model.ContractId} ,'{model.StartDate}','{model.EndDate}',  {GernateCaseNumber},'{DateTime.Now}','Renewal Service Request','{model.Duration}')";
                        var CaseServiceRequestQuery = $@"INSERT INTO CMS.CaseContractDetail(ContractId, StartDate,EndDate, CaseNo,CreatedOn,Type, Duration)
                    VALUES ({model.ContractId} ,@StartDate,@EndDate,  {GernateCaseNumber},'{DateTime.UtcNow.AddHours(4)}','Renewal Service Request',@Duration)";

                        //27-Aug-2022

                        var StartDateParam = new SqlParameter("@StartDate", model.StartDate);//add paramter  
                        if (string.IsNullOrWhiteSpace(model.StartDate))
                        {
                            StartDateParam = new SqlParameter("@StartDate", DBNull.Value);
                        }
                        var EndDateParam = new SqlParameter("@EndDate", model.EndDate);//add paramter  
                        if (string.IsNullOrWhiteSpace(model.EndDate))
                        {
                            EndDateParam = new SqlParameter("@EndDate", DBNull.Value);
                        }
                        var DurationParam = new SqlParameter("@Duration", model.Duration);//add paramter  
                        if (string.IsNullOrWhiteSpace(model.Duration))
                        {
                            DurationParam = new SqlParameter("@Duration", DBNull.Value);
                        }
                        SqlParameter[] CaseRequestsqlParameters = new SqlParameter[]
                       {
                           StartDateParam,
                           EndDateParam,
                           DurationParam
                       };
                        var CaseServiceRequestQueryDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, CaseServiceRequestQuery, CaseRequestsqlParameters);

                        var WorkGroupId = _tenantRepository.UpdateCaseUserIdTransaction(sqlTran, con, configValues.ServiceRequestRenewalCategory, configValues.ServiceRequestRenewalSubCategory, configValues.ServiceRequestCaseTypeId, CaseEmirate, CasePropertyId, GernateCaseNumber);
                        //if (UpdateUser != null)
                        //{
                        //    var Email = _tenantRepository.CaseOwnerEmailTransaction(UpdateUser, sqlTran, con);
                        //    var AddEmailNotification = _tenantRepository.prepareAndAddEmailNotificationTransaction(configValues.RSCaseType, configValues.ServiceRequestRenewalCategory, CaseEmirate, 6, GernateCaseNumber, Email, sqlTran, con);
                        //}

                        var CaseInfo = _tenantRepository.GetCaseInfoTransaction(GernateCaseNumber, sqlTran, con);
                        //Hook code is setup in  cms.NotificationTypes and column  name is code
                        var SendNotification = _tenantRepository.prepareAndAddPushNotificationTransaction(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 6, GernateCaseNumber, UserId.Value, "Tenant", sqlTran, con);

                        var AddTenantEmailNotification = _tenantRepository.prepareAndAddEmailNotificationTransaction(sqlTran, con, 1, CaseInfo.CaseCategouryId, CaseEmirate, 1, GernateCaseNumber, UserData.Email);


                        var AddTicket = _tenantRepository.AddTicketNewSerivceRequestCaseTransaction(sqlTran, con, GernateCaseNumber, UserId, CaseOwnerId);


                        AddRequestDetailVm CheckserviceRequest = new AddRequestDetailVm();
                        var GetCaseQuery = $@"select CaseNo from cms.Cases where CaseNo={GernateCaseNumber}";


                        var CheckCase = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetCaseQuery);

                        if (CheckCase.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<AddRequestDetailVm>(CheckCase);
                            CheckserviceRequest = ListToBeSend.FirstOrDefault();

                            var UpdateAssignee = _tenantRepository.AssignPMAssineeeTransaction(sqlTran, con, GernateCaseNumber, "Insert", true);
                            var getCaseInfo = _tenantRepository.GetCaseDetailInfoTransaction(sqlTran, con, GernateCaseNumber);

                            if (!string.IsNullOrEmpty(WorkGroupId.ToString()) && UpdateAssignee)
                            {

                                //12-11-2024 workgroup email was wrong, commented below row 
                                //string WorkGroupEmail = _tenantRepository.CaseOwnerEmailTransaction(sqlTran, con, WorkGroupId);
                                //var assignee = getCaseInfo.Assignee;
                                //12-11-2024 workgroup email was wrong, Uncommented below row 
                                string WorkGroupEmail = getCaseInfo.WorkGroupEmail;
                                //var category = getCaseInfo.CategoryName;
                                //var title = "New Service Request";
                                //var subject = "New Service Request Assigned";

                                //var body = $@"Dear Team, <br/> A new case no: {GernateCaseNumber} is generated with category {category} and is assigned to {assignee}. <br/>  <br/> System Generated Notification.";
                                var AssigneeTemplate = _notificationRepository.GetTemplateInfo(37);
                                var body = _notificationRepository.NotificationBodyReplacement(sqlTran, con, AssigneeTemplate.BodyContent, GernateCaseNumber);
                                var sendworkGroupEmail = _tenantRepository.AddNewEmailNotificationTransaction(sqlTran, con, body, UserId.Value, AssigneeTemplate.Subject, WorkGroupEmail, AssigneeTemplate.Subject, GernateCaseNumber);

                            }
                        }

                        if (CheckserviceRequest != null)
                        {
                            sqlTran.Commit();

                            var data = new
                            {
                                Status = "Ok",
                                AddServiceRequest = CheckserviceRequest,
                                Message = "Added Successfully"
                            };
                            return Ok(data);
                        }
                        else
                        {
                            sqlTran.Rollback();

                            var data = new
                            {
                                Status = "NotFound",
                                AddServiceRequest = CheckserviceRequest,
                                Message = "No Data Found."
                            };
                            return NotFound(data);
                        }


                    }

                }
                catch (Exception ex)
                {
                    sqlTran.Rollback();
                    var data = new
                    {
                        Status = "Processing Error",
                        StatusCode = "501",
                        Message = "Internal Server Error" + ex.Message,

                    };
                    return BadRequest(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                };
                return Unauthorized(data);
            }
            //}

            //catch (Exception ex)
            //{
            //    var data = new
            //    {
            //        Status = "Processing Error",
            //        StatusCode = "501",
            //        Message = "Internal Server Error",

            //    };
            //    return BadRequest(data);
            //}
        }


        //        [Route("AddRenewalRequestCopy")]
//        [HttpPost]
//        public ActionResult<string> AddRenewalRequestCopy(AddCaseRequest model)
//        {
//            //try
//            //{
//            ConfigValues configValues = new ConfigValues();
//            UserInfo UserData = new UserInfo();
//            if (User.Identity.IsAuthenticated)
//            {
//                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
//                int? UserId = _settings.GetUserIdByToken(_bearer_token);
//                if (UserId == null)
//                {
//                    var data = new
//                    {
//                        Status = "Invalid",
//                        Message = "InvalidSession."
//                    };
//                    return Unauthorized(data);
//                }
//                var tenantId = _tenantRepository.GetTenantId(UserId.Value);
//                var TenantID = GetTenantId(UserId.Value);
//                var contractTenantId = ContractInTenantId(model.ContractId, TenantID);

//                if (!contractTenantId)
//                {
//                    var data1 = new
//                    {
//                        Status = "Invalid",
//                        Message = "Contract not found for this user; please try with valid user."
//                    };
//                    return Ok(data1);
//                }
//                if (CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.StartDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.EndDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.Duration, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.VacatingDate, out _))
//                {
//                    var dataBadReq = new
//                    {
//                        Status = "BadRequest",
//                        Message = "Invalid Request.",
//                        StatusCode = "500"
//                    };
//                    return BadRequest(dataBadReq);
//                }
//                // Get User record
//                var GetUserValueQuery = $@"select UserId, Email,Mobile from app.Users where UserId={UserId}";
//                var constring = _settings.GetConnectionString();
//                constring = constring.ToString();
//                SqlConnection conn = new SqlConnection(constring);
//                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetUserValueQuery);

//                if (RequestDetail.Rows.Count > 0)
//                {
//                    var ListToBeSend = Conn.ToListof<UserInfo>(RequestDetail);
//                    UserData = ListToBeSend.FirstOrDefault();
//                }

//                // insert record to cms.customer data
//                //insert data
//                MaxNumber maxNumber = new MaxNumber();
//                var GetMaxValueQuery = $@"SELECT ISNULL(MAX(CaseNo),0) as MaxCase FROM cms.Cases";
//                var MaxRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetMaxValueQuery);

//                if (MaxRequestDetail.Rows.Count > 0)
//                {
//                    var ListToBeSend = Conn.ToListof<MaxNumber>(MaxRequestDetail);
//                    maxNumber = ListToBeSend.FirstOrDefault();
//                }
//                int GernateCaseNumber = (maxNumber.MaxCase) + 1;

//                int? CaseEmirate = 0;
//                int? CasePropertyId = 0;
//                var GetEmirateId = $@"select EmirateID, con.PropertyID from Pms_Contract con
//                                        join Pms_Property prop on prop.PropertyID= con.PropertyID
//                                        where ContractID={model.ContractId}";
//                var EmirateRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetEmirateId);
//                if (EmirateRequestDetail.Rows.Count > 0)
//                {
//                    var ListToBeSend = Conn.ToListof<CaseEmirateVm>(EmirateRequestDetail);
//                    CaseEmirate = ListToBeSend.Select(p => p.EmirateID).FirstOrDefault();
//                    CasePropertyId = ListToBeSend.Select(p => p.PropertyID).FirstOrDefault();
//                }


//                // Add and check user exist in cms.customer or not if not exist we add the user
//                var CaseOwnerId = _tenantRepository.CaseOwnerId(UserId.Value, UserData.Email, UserData.Mobile);
//                // Insert data to cms.cases
//                var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo, PropertyId,EmirateId, CaseTypeId,  Dated, ContactTimeId,  ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId, OtherContactPersonName,OtherContactPersonMobile, RoleId)

//                    VALUES ({GernateCaseNumber},{CasePropertyId},{CaseEmirate} ,(select id from cms.CaseTypes where code={configValues.RSCaseType}), '{DateTime.UtcNow.AddHours(4)}',(select top 1  Id from cms.ContactTimings),

//    ISNULL((Select top 1 SLAid from cms.SLARulesSet where CategoryId = (select top 1 id from cms.CaseCategory where code={configValues.RSCaseCategory}) and SubCategoryId = (select top 1 id from cms.CaseSubCategory where code={configValues.RSCaseSubCategory}) and TypeId= 1 and EmirateId = {CaseEmirate}),0),


//@CaseOwnerId,@Email,@Mobile, @Mobile,@Mobile, (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus},@Email,@Mobile,1)";
//                // ISNULL((Select SLAid from cms.SLARulesSet where CategoryId = {brCaseCaregory} and SubCategoryId = {brCaseSubCategory} and TypeId= 1 and EmirateId = {CaseEmirate}),0),
//                //27-Aug-2022
//                var CaseOwnerIdParam = new SqlParameter("@CaseOwnerId", CaseOwnerId);//add paramter
//                var EmailParam = new SqlParameter("@Email", UserData.Email);//add paramter
//                if (string.IsNullOrWhiteSpace(UserData.Email))
//                {
//                    EmailParam = new SqlParameter("@Email", DBNull.Value);

//                }
//                var MobileParam = new SqlParameter("@Mobile", UserData.Mobile);//add paramter
//                if (string.IsNullOrWhiteSpace(UserData.Mobile))
//                {
//                    MobileParam = new SqlParameter("@Mobile", DBNull.Value);

//                }

//                var RequestDetail2 = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery, CaseOwnerIdParam, EmailParam, MobileParam);

//                // INSERT data CMS.CaseServiceRequest
//                var SaveRequestQuery = $@"INSERT INTO CMS.CaseServiceRequest(Description, ProposedRemedy, CircumstanceId,  CaseCategouryId, CaseSubCatagouryId, ReasonForVocation, CaseId)
//                    VALUES ('Renewal Service Request','Renewal Service Request',(select top 1 Id  from cms.Circumstances), (select id from cms.CaseCategory where code={configValues.RSCaseCategory}),(select id from cms.CaseSubCategory where code={configValues.RSCaseSubCategory}),'Renewal Service Request' ,{GernateCaseNumber});";
//                var RequestDetail3 = Conn.ExecuteDataTableSqlDA(conn, SaveRequestQuery);
//                try
//                {
//                    var casetype = 4;
//                    var casetypequery = $@"select Top 1 id as CaseTypeId from cms.CaseTypes where code={configValues.RSCaseType}";
//                    var casetypeget = Conn.ExecuteDataTableSqlDA(conn, casetypequery);
//                    if (casetypeget != null && casetypeget.Rows.Count > 0)
//                    {
//                        var ListToBeSend = Conn.ToListof<CaseTypeCodeVm>(casetypeget);
//                        casetype = ListToBeSend.Select(p => p.CaseTypeId).FirstOrDefault();
//                    }
//                    //_tenantRepository.SetSLA(casetype, Convert.ToInt32(configValues.RSCaseCategory), Convert.ToInt32(configValues.RSCaseSubCategory), Convert.ToInt32(CaseEmirate), GernateCaseNumber);

//                }
//                catch (Exception ex)
//                {
//                    _tenantRepository.InsertErrorLog(ex.Message);

//                }
//                //update contract Action Due
//                if (model.DueActionId != null)
//                {
//                    var UpdateContractCase = _tenantRepository.UpdateContractCaseId(model.DueActionId, GernateCaseNumber);
//                    var UpdateStage = _tenantRepository.UpdateStageId(model.DueActionId, 2);
//                }

//                // Insert data to cms.CaseContractDetail
//                //var CaseServiceRequestQuery = $@"INSERT INTO CMS.CaseContractDetail(ContractId, StartDate,EndDate, CaseNo,CreatedOn,Type, Duration)
//                //    VALUES ({model.ContractId} ,'{model.StartDate}','{model.EndDate}',  {GernateCaseNumber},'{DateTime.Now}','Renewal Service Request','{model.Duration}')";
//                var CaseServiceRequestQuery = $@"INSERT INTO CMS.CaseContractDetail(ContractId, StartDate,EndDate, CaseNo,CreatedOn,Type, Duration)
//                    VALUES ({model.ContractId} ,@StartDate,@EndDate,  {GernateCaseNumber},'{DateTime.UtcNow.AddHours(4)}','Renewal Service Request',@Duration)";

//                //27-Aug-2022

//                var StartDateParam = new SqlParameter("@StartDate", model.StartDate);//add paramter  
//                if (string.IsNullOrWhiteSpace(model.StartDate))
//                {
//                    StartDateParam = new SqlParameter("@StartDate", DBNull.Value);
//                }
//                var EndDateParam = new SqlParameter("@EndDate", model.EndDate);//add paramter  
//                if (string.IsNullOrWhiteSpace(model.EndDate))
//                {
//                    EndDateParam = new SqlParameter("@EndDate", DBNull.Value);
//                }
//                var DurationParam = new SqlParameter("@Duration", model.Duration);//add paramter  
//                if (string.IsNullOrWhiteSpace(model.Duration))
//                {
//                    DurationParam = new SqlParameter("@Duration", DBNull.Value);
//                }

//                var CaseServiceRequestQueryDetail = Conn.ExecuteDataTableSqlDA(conn, CaseServiceRequestQuery, StartDateParam, EndDateParam, DurationParam);

//                var UpdateUser = _tenantRepository.UpdateCaseUserId(configValues.ServiceRequestRenewalCategory, configValues.ServiceRequestRenewalSubCategory, configValues.ServiceRequestCaseTypeId, CaseEmirate,CasePropertyId, GernateCaseNumber);
//                if (UpdateUser != null)
//                {
//                    var Email = _tenantRepository.CaseOwnerEmail(UpdateUser);
//                    var AddEmailNotification = _tenantRepository.prepareAndAddEmailNotification(configValues.RSCaseType, configValues.ServiceRequestRenewalCategory, CaseEmirate, 6, GernateCaseNumber, Email);
//                }

//                var CaseInfo = _tenantRepository.GetCaseInfo(GernateCaseNumber);
//                //Hook code is setup in  cms.NotificationTypes and column  name is code
//                var SendNotification = _tenantRepository.prepareAndAddPushNotification(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 6, GernateCaseNumber, UserId.Value, "Tenant");

//                var AddTicket = _tenantRepository.AddTicketNewSerivceRequestCase(GernateCaseNumber, UserId, CaseOwnerId);


//                AddRequestDetailVm CheckserviceRequest = new AddRequestDetailVm();
//                var GetCaseQuery = $@"select CaseNo from cms.Cases where CaseNo={GernateCaseNumber}";


//                var CheckCase = Conn.ExecuteDataTableSqlDA(conn, GetCaseQuery);

//                if (CheckCase.Rows.Count > 0)
//                {
//                    var ListToBeSend = Conn.ToListof<AddRequestDetailVm>(CheckCase);
//                    CheckserviceRequest = ListToBeSend.FirstOrDefault();

//                    var AssignContractor = _tenantRepository.AssignPMAssineee(GernateCaseNumber, "Insert", true);
//                    var getCaseInfo = _tenantRepository.GetCaseDetailInfo(GernateCaseNumber);
//                    if (getCaseInfo != null)
//                    {
//                        var assignee = getCaseInfo.Assignee;
//                        var email = getCaseInfo.WorkGroupEmail;
//                        var category = getCaseInfo.CategoryName;

//                        // string CMSLocationPath = new ConfigValues().GetCMSBaseURL() + "/Admin/Case/Edit/";
//                        //string CMSurl = CMSLocationPath + GernateCaseNumber;
//                        var title = "New Service Request";
//                        var subject = "New Service Request";
//                        //<a href={CMSurl}></a>
//                        var AssigneeTemplate = _notificationRepository.GetTemplateInfo(37);
//                        var body = _notificationRepository.NotificationBodyReplacement(null, conn, AssigneeTemplate.BodyContent, GernateCaseNumber);
//                        //var body = $@"Dear Team, <br/> A new Case No: {GernateCaseNumber} of Category:  {category} is generated and Assigned to: {assignee}.  <br/> System Generated Notification";
//                        try
//                        {
//                            var sendworkGroupEmail = _tenantRepository.AddNewEmailNotification(body, UserId.Value, subject, email, title, GernateCaseNumber);
//                        }
//                        catch (Exception ex)
//                        {
//                            var logged = _tenantRepository.InsertErrorLog(ex.Message); ;
//                        }
//                    }
//                }

//                if (CheckserviceRequest != null)
//                {
//                    var data = new
//                    {
//                        Status = "Ok",
//                        AddServiceRequest = CheckserviceRequest,
//                        Message = "Added Successfully"
//                    };
//                    return Ok(data);
//                }
//                else
//                {
//                    var data = new
//                    {
//                        Status = "NotFound",
//                        AddServiceRequest = CheckserviceRequest,
//                        Message = "No Data Found."
//                    };
//                    return NotFound(data);
//                }
//            }
//            else
//            {
//                var data = new
//                {
//                    Status = "Invalid",
//                    Message = "InvalidSession."
//                };
//                return Unauthorized(data);
//            }
//            //}

//            //catch (Exception ex)
//            //{
//            //    var data = new
//            //    {
//            //        Status = "Processing Error",
//            //        StatusCode = "501",
//            //        Message = "Internal Server Error",

//            //    };
//            //    return BadRequest(data);
//            //}
//        }


        //comment..

        [Route("AddExtensionRequest")]
        [HttpPost]
        public ActionResult<string> AddExtensionRequest(AddCaseRequest model)
        {
            try
            {
                ConfigValues configValues = new ConfigValues();
                UserInfo UserData = new UserInfo();
                if (User.Identity.IsAuthenticated)
                {
                    if (CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.StartDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.EndDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.Duration, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.VacatingDate, out _))
                    {
                        var dataBadReq = new
                        {
                            Status = "BadRequest",
                            Message = "Invalid Request.",
                            StatusCode = "500"
                        };
                        return BadRequest(dataBadReq);
                    }


                    SqlTransaction sqlTran = null;
                    try
                    {
                        using (var con = new SqlConnection(_settings.GetConnectionString()))
                        {
                            con.Open();
                            sqlTran = con.BeginTransaction();
                            var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                            int? UserId = _settings.GetUserIdByToken(_bearer_token);
                            if (UserId == null)
                            {
                                var data = new
                                {
                                    Status = "Invalid",
                                    Message = "InvalidSession."
                                };
                                return Unauthorized(data);
                            }
                            var TenantID = GetTenantId(UserId.Value);
                            var contractTenantId = ContractInTenantIdTransaction(model.ContractId, TenantID, sqlTran, con);
                            var RefNumber = _tenantRepository.GetRefNumber(model.ContractId, sqlTran, con);
                            if (!contractTenantId)
                            {
                                var data1 = new
                                {
                                    Status = "Invalid",
                                    Message = "Contract not found for this user; please try with valid user."
                                };
                                return Ok(data1);
                            }
                            //    var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                            // Get User record

                            var GetUserValueQuery = $@"select UserId, Email,Mobile from app.Users where UserId={UserId}";

                            var RequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetUserValueQuery);

                            if (RequestDetail.Rows.Count > 0)
                            {
                                var ListToBeSend = Conn.ToListof<UserInfo>(RequestDetail);
                                UserData = ListToBeSend.FirstOrDefault();
                            }

                            // insert record to cms.customer data


                            int? CaseEmirate = 0;
                            int? CasePropertyId = 0;
                            var GetEmirateId = $@"select EmirateID,con.PropertyID from Pms_Contract con join Pms_Property prop on prop.PropertyID= con.PropertyID where ContractID={model.ContractId} and con.TenantID = {TenantID}";
                            var EmirateRequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetEmirateId);
                            if (EmirateRequestDetail.Rows.Count > 0)
                            {
                                var ListToBeSend = Conn.ToListof<CaseEmirateVm>(EmirateRequestDetail);
                                CaseEmirate = ListToBeSend.Select(p => p.EmirateID).FirstOrDefault();
                                CasePropertyId = ListToBeSend.Select(p => p.PropertyID).FirstOrDefault();
                            }

                            //insert data
                            MaxNumber maxNumber = new MaxNumber();
                            var GetMaxValueQuery = $@"SELECT ISNULL(MAX(CaseNo),0) as MaxCase FROM cms.Cases";
                            var MaxRequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetMaxValueQuery);

                            if (MaxRequestDetail.Rows.Count > 0)
                            {
                                var ListToBeSend = Conn.ToListof<MaxNumber>(MaxRequestDetail);
                                maxNumber = ListToBeSend.FirstOrDefault();
                            }
                            int GernateCaseNumber = (maxNumber.MaxCase) + 1;


                            // Add and check user exist in cms.customer or not if not exist we add the user

                            var CaseOwnerId = _tenantRepository.CaseOwnerIdTransaction(sqlTran, con, UserId.Value, UserData.Email, UserData.Mobile);
                            // Insert data to cms.customers

                            var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo,PropertyId,EmirateId, CaseTypeId,  Dated, ContactTimeId, ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId, OtherContactPersonName,OtherContactPersonMobile, RoleId,Unitrefno)
                    VALUES ({GernateCaseNumber},{CasePropertyId},{CaseEmirate} ,(select id from cms.CaseTypes where code={configValues.ESCaseType}),'{DateTime.UtcNow.AddHours(4)}',(select top 1  Id from cms.ContactTimings), 

                     ISNULL((Select top 1  SLAid from cms.SLARulesSet where CategoryId = (select top 1 id from cms.CaseCategory where code={configValues.ESCaseCategory}) and SubCategoryId = (select top 1 id from cms.CaseSubCategory where code={configValues.ESCaseSubCategory}) and TypeId= 1),0), 
                    
                        @CaseOwnerId,@Email,@Mobile, @Mobile, @Mobile, (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus},@Email,@Mobile, 1,'{RefNumber}')";

                            //27-Aug-2022
                            var CaseOwnerIdParam = new SqlParameter("@CaseOwnerId", CaseOwnerId);//add paramter
                            var EmailParam = new SqlParameter("@Email", UserData.Email);//add paramter
                            if (string.IsNullOrWhiteSpace(UserData.Email))
                            {
                                EmailParam = new SqlParameter("@Email", DBNull.Value);

                            }
                            var MobileParam = new SqlParameter("@Mobile", UserData.Mobile);//add paramter
                            if (string.IsNullOrWhiteSpace(UserData.Mobile))
                            {
                                MobileParam = new SqlParameter("@Mobile", DBNull.Value);

                            }

                            SqlParameter[] sqlParameters = new SqlParameter[]
                           {
                               CaseOwnerIdParam,
                               EmailParam,
                               MobileParam

                           };
                            var RequestDetail2 = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, AddRequestQuery, sqlParameters);

                            // INSERT data CMS.CaseServiceRequest
                            var SaveRequestQuery = $@"INSERT INTO CMS.CaseServiceRequest(Description, ProposedRemedy, CircumstanceId,  CaseCategouryId, CaseSubCatagouryId, ReasonForVocation, CaseId)
                    VALUES ('Extension service Request','Extension service Request',(select top 1 Id  from cms.Circumstances), (select id from cms.CaseCategory where code={configValues.ESCaseCategory}),(select id from cms.CaseSubCategory where code={configValues.ESCaseSubCategory}),'Extension service Request' ,{GernateCaseNumber});";
                            var RequestDetail3 = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, SaveRequestQuery);

                            try
                            {
                                var casetype = 4;
                                var casetypequery = $@"select Top 1 id as CaseTypeId from cms.CaseTypes where code={configValues.ESCaseType}";
                                var casetypeget = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, casetypequery);
                                if (casetypeget != null && casetypeget.Rows.Count > 0)
                                {
                                    var ListToBeSend = Conn.ToListof<CaseTypeCodeVm>(casetypeget);
                                    casetype = ListToBeSend.Select(p => p.CaseTypeId).FirstOrDefault();
                                }
                                //_tenantRepository.SetSLA(casetype, Convert.ToInt32(configValues.ESCaseCategory), Convert.ToInt32(configValues.ESCaseSubCategory), Convert.ToInt32(CaseEmirate), GernateCaseNumber);

                            }
                            catch (Exception ex)
                            {
                                _tenantRepository.InsertErrorLog(ex.Message);
                                throw ex;

                            }

                            if (model.DueActionId != null)
                            {
                                //update contract Action Due
                                var UpdateContractCase = _tenantRepository.UpdateContractCaseIdNew(model.DueActionId, GernateCaseNumber, sqlTran, con);
                                var UpdateStage = _tenantRepository.UpdateStageIdTransaction(model.DueActionId, 11, sqlTran, con); //Complete
                            }

                            // Insert data to cms.CaseContractDetail
                            var CaseServiceRequestQuery = $@"INSERT INTO CMS.CaseContractDetail(ContractId, StartDate,EndDate, CaseNo,CreatedOn,Type,Duration)
                    VALUES ({model.ContractId} ,@StartDate,@EndDate,  {GernateCaseNumber},'{DateTime.UtcNow.AddHours(4)}','Extension service Request',@Duration)";

                            //27-Aug-2022
                            //var StartDateParam = new SqlParameter("@StartDate", model.StartDate);//add paramter  
                            //var EndDateParam = new SqlParameter("@EndDate", model.EndDate);//add paramter  
                            //var DurationParam = new SqlParameter("@Duration", model.Duration);//add paramter  

                            //sent by irfan to update null
                            var StartDateParam = new SqlParameter { ParameterName = "@StartDate" };
                            if (!string.IsNullOrWhiteSpace(model.StartDate))
                            {
                                StartDateParam.Value = model.StartDate;
                            }
                            else
                            {
                                StartDateParam.Value = DBNull.Value;
                            }
                            var EndDateParam = new SqlParameter { ParameterName = "@EndDate" };
                            if (!string.IsNullOrWhiteSpace(model.EndDate))
                            {
                                EndDateParam.Value = model.EndDate;
                            }
                            else
                            {
                                EndDateParam.Value = DBNull.Value;
                            }
                            var DurationParam = new SqlParameter { ParameterName = "@Duration" };
                            if (!string.IsNullOrWhiteSpace(model.Duration))
                            {
                                DurationParam.Value = model.Duration;
                            }
                            else
                            {
                                DurationParam.Value = DBNull.Value;
                            }
                            //
                            SqlParameter[] sqlParameter = new SqlParameter[]
                           {
                              StartDateParam,
                               EndDateParam,
                               DurationParam
                           };
                            var CaseServiceRequestQueryDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, CaseServiceRequestQuery, sqlParameter);


                            var Notification = _tenantRepository.AddTicketNewSerivceRequestCaseTransaction(sqlTran, con, GernateCaseNumber, UserId, CaseOwnerId);


                            var CaseInfo = _tenantRepository.GetCaseInfoTransaction(GernateCaseNumber, sqlTran, con);
                            //workgroup
                            var WorkGroupId = _tenantRepository.UpdateCaseUserIdTransaction(sqlTran, con, CaseInfo.CaseCategouryId, CaseInfo.CaseSubCatagouryId, CaseInfo.CaseTypeId, CaseEmirate, CasePropertyId, GernateCaseNumber);
                            //if (UpdateUser != null)
                            //{
                            //    var Email = _tenantRepository.CaseOwnerEmailTransaction(UpdateUser, sqlTran, con);
                            //    var AddEmailNotification = _tenantRepository.prepareAndAddEmailNotificationTransaction(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseEmirate, 1, GernateCaseNumber, Email, sqlTran, con);
                            //}

                            //Hook code is setup in  cms.NotificationTypes and column  name is code //hookcode 1 = new service request
                            var SendNotification = _tenantRepository.prepareAndAddPushNotificationTransaction(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 1, GernateCaseNumber, UserId.Value, "Tenant", sqlTran, con);

                            var AddTenantEmailNotification = _tenantRepository.prepareAndAddEmailNotificationTransaction(sqlTran, con, 1, CaseInfo.CaseCategouryId, CaseEmirate, 1, GernateCaseNumber, UserData.Email);

                            AddRequestDetailVm CheckserviceRequest = new AddRequestDetailVm();
                            var GetCaseQuery = $@"select CaseNo from cms.Cases where CaseNo={GernateCaseNumber}";


                            var CheckCase = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetCaseQuery);

                            if (CheckCase.Rows.Count > 0)
                            {
                                var ListToBeSend = Conn.ToListof<AddRequestDetailVm>(CheckCase);
                                CheckserviceRequest = ListToBeSend.FirstOrDefault();
                                //PM Assignee
                                //var AssignContractor = _tenantRepository.AutoAssignContractor(GernateCaseNumber, UserId.Value);
                                bool UpdateAssignee = _tenantRepository.AssignPMAssineeeTransaction(sqlTran, con, GernateCaseNumber, "Insert", false);

                                //var getCaseInfo = _tenantRepository.GetCaseDetailInfoTransaction(sqlTran, con, GernateCaseNumber);
                                if (!string.IsNullOrEmpty(WorkGroupId.ToString()) && UpdateAssignee)
                                {
                                    string WorkGroupEmail = _tenantRepository.CaseOwnerEmailTransaction(sqlTran, con, WorkGroupId);
                                    //var assignee = getCaseInfo.Assignee;
                                    //var email = getCaseInfo.WorkGroupEmail;
                                    //var category = getCaseInfo.CategoryName;
                                    //var title = "New Service Request";
                                    //var subject = "New Service Request Assigned";

                                    //var body = $@"Dear Team, <br/> A new case no: {GernateCaseNumber} is generated with category {category} and is assigned to {assignee}. <br/>  <br/> System Generated Notification.";
                                    var AssigneeTemplate = _notificationRepository.GetTemplateInfo(37);
                                    var body = _notificationRepository.NotificationBodyReplacement(sqlTran, con, AssigneeTemplate.BodyContent, GernateCaseNumber);
                                    var sendworkGroupEmail = _tenantRepository.AddNewEmailNotificationTransaction(sqlTran, con, body, UserId.Value, AssigneeTemplate.Subject, WorkGroupEmail, AssigneeTemplate.Subject, GernateCaseNumber);


                                }
                            }

                            if (CheckserviceRequest != null)
                            {
                                sqlTran.Commit();

                                var data = new
                                {
                                    Status = "Ok",
                                    AddServiceRequest = CheckserviceRequest,
                                    Message = "Added Successfully"
                                };
                                return Ok(data);
                            }
                            else
                            {
                                sqlTran.Rollback();
                                var data = new
                                {
                                    Status = "NotFound",
                                    AddServiceRequest = CheckserviceRequest,
                                    Message = "No Data Found."
                                };
                                return Ok(data);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sqlTran.Rollback();

                        return Ok(ex.Message);
                    }
                }


                else
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data);
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

                return BadRequest(data);
            }
        }

        [Route("AddTerminateRequest")]
        [HttpPost]
        public ActionResult<string> AddTerminateRequest(AddCaseRequest model)
        {
            var logged = _tenantRepository.InsertErrorLog("AddTerminateRequest api call");
            //try
            //{


            if (User.Identity.IsAuthenticated)
            {

                if (CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.StartDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.EndDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.Duration, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.VacatingDate, out _))
                {
                    var dataBadReq = new
                    {
                        Status = "BadRequest",
                        Message = "Invalid Request.",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
                SqlTransaction sqlTran = null;
                ConfigValues configValues = new ConfigValues();
                UserInfo UserData = new UserInfo();
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                if (UserId == null)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    logged = _tenantRepository.InsertErrorLog("AddTerminateRequest api Invalid user");
                    return Unauthorized(data);
                }


                var constring = _settings.GetConnectionString();
                try
                {

                    using (var con = new SqlConnection(_settings.GetConnectionString()))
                    {
                        con.Open();

                        sqlTran = con.BeginTransaction();
                        var TenantID = GetTenantId(UserId.Value);
                        var contractTenantId = ContractInTenantIdTransaction(sqlTran, con, model.ContractId, TenantID);

                        var tenantId = _tenantRepository.GetTenantId(UserId.Value);

                        // Get User record
                        var GetUserValueQuery = $@"select UserId, Email,Mobile from app.Users where UserId={UserId}";


                        var RefNumber = _tenantRepository.GetRefNumber(model.ContractId, sqlTran, con);

                        if (!contractTenantId)
                        {
                            var data1 = new
                            {
                                Status = "Invalid",
                                Message = "Contract not found for this user; please try with valid user."
                            };
                            return Ok(data1);
                        }


                        var RequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetUserValueQuery);

                        if (RequestDetail.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<UserInfo>(RequestDetail);
                            UserData = ListToBeSend.FirstOrDefault();
                            logged = _tenantRepository.InsertErrorLog("AddTerminateRequest api call by " + UserData.FullName);
                        }

                        // insert record to cms.customer data
                        //insert data
                        MaxNumber maxNumber = new MaxNumber();
                        var GetMaxValueQuery = $@"SELECT ISNULL(MAX(CaseNo),0) as MaxCase FROM cms.Cases";
                        var MaxRequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetMaxValueQuery);

                        if (MaxRequestDetail.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<MaxNumber>(MaxRequestDetail);
                            maxNumber = ListToBeSend.FirstOrDefault();
                        }
                        int GernateCaseNumber = (maxNumber.MaxCase) + 1;

                        int? CaseEmirate = 0;
                        int? PropertyId = 0;
                        var GetEmirateId = $@"select EmirateID,con.PropertyID from Pms_Contract con
                                        join Pms_Property prop on prop.PropertyID= con.PropertyID
                                        where ContractID={model.ContractId}";
                        var EmirateRequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetEmirateId);
                        if (EmirateRequestDetail.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<CaseEmirateVm>(EmirateRequestDetail);
                            CaseEmirate = ListToBeSend.Select(p => p.EmirateID).FirstOrDefault();
                            PropertyId = ListToBeSend.Select(p => p.PropertyID).FirstOrDefault();
                        }

                        // Add and check user exist in cms.customer or not if not exist we add the user
                        var CaseOwnerId = _tenantRepository.CaseOwnerIdTransaction(sqlTran, con, UserId.Value, UserData.Email, UserData.Mobile);
                        // Insert data to cms.customers
                        var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo,PropertyId, CaseTypeId,  Dated, ContactTimeId,  ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId, OtherContactPersonName,OtherContactPersonMobile, RoleId,Unitrefno)
                    VALUES ({GernateCaseNumber},{PropertyId} ,(select id from cms.CaseTypes where code={configValues.TSCaseType}), '{DateTime.UtcNow.AddHours(4)}',(select top 1  Id from cms.ContactTimings),
   
                ISNULL((Select top 1  SLAid from cms.SLARulesSet where CategoryId = (select top 1 id from cms.CaseCategory where code={configValues.TSCaseCategory}) and SubCategoryId = (select top 1 id from cms.CaseSubCategory where code={configValues.TSCaseSubCategory}) and TypeId= 1),0),

                @CaseOwnerId,@Email,@Mobile, @Mobile, @Mobile, (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus},@Email,@Mobile, 1,'{RefNumber}')";
                        //27-Aug-2022
                        var CaseOwnerIdParam = new SqlParameter("@CaseOwnerId", CaseOwnerId);//add paramter
                        var EmailParam = new SqlParameter("@Email", UserData.Email);//add paramter
                        if (string.IsNullOrWhiteSpace(UserData.Email))
                        {
                            EmailParam = new SqlParameter("@Email", DBNull.Value);

                        }
                        var MobileParam = new SqlParameter("@Mobile", UserData.Mobile);//add paramter

                        if (string.IsNullOrWhiteSpace(UserData.Mobile))
                        {
                            MobileParam = new SqlParameter("@Mobile", DBNull.Value);

                        }

                        //try
                        //{
                        SqlParameter[] sqlParameter = new SqlParameter[]
                       {
                            EmailParam,
                            MobileParam,
                            CaseOwnerIdParam

                       };

                        var RequestDetail2 = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, AddRequestQuery, sqlParameter);
                        //}

                        //catch (Exception ex)
                        //{

                        //    logged = _tenantRepository.InsertErrorLog(ex.Message);
                        //}


                        // INSERT data CMS.CaseServiceRequest
                        var SaveRequestQuery = $@"INSERT INTO CMS.CaseServiceRequest(Description, ProposedRemedy, CircumstanceId,  CaseCategouryId, CaseSubCatagouryId, ReasonForVocation, CaseId)
                    VALUES (@Description,'Terminate service Request',(select top 1 Id  from cms.Circumstances),(select id from cms.CaseCategory where code={configValues.TSCaseCategory}),(select id from cms.CaseSubCategory where code={configValues.TSCaseSubCategory}),'Terminate service Request' ,{GernateCaseNumber});";

                        //27-Aug-2022
                        var DescriptionParam = new SqlParameter("@Description", model.Description);//add paramter  
                        SqlParameter[] sqlDescriptionParameters = new SqlParameter[]
                           {
                              DescriptionParam

                           };
                        var RequestDetail3 = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, SaveRequestQuery, sqlDescriptionParameters);



                        //try
                        //{
                        var casetype = 4;
                        var casetypequery = $@"select Top 1 id as CaseTypeId from cms.CaseTypes where code={configValues.TSCaseType}";
                        var casetypeget = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, casetypequery);
                        if (casetypeget != null && casetypeget.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<CaseTypeCodeVm>(casetypeget);
                            casetype = ListToBeSend.Select(p => p.CaseTypeId).FirstOrDefault();
                        }
                        //_tenantRepository.SetSLA(casetype, Convert.ToInt32(configValues.TSCaseSubCategory), Convert.ToInt32(configValues.TSCaseSubCategory), Convert.ToInt32(CaseEmirate), GernateCaseNumber);

                        //}
                        //catch (Exception ex)
                        //{
                        //    _tenantRepository.InsertErrorLog(ex.Message);

                        //}

                        if (model.DueActionId != null)
                        {
                            //update contract Action Due
                            var UpdateContractCase = _tenantRepository.UpdateContractCaseIdNew(model.DueActionId, GernateCaseNumber, sqlTran, con);
                            var UpdateStage = _tenantRepository.UpdateStageIdTransaction(model.DueActionId, 11, sqlTran, con);//Mark complete
                        }

                        // Insert data to cms.CaseContractDetail
                        var CaseServiceRequestQuery = $@"INSERT INTO CMS.CaseContractDetail(ContractId, StartDate,EndDate, CaseNo,CreatedOn,Type,Duration,VacatingId,VacatingDate)
                    VALUES ({model.ContractId} ,@StartDate,@EndDate,  {GernateCaseNumber},'{DateTime.UtcNow.AddHours(4)}','Terminate service Request',@Duration,{model.VacatingId},@VacatingDate)";
                        //27-Aug-2022
                        //var StartDateParam = new SqlParameter("@StartDate", model.StartDate);//add paramter 
                        //var EndDateParam = new SqlParameter("@EndDate", model.EndDate);//add paramter 
                        //var DurationParam = new SqlParameter("@Duration", model.Duration);//add paramter 
                        //var VacatingDateParam = new SqlParameter("@VacatingDate", model.VacatingDate);//add paramter 

                        var StartDateParam = new SqlParameter { ParameterName = "@StartDate" };
                        if (!string.IsNullOrWhiteSpace(model.StartDate))
                        {
                            StartDateParam.Value = model.StartDate;
                        }
                        else
                        {
                            StartDateParam.Value = DBNull.Value;
                        }
                        var EndDateParam = new SqlParameter { ParameterName = "@EndDate" };
                        if (!string.IsNullOrWhiteSpace(model.EndDate))
                        {
                            EndDateParam.Value = model.EndDate;
                        }
                        else
                        {
                            EndDateParam.Value = DBNull.Value;
                        }
                        var DurationParam = new SqlParameter { ParameterName = "@Duration" };
                        if (!string.IsNullOrWhiteSpace(model.Duration))
                        {
                            DurationParam.Value = model.Duration;
                        }
                        else
                        {
                            DurationParam.Value = DBNull.Value;
                        }
                        var VacatingDateParam = new SqlParameter { ParameterName = "@VacatingDate" };
                        if (!string.IsNullOrWhiteSpace(model.VacatingDate))
                        {
                            VacatingDateParam.Value = model.VacatingDate;
                        }
                        else
                        {
                            VacatingDateParam.Value = DBNull.Value;
                        }
                        //
                        SqlParameter[] sqlParameters = new SqlParameter[]
                           {
                              StartDateParam,
                              DurationParam,
                              VacatingDateParam,
                              EndDateParam

                           };

                        var CaseServiceRequestQueryDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, CaseServiceRequestQuery, sqlParameters);


                        var Notification = _tenantRepository.AddTicketNewSerivceRequestCaseTransaction(sqlTran, con, GernateCaseNumber, UserId, CaseOwnerId);

                        //=====================================================================
                        var CaseInfo = _tenantRepository.GetCaseInfoTransaction(GernateCaseNumber, sqlTran, con);

                        //try
                        //{  


                        //workgroup
                        // var UpdateUser = _tenantRepository.UpdateCaseUserId(CaseInfo.CaseCategouryId, CaseInfo.CaseTypeId, CaseEmirate, GernateCaseNumber);
                        _tenantRepository.InsertErrorLog("CategoryId: " + CaseInfo.CaseCategouryId.ToString() + " CaseTypeId: " + CaseInfo.CaseTypeId.ToString());
                        
                        var WorkGroupId = _tenantRepository.UpdateCaseUserIdTransaction(sqlTran, con, configValues.ServiceRequestCancellationCategory, configValues.ServiceRequestCancellationSubCategory, configValues.ServiceRequestCaseTypeId, CaseEmirate, PropertyId, GernateCaseNumber);
                        
                        //if (UpdateUser != null)
                        //{
                        //    var Email = _tenantRepository.CaseOwnerEmailTransaction(UpdateUser, sqlTran, con);
                        //    _tenantRepository.InsertErrorLog(Email);
                        //    var AddEmailNotification = _tenantRepository.prepareAndAddEmailNotificationTransaction(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseEmirate, 1, GernateCaseNumber, Email, sqlTran, con);
                        //}
                        var SendNotification = _tenantRepository.prepareAndAddPushNotificationTransaction(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseEmirate, 1, GernateCaseNumber, UserId.Value, "Tenant", sqlTran, con);


                       

                        var AddTenantEmailNotification = _tenantRepository.prepareAndAddEmailNotificationTransaction(sqlTran, con, 1, CaseInfo.CaseCategouryId, CaseEmirate, 1, GernateCaseNumber, UserData.Email);
                        //}
                        //catch (Exception ex)
                        //{

                        //    _tenantRepository.InsertErrorLog(ex.Message);
                        //}


                        //Hook code is setup in  cms.NotificationTypes and column  name is code //hookcode 1 = new service request

                        //try
                        //{
                        //var AssignContractor = _tenantRepository.AssignPMAssineeeTransaction(sqlTran, con, GernateCaseNumber, "Insert", false);
                        //}
                        //catch (Exception ex)
                        //{

                        //    _tenantRepository.InsertErrorLog(ex.Message);
                        //}


                        //===============================================================================

                        AddRequestDetailVm CheckserviceRequest = new AddRequestDetailVm();
                        var GetCaseQuery = $@"select CaseNo from cms.Cases where CaseNo={GernateCaseNumber}";


                        var CheckCase = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetCaseQuery);

                        if (CheckCase.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<AddRequestDetailVm>(CheckCase);
                            CheckserviceRequest = ListToBeSend.FirstOrDefault();

                            // SHould be PM Assignee
                            //var AssignContractor = _tenantRepository.AutoAssignContractor(GernateCaseNumber, UserId.Value);

                            bool UpdateAssignee = _tenantRepository.AssignPMAssineeeTransaction(sqlTran, con, GernateCaseNumber, "Insert", false);

                            //var getCaseInfo = _tenantRepository.GetCaseDetailInfoTransaction(sqlTran, con, GernateCaseNumber);
                            if (!string.IsNullOrEmpty(WorkGroupId.ToString()) && UpdateAssignee)
                            {
                                string WorkGroupEmail = _tenantRepository.CaseOwnerEmailTransaction(sqlTran, con, WorkGroupId);
                                //var assignee = getCaseInfo.Assignee;
                                //var email = getCaseInfo.WorkGroupEmail;
                                //var category = getCaseInfo.CategoryName;
                                //var title = "New Service Request";
                                //var subject = "New Service Request Assigned";

                                //var body = $@"Dear Team, <br/> A new case no: {GernateCaseNumber} is generated with category {category} and is assigned to {assignee}. <br/>  <br/> System Generated Notification.";
                                var AssigneeTemplate = _notificationRepository.GetTemplateInfo(37);
                                var body = _notificationRepository.NotificationBodyReplacement(sqlTran, con, AssigneeTemplate.BodyContent, GernateCaseNumber);
                                var sendworkGroupEmail = _tenantRepository.AddNewEmailNotificationTransaction(sqlTran, con, body, UserId.Value, AssigneeTemplate.Subject, WorkGroupEmail, AssigneeTemplate.Subject, GernateCaseNumber);


                            }
                        }



                        if (CheckserviceRequest != null)
                        {
                            sqlTran.Commit();

                            var data = new
                            {
                                Status = "Ok",
                                AddServiceRequest = CheckserviceRequest,
                                Message = "Added Successfully"
                            };
                            return Ok(data);
                        }
                        else
                        {
                            var data = new
                            {
                                Status = "NotFound",
                                AddServiceRequest = CheckserviceRequest,
                                Message = "No Data Found."
                            };
                            return Ok(data);
                        }



                    }
                }
                catch (Exception ex)
                {
                    sqlTran.Rollback();
                    return BadRequest(ex.Message);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                };
                return Unauthorized(data);
            }

            //}

            //catch (Exception ex)
            //{
            //    var data = new
            //    {
            //        Status = "Processing Error",
            //        StatusCode = "501",
            //        Message = "Internal Server Error",

            //    };
            //    return BadRequest(data);
            //}
        }

        //[Route("AddExtensionRequestCopy")]
        //[HttpPost]
        //public ActionResult<string> AddExtensionRequestCopy(AddCaseRequest model)
        //{
        //    try
        //    {
        //        ConfigValues configValues = new ConfigValues();
        //        UserInfo UserData = new UserInfo();
        //        if (User.Identity.IsAuthenticated)
        //        {
        //            var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
        //            int? UserId = _settings.GetUserIdByToken(_bearer_token);
        //            if (UserId == null)
        //            {
        //                var data = new
        //                {
        //                    Status = "Invalid",
        //                    Message = "InvalidSession."
        //                };
        //                return Unauthorized(data);
        //            }
        //            var TenantID = GetTenantId(UserId.Value);
        //            var contractTenantId = ContractInTenantId(model.ContractId, TenantID);

        //            if (!contractTenantId)
        //            {
        //                var data1 = new
        //                {
        //                    Status = "Invalid",
        //                    Message = "Contract not found for this user; please try with valid user."
        //                };
        //                return Ok(data1);
        //            }
        //            var tenantId = _tenantRepository.GetTenantId(UserId.Value);
        //            if (CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.StartDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.EndDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.Duration, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.VacatingDate, out _))
        //            {
        //                var dataBadReq = new
        //                {
        //                    Status = "BadRequest",
        //                    Message = "Invalid Request.",
        //                    StatusCode = "500"
        //                };
        //                return BadRequest(dataBadReq);
        //            }
        //            // Get User record
        //            var GetUserValueQuery = $@"select UserId, Email,Mobile from app.Users where UserId={UserId}";
        //            var constring = _settings.GetConnectionString();
        //            constring = constring.ToString();
        //            SqlConnection conn = new SqlConnection(constring);
        //            var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetUserValueQuery);

        //            if (RequestDetail.Rows.Count > 0)
        //            {
        //                var ListToBeSend = Conn.ToListof<UserInfo>(RequestDetail);
        //                UserData = ListToBeSend.FirstOrDefault();
        //            }

        //            // insert record to cms.customer data


        //            int? CaseEmirate = 0;
        //            int? CasePropertyId = 0;
        //            var GetEmirateId = $@"select EmirateID,con.PropertyID from Pms_Contract con join Pms_Property prop on prop.PropertyID= con.PropertyID where ContractID={model.ContractId} and con.TenantID = {tenantId}";
        //            var EmirateRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetEmirateId);
        //            if (EmirateRequestDetail.Rows.Count > 0)
        //            {
        //                var ListToBeSend = Conn.ToListof<CaseEmirateVm>(EmirateRequestDetail);
        //                CaseEmirate = ListToBeSend.Select(p => p.EmirateID).FirstOrDefault();
        //                CasePropertyId = ListToBeSend.Select(p => p.PropertyID).FirstOrDefault();
        //            }

        //            //insert data
        //            MaxNumber maxNumber = new MaxNumber();
        //            var GetMaxValueQuery = $@"SELECT ISNULL(MAX(CaseNo),0) as MaxCase FROM cms.Cases";
        //            var MaxRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetMaxValueQuery);

        //            if (MaxRequestDetail.Rows.Count > 0)
        //            {
        //                var ListToBeSend = Conn.ToListof<MaxNumber>(MaxRequestDetail);
        //                maxNumber = ListToBeSend.FirstOrDefault();
        //            }
        //            int GernateCaseNumber = (maxNumber.MaxCase) + 1;


        //            // Add and check user exist in cms.customer or not if not exist we add the user
        //            var CaseOwnerId = _tenantRepository.CaseOwnerId(UserId.Value, UserData.Email, UserData.Mobile);
        //            // Insert data to cms.customers
        //            var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo,PropertyId,EmirateId, CaseTypeId,  Dated, ContactTimeId, ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId, OtherContactPersonName,OtherContactPersonMobile, RoleId)
        //            VALUES ({GernateCaseNumber},{CasePropertyId},{CaseEmirate} ,(select id from cms.CaseTypes where code={configValues.ESCaseType}),'{DateTime.UtcNow.AddHours(4)}',(select top 1  Id from cms.ContactTimings), 

        //             ISNULL((Select top 1  SLAid from cms.SLARulesSet where CategoryId = (select top 1 id from cms.CaseCategory where code={configValues.ESCaseCategory}) and SubCategoryId = (select top 1 id from cms.CaseSubCategory where code={configValues.ESCaseSubCategory}) and TypeId= 1 and EmirateId = {CaseEmirate}),0), 
                    
        //                @CaseOwnerId,@Email,@Mobile, @Mobile, @Mobile, (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus},@Email,@Mobile, 1)";

        //            //27-Aug-2022
        //            var CaseOwnerIdParam = new SqlParameter("@CaseOwnerId", CaseOwnerId);//add paramter
        //            var EmailParam = new SqlParameter("@Email", UserData.Email);//add paramter
        //            if (string.IsNullOrWhiteSpace(UserData.Email))
        //            {
        //                EmailParam = new SqlParameter("@Email", DBNull.Value);

        //            }
        //            var MobileParam = new SqlParameter("@Mobile", UserData.Mobile);//add paramter
        //            if (string.IsNullOrWhiteSpace(UserData.Mobile))
        //            {
        //                MobileParam = new SqlParameter("@Mobile", DBNull.Value);

        //            }

        //            var RequestDetail2 = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery, CaseOwnerIdParam, EmailParam, MobileParam);

        //            // INSERT data CMS.CaseServiceRequest
        //            var SaveRequestQuery = $@"INSERT INTO CMS.CaseServiceRequest(Description, ProposedRemedy, CircumstanceId,  CaseCategouryId, CaseSubCatagouryId, ReasonForVocation, CaseId)
        //            VALUES ('Extension service Request','Extension service Request',(select top 1 Id  from cms.Circumstances), (select id from cms.CaseCategory where code={configValues.ESCaseCategory}),(select id from cms.CaseSubCategory where code={configValues.ESCaseSubCategory}),'Extension service Request' ,{GernateCaseNumber});";
        //            var RequestDetail3 = Conn.ExecuteDataTableSqlDA(conn, SaveRequestQuery);

        //            try
        //            {
        //                var casetype = 4;
        //                var casetypequery = $@"select Top 1 id as CaseTypeId from cms.CaseTypes where code={configValues.ESCaseType}";
        //                var casetypeget = Conn.ExecuteDataTableSqlDA(conn, casetypequery);
        //                if (casetypeget != null && casetypeget.Rows.Count > 0)
        //                {
        //                    var ListToBeSend = Conn.ToListof<CaseTypeCodeVm>(casetypeget);
        //                    casetype = ListToBeSend.Select(p => p.CaseTypeId).FirstOrDefault();
        //                }
        //                //_tenantRepository.SetSLA(casetype, Convert.ToInt32(configValues.ESCaseCategory), Convert.ToInt32(configValues.ESCaseSubCategory), Convert.ToInt32(CaseEmirate), GernateCaseNumber);

        //            }
        //            catch (Exception ex)
        //            {
        //                _tenantRepository.InsertErrorLog(ex.Message);

        //            }

        //            if (model.DueActionId != null)
        //            {
        //                //update contract Action Due
        //                var UpdateContractCase = _tenantRepository.UpdateContractCaseId(model.DueActionId, GernateCaseNumber);
        //                var UpdateStage = _tenantRepository.UpdateStageId(model.DueActionId, 11); //Complete
        //            }

        //            // Insert data to cms.CaseContractDetail
        //            var CaseServiceRequestQuery = $@"INSERT INTO CMS.CaseContractDetail(ContractId, StartDate,EndDate, CaseNo,CreatedOn,Type,Duration)
        //            VALUES ({model.ContractId} ,@StartDate,@EndDate,  {GernateCaseNumber},'{DateTime.UtcNow.AddHours(4)}','Extension service Request',@Duration)";

        //            //27-Aug-2022
        //            //var StartDateParam = new SqlParameter("@StartDate", model.StartDate);//add paramter  
        //            //var EndDateParam = new SqlParameter("@EndDate", model.EndDate);//add paramter  
        //            //var DurationParam = new SqlParameter("@Duration", model.Duration);//add paramter  

        //            //sent by irfan to update null
        //            var StartDateParam = new SqlParameter { ParameterName = "@StartDate" };
        //            if (!string.IsNullOrWhiteSpace(model.StartDate))
        //            {
        //                StartDateParam.Value = model.StartDate;
        //            }
        //            else
        //            {
        //                StartDateParam.Value = DBNull.Value;
        //            }
        //            var EndDateParam = new SqlParameter { ParameterName = "@EndDate" };
        //            if (!string.IsNullOrWhiteSpace(model.EndDate))
        //            {
        //                EndDateParam.Value = model.EndDate;
        //            }
        //            else
        //            {
        //                EndDateParam.Value = DBNull.Value;
        //            }
        //            var DurationParam = new SqlParameter { ParameterName = "@Duration" };
        //            if (!string.IsNullOrWhiteSpace(model.Duration))
        //            {
        //                DurationParam.Value = model.Duration;
        //            }
        //            else
        //            {
        //                DurationParam.Value = DBNull.Value;
        //            }
        //            //

        //            var CaseServiceRequestQueryDetail = Conn.ExecuteDataTableSqlDA(conn, CaseServiceRequestQuery, StartDateParam, EndDateParam, DurationParam);


        //            var Notification = _tenantRepository.AddTicketNewSerivceRequestCase(GernateCaseNumber, UserId, CaseOwnerId);


        //            var CaseInfo = _tenantRepository.GetCaseInfo(GernateCaseNumber);
        //            //workgroup
        //            var UpdateUser = _tenantRepository.UpdateCaseUserId(CaseInfo.CaseCategouryId, CaseInfo.CaseTypeId, CaseEmirate, GernateCaseNumber);
        //            if (UpdateUser != null)
        //            {
        //                var Email = _tenantRepository.CaseOwnerEmail(UpdateUser);
        //                var AddEmailNotification = _tenantRepository.prepareAndAddEmailNotification(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseEmirate, 1, GernateCaseNumber, Email);
        //            }

        //            //Hook code is setup in  cms.NotificationTypes and column  name is code //hookcode 1 = new service request
        //            var SendNotification = _tenantRepository.prepareAndAddPushNotification(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 1, GernateCaseNumber, UserId.Value, "Tenant");

        //            AddRequestDetailVm CheckserviceRequest = new AddRequestDetailVm();
        //            var GetCaseQuery = $@"select CaseNo from cms.Cases where CaseNo={GernateCaseNumber}";


        //            var CheckCase = Conn.ExecuteDataTableSqlDA(conn, GetCaseQuery);

        //            if (CheckCase.Rows.Count > 0)
        //            {
        //                var ListToBeSend = Conn.ToListof<AddRequestDetailVm>(CheckCase);
        //                CheckserviceRequest = ListToBeSend.FirstOrDefault();
        //                //PM Assignee
        //                //var AssignContractor = _tenantRepository.AutoAssignContractor(GernateCaseNumber, UserId.Value);
        //                var AssignContractor = _tenantRepository.AssignPMAssineee(GernateCaseNumber, "Insert", false);

        //                var getCaseInfo = _tenantRepository.GetCaseDetailInfo(GernateCaseNumber);
        //                if (getCaseInfo != null)
        //                {
        //                    var assignee = getCaseInfo.Assignee;
        //                    var email = getCaseInfo.WorkGroupEmail;
        //                    var category = getCaseInfo.CategoryName;

        //                    //  string CMSLocationPath = new ConfigValues().GetCMSBaseURL() + "/Admin/Case/Edit/";
        //                    //  string CMSurl = CMSLocationPath + GernateCaseNumber;
        //                    var title = "New Service Request";
        //                    var subject = "New Service Request";

        //                    //<a href={CMSurl}></a>
        //                    var AssigneeTemplate = _notificationRepository.GetTemplateInfo(37);
        //                    var body = _notificationRepository.NotificationBodyReplacement(null, conn, AssigneeTemplate.BodyContent, GernateCaseNumber);
        //                    //var body = $@"Dear Team, <br/> A new Case No: {GernateCaseNumber} of Category:  {category} is generated and Assigned to: {assignee}.  <br/> System Generated Notification";

        //                    try
        //                    {
        //                        var sendworkGroupEmail = _tenantRepository.AddNewEmailNotification(body, UserId.Value, subject, email, title, GernateCaseNumber);
        //                    }
        //                    catch (Exception ex)
        //                    {

        //                        var logged = _tenantRepository.InsertErrorLog(ex.Message); ;
        //                    }
        //                }
        //            }


        //            if (CheckserviceRequest != null)
        //            {
        //                var data = new
        //                {
        //                    Status = "Ok",
        //                    AddServiceRequest = CheckserviceRequest,
        //                    Message = "Added Successfully"
        //                };
        //                return Ok(data);
        //            }
        //            else
        //            {
        //                var data = new
        //                {
        //                    Status = "NotFound",
        //                    AddServiceRequest = CheckserviceRequest,
        //                    Message = "No Data Found."
        //                };
        //                return Ok(data);
        //            }
        //        }
        //        else
        //        {
        //            var data = new
        //            {
        //                Status = "Invalid",
        //                Message = "InvalidSession."
        //            };
        //            return Unauthorized(data);
        //        }
        //    }

        //    catch (Exception ex)
        //    {
        //        var data = new
        //        {
        //            Status = "Processing Error",
        //            StatusCode = "501",
        //            Message = "Internal Server Error",

        //        };
        //        return BadRequest(data);
        //    }
        //}

        //[Route("AddTerminateRequestCopy")]
        //[HttpPost]
        //public ActionResult<string> AddTerminateRequestCopy(AddCaseRequest model)
        //{
        //    var logged = _tenantRepository.InsertErrorLog("AddTerminateRequest api call");
        //    //try
        //    //{


        //    if (User.Identity.IsAuthenticated)
        //    {

        //        if (CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.StartDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.EndDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.Duration, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.VacatingDate, out _))
        //        {
        //            var dataBadReq = new
        //            {
        //                Status = "BadRequest",
        //                Message = "Invalid Request.",
        //                StatusCode = "500"
        //            };
        //            return BadRequest(dataBadReq);
        //        }
        //        ConfigValues configValues = new ConfigValues();
        //        UserInfo UserData = new UserInfo();
        //        var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
        //        int? UserId = _settings.GetUserIdByToken(_bearer_token);
        //        if (UserId == null)
        //        {
        //            var data = new
        //            {
        //                Status = "Invalid",
        //                Message = "InvalidSession."
        //            };
        //            logged = _tenantRepository.InsertErrorLog("AddTerminateRequest api Invalid user");
        //            return Unauthorized(data);
        //        }
        //        var TenantID = GetTenantId(UserId.Value);
        //        var contractTenantId = ContractInTenantId(model.ContractId, TenantID);

        //        if (!contractTenantId)
        //        {
        //            var data1 = new
        //            {
        //                Status = "Invalid",
        //                Message = "Contract not found for this user; please try with valid user."
        //            };
        //            return Ok(data1);
        //        }

        //        // Get User record
        //        var GetUserValueQuery = $@"select UserId, Email,Mobile from app.Users where UserId={UserId}";
        //        var constring = _settings.GetConnectionString();
        //        constring = constring.ToString();
        //        SqlConnection conn = new SqlConnection(constring);
        //        var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetUserValueQuery);

        //        if (RequestDetail.Rows.Count > 0)
        //        {
        //            var ListToBeSend = Conn.ToListof<UserInfo>(RequestDetail);
        //            UserData = ListToBeSend.FirstOrDefault();
        //            logged = _tenantRepository.InsertErrorLog("AddTerminateRequest api call by " + UserData.FullName);
        //        }

        //        // insert record to cms.customer data
        //        //insert data
        //        MaxNumber maxNumber = new MaxNumber();
        //        var GetMaxValueQuery = $@"SELECT ISNULL(MAX(CaseNo),0) as MaxCase FROM cms.Cases";
        //        var MaxRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetMaxValueQuery);

        //        if (MaxRequestDetail.Rows.Count > 0)
        //        {
        //            var ListToBeSend = Conn.ToListof<MaxNumber>(MaxRequestDetail);
        //            maxNumber = ListToBeSend.FirstOrDefault();
        //        }
        //        int GernateCaseNumber = (maxNumber.MaxCase) + 1;

        //        int? CaseEmirate = 0;
        //        var GetEmirateId = $@"select EmirateID from Pms_Contract con
        //                                join Pms_Property prop on prop.PropertyID= con.PropertyID
        //                                where ContractID={model.ContractId}";
        //        var EmirateRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetEmirateId);
        //        if (EmirateRequestDetail.Rows.Count > 0)
        //        {
        //            var ListToBeSend = Conn.ToListof<CaseEmirateVm>(MaxRequestDetail);
        //            CaseEmirate = ListToBeSend.Select(p => p.EmirateID).FirstOrDefault();
        //        }

        //        // Add and check user exist in cms.customer or not if not exist we add the user
        //        var CaseOwnerId = _tenantRepository.CaseOwnerId(UserId.Value, UserData.Email, UserData.Mobile);
        //        // Insert data to cms.customers
        //        var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo, CaseTypeId,  Dated, ContactTimeId,  ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId, OtherContactPersonName,OtherContactPersonMobile, RoleId)
        //            VALUES ({GernateCaseNumber} ,(select id from cms.CaseTypes where code={configValues.TSCaseType}), '{DateTime.UtcNow.AddHours(4)}',(select top 1  Id from cms.ContactTimings),
   
        //        ISNULL((Select top 1  SLAid from cms.SLARulesSet where CategoryId = (select top 1 id from cms.CaseCategory where code={configValues.TSCaseCategory}) and SubCategoryId = (select top 1 id from cms.CaseSubCategory where code={configValues.TSCaseSubCategory}) and TypeId= 1 and EmirateId = {CaseEmirate}),0),

        //        @CaseOwnerId,@Email,@Mobile, @Mobile, @Mobile, (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus},@Email,@Mobile, 1)";
        //        //27-Aug-2022
        //        var CaseOwnerIdParam = new SqlParameter("@CaseOwnerId", CaseOwnerId);//add paramter
        //        var EmailParam = new SqlParameter("@Email", UserData.Email);//add paramter
        //        if (string.IsNullOrWhiteSpace(UserData.Email))
        //        {
        //            EmailParam = new SqlParameter("@Email", DBNull.Value);

        //        }
        //        var MobileParam = new SqlParameter("@Mobile", UserData.Mobile);//add paramter
        //        if (string.IsNullOrWhiteSpace(UserData.Mobile))
        //        {
        //            MobileParam = new SqlParameter("@Mobile", DBNull.Value);

        //        }

        //        try
        //        {
        //            var RequestDetail2 = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery, CaseOwnerIdParam, EmailParam, MobileParam);
        //        }
        //        catch (Exception ex)
        //        {

        //            logged = _tenantRepository.InsertErrorLog(ex.Message);
        //        }


        //        // INSERT data CMS.CaseServiceRequest
        //        var SaveRequestQuery = $@"INSERT INTO CMS.CaseServiceRequest(Description, ProposedRemedy, CircumstanceId,  CaseCategouryId, CaseSubCatagouryId, ReasonForVocation, CaseId)
        //            VALUES (@Description,'Terminate service Request',(select top 1 Id  from cms.Circumstances),(select id from cms.CaseCategory where code={configValues.TSCaseCategory}),(select id from cms.CaseSubCategory where code={configValues.TSCaseSubCategory}),'Terminate service Request' ,{GernateCaseNumber});";

        //        //27-Aug-2022
        //        var DescriptionParam = new SqlParameter("@Description", model.Description);//add paramter  

        //        var RequestDetail3 = Conn.ExecuteDataTableSqlDA(conn, SaveRequestQuery, DescriptionParam);



        //        try
        //        {
        //            var casetype = 4;
        //            var casetypequery = $@"select Top 1 id as CaseTypeId from cms.CaseTypes where code={configValues.TSCaseType}";
        //            var casetypeget = Conn.ExecuteDataTableSqlDA(conn, casetypequery);
        //            if (casetypeget != null && casetypeget.Rows.Count > 0)
        //            {
        //                var ListToBeSend = Conn.ToListof<CaseTypeCodeVm>(casetypeget);
        //                casetype = ListToBeSend.Select(p => p.CaseTypeId).FirstOrDefault();
        //            }
        //            //_tenantRepository.SetSLA(casetype, Convert.ToInt32(configValues.TSCaseSubCategory), Convert.ToInt32(configValues.TSCaseSubCategory), Convert.ToInt32(CaseEmirate), GernateCaseNumber);

        //        }
        //        catch (Exception ex)
        //        {
        //            _tenantRepository.InsertErrorLog(ex.Message);

        //        }
        //        if (model.DueActionId != null)
        //        {
        //            //update contract Action Due
        //            var UpdateContractCase = _tenantRepository.UpdateContractCaseId(model.DueActionId, GernateCaseNumber);
        //            var UpdateStage = _tenantRepository.UpdateStageId(model.DueActionId, 11);//Mark complete
        //        }

        //        // Insert data to cms.CaseContractDetail
        //        var CaseServiceRequestQuery = $@"INSERT INTO CMS.CaseContractDetail(ContractId, StartDate,EndDate, CaseNo,CreatedOn,Type,Duration,VacatingId,VacatingDate)
        //            VALUES ({model.ContractId} ,@StartDate,@EndDate,  {GernateCaseNumber},'{DateTime.UtcNow.AddHours(4)}','Terminate service Request',@Duration,{model.VacatingId},@VacatingDate)";
        //        //27-Aug-2022
        //        //var StartDateParam = new SqlParameter("@StartDate", model.StartDate);//add paramter 
        //        //var EndDateParam = new SqlParameter("@EndDate", model.EndDate);//add paramter 
        //        //var DurationParam = new SqlParameter("@Duration", model.Duration);//add paramter 
        //        //var VacatingDateParam = new SqlParameter("@VacatingDate", model.VacatingDate);//add paramter 

        //        var StartDateParam = new SqlParameter { ParameterName = "@StartDate" };
        //        if (!string.IsNullOrWhiteSpace(model.StartDate))
        //        {
        //            StartDateParam.Value = model.StartDate;
        //        }
        //        else
        //        {
        //            StartDateParam.Value = DBNull.Value;
        //        }
        //        var EndDateParam = new SqlParameter { ParameterName = "@EndDate" };
        //        if (!string.IsNullOrWhiteSpace(model.EndDate))
        //        {
        //            EndDateParam.Value = model.EndDate;
        //        }
        //        else
        //        {
        //            EndDateParam.Value = DBNull.Value;
        //        }
        //        var DurationParam = new SqlParameter { ParameterName = "@Duration" };
        //        if (!string.IsNullOrWhiteSpace(model.Duration))
        //        {
        //            DurationParam.Value = model.Duration;
        //        }
        //        else
        //        {
        //            DurationParam.Value = DBNull.Value;
        //        }
        //        var VacatingDateParam = new SqlParameter { ParameterName = "@VacatingDate" };
        //        if (!string.IsNullOrWhiteSpace(model.VacatingDate))
        //        {
        //            VacatingDateParam.Value = model.VacatingDate;
        //        }
        //        else
        //        {
        //            VacatingDateParam.Value = DBNull.Value;
        //        }
        //        //


        //        var CaseServiceRequestQueryDetail = Conn.ExecuteDataTableSqlDA(conn, CaseServiceRequestQuery, StartDateParam, DurationParam, VacatingDateParam, EndDateParam);


        //        var Notification = _tenantRepository.AddTicketNewSerivceRequestCase(GernateCaseNumber, UserId, CaseOwnerId);

        //        //=====================================================================
        //        var CaseInfo = _tenantRepository.GetCaseInfo(GernateCaseNumber);

        //        try
        //        {  //workgroup
        //           // var UpdateUser = _tenantRepository.UpdateCaseUserId(CaseInfo.CaseCategouryId, CaseInfo.CaseTypeId, CaseEmirate, GernateCaseNumber);
        //            _tenantRepository.InsertErrorLog("CategoryId: " + CaseInfo.CaseCategouryId.ToString() + " CaseTypeId: " + CaseInfo.CaseTypeId.ToString());
        //            var UpdateUser = _tenantRepository.UpdateCaseUserId(configValues.ServiceRequestCancellationCategory, configValues.ServiceRequestCaseTypeId, CaseEmirate, GernateCaseNumber);
        //            if (UpdateUser != null)
        //            {
        //                var Email = _tenantRepository.CaseOwnerEmail(UpdateUser);
        //                _tenantRepository.InsertErrorLog(Email);
        //                var AddEmailNotification = _tenantRepository.prepareAndAddEmailNotification(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseEmirate, 1, GernateCaseNumber, Email);
        //            }
        //            var SendNotification = _tenantRepository.prepareAndAddPushNotification(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 1, GernateCaseNumber, UserId.Value, "Tenant");
        //        }
        //        catch (Exception ex)
        //        {

        //            _tenantRepository.InsertErrorLog(ex.Message);
        //        }


        //        //Hook code is setup in  cms.NotificationTypes and column  name is code //hookcode 1 = new service request

        //        try
        //        {
        //            var AssignContractor = _tenantRepository.AssignPMAssineee(GernateCaseNumber, "Insert", false);
        //        }
        //        catch (Exception ex)
        //        {

        //            _tenantRepository.InsertErrorLog(ex.Message);
        //        }


        //        //===============================================================================

        //        AddRequestDetailVm CheckserviceRequest = new AddRequestDetailVm();
        //        var GetCaseQuery = $@"select CaseNo from cms.Cases where CaseNo={GernateCaseNumber}";


        //        var CheckCase = Conn.ExecuteDataTableSqlDA(conn, GetCaseQuery);

        //        if (CheckCase.Rows.Count > 0)
        //        {
        //            var ListToBeSend = Conn.ToListof<AddRequestDetailVm>(CheckCase);
        //            CheckserviceRequest = ListToBeSend.FirstOrDefault();

        //            // SHould be PM Assignee
        //            //var AssignContractor = _tenantRepository.AutoAssignContractor(GernateCaseNumber, UserId.Value);

        //            var AssignContractor = _tenantRepository.AssignPMAssineee(GernateCaseNumber, "Insert", false);

        //            var getCaseInfo = _tenantRepository.GetCaseDetailInfo(GernateCaseNumber);
        //            if (getCaseInfo != null)
        //            {
        //                var assignee = getCaseInfo.Assignee;
        //                var email = getCaseInfo.WorkGroupEmail;
        //                var category = getCaseInfo.CategoryName;
        //                //  string CMSLocationPath = new ConfigValues().GetCMSBaseURL() + "/Admin/Case/Edit/";
        //                //  string CMSurl = CMSLocationPath + GernateCaseNumber;
        //                var title = "New Service Request";
        //                var subject = "New Service Request";
        //                //<a href={CMSurl}></a>
        //                var AssigneeTemplate = _notificationRepository.GetTemplateInfo(37);
        //                var body = _notificationRepository.NotificationBodyReplacement(null, conn, AssigneeTemplate.BodyContent, GernateCaseNumber);
        //                //var body = $@"Dear Team, <br/> A new Case No: {GernateCaseNumber} of Category:  {category} is generated and Assigned to: {assignee}.  <br/> System Generated Notification";
        //                try
        //                {
        //                    var sendworkGroupEmail = _tenantRepository.AddNewEmailNotification(body, UserId.Value, subject, email, title, GernateCaseNumber);
        //                }
        //                catch (Exception ex)
        //                {

        //                    logged = _tenantRepository.InsertErrorLog(ex.Message);
        //                }
        //            }
        //        }


        //        if (CheckserviceRequest != null)
        //        {
        //            var data = new
        //            {
        //                Status = "Ok",
        //                AddServiceRequest = CheckserviceRequest,
        //                Message = "Added Successfully"
        //            };
        //            return Ok(data);
        //        }
        //        else
        //        {
        //            var data = new
        //            {
        //                Status = "NotFound",
        //                AddServiceRequest = CheckserviceRequest,
        //                Message = "No Data Found."
        //            };
        //            return Ok(data);
        //        }
        //    }
        //    else
        //    {
        //        var data = new
        //        {
        //            Status = "Invalid",
        //            Message = "InvalidSession."
        //        };
        //        return Unauthorized(data);
        //    }
        //    //}

        //    //catch (Exception ex)
        //    //{
        //    //    var data = new
        //    //    {
        //    //        Status = "Processing Error",
        //    //        StatusCode = "501",
        //    //        Message = "Internal Server Error",

        //    //    };
        //    //    return BadRequest(data);
        //    //}
        //}


        private bool ContractInTenantIdTransaction(int contractId, int tenantId, SqlTransaction tran, SqlConnection conn)
        {


            var Contracts = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, conn,
                $"SELECT 1 FROM Pms_Contract WHERE (TenantID = {tenantId}) AND (ContractID = {contractId})");
            return Contracts.Rows.Count > 0;
        }



        private bool ContractInTenantId(int contractId, int tenantId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Contracts = Conn.ExecuteDataTableSqlDA(conn, $"SELECT 1 FROM Pms_Contract WHERE (TenantID = {tenantId}) AND (ContractID = {contractId})");
            return Contracts.Rows.Count > 0;
        }



        private bool ContractInTenantIdTransaction(SqlTransaction tran, SqlConnection con, int contractId, int tenantId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Contracts = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, con, $"SELECT 1 FROM Pms_Contract WHERE (TenantID = {tenantId}) AND (ContractID = {contractId})");
            return Contracts.Rows.Count > 0;
        }
        private int GetTenantId(int UserId)
        {
            int TenantId = 0;
            TenantId = _tenantRepository.GetTenantId(UserId);
            return TenantId;
        }
        [Route("GetExtensionPeriod")]
        [HttpPost]
        public ActionResult<string> GetExtensionPeriod([FromBody] DownloadContractTerms requestModel)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                    int? UserId = _settings.GetUserIdByToken(_bearer_token);
                    if (UserId == null)
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            Message = "InvalidSession."
                        };
                        return Unauthorized(data1);
                    }
                    var TenantID = GetTenantId(UserId.Value);
                    var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantID);

                    if (!contractTenantId)
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data1);
                    }
                    OtherRenewalContractInfoVm Extend1Month = new OtherRenewalContractInfoVm();
                    OtherRenewalContractInfoVm Extend3Month = new OtherRenewalContractInfoVm();
                    OtherRenewalContractInfoVm Extend6Month = new OtherRenewalContractInfoVm();

                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);


                    var GetExtend1MonthInfo = $@"select pc.ContractID,pc.contractno,emi.EmirateName,
                                  FORMAT(fromDate,'dd-MM-yyyy') as fromDate,
                                  FORMAT(ToDate,'dd-MM-yyyy') as EndDate,
                                  FORMAT(DATEADD(day, 1, ToDate),'dd-MM-yyyy') as AddNewDate, 
                                   FORMAT(DATEADD(month, 1, ToDate),'dd-MM-yyyy')  as EndNewDate,
								   (COALESCE ( pu.contractrenewalrentrate, pu.RentPerAnnumMin))/12 as 'TotalAmount'
								   from Pms_Contract pc
								   join Pms_ContractUnits pcu on pc.ContractID=pcu.ContractID
								   join Pms_Unit pu on pu.UnitID=pcu.UnitID
								   left join Pms_Property prop on prop.PropertyID=pc.PropertyID
								   left join Pms_Emirate emi on emi.EmirateID=prop.EmirateID
								   where pc.ContractID={int.Parse(requestModel.ContractId)}";

                    var AddRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetExtend1MonthInfo);
                    if (AddRequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<OtherRenewalContractInfoVm>(AddRequestDetail);
                        Extend1Month = ListToBeSend.FirstOrDefault();
                        if (Extend1Month.EmirateName.ToLower().Trim() == "dubai")
                        {
                            Extend1Month.Amount = null;
                            Extend1Month.TotalAmount = null;
                        }
                    }

                    var GetExtend3MonthInfo = $@"select pc.ContractID,pc.contractno,emi.EmirateName,
                                  FORMAT(fromDate,'dd-MM-yyyy') as fromDate,
                                  FORMAT(ToDate,'dd-MM-yyyy')as EndDate,
                                  FORMAT(DATEADD(day, 1, ToDate),'dd-MM-yyyy') as AddNewDate, 
                                   FORMAT(DATEADD(month, 3, ToDate),'dd-MM-yyyy')  as EndNewDate,
								   3*((COALESCE ( pu.contractrenewalrentrate, pu.RentPerAnnumMin))/12) as 'TotalAmount'
								   from Pms_Contract pc
								   join Pms_ContractUnits pcu on pc.ContractID=pcu.ContractID
								    join Pms_Unit pu on pu.UnitID=pcu.UnitID
                                    left join Pms_Property prop on prop.PropertyID=pc.PropertyID
								   left join Pms_Emirate emi on emi.EmirateID=prop.EmirateID
								   where pc.ContractID={int.Parse(requestModel.ContractId)}";

                    var GetExtend3MonthInfoDetail = Conn.ExecuteDataTableSqlDA(conn, GetExtend3MonthInfo);
                    if (GetExtend3MonthInfoDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<OtherRenewalContractInfoVm>(GetExtend3MonthInfoDetail);
                        Extend3Month = ListToBeSend.FirstOrDefault();

                        if (Extend3Month.EmirateName.ToLower().Trim() == "dubai")
                        {
                            Extend3Month.Amount = null;
                            Extend3Month.TotalAmount = null;
                        }
                    }

                    var GetExtend6MonthInfo = $@"select pc.ContractID,pc.contractno,emi.EmirateName,
                                  FORMAT(fromDate,'dd-MM-yyyy') as fromDate,
                                  FORMAT(ToDate,'dd-MM-yyyy')as EndDate,
                                  FORMAT(DATEADD(day, 1, ToDate),'dd-MM-yyyy') as AddNewDate, 
                                   FORMAT(DATEADD(month, 6, ToDate),'dd-MM-yyyy')  as EndNewDate,
								   6*((COALESCE ( pu.contractrenewalrentrate, pu.RentPerAnnumMin))/12) as 'TotalAmount'
								   from Pms_Contract pc
								   join Pms_ContractUnits pcu on pc.ContractID=pcu.ContractID
								    join Pms_Unit pu on pu.UnitID=pcu.UnitID
                                    left join Pms_Property prop on prop.PropertyID=pc.PropertyID
								   left join Pms_Emirate emi on emi.EmirateID=prop.EmirateID
								   where pc.ContractID={int.Parse(requestModel.ContractId)}";

                    var GetExtend6MonthInfoDetail = Conn.ExecuteDataTableSqlDA(conn, GetExtend6MonthInfo);
                    if (GetExtend6MonthInfoDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<OtherRenewalContractInfoVm>(GetExtend6MonthInfoDetail);
                        Extend6Month = ListToBeSend.FirstOrDefault();
                        if (Extend6Month.EmirateName.ToLower().Trim() == "dubai")
                        {
                            Extend6Month.Amount = null;
                            Extend6Month.TotalAmount = null;
                        }
                    }

                    List<ExtensionPeriodVm> extensionList = new List<ExtensionPeriodVm>();
                    ExtensionPeriodVm extension1Month = new ExtensionPeriodVm
                    {
                        Duration = "1 Month",
                        Amount = Extend1Month.TotalAmount,
                        ExtensionDetail = Extend1Month

                    };
                    extensionList.Add(extension1Month);
                    ExtensionPeriodVm extension3Month = new ExtensionPeriodVm
                    {
                        Duration = "3 Month",
                        Amount = Extend3Month.TotalAmount,
                        ExtensionDetail = Extend3Month
                    };
                    extensionList.Add(extension3Month);
                    ExtensionPeriodVm extension6Month = new ExtensionPeriodVm
                    {
                        Duration = "6 Month",
                        Amount = Extend6Month.TotalAmount,
                        ExtensionDetail = Extend6Month
                    };
                    extensionList.Add(extension6Month);
                    var data = new
                    {
                        status = "Ok",
                        ExtensionPeriod = extensionList
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data);
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
                return BadRequest(data);
            }
        }

        [Route("GetRenewalContractInfo")]
        [HttpPost]
        public ActionResult<string> GetRenewalContractInfo([FromBody] DownloadContractTerms requestModel)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                    int? UserId = _settings.GetUserIdByToken(_bearer_token);
                    if (UserId == null)
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            Message = "InvalidSession."
                        };
                        return Unauthorized(data1);
                    }
                    var TenantID = GetTenantId(UserId.Value);
                    var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantID);

                    if (!contractTenantId)
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data1);
                    }
                    string Link = null;
                    OtherRenewalContractInfoVm renewalContract = new OtherRenewalContractInfoVm();

                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    //change coolasce 30-12-2022
                    var GetRewalInfo = $@"select pc.ContractID,pc.contractno,ISNULL(Installments,0) as Installments,ISNULL(sum(TotalAmount),0) as TotalAmount,emi.EmirateName,
                                    FORMAT(fromDate,'dd-MM-yyyy') as fromDate,
                                    FORMAT(ToDate,'dd-MM-yyyy')as EndDate,
                                    FORMAT(DATEADD(DAY, 1, ToDate),'dd-MM-yyyy') as AddNewDate, 
                                    FORMAT(DATEADD(YEAR, 1, ToDate),'dd-MM-yyyy') as EndNewDate,
                                    COALESCE (sum(contractrenewalrentrate), sum(pu.RentPerAnnumMin)) as 'Amount',
                                    prop.EmirateID
                                     from Pms_Contract pc
                                     join Pms_ContractUnits pcu on pc.ContractID=pcu.ContractID  and pcu.Suspend=0
                                     join Pms_Unit pu on pu.UnitID=pcu.UnitID                        
                                     join Pms_Property prop on pu.PropertyID=prop.PropertyID
                                     join Pms_Emirate emi on emi.EmirateID=prop.EmirateID
                                     where pc.ContractID={int.Parse(requestModel.ContractId)} group by pc.ContractID,pc.contractno ,Installments,EmirateName,fromDate,ToDate,prop.EmirateID";
                    //            var GetRewalInfo = $@"select pc.ContractID,pc.contractno,ISNULL(Installments,0) as Installments,ISNULL(TotalAmount,0) as                           TotalAmount,emi.EmirateName,
                    //                           FORMAT(fromDate,'dd-MM-yyyy') as fromDate,
                    //                           FORMAT(ToDate,'dd-MM-yyyy')as EndDate,
                    //                           FORMAT(DATEADD(DAY, 1, ToDate),'dd-MM-yyyy') as AddNewDate, 
                    //                           FORMAT(DATEADD(YEAR, 1, ToDate),'dd-MM-yyyy') as EndNewDate,
                    //                           COALESCE (contractrenewalrentrate, RentPerAnnumMin) as 'Amount',
                    //                           prop.EmirateID
                    //   from Pms_Contract pc
                    //   join Pms_ContractUnits pcu on pc.ContractID=pcu.ContractID  and pcu.Suspend=0
                    //   join Pms_Unit pu on pu.UnitID=pcu.UnitID                        
                    //join Pms_Property prop on pu.PropertyID=prop.PropertyID
                    //join Pms_Emirate emi on emi.EmirateID=prop.EmirateID

                    //  where pc.ContractID={int.Parse(requestModel.ContractId)}";

                    var AddRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetRewalInfo);
                    if (AddRequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<OtherRenewalContractInfoVm>(AddRequestDetail);
                        renewalContract = ListToBeSend.FirstOrDefault();
                        if (renewalContract.EmirateID == 1)// for Abu Dhabi
                        {
                            Link = "";
                        }
                        if (renewalContract.EmirateName.ToLower().Trim() == "dubai")// for dubai
                        {
                            renewalContract.Amount = null;
                        }

                    }
                    var data = new
                    {
                        Status = "ok",
                        Record = renewalContract,
                        link = Link,
                    };
                    return Ok(data);

                }
                else
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data);
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
                return BadRequest(data);
            }
        }


        [Route("CreateSignatureRequest")]
        [HttpPost]
        public ActionResult<string> CreateSignatureRequest(AddCaseRequest model)
        {
            //try
            //{
            ConfigValues configValues = new ConfigValues();
            UserInfo UserData = new UserInfo();
            if (User.Identity.IsAuthenticated)
            {
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                if (UserId == null)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data);
                }
                var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                var TenantID = GetTenantId(UserId.Value);
                var contractTenantId = ContractInTenantId(model.ContractId, TenantID);

                if (!contractTenantId)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data1);
                }
                if (CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.StartDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.EndDate, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.Duration, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.VacatingDate, out _))
                {
                    var dataBadReq = new
                    {
                        Status = "BadRequest",
                        Message = "Invalid Request.",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
                // Get User record
                var GetUserValueQuery = $@"select UserId, Email,Mobile from app.Users where UserId={UserId}";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetUserValueQuery);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<UserInfo>(RequestDetail);
                    UserData = ListToBeSend.FirstOrDefault();
                }

                // insert record to cms.customer data
                //insert data
                MaxNumber maxNumber = new MaxNumber();
                var GetMaxValueQuery = $@"SELECT ISNULL(MAX(CaseNo),0) as MaxCase FROM cms.Cases";
                var MaxRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetMaxValueQuery);

                if (MaxRequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<MaxNumber>(MaxRequestDetail);
                    maxNumber = ListToBeSend.FirstOrDefault();
                }
                int GernateCaseNumber = (maxNumber.MaxCase) + 1;


                // Add and check user exist in cms.customer or not if not exist we add the user
                var CaseOwnerId = _tenantRepository.CaseOwnerId(UserId.Value, UserData.Email, UserData.Mobile);
                // Insert data to cms.customers
                var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo, CaseTypeId,  Dated, ContactTimeId,  ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId, OtherContactPersonName,OtherContactPersonMobile, RoleId)
                    VALUES ({GernateCaseNumber} ,{configValues.SSCaseType},'{DateTime.UtcNow.AddHours(4)}',(select top 1  Id from cms.ContactTimings), (select top 1 Id from cms.SLA), {CaseOwnerId},@Email,@Mobile, @Mobile, @Mobile, (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus},@Email,@Mobile, 1)";

                //27-Aug-2022
                var EmailParam = new SqlParameter("@Email", UserData.Email);//add paramter
                if (string.IsNullOrWhiteSpace(UserData.Email))
                {
                    EmailParam = new SqlParameter("@Email", DBNull.Value);
                }
                var MobileParam = new SqlParameter("@Mobile", UserData.Mobile);//add paramter
                if (string.IsNullOrWhiteSpace(UserData.Mobile))
                {
                    MobileParam = new SqlParameter("@Mobile", DBNull.Value);
                }

                var RequestDetail2 = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery, EmailParam, MobileParam);



                // INSERT data CMS.CaseServiceRequest
                var SaveRequestQuery = $@"INSERT INTO CMS.CaseServiceRequest(Description, ProposedRemedy, CircumstanceId,  CaseCategouryId, CaseSubCatagouryId, ReasonForVocation, CaseId)
                    VALUES ('Signature service Request','Signature service Request',(select top 1 Id  from cms.Circumstances), (select id from cms.CaseCategory where code={configValues.SSCaseCategory}),(select id from cms.CaseSubCategory where code={configValues.SSCaseSubCategory}),'Signature service Request' ,{GernateCaseNumber});";
                var RequestDetail3 = Conn.ExecuteDataTableSqlDA(conn, SaveRequestQuery);

                // Insert data to cms.CaseContractDetail
                var CaseServiceRequestQuery = $@"INSERT INTO CMS.CaseContractDetail(ContractId, CaseNo,CreatedOn,Type)
                    VALUES ({model.ContractId} ,  {GernateCaseNumber},'{DateTime.UtcNow.AddHours(4)}','Create Case Request')";
                var CaseServiceRequestQueryDetail = Conn.ExecuteDataTableSqlDA(conn, CaseServiceRequestQuery);

                var Notification = _tenantRepository.AddTicketNewSerivceRequestCase(GernateCaseNumber, UserId, CaseOwnerId);


                AddRequestDetailVm CheckserviceRequest = new AddRequestDetailVm();
                var GetCaseQuery = $@"select CaseNo from cms.Cases where CaseNo={GernateCaseNumber}";


                var CheckCase = Conn.ExecuteDataTableSqlDA(conn, GetCaseQuery);

                if (CheckCase.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<AddRequestDetailVm>(CheckCase);
                    CheckserviceRequest = ListToBeSend.FirstOrDefault();

                    var AssignContractor = _tenantRepository.AutoAssignContractor(GernateCaseNumber, UserId.Value);
                }


                if (CheckserviceRequest != null)
                {
                    var data = new
                    {
                        Status = "Ok",
                        AddServiceRequest = CheckserviceRequest,
                        Message = "Added Successfully"
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        AddServiceRequest = CheckserviceRequest,
                        Message = "No Data Found."
                    };
                    return Ok(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                };
                return Unauthorized(data);
            }
            //}

            //catch (Exception ex)
            //{
            //    var data = new
            //    {
            //        Status = "Processing Error",
            //        StatusCode = "501",
            //        Message = "Internal Server Error",

            //    };
            //    return BadRequest(data);
            //}
        }

        [Route("CanAddNewCase")]
        [HttpPost]
        public ActionResult<string> CanAddNewCase(int Contractid)
        {
            //try
            //{
            ConfigValues configValues = new ConfigValues();
            UserInfo UserData = new UserInfo();
            if (User.Identity.IsAuthenticated)
            {
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                if (UserId == null)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data);
                }

                MaxNumber maxNumber = new MaxNumber();
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var checkCaseExist = $@"select cas.caseNo as MaxCase,Contractid,cas.StatusId from cms.Cases cas
                                        join cms.CaseContractDetail as cd on cd.CaseNo=cas.CaseNo
                                        join cms.Customer cus on cus.CustomerId=cas.CaseOwnerId
                                        where cas.StatusId in(7,8,9,10,11,12,13,14,15,16,18,19,20) and cd.ContractId={Contractid} and cus.UserId={UserId}";


                var CheckCaseDetail = Conn.ExecuteDataTableSqlDA(conn, checkCaseExist);
                if (CheckCaseDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<MaxNumber>(CheckCaseDetail);
                    maxNumber = ListToBeSend.FirstOrDefault();

                    var data = new
                    {
                        Status = "Not Ok",
                        CaseNo = maxNumber.MaxCase,
                        Message = "Case Already Exist",
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "Ok",
                        CaseNo = 0,
                        Message = "You can add new case"
                    };
                    return Ok(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                };
                return Ok(data);
            }
            //}

            //catch (Exception ex)
            //{
            //    var data = new
            //    {
            //        Status = "Processing Error",
            //        StatusCode = "501",
            //        Message = "Internal Server Error",

            //    };
            //    return BadRequest(data);
            //}
        }


        [Route("CheckinContract")]
        [HttpPost]
        public ActionResult<string> CheckinContract(CheckInCaseRequest model)
        {
            //try
            //{
            ConfigValues configValues = new ConfigValues();
            UserInfo UserData = new UserInfo();
            if (User.Identity.IsAuthenticated)
            {
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                if (UserId == null)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data);
                }
                var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                var TenantID = GetTenantId(UserId.Value);
                var contractTenantId = ContractInTenantId(model.ContractId, TenantID);

                if (!contractTenantId)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data1);
                }
                if (CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _))
                {
                    var dataBadReq = new
                    {
                        Status = "BadRequest",
                        Message = "Invalid Request.",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
                // Get User record
                var GetUserValueQuery = $@"select UserId, Email,Mobile, FullName from app.Users where UserId={UserId}";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetUserValueQuery);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<UserInfo>(RequestDetail);
                    UserData = ListToBeSend.FirstOrDefault();
                }

                // insert record to cms.customer data




                //insert data
                MaxNumber maxNumber = new MaxNumber();
                var GetMaxValueQuery = $@"SELECT ISNULL(MAX(CaseNo),0) as MaxCase FROM cms.Cases";
                var MaxRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetMaxValueQuery);

                if (MaxRequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<MaxNumber>(MaxRequestDetail);
                    maxNumber = ListToBeSend.FirstOrDefault();
                }
                int GernateCaseNumber = (maxNumber.MaxCase) + 1;


                // Add and check user exist in cms.customer or not if not exist we add the user
                var CaseOwnerId = _tenantRepository.CaseOwnerId(UserId.Value, UserData.Email, UserData.Mobile);
                // Insert data to cms.customers
                //var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo, CaseTypeId,  Dated, ContactTimeId,  ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId, OtherContactPersonName,OtherContactPersonMobile, RoleId)
                //    VALUES ({GernateCaseNumber} ,(select id from cms.CaseTypes where code={configValues.CSCaseType}),'{DateTime.Now}',(select top 1  Id from cms.ContactTimings), (select top 1 Id from cms.SLA), {CaseOwnerId},'{UserData.Email}','{UserData.Mobile}', '{UserData.Mobile}', '{UserData.Mobile}', (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus},'{UserData.FullName}','{UserData.Mobile}', 1)";
                var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo, CaseTypeId,  Dated, ContactTimeId,  ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId, OtherContactPersonName,OtherContactPersonMobile, RoleId)
                    VALUES ({GernateCaseNumber} ,(select id from cms.CaseTypes where code={configValues.CSCaseType}),'{DateTime.UtcNow.AddHours(4)}',(select top 1  Id from cms.ContactTimings), (select top 1 Id from cms.SLA), {CaseOwnerId},@Email,@Mobile, @Mobile, @Mobile, (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus},@FullName,@Mobile, 1)";


                //27-Aug-2022
                var FullNameParam = new SqlParameter("@FullName", UserData.FullName);//add paramter
                if (string.IsNullOrWhiteSpace(UserData.FullName))
                {
                    FullNameParam = new SqlParameter("@FullName", DBNull.Value);//add paramter
                }
                var EmailParam = new SqlParameter("@Email", UserData.Email);//add paramter
                if (string.IsNullOrWhiteSpace(UserData.Email))
                {
                    EmailParam = new SqlParameter("@Email", DBNull.Value);//add paramter
                }
                var MobileParam = new SqlParameter("@Mobile", UserData.Mobile);//add paramter
                if (string.IsNullOrWhiteSpace(UserData.Mobile))
                {
                    MobileParam = new SqlParameter("@Mobile", DBNull.Value);//add paramter
                }
                var RequestDetail2 = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery, FullNameParam, EmailParam, MobileParam);

                // INSERT data CMS.CaseServiceRequest
                var SaveRequestQuery = $@"INSERT INTO CMS.CaseServiceRequest(Description, ProposedRemedy, CircumstanceId,  CaseCategouryId, CaseSubCatagouryId, ReasonForVocation, CaseId)
                    VALUES ('Check In','Check In',(select top 1 Id  from cms.Circumstances), (select id from cms.CaseCategory where code={configValues.CSCaseCategory}),(select id from cms.CaseSubCategory where code={configValues.CSCaseSubCategory}),'Check in service Request' ,{GernateCaseNumber});";
                var RequestDetail3 = Conn.ExecuteDataTableSqlDA(conn, SaveRequestQuery);

                // Insert data to cms.CaseContractDetail
                var CaseServiceRequestQuery = $@"INSERT INTO CMS.CaseContractDetail(ContractId, CaseNo,CreatedOn,Type)
                    VALUES ({model.ContractId} ,  {GernateCaseNumber},'{DateTime.UtcNow.AddHours(4)}','Check in Contract')";
                var CaseServiceRequestQueryDetail = Conn.ExecuteDataTableSqlDA(conn, CaseServiceRequestQuery);

                var Notification = _tenantRepository.AddTicketNewSerivceRequestCase(GernateCaseNumber, UserId, CaseOwnerId);


                AddRequestDetailVm CheckserviceRequest = new AddRequestDetailVm();
                var GetCaseQuery = $@"select CaseNo from cms.Cases where CaseNo={GernateCaseNumber}";


                var CheckCase = Conn.ExecuteDataTableSqlDA(conn, GetCaseQuery);

                if (CheckCase.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<AddRequestDetailVm>(CheckCase);
                    CheckserviceRequest = ListToBeSend.FirstOrDefault();

                    var AssignContractor = _tenantRepository.AutoAssignContractor(GernateCaseNumber, UserId.Value);
                }


                if (CheckserviceRequest != null)
                {
                    var data = new
                    {
                        Status = "Ok",
                        AddServiceRequest = CheckserviceRequest,
                        Message = "Added Successfully"
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        AddServiceRequest = CheckserviceRequest,
                        Message = "No Data Found."
                    };
                    return Ok(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                };
                return Unauthorized(data);
            }
            //}

            //catch (Exception ex)
            //{
            //    var data = new
            //    {
            //        Status = "Processing Error",
            //        StatusCode = "501",
            //        Message = "Internal Server Error",

            //    };
            //    return BadRequest(data);
            //}
        }

        [Route("GetVacatingList")]
        [HttpPost]
        public ActionResult<string> GetVacatingList()
        {
            //try
            //{
            List<VacatingVm> Vacating = new List<VacatingVm>();
            if (User.Identity.IsAuthenticated)
            {
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                if (UserId == null)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data);
                }

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var VacatingQuery = $@"select VacatingId, Title, TitleAr, Description,DescriptionAR from cms.VacatingReason";


                var VacatingDetail = Conn.ExecuteDataTableSqlDA(conn, VacatingQuery);
                if (VacatingDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<VacatingVm>(VacatingDetail);
                    Vacating = ListToBeSend.ToList();

                    var data = new
                    {
                        Status = "Ok",
                        Record = Vacating,
                        Message = "Record Found",
                        Note = "Select some Reason"
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "Ok",
                        Record = Vacating,
                        Message = "No Record Found",
                        Note = ""
                    };
                    return Ok(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                };
                return Ok(data);
            }
            //}

            //catch (Exception ex)
            //{
            //    var data = new
            //    {
            //        Status = "Processing Error",
            //        StatusCode = "501",
            //        Message = "Internal Server Error",

            //    };
            //    return BadRequest(data);
            //}
        }

        [Route("LegalSettlementContract")]
        [HttpPost]
        public ActionResult<string> LegalSettlementContract(CheckInCaseRequest model)
        {
            try
            {
                ConfigValues configValues = new ConfigValues();
                UserInfo UserData = new UserInfo();
                if (User.Identity.IsAuthenticated)
                {
                    var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                    int? UserId = _settings.GetUserIdByToken(_bearer_token);
                    if (UserId == null)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            Message = "InvalidSession."
                        };
                        return Unauthorized(data);
                    }
                    var TenantID = GetTenantId(UserId.Value);
                    var contractTenantId = ContractInTenantId(model.ContractId, TenantID);

                    if (!contractTenantId)
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data1);
                    }
                    var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                    if (CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _))
                    {
                        var dataBadReq = new
                        {
                            Status = "BadRequest",
                            Message = "Invalid Request.",
                            StatusCode = "500"
                        };
                        return BadRequest(dataBadReq);
                    }
                    // Get User record
                    var GetUserValueQuery = $@"select UserId, Email,Mobile from app.Users where UserId={UserId}";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetUserValueQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<UserInfo>(RequestDetail);
                        UserData = ListToBeSend.FirstOrDefault();
                    }

                    // insert record to cms.customer data


                    //insert data
                    MaxNumber maxNumber = new MaxNumber();
                    var GetMaxValueQuery = $@"SELECT ISNULL(MAX(CaseNo),0) as MaxCase FROM cms.Cases";
                    var MaxRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetMaxValueQuery);

                    if (MaxRequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<MaxNumber>(MaxRequestDetail);
                        maxNumber = ListToBeSend.FirstOrDefault();
                    }
                    int GernateCaseNumber = (maxNumber.MaxCase) + 1;


                    // Add and check user exist in cms.customer or not if not exist we add the user
                    var CaseOwnerId = _tenantRepository.CaseOwnerId(UserId.Value, UserData.Email, UserData.Mobile);
                    // Insert data to cms.customers
                    //var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo, CaseTypeId,  Dated, ContactTimeId,  ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId, OtherContactPersonName,OtherContactPersonMobile, RoleId)
                    //VALUES ({GernateCaseNumber} ,(select id from cms.CaseTypes where code={configValues.ESCaseType}),'{DateTime.Now}',(select top 1  Id from cms.ContactTimings), (select top 1 Id from cms.SLA), {CaseOwnerId},'{UserData.Email}','{UserData.Mobile}', '{UserData.Mobile}', '{UserData.Mobile}', (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus},'{UserData.Email}','{UserData.Mobile}', 1)";
                    var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo, CaseTypeId,  Dated, ContactTimeId,  ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId, OtherContactPersonName,OtherContactPersonMobile, RoleId)
                    VALUES ({GernateCaseNumber} ,(select id from cms.CaseTypes where code={configValues.ESCaseType}),'{DateTime.UtcNow.AddHours(4)}',(select top 1  Id from cms.ContactTimings), (select top 1 Id from cms.SLA), {CaseOwnerId},@Email,@Mobile, @Mobile, @Mobile, (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus},@Email,@Mobile, 1)";

                    //27-Aug-2022

                    var EmailParam = new SqlParameter("@Email", UserData.Email);//add paramter
                    if (string.IsNullOrWhiteSpace(UserData.Email))
                    {
                        EmailParam = new SqlParameter("@Email", DBNull.Value);
                    }
                    var MobileParam = new SqlParameter("@Mobile", UserData.Mobile);//add paramter
                    if (string.IsNullOrWhiteSpace(UserData.Mobile))
                    {
                        MobileParam = new SqlParameter("@Mobile", DBNull.Value);
                    }

                    var RequestDetail2 = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery, MobileParam, EmailParam);

                    // INSERT data CMS.CaseServiceRequest
                    var SaveRequestQuery = $@"INSERT INTO CMS.CaseServiceRequest(Description, ProposedRemedy, CircumstanceId, CaseCategouryId, CaseSubCatagouryId, ReasonForVocation, CaseId)
                    VALUES ('Legal Settlement Contract','Legal Settlement Contract',(select top 1 Id  from cms.Circumstances), (select id from cms.CaseCategory where code={configValues.CSCaseCategory}),(select id from cms.CaseSubCategory where code={configValues.LSCaseSubCategory}),'Legal settlement service Request' ,{GernateCaseNumber});";
                    var RequestDetail3 = Conn.ExecuteDataTableSqlDA(conn, SaveRequestQuery);

                    if (model.DueActionId != null)
                    {
                        //update contract Action Due
                        var UpdateContractCase = _tenantRepository.UpdateContractCaseId(model.DueActionId, GernateCaseNumber);
                        var UpdateStage = _tenantRepository.UpdateStageId(model.DueActionId, 2);
                    }

                    // Insert data to cms.CaseContractDetail
                    var CaseServiceRequestQuery = $@"INSERT INTO CMS.CaseContractDetail(ContractId, CaseNo,CreatedOn,Type)
                    VALUES ({model.ContractId} ,  {GernateCaseNumber},'{DateTime.UtcNow.AddHours(4)}','Legal settlement Contract')";
                    var CaseServiceRequestQueryDetail = Conn.ExecuteDataTableSqlDA(conn, CaseServiceRequestQuery);



                    var Notification = _tenantRepository.AddTicketNewSerivceRequestCase(GernateCaseNumber, UserId, CaseOwnerId);


                    AddRequestDetailVm CheckserviceRequest = new AddRequestDetailVm();
                    var GetCaseQuery = $@"select CaseNo from cms.Cases where CaseNo={GernateCaseNumber}";


                    var CheckCase = Conn.ExecuteDataTableSqlDA(conn, GetCaseQuery);

                    if (CheckCase.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<AddRequestDetailVm>(CheckCase);
                        CheckserviceRequest = ListToBeSend.FirstOrDefault();

                        var AssignContractor = _tenantRepository.AutoAssignContractor(GernateCaseNumber, UserId.Value);
                    }


                    if (CheckserviceRequest != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            AddServiceRequest = CheckserviceRequest,
                            Message = "Added Successfully"
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            AddServiceRequest = CheckserviceRequest,
                            Message = "No Data Found."
                        };
                        return Ok(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data);
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
                return BadRequest(data);
            }
        }
    }

    public class DownloadContractTerms
    {
        public string ContractId { get; set; } = "0";
    }


}