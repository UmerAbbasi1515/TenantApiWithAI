using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FABTenantApi.Classes;
using FABTenantApi.Models;
using FABTenantD2Api.Classes;
using FABTenantD2Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace FABTenantD2Api.Controllers
{
    [Route("api/Tenant/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private IConfiguration _config;
        private readonly ITenantRepository _TenantRepository;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        public ProfileController(IConfiguration config, ITokenService tokenService, ITenantRepository TenantRepository)
        {
            _config = config;
            _TenantRepository = TenantRepository;
            _tokenService = tokenService;
            _settings = new Settings();
        }

        [HttpPost]
        [Route("Get")]
        public ActionResult<string> Get()
        {
            //try { 
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

                var Tenant = new TenantVm();
                var TenantId = _TenantRepository.GetTenantId(UserId.Value);
                if (TenantId != 0)
                {
                    Tenant = _TenantRepository.GetProfile(TenantId);
                    if (Tenant == null)
                    {
                        var nodata = new
                        {
                            Status = "NotFound",
                            profile = Tenant,
                            statuCode = "404",
                            Message = "No Record Found."
                        };
                        return NotFound(nodata);
                    }
                    var data = new
                    {
                        Status = "Ok",
                        statuCode = "200",
                        profile = Tenant,
                        Message = "Tenant Profile Found."
                    };
                    return Ok(data);
                }
                else
                {
                    var nodata = new
                    {
                        Status = "NotFound",
                        statuCode = "404",
                        profile = Tenant,
                        Message = "No Record Found."
                    };
                    return NotFound(nodata);
                }

            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    statuCode = "404",
                    profile = new TenantVm(),
                    Message = "Invalid Session."
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


        [Route("UpdateProfileRequest")]
        [HttpPost]
        public ActionResult<string> UpdateProfileRequest(UpdateProfileServiceRequest model)
        {
            //try
            //{
            ConfigValues configValues = new ConfigValues();
            UserInfo UserData = new UserInfo();
            if (User.Identity.IsAuthenticated)
            {
                if (CrossSiteScriptingValidation.IsDangerousParameter(model.Address, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.PersonMobile, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.PersonName, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _))
                {
                    var dataBadReq = new
                    {
                        Status = "BadRequest",
                        Message = "Invalid Request.",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
                if(!CrossSiteScriptingValidation.RegexEmailCheck(model.PersonEmail))
                {
                    var dataBadReq = new
                    {
                        Status = "BadRequest",
                        Message = "Invalid Email Parameter In Request .",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
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
                MaxNumber maxNumber = new MaxNumber();
                /* coomented the opended  case checking  15/12/2022
             
              var checkCaseExist = $@"select CaseNo as MaxCase from cms.Cases ca 
join cms.Customer cus on   cus.customerid=ca.CaseOwnerId
join cms.CaseServiceRequest csq on   csq.CaseId=ca.CaseNo
where UserRoleType=1 and cus.UserId={UserId} and CaseTypeId=1 and StatusId in(7,8,9,10,11,12,13,14,15,16,18,19,20) and csq.CaseCategouryId=14";


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
*/
                // Get User record
                var GetUserValueQuery = $@"select UserId, Email,Mobile from app.Users where UserId={UserId}";
                
                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetUserValueQuery);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<UserInfo>(RequestDetail);
                    UserData = ListToBeSend.FirstOrDefault();
                }

                // insert record to cms.customer data
                //insert data
                
                var GetMaxValueQuery = $@"SELECT ISNULL(MAX(CaseNo),0) as MaxCase FROM cms.Cases";
                var MaxRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetMaxValueQuery);

                if (MaxRequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<MaxNumber>(MaxRequestDetail);
                    maxNumber = ListToBeSend.FirstOrDefault();
                }

                //insert data

                int GernateCaseNumber = (maxNumber.MaxCase) + 1;



                var CaseOwnerId = _TenantRepository.CaseOwnerId(UserId.Value, model.PersonName, model.PersonMobile);
                model.PersonName = model.PersonName.Replace("'","");
                var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo, CaseTypeId,  Dated, ContactTimeId,  ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId,OtherContactPersonName,OtherContactPersonMobile,UserRoleType, RoleId)
                    VALUES ({GernateCaseNumber} ,(select id from cms.CaseTypes where code={configValues.UPCaseType}),'{DateTime.UtcNow.AddHours(4)}',(select top 1  Id from cms.ContactTimings), 
                ISNULL((Select top 1 SLAid from cms.SLARulesSet where CategoryId = (select top 1 id from cms.CaseCategory where code={configValues.UPCaseCategory}) and SubCategoryId = (select top 1 id from cms.CaseSubCategory where code={configValues.UPCaseSubCategory}) and TypeId= 1 and EmirateId = 1),0), 
{CaseOwnerId},@Email,@Mobile, @Mobile, @Mobile, (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus},@PersonName,@PersonMobile,1,1)";

                var personnameparam = new SqlParameter("@PersonName", model.PersonName);
                if(string.IsNullOrWhiteSpace(model.PersonName))
                {
                    personnameparam = new SqlParameter("@PersonName", DBNull.Value);
                }
                var personmobileparam = new SqlParameter("@PersonMobile", model.PersonMobile);
                if (string.IsNullOrWhiteSpace(model.PersonMobile))
                {
                    personmobileparam = new SqlParameter("@PersonMobile", DBNull.Value);
                }
                var Emailparam = new SqlParameter("@Email", UserData.Email);
                if (string.IsNullOrWhiteSpace(UserData.Email))
                {
                    Emailparam = new SqlParameter("@Email", DBNull.Value);
                }
                var Mobileparam = new SqlParameter("@Mobile", UserData.Mobile);
                if (string.IsNullOrWhiteSpace(UserData.Email))
                {
                    Mobileparam = new SqlParameter("@Mobile", DBNull.Value);
                }
                var RequestDetail2 = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery, personnameparam, personmobileparam, Emailparam,Mobileparam);
                model.Description = "Update of contact Details";
                var SaveRequestQuery = $@"INSERT INTO CMS.CaseServiceRequest(Description, ProposedRemedy, CircumstanceId, CaseCategouryId, CaseSubCatagouryId, ReasonForVocation, CaseId)
                    VALUES (@Description,@Description,(select top 1 Id  from cms.Circumstances), (select top 1 id from cms.CaseCategory where code={configValues.UPCaseCategory}),(select top 1 id from cms.CaseSubCategory where code={configValues.UPCaseSubCategory}),@Description ,{GernateCaseNumber});";
                var Descriptionparam = new SqlParameter("@Description", model.Description);
                if (string.IsNullOrWhiteSpace(model.Description) || model.Description.Contains("null"))
                {
                    Descriptionparam = new SqlParameter("@Description", DBNull.Value);
                }


                var RequestDetail3 = Conn.ExecuteDataTableSqlDA(conn, SaveRequestQuery, Descriptionparam);
                
                var SaveProfiledetailsQuery = $@"INSERT INTO CMS.TenantProfileDetail(Name, Email, PhoneNumber, Address, CaseNo)VALUES (@PersonName,@PersonEmail,@PersonMobile,@Address ,{GernateCaseNumber});";
                var personSqlparam = new SqlParameter("@PersonName", model.PersonName);
                if (string.IsNullOrWhiteSpace(model.PersonName))
                {
                    personSqlparam = new SqlParameter("@PersonName", DBNull.Value);
                }
                var PersonEmailparam = new SqlParameter("@PersonEmail", model.PersonEmail);
                if (string.IsNullOrWhiteSpace(model.PersonEmail))
                {
                    PersonEmailparam = new SqlParameter("@PersonEmail", DBNull.Value);
                }
                var personMobileparam = new SqlParameter("@PersonMobile", model.PersonMobile);
                if (string.IsNullOrWhiteSpace(model.PersonMobile))
                {
                    personMobileparam = new SqlParameter("@PersonMobile", DBNull.Value);
                }
                var personAddresparam = new SqlParameter("@Address", model.Address);
                if (string.IsNullOrWhiteSpace(model.Address))
                {
                    personAddresparam = new SqlParameter("@Address", DBNull.Value);
                }

                var RequestDetail4 = Conn.ExecuteDataTableSqlDA(conn, SaveProfiledetailsQuery, personSqlparam, personMobileparam, PersonEmailparam, personAddresparam);

                string Subject = "Ticket Added Against Case No " + GernateCaseNumber;
                string Message = "Ticket and notification added successfully Against Case No " + GernateCaseNumber;
                string ShortDescription = "Ticket and notification added successfully Against Case No " + GernateCaseNumber;
                var AddTicketQuery = $@"INSERT INTO CMS.Ticket(TicketNo,Subject, Message, PerorityId, LastUpdated, IsAnswered, UserId, StatusId, DateTime)VALUES ({GernateCaseNumber},@Subject,@Message,2, '{DateTime.UtcNow.AddHours(4)}',0,(select top 1 UserId from cms.Users),(select Id from cms.TicketStatus where Name='Open'), '{DateTime.UtcNow.AddHours(4)}');";
                var MessageSqlparam = new SqlParameter("@Message", Message);
                if (string.IsNullOrWhiteSpace(Message))
                {
                    MessageSqlparam = new SqlParameter("@Message", DBNull.Value);
                }
                var SubjectSqlparam = new SqlParameter("@Subject", Subject);
                if (string.IsNullOrWhiteSpace(Subject))
                {
                    SubjectSqlparam = new SqlParameter("@Subject", DBNull.Value);
                }
                var RequestDetail5 = Conn.ExecuteDataTableSqlDA(conn, AddTicketQuery, MessageSqlparam, SubjectSqlparam);
                Subject = "Profile update request";
                Message = $@"your profile service request against case no {GernateCaseNumber} has been submitted successfully";

                //added 22/12-2022 ar
               var SubjectAr = "طلب تحديث الملف الشخصي";
               var  MessageAr = $@"تم إرسالها بنجاح {GernateCaseNumber}طلب خدمة ملف التعريف الخاص بك ضد القضية رقم";

                var AddNotification = _TenantRepository.SendPushNotification(UserId.Value, Message, ShortDescription, Subject,SubjectAr,MessageAr, "Tenant",GernateCaseNumber, "case");


                AddServiceRequestDetailVm CheckserviceRequest = new AddServiceRequestDetailVm();
                var GetCaseQuery = $@"select CaseNo from cms.Cases where CaseNo={GernateCaseNumber}";


                var CheckCase = Conn.ExecuteDataTableSqlDA(conn, GetCaseQuery);

                if (CheckCase.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<AddServiceRequestDetailVm>(CheckCase);
                    CheckserviceRequest = ListToBeSend.FirstOrDefault();
                }


                if (CheckserviceRequest != null)
                {
                    var data = new
                    {
                        Status = "Ok",
                        AddServiceRequest = CheckserviceRequest,
                        Message = "New Added Successfully",
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

        [Route("CanEditProfile")]
        [HttpPost]
        public ActionResult<string> CanEditProfile()
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
                var tenantId = _TenantRepository.GetTenantId(UserId.Value);

                MaxNumber maxNumber = new MaxNumber();
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var checkCaseExist = $@"select CaseNo as MaxCase from cms.Cases ca 
join cms.Customer cus on   cus.customerid=ca.CaseOwnerId
join cms.CaseServiceRequest csq on   csq.CaseId=ca.CaseNo
where UserRoleType=1 and cus.UserId={UserId} and CaseTypeId=1 and StatusId in(7,8,9,10,11,12,13,14,15,16,18,19,20) and csq.CaseCategouryId=14";


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
    }
}