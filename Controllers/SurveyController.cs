using FABModels.Model;
using FABTenantApi.Classes;
using FABTenantApi.Models;
using FABTenantD2Api.Classes;
using FABTenantD2Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantD2Api.Controllers
{
    [Route("api/Tenant/[controller]")]
    [ApiController]
    public class SurveyController : ControllerBase
    {

        private IConfiguration _config;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        public SurveyController(IConfiguration config, ITokenService tokenService, ITenantRepository tenantRepository)
        {
            _config = config;
            _tenantRepository = tenantRepository;
            _tokenService = tokenService;
            _settings = new Settings();
        }
        private int GetCaseOwnerId(int UserId)
        {
            int ownerid = 0;
            CaseOwnerViewModal caseOwnerView = new CaseOwnerViewModal();
            var ServiceRequestQuery = $@"Select CustomerId as CaseOwnerId From CMS.Customer Where UserId = {UserId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, ServiceRequestQuery);

            if (RequestDetail.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CaseOwnerViewModal>(RequestDetail);
                caseOwnerView = ListToBeSend.FirstOrDefault();
                if (caseOwnerView != null && caseOwnerView.CaseOwnerId > 0)
                {
                    ownerid = caseOwnerView.CaseOwnerId;
                }
            }
            return ownerid;
        }
        private bool CaseInUserId(int caseNo, int userId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Contracts = 
                Conn.ExecuteDataTableSqlDA(conn, $"SELECT 1 FROM CMS.Cases WHERE (CaseOwnerId = {userId}) AND (CaseNo = {caseNo})");
            return Contracts.Rows.Count > 0;
        }
        [Route("GetSurveyQuestions")]
        [HttpPost]
        public ActionResult<string> GetSurveyQuestions([FromBody] CaseNoBody requestModel)
        {
            try
            {

                List<FaqQuestionVm> FaqQuestionData = new List<FaqQuestionVm>();
                CaseServiceRequestVM caseCategorty = new CaseServiceRequestVM();
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
                    var userid = GetCaseOwnerId(UserId.Value);
                    var isValidUserForCase = CaseInUserId(int.Parse(requestModel.CaseNo), userid);

                    if (!isValidUserForCase)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            Message = "Not a Valid User For Case."
                        };
                        return Unauthorized(data);
                    }
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var CaseCategoryQuery = $@"select CaseCategouryId from CMS.CaseServiceRequest where CaseId={int.Parse(requestModel.CaseNo)}";
                    var caseRequestDetail = Conn.ExecuteDataTableSqlDA(conn, CaseCategoryQuery);
                    if (caseRequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<CaseServiceRequestVM>(caseRequestDetail);
                        caseCategorty = ListToBeSend.FirstOrDefault();
                    }

                    var add = AddSurvey(caseCategorty.CaseCategouryId, int.Parse(requestModel.CaseNo));
                    var FaqQuestionQuery = $@"select ans.QuestionId,CustomerChoice,ques.TitleAR,ques.Title,ans.Description, ans.AnswerId from cms.SurveyAnswers ans
                      left join cms.SurveyQuestions  ques on ques.QuestionId=ans.QuestionId
                      where ans.ServiceRequestId ={int.Parse(requestModel.CaseNo)}";

                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, FaqQuestionQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<FaqQuestionVm>(RequestDetail);
                        FaqQuestionData = ListToBeSend;
                    }
                    if (FaqQuestionData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            FaqQuestion = FaqQuestionData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            FaqQuestion = FaqQuestionData,
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

        [Route("GetSurveyQuestionAnswer")]
        [HttpPost]
        public ActionResult<string> GetSurveyQuestionAnswer([FromBody] GetSurveyQuestionAnswer requestModel)
        {
            try
            {
                List<ServeryOptions> FaqData = new List<ServeryOptions>();
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

                    var FaqQuery = $@"select OptionId,QuestionId,OptionTitle,DescriptionRequired,OptionTitleAR from cms.SurveyOptions where QuestionId={int.Parse(requestModel.QuestionId)} and active=1";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, FaqQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ServeryOptions>(RequestDetail);
                        FaqData = ListToBeSend;
                    }
                    if (FaqData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            FaqOptions = FaqData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            FaqOptions = FaqData,
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

        [Route("SaveSurveyAnswers")]
        [HttpPost]
        public ActionResult<string> SaveSurveyAnswers(SaveSurveyVm model)
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
                        var data1 = new
                        {
                            Status = "Invalid",
                            Message = "InvalidSession."
                        };
                        return Unauthorized(data1);
                    }
                    if (FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _))
                    {
                        var dataBadReq = new
                        {
                            Status = "BadRequest",
                            Message = "Invalid Request.",
                            StatusCode = "500"
                        };
                        return BadRequest(dataBadReq);
                    }
                    
                    var userid = GetCaseOwnerId(UserId.Value);
                    var caseNo = GetCaseNo(model.AnswerId);
                    var isValidUserForCase = CaseInUserId(caseNo, userid);

                    if (!isValidUserForCase)
                    {
                        var dataValid = new
                        {
                            Status = "Invalid",
                            Message = "Not a Valid User For Case."
                        };
                        return Unauthorized(dataValid);
                    }
                    var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);

                    var AddRequestQuery = $@"update CMS.SurveyAnswers set CustomerChoice={model.CustomerChoice}, Description=@Description, IsCompleted={model.IsCompleted} where AnswerId={model.AnswerId}";
                    
                    //param updated on 08/09/2022
                    var DescriptionParam = new SqlParameter("@Description", model.Description);//add paramter
                    if (!string.IsNullOrWhiteSpace(model.Description))
                    {
                        DescriptionParam.Value = model.Description;
                    }
                    else
                    {
                        DescriptionParam.Value = DBNull.Value;
                    }
                    var RequestDetail2 = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery, DescriptionParam);

                    var data = new
                    {
                        Status = "Ok",
                        Message = "Added Successfully"
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
        public class cassenovm{
            public int ServiceRequestId { get; set; }
        }
        private int GetCaseNo(int answerid) {

            var query = $@"select ServiceRequestId from CMS.SurveyAnswers where AnswerId = {answerid}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var data = Conn.ExecuteDataTableSqlDA(conn, query);
            if(data != null && data.Rows.Count > 0)
            {
                var listtobeget = Conn.ToListof<cassenovm>(data);
                return listtobeget.Select(p => p.ServiceRequestId).FirstOrDefault();
            }
            return 0;
        }

        // add survery 
        [Route("AddSurvey")]
        [HttpPost]
        public ActionResult<string> AddSurvey(int CategoryId, int CaseNo)
        {
            try
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
                List<FaqQuestionVm> FaqQuestionData = new List<FaqQuestionVm>();
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var userid = GetCaseOwnerId(UserId.Value);
                var caseNo = CaseNo;
                var isValidUserForCase = CaseInUserId(caseNo, userid);

                if (!isValidUserForCase)
                {
                    var dataValid = new
                    {
                        Status = "Invalid",
                        Message = "Not a Valid User For Case."
                    };
                    return Unauthorized(dataValid);
                }
                var IsSurveyAddedQuery = $@"select AnswerId from cms.SurveyAnswers where ServiceRequestId={CaseNo}";
                var IsSurveyAddedRequest = Conn.ExecuteDataTableSqlDA(conn, IsSurveyAddedQuery);
                if (IsSurveyAddedRequest.Rows.Count > 0)
                {
                    var data = new
                    {
                        Status = "Alreay exist",
                        Message = "Data Found."
                    };
                    return Ok(data);
                }
                var FaqQuestionQuery = $@"select QuestionId,Title,TitleAR,Description,DescriptionAR from cms.SurveyQuestions where CaseCategoryId={CategoryId} and active=1";

                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, FaqQuestionQuery);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<FaqQuestionVm>(RequestDetail);
                    FaqQuestionData = ListToBeSend;

                    foreach (var item in FaqQuestionData)
                    {
                        var AddRequestQuery = $@"INSERT INTO CMS.SurveyAnswers(ServiceRequestId, QuestionId,IsCompleted) VALUES ({CaseNo},{item.QuestionId},0)";
                        var RequestDetail2 = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery);
                    }
                }
                if (FaqQuestionData != null)
                {
                    var data = new
                    {
                        Status = "Ok",
                        FaqQuestion = FaqQuestionData,
                        Message = "Data Found."
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        FaqQuestion = FaqQuestionData,
                        Message = "No Data Found."
                    };
                    return Ok(data);
                }


            }
            catch (Exception ex)
            {
                var data = new
                {
                    Status = "Processing Error",
                    StatusCode = "501",
                    Message = ex.Message,

                };
                return BadRequest(data);
            }
        }
    }

    public class GetSurveyQuestionAnswer
    {
        public string QuestionId { get; set; }
    }
}