using FABModels.Model;
using FABTenantApi.Classes;
using FABTenantD2Api.Classes;
using FABTenantD2Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Microsoft.Security.Application;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static FABTenantD2Api.Classes.ValidateQueryParam;

namespace FABTenantD2Api.Controllers
{
    [Route("api/Tenant/[controller]")]
    [ApiController]
    public class ServiceRequestsController : ControllerBase
    {
        Decryption Decryption = new Decryption();
        private readonly FABAppDbContext _dbContext;
        private readonly INotificationRepository _notificationRepository;

        [Route("UploadServiceRequestFiles2")]
        public async Task<IActionResult> UploadServiceRequestFiles2()
        {
            return Content("1");
        }

        private IConfiguration _config;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ServiceRequestsController(IConfiguration config, ITokenService tokenService, ITenantRepository tenantRepository, FABAppDbContext dbContext, INotificationRepository notificationRepository, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _config = config;
            _tenantRepository = tenantRepository;
            _notificationRepository = notificationRepository;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _settings = new Settings();

            _tenantRepository.InsertErrorLog("Constructor");
        }

        [Route("GetTenantServiceRequests")]
        [HttpPost]
        public ActionResult<string> GetTenantServiceRequests([FromBody] GetTenantServiceRequests requestModel)
        {
            _tenantRepository.InsertErrorLog("GetTenantServiceRequests");
            try
            {
                List<ServiceRequest> ServiceRequestData = new List<ServiceRequest>();
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
                    if (CrossSiteScriptingValidation.IsDangerousParameter(requestModel.type, out _))
                    {
                        var dataBadReq = new
                        {
                            Status = "BadRequest",
                            Message = "Invalid Request.",
                            StatusCode = "500"
                        };
                        return BadRequest(dataBadReq);
                    }
                    var typeParam = new SqlParameter("@Type", requestModel.type)
                    {
                        Size = 10
                    };

                    var SqlPrameterParam = new SqlParameter("@Search", requestModel.search)
                    {
                        Size = 100
                    };

                    if (string.IsNullOrWhiteSpace(requestModel.type))
                    {
                        typeParam = new SqlParameter("@Type", DBNull.Value);
                    }
                    var Query = $@"SELECT CaseNo AS RequestNo
	                    ,ty.name AS CaseType
	                    ,ty.code
	                    ,ISNULL(act.DueActionid, 0) AS DueActionid
	                    ,act.stageId
	                    ,stage.Name AS StageName
	                    ,cate.Name AS Category
	                    ,scate.Name AS SubCategory
	                    ,scate.NameAR AS SubCategoryAR
	                    ,cate.NameAR AS CategoryAR
	                    ,pu.UnitRefNo
	                    ,Format(ca.Dated, '{_settings.DateFormat()}') AS Date
	                    ,ass.Name AS Detail
	                    ,prop.PropertyName AS PropertyName
	                    ,prop.PropertyNameAR AS PropertyNameAR
	                    ,ISNULL(ca.Units, 0) AS Units
	                    ,sta.Name AS Status
	                    ,ISNULL(sta.NameAR, '') AS StatusAR
                    FROM CMS.Cases ca
                    LEFT JOIN cms.CaseTypes ty ON ty.Id = ca.CaseTypeId
                    LEFT JOIN cms.CaseServiceRequest csr ON csr.CaseId = ca.CaseNo
                    LEFT JOIN cms.CaseCategory cate ON cate.Id = csr.CaseCategouryId
                    LEFT JOIN cms.CaseSubCategory scate ON scate.Id = csr.CaseSubCatagouryId
                    LEFT JOIN Pms_Property prop ON prop.PropertyID = ca.PropertyId
                    LEFT JOIN cms.CaseStatuses sta ON sta.Id = ca.StatusId
                    LEFT JOIN cms.InitialAssesments ass ON ass.Id = ca.InnitialAssesmentId
                    LEFT JOIN Pms_ContractUnits pcu ON pcu.ContractUnitID = ca.ContractUnitID
                    LEFT JOIN cms.Customer cus ON cus.CustomerId = ca.CaseOwnerId
                    LEFT JOIN cms.ContractDueAction act ON act.CaseId = ca.CaseNo
                    LEFT JOIN cms.stages stage ON stage.StageId = act.StageId
                    LEFT JOIN Pms_Unit pu ON pu.UnitID = pcu.UnitID
                    WHERE cus.UserId = {UserId}
	                    AND cate.Type=@Type
                       ";


                    if (!string.IsNullOrWhiteSpace(requestModel.search))
                    {
                        Query += $@" AND(
                             CaseNo like '%' + @Search + '%' OR
                             cate.Name like '%' + @Search + '%' OR
                             cate.NameAR like '%' + @Search + '%'
                           )";
                    }

                    if (string.IsNullOrWhiteSpace(requestModel.search))
                    {
                        SqlPrameterParam = new SqlParameter("@Search", DBNull.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(requestModel.fromDate) && !string.IsNullOrWhiteSpace(requestModel.toDate))
                    {
                        Query += $@" AND CAST(ca.Dated AS DATE) >= @FromDate AND CAST(ca.Dated AS DATE) <= @ToDate";
                    }

                    string FromDate = null;
                    string ToDate = null;

                    string pattern = "MM-dd-yyyy";
                    CultureInfo provider = CultureInfo.InvariantCulture;

                    if (!string.IsNullOrWhiteSpace(requestModel.fromDate))
                    {

                        DateTime FromDate1 = DateTime.ParseExact(requestModel.fromDate, pattern, provider);
                        FromDate = FromDate1.ToString("yyyy-MM-dd");
                    }
                    if (!string.IsNullOrWhiteSpace(requestModel.toDate))
                    {
                        //ToDate = DateTime.ParseExact(requestModel.toDate, pattern, provider);

                        DateTime tod = DateTime.ParseExact(requestModel.toDate, pattern, provider);
                        ToDate = tod.ToString("yyyy-MM-dd");
                    }

                    //27-Aug-2022
                    var FromDateSql = new SqlParameter("@FromDate", FromDate); //requestBody.ContractDateFrom);//add paramter

                    if (FromDate == null)
                    {
                        FromDateSql = new SqlParameter("@FromDate", DBNull.Value);
                    }

                    var ToDateParam = new SqlParameter("@ToDate", ToDate);//requestBody.ContractDateTo);//add paramter
                    if (ToDate == null)
                    {
                        ToDateParam = new SqlParameter("@ToDate", DBNull.Value);
                    }
                    Query = Query + $@"
                    ORDER BY CaseNo DESC";

                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);

                    var Charges = Conn.ExecuteDataTableSqlDA(conn, Query, typeParam, SqlPrameterParam, FromDateSql, ToDateParam);


                    if (Charges.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ServiceRequest>(Charges);
                        ServiceRequestData = ListToBeSend;
                    }
                    if (ServiceRequestData.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            ServiceRequests = ServiceRequestData,
                            Message = "Data Found.",
                            ResponseMessage = _settings.PaymentMessage
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            ServiceRequests = ServiceRequestData,
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
                    Status = "Processing Error" + ex.Message,
                    StatusCode = "501",
                    Message = "Internal Server Error",

                };
                return BadRequest(data);
            }
        }

        [Route("GetCaseTypes")]
        [HttpPost]
        public ActionResult<string> GetCaseTypes()
        {

            try
            {
                List<CaseTypes> ServiceRequestData = new List<CaseTypes>();
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
                    var ServiceRequestQuery = $@"select Id, Name, ISNULL(NameAR,'') as NameAR from cms.CaseTypes";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Charges = Conn.ExecuteDataTableSqlDA(conn, ServiceRequestQuery);

                    if (Charges.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<CaseTypes>(Charges);
                        ServiceRequestData = ListToBeSend;
                    }
                    if (ServiceRequestData.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            ServiceRequests = ServiceRequestData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            ServiceRequests = ServiceRequestData,
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

        [Route("GetUnitCategories")]
        [HttpPost]
        public ActionResult<string> GetUnitCategories()
        {

            try
            {
                List<UnitCategories> UnitCategoriesData = new List<UnitCategories>();
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
                    var UnitCategoriesQuery = $@"select UnitCategoryID, UnitCategoryName, UnitCategoryNameAR  from Pms_UnitCategory where Suspend=0";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Charges = Conn.ExecuteDataTableSqlDA(conn, UnitCategoriesQuery);

                    if (Charges.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<UnitCategories>(Charges);
                        UnitCategoriesData = ListToBeSend;
                    }
                    if (UnitCategoriesData.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            UnitCategories = UnitCategoriesData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            UnitCategories = UnitCategoriesData,
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

        [Route("GetCities")]
        [HttpPost]
        public ActionResult<string> GetCities()
        {
            var logged = _tenantRepository.InsertErrorLog("GetCities");
            try
            {
                List<CitiesVm> CitiesData = new List<CitiesVm>();
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
                    var CitiesQuery = $@"select CityID, CityName, CityNameAR  from Pms_City where Suspend=0";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Charges = Conn.ExecuteDataTableSqlDA(conn, CitiesQuery);

                    if (Charges.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<CitiesVm>(Charges);
                        CitiesData = ListToBeSend;
                    }
                    if (CitiesData.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            Cities = CitiesData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            Cities = CitiesData,
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




        [Route("GetTenantContractUnit")]
        [HttpPost]
        // [ValidateActionParameters]
        public ActionResult<string> GetTenantContractUnit([FromBody] GetTenantContractUnit requestModel)
        {

            try
            {
                int Tenantid = int.Parse(requestModel.Tenantid);
                List<ContractUnitVm> PropertiesData = new List<ContractUnitVm>();
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
                    //var tenantId = GetTenantId(UserId.Value);
                    //if (Tenantid != tenantId)
                    //{
                    //    var data = new
                    //    {
                    //        Status = "Invalid",
                    //        Message = "Invalid User."
                    //    };
                    //    return Unauthorized(data);
                    //}
                    var NewtenantId = _tenantRepository.GetTenantId(UserId.Value);
                    var PropertiesQuery = $@"
                    SELECT 
                         pc.ContractUnitID
	                    ,pu.UnitRefNo as UnitName
						,pu.UnitRefNoAR as UnitNameAR
	                    ,pcon.Contractno
	                    ,pp.PropertyName
						,pp.PropertyNameAR


                    FROM Pms_ContractUnits pc
                    LEFT JOIN Pms_Unit pu ON pc.UnitID = pu.UnitID
                    LEFT JOIN Pms_Contract pcon ON pcon.ContractID = pc.ContractID
                    LEFT JOIN Pms_Property pp ON pp.PropertyID = pcon.PropertyID
                    WHERE pc.Suspend = 0
                        AND pu.Suspend = 0
                        AND pcon.Suspend = 0
                        AND pp.Suspend = 0
                        AND pcon.ContractType = 1
                        AND(pcon.ToDate >= GETDATE())
                        AND pcon.TenantID = {NewtenantId}";

                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Properties = Conn.ExecuteDataTableSqlDA(conn, PropertiesQuery);

                    if (Properties.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractUnitVm>(Properties);
                        PropertiesData = ListToBeSend;
                    }
                    if (PropertiesData.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            Properties = PropertiesData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            Properties = PropertiesData,
                            Message = "No Data Found"
                        };
                        return Ok(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession"
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
                    Message = "Internal Server Error" + ex.Message,

                };
                return BadRequest(data);
            }
        }


        [Route("GetPropertyUnits")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetPropertyUnits(int Tenantid)
        {
            try
            {

                List<PropertyUnitVm> PropertiesData = new List<PropertyUnitVm>();
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
                    if (Tenantid != tenantId)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            Message = "Invalid User."
                        };
                        return Unauthorized(data);
                    }
                    var PropertiesQuery = $@"SELECT Pms_Contract.Contractno
	                    ,Pms_ContractUnits.UnitID
	                    ,PropertyName
	                    ,PropertyNameAR
                        FROM Pms_Contract
                        INNER JOIN Pms_ContractUnits ON Pms_Contract.ContractID = Pms_ContractUnits.ContractID
                        INNER JOIN Pms_Unit ON Pms_Contract.ContractID = Pms_Unit.ContractID
                        INNER JOIN Pms_Property ON Pms_Unit.PropertyID = Pms_Property.PropertyID
                        WHERE TenantID = {Tenantid}";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Properties = Conn.ExecuteDataTableSqlDA(conn, PropertiesQuery);

                    if (Properties.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<PropertyUnitVm>(Properties);
                        PropertiesData = ListToBeSend;
                    }
                    if (PropertiesData.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            Cities = PropertiesData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            Cities = PropertiesData,
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

        private int GetCaseOwnerId(int UserId)
        {
            int ownerid = 0;
            CaseOwnerViewModal caseOwnerView = new CaseOwnerViewModal();
            var ServiceRequestQuery = $@"Select top 1 CustomerId as CaseOwnerId From CMS.Customer Where UserId = {UserId}";
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


        [Route("GetServiceRequestInfo")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetServiceRequestInfo(int CaseNo)
        {
            //try
            //{
            var logged = _tenantRepository.InsertErrorLog("get service req");
            ServiceRequestInfoVm RequestDetailData = new ServiceRequestInfoVm();
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
                //security api update start
                // add caseownerid to get cases
                var userid = GetCaseOwnerId(UserId.Value);
                var isValidUserForCase = CaseInUserId(CaseNo, userid);

                if (!isValidUserForCase)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "Not a Valid User For Case."
                    };
                    return Unauthorized(data);
                }
                //security api update end

                var ServiceRequestQuery = $@" select CaseNo,CaseTypeId,PropertyId,Dated,ContactTimeId,InnitialAssesmentId,ServiceLevelId,Units,(select top 1 Description from cms.CaseServiceRequest where CaseId=CaseNo) as Description, StatusId, PreferredLanguageId from cms.Cases  where CaseNo={CaseNo}";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, ServiceRequestQuery);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ServiceRequestInfoVm>(RequestDetail);
                    RequestDetailData = ListToBeSend.FirstOrDefault();
                }
                if (RequestDetailData != null)
                {
                    var data = new
                    {
                        Status = "Ok",
                        Detail = RequestDetailData,
                        Message = "Data Found."
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        Detail = RequestDetailData,
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

        [Route("GetServiceRequestDetails")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetServiceRequestDetails([FromBody] CaseNoBody requestModel)
        {
            //try
            //{
            ServiceRequestDetailVm RequestDetailData = new ServiceRequestDetailVm();
            RequestDetailFeedbackVM requestFeedback = new RequestDetailFeedbackVM();
            ContractInfoVm ContractInfo = new ContractInfoVm();
            var logged = _tenantRepository.InsertErrorLog("GeContractUnitPropdetail");
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

                //security api update start
                //var userid = GetCaseOwnerId(UserId.Value);
                //var isValidUserForCase = CaseInUserId(int.Parse(requestModel.CaseNo), userid);

                //if (!isValidUserForCase)
                //{
                //    var data = new
                //    {
                //        Status = "Invalid",
                //        Message = "Not a Valid User For Case."
                //    };
                //    return Unauthorized(data);
                //}
                //security api update end

                var ServiceRequestQuery = $@"select cc.Type RequestType, CaseNo,cty.Name as CaseType,cty.Code,prop.PropertyName,prop.PropertyNameAR,FORMAT(Dated,'dd MM yyyy') as Date,FORMAT(Dated,'hh:mm') as Time ,ct.Name as ContactTiming,ass.Name as InitialAssesment ,level.Name as CaseServiceLevel ,ca.Units,(select top 1 Description from cms.CaseServiceRequest where CaseId=CaseNo) as Description, status.Name as status,status.NameAR as statusAR, lang.Name as language,ca.ContractUnitId,pu.UnitRefNo,cc.Name Category,csc.Name SubCategory, cc.NameAR CategoryAR,csc.NameAR SubCategoryAR ,ISNULL(COALESCE ( ca.OtherContactPersonName, cus.CustomerName  ),'') as 'UnitRefNoOtherContactPersonName',ISNULL(COALESCE ( ca.OtherContactPersonMobile, cus.CustomerContact),'') as 'OtherContactPersonMobile',csr.CaseCategouryId,csr.CaseSubCatagouryId, (select count(*) from cms.ServiceRequestFeedback  where CaseNo={int.Parse(requestModel.CaseNo)})CaseFeedback, (select count(*) from cms.SurveyAnswers  where ServiceRequestId={int.Parse(requestModel.CaseNo)} and IsCompleted=1)CaseSurvey   from cms.Cases  ca
                    left join cms.ContactTimings ct on ct.Id=ca.ContactTimeId
                    left join cms.CaseTypes cty on cty.Id=ca.CaseTypeId
                    left join Pms_Property prop on prop.PropertyID=ca.PropertyId
                    left join cms.InitialAssesments ass on ass.Id=ca.InnitialAssesmentId
                    left join cms.CaseServiceLevels level on level.Id=ca.ServiceLevelId
                    left join cms.CaseStatuses status on status.Id=ca.StatusId
                    left join cms.PreferredLanguages lang on lang.Id=ca.PreferredLanguageId
					left join Pms_ContractUnits pcc on pcc.ContractUnitID=ca.ContractUnitId
					left join Pms_Unit pu on pu.UnitID=pcc.UnitID
					left join cms.CaseServiceRequest csr on csr.CaseId=ca.CaseNo
				left join cms.CaseCategory cc on cc.Id=csr.CaseCategouryId
				left join cms.CaseSubCategory csc on csc.Id=csr.CaseSubCatagouryId
				left join cms.Customer cus on cus.CustomerId=ca.CaseOwnerId
                    where ca.CaseNo={int.Parse(requestModel.CaseNo)}";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, ServiceRequestQuery);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ServiceRequestDetailVm>(RequestDetail);
                    RequestDetailData = ListToBeSend.FirstOrDefault();
                }

                // contract Info
                var COntractInfo = $@"select Contractno, pc.ContractID, pp.PropertyName, pp.PropertyNameAR  from  cms.CaseContractDetail ccd
                                    join Pms_Contract pc on pc.ContractID=ccd.ContractId
                                    join Pms_Property pp on pc.PropertyID=pp.PropertyID 
                                    where ccd.CaseNo={int.Parse(requestModel.CaseNo)}";

                var COntractInfoRequestDetail = Conn.ExecuteDataTableSqlDA(conn, COntractInfo);

                if (COntractInfoRequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ContractInfoVm>(COntractInfoRequestDetail);
                    ContractInfo = ListToBeSend.FirstOrDefault();
                }
                // fm false upload document


                if (RequestDetailData != null)
                {
                    requestFeedback.CanCancel = false;
                    requestFeedback.CanReopen = false;
                    requestFeedback.CanAddFeedback = false;
                    requestFeedback.CanTakeSurvey = false;
                    requestFeedback.RequestClosed = false;
                    requestFeedback.CanUploadDocuments = true;
                    requestFeedback.CanUploadPhotos = true;
                    requestFeedback.CanChat = true;
                    RequestDetailData.status = RequestDetailData.status.Trim();

                    if (RequestDetailData.RequestType == "FM")
                    {
                        requestFeedback.CanUploadDocuments = false;
                    }

                    if (RequestDetailData.status == "Pending" || RequestDetailData.status == "Open" || RequestDetailData.status == "Re-Opened" || RequestDetailData.status == "Received")
                    {
                        requestFeedback.CanCancel = true;
                    }

                    if (RequestDetailData.status == "Closed")
                    {
                        requestFeedback.CanReopen = true;
                        requestFeedback.CanTakeSurvey = true;
                        requestFeedback.CanAddFeedback = true;
                        requestFeedback.RequestClosed = true;
                    }
                    if (RequestDetailData.status == "Cancelled")
                    {
                        requestFeedback.CanReopen = true;
                        requestFeedback.CanTakeSurvey = false;
                        requestFeedback.CanAddFeedback = false;
                        requestFeedback.RequestClosed = true;
                        requestFeedback.CanChat = false;
                        requestFeedback.CanUploadPhotos = false;
                        requestFeedback.CanUploadDocuments = false;
                    }

                    if (RequestDetailData.status == "Closed")
                    {
                        requestFeedback.CanAddFeedback = true;
                    }
                    else
                    {
                        requestFeedback.CanAddFeedback = false;
                    }

                    if (RequestDetailData.CaseFeedback > 0)
                    {
                        requestFeedback.CanAddFeedback = false;
                    }

                    //19-March-2023
                    // temparoray change  to true must check this later
                    requestFeedback.CanTakeSurvey = true;
                    if (RequestDetailData.CaseSurvey > 0)
                    {
                        requestFeedback.CanTakeSurvey = false;
                    }



                    var data = new
                    {
                        Status = "Ok",
                        Detail = RequestDetailData,
                        StatusInfo = requestFeedback,
                        Message = "Data Found.",
                        ContractInfo = ContractInfo,
                        StageInfo = _tenantRepository.CaseStageInfo(int.Parse(requestModel.CaseNo))
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        Detail = RequestDetailData,
                        StatusInfo = requestFeedback,
                        Message = "No Data Found.",
                        StageInfo = _tenantRepository.CaseStageInfo(int.Parse(requestModel.CaseNo))
                    };
                    return Ok(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession.",

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

        private bool CaseInUserId(int caseNo, int userId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Contracts = Conn.ExecuteDataTableSqlDA(conn, $"SELECT 1 FROM CMS.Cases WHERE (CaseOwnerId = {userId}) AND (CaseNo = {caseNo})");
            return Contracts.Rows.Count > 0;
        }
        private bool CheckInCloseOrCancelled(int caseNo, int userId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            //var Contracts = Conn.ExecuteDataTableSqlDA(conn, $"SELECT 1 FROM CMS.Cases c inner join cms.CaseStatuses cs on cs.Id = c.StatusId WHERE (c.CaseOwnerId = {userId}) AND (c.CaseNo = {caseNo}) and (cs.Name like '%Close%' or cs.Name like '%Cancel%')");

            var Contracts = Conn.ExecuteDataTableSqlDA(conn, $"SELECT 1 FROM CMS.Cases c inner join cms.CaseStatuses cs on cs.Id = c.StatusId WHERE  (c.CaseNo = {caseNo}) and (cs.Name  like '%Close%' or cs.Name  like '%Cancel%')");
            return Contracts.Rows.Count > 0;
        }
        //private bool CheckInDocumentUploaded(int caseNo, int userId)
        //{
        //    var constring = _settings.GetConnectionString();
        //    constring = constring.ToString();
        //    SqlConnection conn = new SqlConnection(constring);
        //    var Contracts = Conn.ExecuteDataTableSqlDA(conn, $"SELECT top 1 1 FROM CMS.ContractDueAction c inner join cms.Cases cs on cs.CaseNo = c.CaseId inner join cms.Stages s on s.StageId = c.StageId WHERE (cs.CaseOwnerId = {userId}) AND (cs.CaseNo = {caseNo}) and s.Name like '%Docs Submitted%'");
        //    return Contracts.Rows.Count > 0;
        //}


        [Route("GetContactTiming")]
        [HttpPost]
        public ActionResult<string> GetContactTiming()
        {
            try
            {

                List<GetContactTiming> ContactTimingData = new List<GetContactTiming>();
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
                    var ServiceRequestQuery = $@"select Id, Name from cms.ContactTimings";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, ServiceRequestQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<GetContactTiming>(RequestDetail);
                        ContactTimingData = ListToBeSend;
                    }
                    if (ContactTimingData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            ContactTiming = ContactTimingData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            ContactTiming = ContactTimingData,
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


        [Route("GetInitialAssesments")]
        [HttpPost]
        public ActionResult<string> GetInitialAssesments()
        {
            try
            {
                List<InitialAssesments> InitialAssesmentsData = new List<InitialAssesments>();
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
                    var InitialAssesmentsQuery = $@"select Id, Name, NameAR from cms.InitialAssesments";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, InitialAssesmentsQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<InitialAssesments>(RequestDetail);
                        InitialAssesmentsData = ListToBeSend;
                    }
                    if (InitialAssesmentsData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            ContactTiming = InitialAssesmentsData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            ContactTiming = InitialAssesmentsData,
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

        [Route("GetServiceLevels")]
        [HttpPost]
        public ActionResult<string> GetServiceLevels()
        {
            try
            {
                List<ServiceLevels> ServiceLevelsData = new List<ServiceLevels>();
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
                    var ServiceLevelsQuery = $@"select Id, Name from CMS.SLA";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, ServiceLevelsQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ServiceLevels>(RequestDetail);
                        ServiceLevelsData = ListToBeSend;
                    }
                    if (ServiceLevelsData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            ServiceLevels = ServiceLevelsData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            ServiceLevels = ServiceLevelsData,
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

        [Route("GetPreferredLanguages")]
        [HttpPost]
        public ActionResult<string> GetPreferredLanguages()
        {
            try
            {
                List<PreferredLanguages> PreferredLanguagesData = new List<PreferredLanguages>();
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
                    var PreferredLanguagesQuery = $@"select Id,Name  from cms.PreferredLanguages";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, PreferredLanguagesQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<PreferredLanguages>(RequestDetail);
                        PreferredLanguagesData = ListToBeSend;
                    }
                    if (PreferredLanguagesData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            PreferredLanguages = PreferredLanguagesData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            PreferredLanguages = PreferredLanguagesData,
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

        [Route("CaseCategory")]
        [HttpPost]
        public ActionResult<string> CaseCategory()
        {
            //try
            //{
            List<CaseType> CaseCategoryData = new List<CaseType>();
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
                var CaseCategoryQuery = $@"select id as Id, Name, NameAR,Type from cms.CaseCategory where ShowinApp=1";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, CaseCategoryQuery);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseType>(RequestDetail);
                    CaseCategoryData = ListToBeSend;
                }
                if (CaseCategoryData != null)
                {
                    var data = new
                    {
                        Status = "Ok",
                        CaseCategories = CaseCategoryData,
                        Message = "Data Found."
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        CaseCategories = CaseCategoryData,
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

        [Route("CaseSubCategory")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> CaseSubCategory([FromBody] CaseSubCategoryModel requestModel)
        {
            try
            {
                List<CaseType> CaseCategoryData = new List<CaseType>();
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
                    var CaseCategoryQuery = $@"select id as Id, Name,NameAR from cms.CaseSubCategory where CategoryId={requestModel.CategoryId}";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, CaseCategoryQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<CaseType>(RequestDetail);
                        CaseCategoryData = ListToBeSend;
                    }
                    if (CaseCategoryData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            CaseSubCategories = CaseCategoryData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            CaseSubCategories = CaseCategoryData,
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

        [Route("Circumstances")]
        [HttpPost]
        public ActionResult<string> Circumstances()
        {
            try
            {
                List<CaseType> CaseCategoryData = new List<CaseType>();
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
                    var CircumstancesQuery = $@"select id, Name from cms.Circumstances";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, CircumstancesQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<CaseType>(RequestDetail);
                        CaseCategoryData = ListToBeSend;
                    }
                    if (CaseCategoryData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            Circumstances = CaseCategoryData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            Circumstances = CaseCategoryData,
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

        [Route("LandlordApproval")]
        [HttpPost]
        public ActionResult<string> LandlordApproval()
        {
            try
            {
                List<CaseType> LandlordApprovalsData = new List<CaseType>();
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
                    var CircumstancesQuery = $@"select id, Name from cms.Save";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, CircumstancesQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<CaseType>(RequestDetail);
                        LandlordApprovalsData = ListToBeSend;
                    }
                    if (LandlordApprovalsData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            LandlordApproval = LandlordApprovalsData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            LandlordApproval = LandlordApprovalsData,
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








        [Route("SaveServiceRequestv1")]
        [HttpPost]
        public ActionResult<string> SaveServiceRequestv1(AddServiceRequest model)
        {

            ConfigValues configValues = new ConfigValues();
            UserInfo UserData = new UserInfo();
            if (User.Identity.IsAuthenticated)
            {

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
                        var tenantId = _tenantRepository.GetTenantId(UserId.Value);

                        if (CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.OtherContactPersonMobile, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.OtherContactPersonName, out _))
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

                        var RequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetUserValueQuery);

                        if (RequestDetail.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<UserInfo>(RequestDetail);
                            UserData = ListToBeSend.FirstOrDefault();
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

                        //insert data

                        int GernateCaseNumber = (maxNumber.MaxCase) + 1;

                        var UserInfo = _dbContext.Users.Where(p => p.UserId == UserId).FirstOrDefault();

                        var CaseOwnerId = _tenantRepository.CaseOwnerIdTransaction(sqlTran, con, UserId.Value, UserInfo.FullName, UserInfo.Mobile);

                        if (model.OtherContactPersonName == null)
                        {
                            model.OtherContactPersonName = UserInfo.FullName;
                        }
                        if (model.OtherContactPersonMobile == null)
                        {
                            model.OtherContactPersonMobile = UserInfo.Mobile;
                        }
                        if (model.ContactTimeId == null)
                        {
                            model.ContactTimeId = 0;
                        }

                        var refNo = _tenantRepository.GetRefNumberByContactUnitId(sqlTran, con, model.ContractUnitId);

                        var AddRequestQuery = $@"INSERT INTO CMS.Cases (
                                                     CaseNo
                                                     ,CaseTypeId
                                                     ,EmirateID
                                                     ,PropertyId
                                                     ,Dated
                                                     ,ContactTimeId
                                                     ,ServiceLevelId
                                                     ,CaseOwnerId
                                                     ,Email
                                                     ,ResidenceTel
                                                     ,WorkNo
                                                     ,MobileTel
                                                     ,PreferredLanguageId
                                                     ,StatusId
                                                     ,CityId
                                                     ,ContractUnitId
                                                     ,OtherContactPersonName
                                                     ,OtherContactPersonMobile
                                                     ,RoleId
                                                     ,Unitrefno
                                                     )
                                                    VALUES (
                                                     {GernateCaseNumber}
                                                     ,1
                                                     ,(
                                                      SELECT TOP 1 EmirateID
                                                      FROM Pms_ContractUnits pc
                                                      LEFT JOIN Pms_Unit pu ON pc.UnitID = pu.UnitID
                                                      LEFT JOIN Pms_Contract pcon ON pcon.ContractID = pc.ContractID
                                                      LEFT JOIN Pms_Property pp ON pp.PropertyID = pcon.PropertyID
                                                      WHERE pc.ContractUnitID = {model.ContractUnitId}
                                                      )
                                                     ,(
                                                      SELECT TOP 1 pcon.PropertyID
                                                      FROM Pms_ContractUnits pc
                                                      LEFT JOIN Pms_Unit pu ON pc.UnitID = pu.UnitID
                                                      LEFT JOIN Pms_Contract pcon ON pcon.ContractID = pc.ContractID
                                                      LEFT JOIN Pms_Property pp ON pp.PropertyID = pcon.PropertyID
                                                      WHERE pc.ContractUnitID = {model.ContractUnitId}
                                                      )
                                                     ,'{DateTime.UtcNow.AddHours(4)}'
                                                     ,{model.ContactTimeId}
                                                     ,ISNULL((
                                                       SELECT top 1 SLAid
                                                       FROM cms.SLARulesSet
                                                       WHERE CategoryId = {model.CaseCategoryId}
                                                        AND SubCategoryId = {model.CaseSubCategoryId}
                                                        AND TypeId = 1
                                                       ), 0)
                                                     ,'{CaseOwnerId}'
                                                     ,'{UserData.Email}'
                                                     ,''
                                                     ,''
                                                     ,'{UserData.Mobile}'
                                                     ,(
                                                      SELECT TOP 1 Id
                                                      FROM cms.PreferredLanguages
                                                      WHERE Name = 'English'
                                                      )
                                                     ,{configValues.DefaultCaseStatus}
                                                     ,(
                                                      SELECT TOP 1 isnull(CityID, 0) AS CityID
                                                      FROM Pms_Property
                                                      WHERE PropertyID = (
                                                        SELECT pcon.PropertyID
                                                        FROM Pms_ContractUnits pc
                                                        LEFT JOIN Pms_Unit pu ON pc.UnitID = pu.UnitID
                                                        LEFT JOIN Pms_Contract pcon ON pcon.ContractID =                                                pc.ContractID
                                                        LEFT JOIN Pms_Property pp ON pp.PropertyID =                                                    pcon.PropertyID
                                                        WHERE pc.ContractUnitID = {model.ContractUnitId}
                                                        )
                                                      )
                                                     ,{model.ContractUnitId}
                                                     ,N'{model.OtherContactPersonName}'
                                                     ,@OtherContactPersonMobile
                                                     ,1
                                                     ,'{refNo}')";

                        var sqlNameParameter = new SqlParameter("@OtherContactPersonName", model.OtherContactPersonName);
                        if (string.IsNullOrWhiteSpace(model.OtherContactPersonName))
                        {
                            sqlNameParameter = new SqlParameter("@OtherContactPersonName", DBNull.Value);
                        }
                        var sqlMobileParameter = new SqlParameter("@OtherContactPersonMobile", model.OtherContactPersonMobile);
                        if (string.IsNullOrWhiteSpace(model.OtherContactPersonMobile))
                        {
                            sqlMobileParameter = new SqlParameter("@OtherContactPersonMobile", DBNull.Value);
                        }

                        SqlParameter[] sqlParameters = new SqlParameter[]
                        {
                            new SqlParameter("@OtherContactPersonName",           model.OtherContactPersonName),
                            new SqlParameter("@OtherContactPersonMobile",         model.OtherContactPersonMobile),
                        };


                        var RequestDetail2 = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, AddRequestQuery, sqlParameters);

                        int? CaseEmirate = 0;
                        var GetEmirateId = $@"select EmirateID from Pms_ContractUnits pc 
                                                left join Pms_Unit pu on pc.UnitID=pu.UnitID 
                                                left join Pms_Contract pcon on pcon.ContractID=pc.ContractID
                                                left join Pms_Property pp on pp.PropertyID=pcon.PropertyID
                                                where pc.ContractUnitID={model.ContractUnitId}";


                        var EmirateRequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetEmirateId);

                        if (EmirateRequestDetail.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<CaseEmirateVm>(EmirateRequestDetail);
                            CaseEmirate = ListToBeSend.Select(p => p.EmirateID).FirstOrDefault();
                        }
                        int? CasePropertyId = 0;
                        var GetPropertyId = $@"SELECT TOP 1 pcon.PropertyID
                                                      FROM Pms_ContractUnits pc
                                                      LEFT JOIN Pms_Unit pu ON pc.UnitID = pu.UnitID
                                                      LEFT JOIN Pms_Contract pcon ON pcon.ContractID = pc.ContractID
                                                      LEFT JOIN Pms_Property pp ON pp.PropertyID = pcon.PropertyID
                                                      WHERE pc.ContractUnitID = {model.ContractUnitId}";


                        var PropertyIdRequestDetail = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetEmirateId);

                        if (PropertyIdRequestDetail.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<CaseEmirateVm>(PropertyIdRequestDetail);
                            CasePropertyId = ListToBeSend.Select(p => p.PropertyID).FirstOrDefault();
                        }

                        //Regex rRemScript = new Regex(@"<script[^>]*>[\s\S]*?</script>");
                        //model.Description = rRemScript.Replace(model.Description, "");

                        // Later add Arabic part also
                        //model.Description = _settings.RemoveSpecialChar(model.Description);

                        var SaveRequestQuery = $@"INSERT INTO CMS.CaseServiceRequest (
                                             Description
                                             ,ProposedRemedy
                                             ,CircumstanceId
                                             ,CaseCategouryId
                                             ,CaseSubCatagouryId
                                             ,ReasonForVocation
                                             ,CaseId
                                             )
                                            VALUES (
                                              N'{model.Description}'
                                             ,N'{model.Description}'
                                             ,(
                                              SELECT TOP 1 Id
                                              FROM cms.Circumstances
                                              )
                                             ,{model.CaseCategoryId}
                                             ,{model.CaseSubCategoryId}
                                             ,N'{model.Description}'
                                             ,{GernateCaseNumber}
                                             );";



                        Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, SaveRequestQuery);

                        //asign owner id, 1 is id for casetype service request in table
                        var WorkGroupId = _tenantRepository.UpdateCaseUserIdTransaction(sqlTran, con, model.CaseCategoryId, model.CaseSubCategoryId, configValues.ServiceRequestCaseTypeId, CaseEmirate, CasePropertyId, GernateCaseNumber);


                        //get work group to send email 
                        //var workgroupId = _tenantRepository.GetWorkGroupIdTransaction(sqlTran, con, model.CaseCategoryId, model.CaseSubCategoryId, configValues.ServiceRequestCaseTypeId, CaseEmirate, CasePropertyId, GernateCaseNumber);

                        CaseInfoModel CaseInfo = new CaseInfoModel();
                        CaseInfo = _tenantRepository.GetCaseInfoTrnsaction(sqlTran, con, GernateCaseNumber);



                        var AddTenantPushNotification = _tenantRepository.prepareAndAddPushNotificationTransaction(sqlTran, con, 1, Convert.ToInt32(model.CaseCategoryId), CaseEmirate, 1, GernateCaseNumber, UserId.Value, "Tenant");

                        var AddTenantEmailNotification = _tenantRepository.prepareAndAddEmailNotificationTransaction(sqlTran, con, 1, Convert.ToInt32(model.CaseCategoryId), CaseEmirate, 1, GernateCaseNumber, UserData.Email);


                        //Generate tickets against case
                        var Notification = _tenantRepository.AddTicketNewSerivceRequestCaseTransaction(sqlTran, con, GernateCaseNumber, UserId, CaseOwnerId);

                        AddServiceRequestDetailVm CheckserviceRequest = new AddServiceRequestDetailVm();
                        var GetCaseQuery = $@"select CaseNo from cms.Cases where CaseNo={GernateCaseNumber}";


                        var CheckCase = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, con, GetCaseQuery);

                        if (CheckCase.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<AddServiceRequestDetailVm>(CheckCase);
                            CheckserviceRequest = ListToBeSend.FirstOrDefault();

                            var CategoryInfo = _tenantRepository.CategoryInfoTransaction(sqlTran, con, model.CaseCategoryId.Value);
                            if (CategoryInfo == "PM")
                            {

                                var categoryid = _tenantRepository.GetCaseServiceRequestCategoryIdTransaction(sqlTran, con, GernateCaseNumber);
                                bool isRenewal = false;
                                if (categoryid > 0 && categoryid == 8)
                                {
                                    isRenewal = true;
                                }


                                bool UpdateAssignee = _tenantRepository.AssignPMAssineeeTransaction(sqlTran, con, GernateCaseNumber, "Insert", isRenewal);
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
                            else if (CategoryInfo == "FM")
                            {
                                _tenantRepository.AutoAssignContractorTransaction(sqlTran, con, GernateCaseNumber, UserId.Value);
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

                    var dataBadReq = new
                    {
                        Status = "Internal Server Error",
                        Message = "Fail to save Service invoice " + ex.Message,
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
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





        //[Route("SaveServiceRequest")]
        //[HttpPost]
        //public ActionResult<string> SaveServiceRequest(AddServiceRequest model)
        //{
        //    //try
        //    //{
        //    ConfigValues configValues = new ConfigValues();
        //    UserInfo UserData = new UserInfo();
        //    if (User.Identity.IsAuthenticated)
        //    {
        //        var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
        //        int? UserId = _settings.GetUserIdByToken(_bearer_token);
        //        if (UserId == null)
        //        {
        //            var data = new
        //            {
        //                Status = "Invalid",
        //                Message = "InvalidSession."
        //            };
        //            return Unauthorized(data);
        //        }
        //        var tenantId = _tenantRepository.GetTenantId(UserId.Value);

        //        if (CrossSiteScriptingValidation.IsDangerousParameter(model.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.OtherContactPersonMobile, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.OtherContactPersonName, out _))
        //        {
        //            var dataBadReq = new
        //            {
        //                Status = "BadRequest",
        //                Message = "Invalid Request.",
        //                StatusCode = "500"
        //            };
        //            return BadRequest(dataBadReq);
        //        }

        //        // Get User record
        //        var GetUserValueQuery = $@"select UserId, Email,Mobile from app.Users where UserId={UserId}";
        //        var constring = _settings.GetConnectionString();
        //        constring = constring.ToString();
        //        SqlConnection conn = new SqlConnection(constring);
        //        try
        //        {
        //            var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetUserValueQuery);

        //            if (RequestDetail.Rows.Count > 0)
        //            {
        //                var ListToBeSend = Conn.ToListof<UserInfo>(RequestDetail);
        //                UserData = ListToBeSend.FirstOrDefault();
        //            }
        //        }
        //        catch (Exception ex)
        //        {

        //            var error = _tenantRepository.InsertErrorLog(ex.Message);
        //        }

        //        //insert data
        //        MaxNumber maxNumber = new MaxNumber();
        //        var GetMaxValueQuery = $@"SELECT ISNULL(MAX(CaseNo),0) as MaxCase FROM cms.Cases";
        //        var MaxRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetMaxValueQuery);

        //        if (MaxRequestDetail.Rows.Count > 0)
        //        {
        //            var ListToBeSend = Conn.ToListof<MaxNumber>(MaxRequestDetail);
        //            maxNumber = ListToBeSend.FirstOrDefault();
        //        }

        //        //insert data

        //        int GernateCaseNumber = (maxNumber.MaxCase) + 1;

        //        var UserInfo = _dbContext.Users.Where(p => p.UserId == UserId).FirstOrDefault();

        //        var CaseOwnerId = _tenantRepository.CaseOwnerId(UserId.Value, UserInfo.FullName, UserInfo.Mobile);

        //        if (model.OtherContactPersonName == null)
        //        {
        //            model.OtherContactPersonName = UserInfo.FullName;
        //        }
        //        if (model.OtherContactPersonMobile == null)
        //        {
        //            model.OtherContactPersonMobile = UserInfo.Mobile;
        //        }
        //        if (model.ContactTimeId == null)
        //        {
        //            model.ContactTimeId = 0;
        //        }
        //        //(select id from cms.CaseTypes where code ={ configValues.SRCaseType}),
        //        //change this to 1

        //        /*
        //        var AddRequestQuery = $@"INSERT INTO CMS.Cases(CaseNo, CaseTypeId, EmirateID, PropertyId, Dated, ContactTimeId,  ServiceLevelId,CaseOwnerId, Email,ResidenceTel, WorkNo, MobileTel, PreferredLanguageId, StatusId, CityId,ContractUnitId,OtherContactPersonName,OtherContactPersonMobile, RoleId)
        //            VALUES ({GernateCaseNumber} ,
        //        1,
        //        (select top 1 EmirateID from Pms_ContractUnits pc 
        //            left join Pms_Unit pu on pc.UnitID=pu.UnitID 
        //            left join Pms_Contract pcon on pcon.ContractID=pc.ContractID
        //            left join Pms_Property pp on pp.PropertyID=pcon.PropertyID
        //            where pc.ContractUnitID={model.ContractUnitId}), 
        //    (select pcon.PropertyID from Pms_ContractUnits pc 
        //            left join Pms_Unit pu on pc.UnitID=pu.UnitID 
        //            left join Pms_Contract pcon on pcon.ContractID=pc.ContractID
        //            left join Pms_Property pp on pp.PropertyID=pcon.PropertyID
        //            where pc.ContractUnitID={model.ContractUnitId}),
        //    '{DateTime.UtcNow.AddHours(4)}',
        //    {model.ContactTimeId}, 
        //    ISNULL((Select SLAid from cms.SLARulesSet where CategoryId = {model.CaseCategoryId} and SubCategoryId = {model.CaseSubCategoryId} and TypeId= 1 and EmirateId = (select top 1 EmirateID from Pms_ContractUnits pc  left join Pms_Unit pu on pc.UnitID=pu.UnitID  left join Pms_Contract pcon on pcon.ContractID=pc.ContractID left join Pms_Property pp on pp.PropertyID=pcon.PropertyID where pc.ContractUnitID = {model.ContractUnitId})),0),
        //        '{CaseOwnerId}','{UserData.Email}','', '', '{UserData.Mobile}', 
        //    (select top 1 Id  from cms.PreferredLanguages where Name = 'English'),{configValues.DefaultCaseStatus}, 
        //    (select isnull(CityID ,0) as CityID from Pms_Property where PropertyID=(select pcon.PropertyID from Pms_ContractUnits pc 
        //            left join Pms_Unit pu on pc.UnitID=pu.UnitID 
        //            left join Pms_Contract pcon on pcon.ContractID=pc.ContractID
        //            left join Pms_Property pp on pp.PropertyID=pcon.PropertyID
        //            where pc.ContractUnitID={model.ContractUnitId})),{model.ContractUnitId},@OtherContactPersonName,@OtherContactPersonMobile, 1)";

        //        */

        //        var AddRequestQuery = $@"
        //                            INSERT INTO CMS.Cases (
        //                                             CaseNo
        //                                             ,CaseTypeId
        //                                             ,EmirateID
        //                                             ,PropertyId
        //                                             ,Dated
        //                                             ,ContactTimeId
        //                                             ,ServiceLevelId
        //                                             ,CaseOwnerId
        //                                             ,Email
        //                                             ,ResidenceTel
        //                                             ,WorkNo
        //                                             ,MobileTel
        //                                             ,PreferredLanguageId
        //                                             ,StatusId
        //                                             ,CityId
        //                                             ,ContractUnitId
        //                                             ,OtherContactPersonName
        //                                             ,OtherContactPersonMobile
        //                                             ,RoleId
        //                                             )
        //                                            VALUES (
        //                                             {GernateCaseNumber}
        //                                             ,1
        //                                             ,(
        //                                              SELECT TOP 1 EmirateID
        //                                              FROM Pms_ContractUnits pc
        //                                              LEFT JOIN Pms_Unit pu ON pc.UnitID = pu.UnitID
        //                                              LEFT JOIN Pms_Contract pcon ON pcon.ContractID = pc.ContractID
        //                                              LEFT JOIN Pms_Property pp ON pp.PropertyID = pcon.PropertyID
        //                                              WHERE pc.ContractUnitID = {model.ContractUnitId}
        //                                              )
        //                                             ,(
        //                                              SELECT TOP 1 pcon.PropertyID
        //                                              FROM Pms_ContractUnits pc
        //                                              LEFT JOIN Pms_Unit pu ON pc.UnitID = pu.UnitID
        //                                              LEFT JOIN Pms_Contract pcon ON pcon.ContractID = pc.ContractID
        //                                              LEFT JOIN Pms_Property pp ON pp.PropertyID = pcon.PropertyID
        //                                              WHERE pc.ContractUnitID = {model.ContractUnitId}
        //                                              )
        //                                             ,'{DateTime.UtcNow.AddHours(4)}'
        //                                             ,{model.ContactTimeId}
        //                                             ,ISNULL((
        //                                               SELECT SLAid
        //                                               FROM cms.SLARulesSet
        //                                               WHERE CategoryId = {model.CaseCategoryId}
        //                                                AND SubCategoryId = {model.CaseSubCategoryId}
        //                                                AND TypeId = 1
        //                                                AND EmirateId = (
        //                                                 SELECT TOP 1 EmirateID
        //                                                 FROM Pms_ContractUnits pc
        //                                                 LEFT JOIN Pms_Unit pu ON pc.UnitID = pu.UnitID
        //                                                 LEFT JOIN Pms_Contract pcon ON pcon.ContractID =                                                pc.ContractID
        //                                                 LEFT JOIN Pms_Property pp ON pp.PropertyID =                                                    pcon.PropertyID
        //                                                 WHERE pc.ContractUnitID = {model.ContractUnitId}
        //                                                 )
        //                                               ), 0)
        //                                             ,'{CaseOwnerId}'
        //                                             ,'{UserData.Email}'
        //                                             ,''
        //                                             ,''
        //                                             ,'{UserData.Mobile}'
        //                                             ,(
        //                                              SELECT TOP 1 Id
        //                                              FROM cms.PreferredLanguages
        //                                              WHERE Name = 'English'
        //                                              )
        //                                             ,{configValues.DefaultCaseStatus}
        //                                             ,(
        //                                              SELECT TOP 1 isnull(CityID, 0) AS CityID
        //                                              FROM Pms_Property
        //                                              WHERE PropertyID = (
        //                                                SELECT pcon.PropertyID
        //                                                FROM Pms_ContractUnits pc
        //                                                LEFT JOIN Pms_Unit pu ON pc.UnitID = pu.UnitID
        //                                                LEFT JOIN Pms_Contract pcon ON pcon.ContractID =                                                pc.ContractID
        //                                                LEFT JOIN Pms_Property pp ON pp.PropertyID =                                                    pcon.PropertyID
        //                                                WHERE pc.ContractUnitID = {model.ContractUnitId}
        //                                                )
        //                                              )
        //                                             ,{model.ContractUnitId}
        //                                             ,@OtherContactPersonName
        //                                             ,@OtherContactPersonMobile
        //                                             ,1
        //                                             )

        //               ";

        //        var sqlNameParameter = new SqlParameter("@OtherContactPersonName", model.OtherContactPersonName);
        //        if (string.IsNullOrWhiteSpace(model.OtherContactPersonName))
        //        {
        //            sqlNameParameter = new SqlParameter("@OtherContactPersonName", DBNull.Value);
        //        }
        //        var sqlMobileParameter = new SqlParameter("@OtherContactPersonMobile", model.OtherContactPersonMobile);
        //        if (string.IsNullOrWhiteSpace(model.OtherContactPersonMobile))
        //        {
        //            sqlMobileParameter = new SqlParameter("@OtherContactPersonMobile", DBNull.Value);
        //        }
        //        try
        //        {
        //            var RequestDetail2 = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery, sqlNameParameter, sqlMobileParameter);
        //        }
        //        catch (Exception ex)
        //        {

        //            var error = _tenantRepository.InsertErrorLog(ex.Message);
        //        }
        //        int? CaseEmirate = 0;
        //        var GetEmirateId = $@"select EmirateID from Pms_ContractUnits pc 
        //            left join Pms_Unit pu on pc.UnitID=pu.UnitID 
        //            left join Pms_Contract pcon on pcon.ContractID=pc.ContractID
        //            left join Pms_Property pp on pp.PropertyID=pcon.PropertyID
        //            where pc.ContractUnitID={model.ContractUnitId}";

        //        try
        //        {
        //            var EmirateRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetEmirateId);

        //            if (EmirateRequestDetail.Rows.Count > 0)
        //            {
        //                var ListToBeSend = Conn.ToListof<CaseEmirateVm>(EmirateRequestDetail);
        //                CaseEmirate = ListToBeSend.Select(p => p.EmirateID).FirstOrDefault();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            var error = _tenantRepository.InsertErrorLog(ex.Message);
        //        }

        //        //Regex rRemScript = new Regex(@"<script[^>]*>[\s\S]*?</script>");
        //        //model.Description = rRemScript.Replace(model.Description, "");

        //        // Later add Arabic part also
        //        //model.Description = _settings.RemoveSpecialChar(model.Description);

        //        var SaveRequestQuery = $@"INSERT INTO CMS.CaseServiceRequest (
        //                                     Description
        //                                     ,ProposedRemedy
        //                                     ,CircumstanceId
        //                                     ,CaseCategouryId
        //                                     ,CaseSubCatagouryId
        //                                     ,ReasonForVocation
        //                                     ,CaseId
        //                                     )
        //                                    VALUES (
        //                                      N'{model.Description}'
        //                                     ,N'{model.Description}'
        //                                     ,(
        //                                      SELECT TOP 1 Id
        //                                      FROM cms.Circumstances
        //                                      )
        //                                     ,{model.CaseCategoryId}
        //                                     ,{model.CaseSubCategoryId}
        //                                     ,N'{model.Description}'
        //                                     ,{GernateCaseNumber}
        //                                     );";


        //        //var sqlDescriptionParameter = new SqlParameter("@Description",  model.Description);


        //        SqlParameter[] sqlDescriptionParameter = new SqlParameter[]
        //               {
        //                    new SqlParameter("@Description",     SqlDbType.NVarChar,500,model.Description),

        //           };

        //        //var sqlDescriptionParameter = new SqlParameter("@Description", SqlDbType.NVarChar, 500).Value=model.Description;
        //        //

        //        if (string.IsNullOrWhiteSpace(model.Description))
        //        {
        //            // sqlDescriptionParameter = new SqlParameter("@Description", DBNull.Value);
        //        }

        //        try
        //        {
        //            var RequestDetail3 = Conn.ExecuteDataTableSqlDA(conn, SaveRequestQuery);
        //        }
        //        catch (Exception ex)
        //        {
        //            var error = _tenantRepository.InsertErrorLog("SaveCaseServiceRequest->CMS.CaseServiceRequest" + ex.Message);
        //        }
        //        try
        //        {
        //            //_tenantRepository.SetSLA(Convert.ToInt32(model.CaseTypeId), Convert.ToInt32(model.CaseCategoryId), Convert.ToInt32(model.CaseSubCategoryId), Convert.ToInt32(CaseEmirate), GernateCaseNumber);
        //        }
        //        catch (Exception ex)
        //        {
        //            _tenantRepository.InsertErrorLog(ex.Message);

        //        }
        //        try
        //        {
        //            //asign owner id, 1 is id for casetype service request in table
        //            var UpdateUser = _tenantRepository.UpdateCaseUserId(model.CaseCategoryId, configValues.ServiceRequestCaseTypeId, CaseEmirate, GernateCaseNumber);
        //            //get work group to send email 
        //            var workgroupId = _tenantRepository.GetWorkGroupId(model.CaseCategoryId, configValues.ServiceRequestCaseTypeId, CaseEmirate, GernateCaseNumber);

        //            CaseInfoModel CaseInfo = new CaseInfoModel();
        //            try
        //            {
        //                CaseInfo = _tenantRepository.GetCaseInfo1(GernateCaseNumber);
        //            }
        //            catch (Exception ex)
        //            {

        //                _tenantRepository.InsertErrorLog(ex.Message);
        //            }



        //            try
        //            {




        //                //_tenantRepository.InsertErrorLog("CaseInfo.CaseTypeId" + CaseInfo.CaseTypeId);
        //                //_tenantRepository.InsertErrorLog("CaseInfo.CaseCategouryId" + CaseInfo.CaseCategouryId);
        //                //_tenantRepository.InsertErrorLog("CaseInfo.EmirateId" + CaseInfo.EmirateId);

        //                //var AddPushNotification = _tenantRepository.prepareAndAddPushNotification(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 1, GernateCaseNumber, UserId.Value, "Tenant");

        //                //int CaseCategoryId = ;


        //                var AddPushNotification = _tenantRepository.prepareAndAddPushNotification(1, Convert.ToInt32(model.CaseCategoryId), CaseEmirate, 1, GernateCaseNumber, UserId.Value, "Tenant");




        //            }
        //            catch (Exception ex)
        //            {
        //                var message = "prepareAndAddPushNotification" + ex.Message + " " + ex.InnerException;
        //                var error = _tenantRepository.InsertErrorLog(message);
        //            }

        //            if (UpdateUser != null)
        //            {
        //                try
        //                {
        //                    var CaseOwnerEmail = _tenantRepository.CaseOwnerEmail(UpdateUser);
        //                    var AddEmailNotification = _tenantRepository.prepareAndAddEmailNotification(1, Convert.ToInt32(model.CaseCategoryId), CaseEmirate, 1, GernateCaseNumber, CaseOwnerEmail);

        //                }
        //                catch (Exception ex)
        //                {
        //                    var message = ex.Message + " " + ex.InnerException;
        //                    var error = _tenantRepository.InsertErrorLog(message);
        //                }
        //            }
        //            // email code change this code is replace sohail
        //            //if (workgroupId != null)
        //            //{
        //            //    // send email to workgroup
        //            //    try
        //            //    {
        //            //        var workgroupemail = _tenantRepository.GetWorkGroupEmail(workgroupId, CaseInfo.EmirateId);
        //            //        if (!string.IsNullOrWhiteSpace(workgroupemail))
        //            //        {
        //            //            var AddEmailNotification = _tenantRepository.prepareAndAddEmailNotification(6, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 20, GernateCaseNumber, workgroupemail);
        //            //        }
        //            //    }
        //            //    catch (Exception ex)
        //            //    {

        //            //        var message = ex.Message + " " + ex.InnerException;
        //            //        var error = _tenantRepository.InsertErrorLog(message);
        //            //    }
        //            //}
        //        }
        //        catch (Exception ex)
        //        {
        //            var logged = _tenantRepository.InsertErrorLog(ex.Message);
        //        }

        //        //Generate tickets against case
        //        var Notification = _tenantRepository.AddTicketNewSerivceRequestCase(GernateCaseNumber, UserId, CaseOwnerId);

        //        AddServiceRequestDetailVm CheckserviceRequest = new AddServiceRequestDetailVm();
        //        var GetCaseQuery = $@"select CaseNo from cms.Cases where CaseNo={GernateCaseNumber}";

        //        try
        //        {
        //            var CheckCase = Conn.ExecuteDataTableSqlDA(conn, GetCaseQuery);

        //            if (CheckCase.Rows.Count > 0)
        //            {
        //                var ListToBeSend = Conn.ToListof<AddServiceRequestDetailVm>(CheckCase);
        //                CheckserviceRequest = ListToBeSend.FirstOrDefault();

        //                var CategoryInfo = _tenantRepository.CategoryInfo(model.CaseCategoryId.Value);
        //                if (CategoryInfo == "PM")
        //                {
        //                    try
        //                    {
        //                        var categoryid = _tenantRepository.GetCaseServiceRequestCategoryId(GernateCaseNumber);
        //                        bool isRenewal = false;
        //                        if (categoryid > 0 && categoryid == 8)
        //                        {
        //                            isRenewal = true;
        //                        }
        //                        var UpdateAssignee = _tenantRepository.AssignPMAssineee(GernateCaseNumber, "Insert", isRenewal);
        //                        var getCaseInfo = _tenantRepository.GetCaseDetailInfo(GernateCaseNumber);
        //                        if (getCaseInfo != null)
        //                        {

        //                            var assignee = getCaseInfo.Assignee;
        //                            var email = getCaseInfo.WorkGroupEmail;
        //                            var category = getCaseInfo.CategoryName;
        //                            var title = "New Service Request";
        //                            var subject = "New Service Request Assigned";
        //                            var body = $@"Dear Team, <br/> A new case no: {GernateCaseNumber} is generated         with category {category} and is assigned to {assignee}. <br/>  <br/> System             Generated Notification.";

        //                            try
        //                            {
        //                                var sendworkGroupEmail = _tenantRepository.AddNewEmailNotification(body, UserId.Value, subject, email, title, GernateCaseNumber);
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                var logged = _tenantRepository.InsertErrorLog(ex.Message); ;
        //                            }
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        var error = _tenantRepository.InsertErrorLog(ex.Message);
        //                    }
        //                }
        //                else if (CategoryInfo == "FM")
        //                {
        //                    try
        //                    {
        //                        var AssignContractor = _tenantRepository.AutoAssignContractor(GernateCaseNumber, UserId.Value);
        //                    }
        //                    catch (Exception ex)
        //                    {

        //                        var error = _tenantRepository.InsertErrorLog(ex.Message);
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            var error = _tenantRepository.InsertErrorLog(ex.Message);
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
        //            return NotFound(data);
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



        [Route("UpdateServiceRequest")]
        [HttpPost]
        public ActionResult<string> UpdateServiceRequest(UpdateServiceRequest model)
        {

            try
            {
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
                    //security api update start
                    var userid = GetCaseOwnerId(UserId.Value);
                    var isValidUserForCase = CaseInUserId(model.CaseNo, userid);

                    if (!isValidUserForCase)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            Message = "Not a Valid User For Case."
                        };
                        return Unauthorized(data);
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
                    //security api update end
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);

                    var AddRequestQuery = $@"update cms.Cases set PropertyId={model.PropertyId},  StatusId={model.StatusId} where CaseNo={model.CaseNo}";
                    var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery);

                    var UpdateRequestQuery = $@"update cms.CaseServiceRequest set CaseCategouryId={model.CaseCategoryId},  Description={model.Description} where CaseNo={model.CaseNo}";
                    var UpdateDescRecord = Conn.ExecuteDataTableSqlDA(conn, UpdateRequestQuery);


                    //Coomented on 25/08/2022 finidng it unuseful
                    //var AddNotification = _tenantRepository.SendPushNotification(UserId.Value, "Service request " + model.CaseNo, "Your service request has been updated successfully", "Your service request has been updated successfully", "Tenant", model.CaseNo, "case");

                    AddServiceRequestDetailVm CheckserviceRequest = new AddServiceRequestDetailVm();
                    var GetCaseNoQuery = $@"select CaseNo from cms.Cases where CaseNo={model.CaseNo}";

                    var CheckCase = Conn.ExecuteDataTableSqlDA(conn, GetCaseNoQuery);

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
                            UpdateServiceRequest = CheckserviceRequest,
                            Message = "Added Successfully"
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            UpdateServiceRequest = CheckserviceRequest,
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

        [Route("CancelRequest")]
        [HttpPost]
        public ActionResult<string> CancelRequest(CaseNoBody requestModel)
        {

            if (User.Identity.IsAuthenticated)
            {
                int CaseNo = int.Parse(requestModel.CaseNo);
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
                //security api update start
                var tenantid = GetTenantId(UserId.Value);
                var userid = GetCaseOwnerId(UserId.Value);
                var isValidUserForCase = CaseInUserId(CaseNo, userid);

                if (!isValidUserForCase)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "Not a Valid User For Case."
                    };
                    return Unauthorized(data);
                }
                var isclosedorcancelled = CheckInCloseOrCancelled(CaseNo, userid);
                if (isclosedorcancelled)
                {
                    var validdata = new
                    {
                        Status = "Invalid",
                        Message = "Error Not a Valid Case or Case Closed/Cancelled."
                    };

                    return Ok(validdata);
                }
                //security api update end
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                AddServiceRequestDetailVm addServiceRequestDetailVm = new AddServiceRequestDetailVm();
                var GetUserValueQuery = $@"select CaseNo from cms.Cases where CaseNo={CaseNo}";
                constring = constring.ToString();
                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetUserValueQuery);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<AddServiceRequestDetailVm>(RequestDetail);
                    addServiceRequestDetailVm = ListToBeSend.FirstOrDefault();
                }
                if (addServiceRequestDetailVm != null)
                {
                    ConfigValues configValues = new ConfigValues();
                    var AddRequestQuery = $@"update cms.Cases set StatusId={configValues.CancelStatus} where CaseNo={CaseNo}";
                    var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery);
                    var MessageAr = "تم إلغاء طلب الخدمة الخاص بك";
                    var AddNotification = _tenantRepository.SendPushNotification(UserId.Value, "Your service request has been cancelled", "Service request " + CaseNo, "Your service request has been cancelled", MessageAr, MessageAr, "Tenant", CaseNo, "case");

                    AddServiceRequestDetailVm CheckserviceRequest = new AddServiceRequestDetailVm();
                    var GetCaseNoQuery = $@"select CaseNo from cms.Cases where CaseNo={CaseNo}";

                    var CheckCase = Conn.ExecuteDataTableSqlDA(conn, GetCaseNoQuery);

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
                            Message = "Request cancelled successfully"
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        Message = "No Data Found."
                    };
                    return NotFound(data);
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


        [Route("GetTicketReplies")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetTicketReplies([FromBody] GetTicketReplies requestModel)
        {
            //try
            //{
            List<TicketReplyVm> TicketReply = new List<TicketReplyVm>();
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
                //security api update start
                // add caseownerid to get cases
                var userid = GetCaseOwnerId(UserId.Value);
                var isValidUserForCase = CaseInUserId(int.Parse(requestModel.CaseId), userid);

                if (!isValidUserForCase)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "Not a Valid User For Case."
                    };
                    return Unauthorized(data);
                }
                //security api update end
                var TicketReplyQuery = $@"select  Id as TicketReplyId,Reply, Format(DateTime, 'dd-MM-yyyy hh:mm tt') as DateTime,TenantId as UserId, FileName from cms.TicketReply where CaseId={int.Parse(requestModel.CaseId)} and ReplyTo=1";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, TicketReplyQuery);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<TicketReplyVm>(RequestDetail);
                    TicketReply = ListToBeSend;
                }
                if (TicketReply != null)
                {
                    var data = new
                    {
                        Status = "Ok",
                        TicketReply = TicketReply,
                        Message = "Data Found."
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        TicketReply = TicketReply,
                        Message = "No Data Found."
                    };
                    return NotFound(data);
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
         
        [Route("AddTicketReply")]
        [HttpPost]
        [RequestSizeLimit(15000000)]
        //[Consumes("multipart/form-data")]
        public async Task<IActionResult> AddTicketReply([FromForm] AddTicketVm model)
        {
            try
            {
                _tenantRepository.InsertErrorLog("Tenant Ticket Reply Api Called TOP");
                if (User.Identity.IsAuthenticated)
                {
                    var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                    int? UserId = _settings.GetUserIdByToken(_bearer_token);
                    if (UserId == null)
                    {
                        var UnAuthorize = new
                        {
                            Status = "Invalid",
                            Message = "InvalidSession."
                        };
                        return Unauthorized(UnAuthorize);
                    }
                    //security api update start
                    model.CaseId = Decryption.Decrypt(model.CaseId);
                    model.Reply = Decryption.Decrypt(model.Reply);
                    var userid = GetCaseOwnerId(UserId.Value);
                    //var isValidUserForCase = CaseInUserId(int.Parse(model.CaseId), userid);
                    _tenantRepository.InsertErrorLog("Tenant Ticket Reply Api Called 1");
                    //if (!isValidUserForCase)
                    //{
                    //    var validdata = new
                    //    {
                    //        Status = "Invalid",
                    //        Message = "Not a Valid User For Case."
                    //    };
                    //    _tenantRepository.InsertErrorLog("tenant Not a Valid User For Case");
                    //    return BadRequest(validdata);
                    //}

                    var isclosedorcancelled = CheckInCloseOrCancelled(int.Parse(model.CaseId), userid);
                    if (isclosedorcancelled)
                    {
                        var validdata = new
                        {
                            Status = "Invalid",
                            Message = "Error Not a Valid Case or Case Closed/Cancelled."
                        };

                        return Ok(validdata);
                    }
                    if (CrossSiteScriptingValidation.IsDangerousString(model.Reply, out _))
                    {
                        var dataBadReq = new
                        {
                            Status = "BadRequest",
                            Message = "Invalid Request.",
                            StatusCode = "500"
                        };
                        return BadRequest(dataBadReq);
                    }
                    _tenantRepository.InsertErrorLog("Tenant Ticket Reply Api Called 2");
                    //security api update end
                    var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    string Filname = null;
                    //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };//point no 10
                    _tenantRepository.InsertErrorLog("Ticket Reply Api Called 3");
                    if (model.File != null)
                    {
                        _tenantRepository.InsertErrorLog("Uploading File 1");
                        var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                        if (!Directory.Exists(GetFilePath))
                        {
                            Directory.CreateDirectory(GetFilePath);
                        }
                        //var ServiceRequestThumbnail = _config["Path:ServiceRequestThumbnail"].ToString();
                        Filname = DateTime.UtcNow.AddHours(4).ToString("yyyyMMddHHmmssffff") + model.File.FileName;
                        var path = GetFilePath + Filname;
                        _tenantRepository.InsertErrorLog("Uploading File 2");
                        var bytes = model.File.GetBytes();
                        _tenantRepository.InsertErrorLog("get filebytes");
                        _tenantRepository.InsertErrorLog("get file type: " + model.File.ContentType);
                        //adding funtion for comparing file extention 02-09-2022
                        var extention = Path.GetExtension(model.File.FileName);
                        //check for file validity
                        if (MimeTypes.IsValidMimeType(bytes, model.File.ContentType, extention))
                        {
                            _tenantRepository.InsertErrorLog("Mime Type Validation");
                            //var Thumbnailpath = ServiceRequestThumbnail + Filname;
                            using (var stream = new FileStream(path, FileMode.Create))
                            {
                                await model.File.CopyToAsync(stream);
                                //GenerateTumbnail.ThumbNail(320, 240, stream, Thumbnailpath);
                            }
                        }
                        else
                        {
                            Filname = null;
                            _tenantRepository.InsertErrorLog("Error Saving File");
                            var dataError = new
                            {
                                Status = "Error",
                                Message = "Invalid File",
                                StatusCode = "500"
                            };
                            return BadRequest(dataError);
                        }
                    }
                    _tenantRepository.InsertErrorLog("trying to add ticket");
                    if (!string.IsNullOrWhiteSpace(model.Reply))
                    {
                        model.Reply = Sanitizer.GetSafeHtmlFragment(model.Reply);
                        //model.Reply = _settings.RemoveSpecialChar(model.Reply);
                    }
                    var AddReplyQuery = $@"INSERT INTO CMS.TicketReply(CaseId,Reply, DateTime, UserId,TenantId,FileName,ReplyTo)
				                                VALUES ({model.CaseId},N'{model.Reply}','{DateTime.UtcNow.AddHours(4)}',(select top 1 UserId from cms.Users),{tenantId},@FileName,1);";
                    //var replyParm = new SqlParameter("@Reply", "N'" + model.Reply);
                    //if (string.IsNullOrWhiteSpace(model.Reply))
                    //{
                    //    replyParm = new SqlParameter("@Reply", DBNull.Value);
                    //}
                    var FilnameParm = new SqlParameter("@FileName", Filname);
                    if (string.IsNullOrWhiteSpace(Filname))
                    {
                        FilnameParm = new SqlParameter("@FileName", DBNull.Value);
                    }
                    try
                    {
                        Conn.ExecuteDataTableSqlDA(conn, AddReplyQuery, FilnameParm);
                        _tenantRepository.InsertErrorLog("after case resply insert");
                    }
                    catch (Exception EX)
                    {

                        _tenantRepository.InsertErrorLog(EX.Message);
                    }

                    string Subject = "Case No " + model.CaseId;
                    string Message = "Reply submitted against Case No " + model.CaseId;
                    //added 22-12-2022 ar
                    string SubjectAr = model.CaseId + "القضية رقم ";
                    string MessageAr = model.CaseId + "تم تقديم الرد ضد القضية رقم";
                    try
                    {
                        _tenantRepository.SendPushNotification(UserId.Value, Message, Message, Subject, SubjectAr, MessageAr, "Tenant", int.Parse(model.CaseId), "case");
                        _tenantRepository.SendEmailNotification(UserId.Value, Message, Subject, SubjectAr, MessageAr, "Tenant");

                        _tenantRepository.InsertErrorLog("notification sent");
                    }
                    catch (Exception ex)
                    {

                        _tenantRepository.InsertErrorLog(ex.Message);
                    }

                    var data = new
                    {
                        Status = "Ok",
                        Message = "Ticket Submitted Successfully"
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

        [Route("GetFaqCategories")]
        [HttpPost]
        public ActionResult<string> GetFaqCategories()
        {
            try
            {
                List<FaqCategoriesVm> FaqCategoriesData = new List<FaqCategoriesVm>();
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
                    var FaqCategoriesQuery = $@"SELECT Title, CategoryId, TitleAR from cms.FAQCategories where Active=1 AND RoleId=1";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, FaqCategoriesQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<FaqCategoriesVm>(RequestDetail);
                        FaqCategoriesData = ListToBeSend;
                    }
                    if (FaqCategoriesData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            FaqCategories = FaqCategoriesData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            FaqCategories = FaqCategoriesData,
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

        [Route("GetFaqs")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetFaqs([FromBody] CaseSubCategoryModel requestModel)
        {
            try
            {
                int CategoryId = int.Parse(requestModel.CategoryId);
                List<FaqVm> FaqData = new List<FaqVm>();
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
                    var FaqQuery = $@"SELECT faq.Title,faq.TitleAR, faq.Description, faq.DescriptionAR
                                                FROM   CMS.FAQ  faq
                                                INNER JOIN CMS.FAQCategories fc  ON faq.CategoryId = fc.CategoryId 
                                                INNER JOIN CMS.FAQRoleType frt ON fc.RoleId = frt.Id
                                                where fc.Active=1 and faq.Active=1 and faq.CategoryId={CategoryId}";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, FaqQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<FaqVm>(RequestDetail);
                        FaqData = ListToBeSend;
                    }
                    if (FaqData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            Faq = FaqData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            Faq = FaqData,
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


        //[HttpPost]
        //[RequestSizeLimit(1073741824)]
        //[Route("UploadServiceRequestFiles")]
        //public async Task<IActionResult> UploadServiceRequestFiles([FromForm] ServiceRequestPhotosVm model)

        //{
        //    if (User.Identity.IsAuthenticated)
        //    {
        //        var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
        //        int? UserId = _settings.GetUserIdByToken(_bearer_token);
        //        if (UserId == null)
        //        {
        //            var data = new
        //            {
        //                Status = "Invalid",
        //                Message = "InvalidSession."
        //            };
        //            return Unauthorized(data);
        //        }

        //        var tenantId = _tenantRepository.GetTenantId(UserId.Value);
        //        if (model.File == null)
        //        {
        //            var data = new
        //            {
        //                Status = "file Not found",
        //            };
        //            return Ok(data);
        //        }
        //        else if (model.File.Count > 0)
        //        {
        //            InsertedPhotoVM selected = new InsertedPhotoVM();
        //            var constring = _settings.GetConnectionString();
        //            constring = constring.ToString();
        //            SqlConnection conn = new SqlConnection(constring);

        //            var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
        //            //var ServiceRequestThumbnail = _config["Path:ServiceRequestThumbnail"].ToString();

        //            foreach (var item in model.File)
        //            {
        //                string Filename = DateTime.Now.ToString("yyyyMMddHHmmssffff") + item.FileName;
        //                var path = GetFilePath + Filename;
        //                //var Thumbnailpath = ServiceRequestThumbnail + Filename;




        //                using (var stream = new FileStream(path, FileMode.Create))
        //                {
        //                    await item.CopyToAsync(stream);

        //                }


        //                string ext = Path.GetExtension(path);
        //                var AddFileInfoQuery = $@"INSERT INTO CMS.Attachment(Description,AttachmentUrl,BaseUrl,Extentsion,AttachmentDate, UploadedCaseId, RoleTypeId,AttachmentType,ExpireDate,FileSize,DocumentTypeId, IsRejected) VALUES ('','{Filename}','-','{ext}','{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}',{model.CaseNo},1,'{model.AttachmentType}','{model.ExpireDate}','{item.Length.ToString()}',{model.DocumentTypeId},0) SELECT SCOPE_IDENTITY() as SelectedId";
        //                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, AddFileInfoQuery);
        //                if (RequestDetail.Rows.Count > 0)
        //                {
        //                    var ListToBeSend = Conn.ToListof<InsertedPhotoVM>(RequestDetail);
        //                    selected = ListToBeSend.FirstOrDefault();
        //                }


        //                if (model.AttachmentType.ToLower() == "signature")
        //                {
        //                    var CaseInfo = _tenantRepository.GetCaseInfo(model.CaseNo);
        //                    var addNotification = _tenantRepository.prepareAndAddPushNotification(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 11, model.CaseNo, UserId.Value, "Tenant");
        //                }

        //            }

        //            var data = new
        //            {
        //                Status = "file Uploaded",
        //                PhotoId = selected.SelectedId
        //            };
        //            return Ok(data);
        //        }
        //        else
        //        {
        //            var data = new
        //            {
        //                Status = "file Not found",
        //            };
        //            return NotFound(data);
        //        }

        //    }
        //    else
        //    {
        //        var data = new
        //        {
        //            Status = "Invalid",
        //            Message = "InvalidSession."
        //        };
        //        return Ok(data);
        //    }


        //}

        [HttpPost]
        [Route("UploadServiceRequestFilesNew")]
        [RequestSizeLimit(15000000)]// 1073741824
        public async Task<IActionResult> UploadServiceRequestFilesNew([FromForm] ServiceRequestPhotosVm model)

        {
            try
            {
                var logged = _tenantRepository.InsertErrorLog("UploadServiceRequestFiles start");
                if (User.Identity.IsAuthenticated)
                {

                    //commented -17-10-2022
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
                    logged = _tenantRepository.InsertErrorLog("uploadservice request file after authentication");
                    model.AttachmentType = Decryption.Decrypt(model.AttachmentType);
                    var expirydate = "";
                    if (!model.ExpireDate.IsNullOrEmpty())
                    {
                        model.ExpireDate = Decryption.Decrypt(model.ExpireDate);
                        var expdate = DateTime.ParseExact(model.ExpireDate, "dd-MM-yyyy", null);
                        expirydate = expdate.ToString("dd-MM-yyyy");
                        _tenantRepository.InsertErrorLog(expirydate);
                    }
                    if (!model.DocumentTypeId.IsNullOrEmpty())
                    {
                        model.DocumentTypeId = Decryption.Decrypt(model.DocumentTypeId);
                    }
                    model.CaseNo = Decryption.Decrypt(model.CaseNo);
                    //if (CrossSiteScriptingValidation.IsDangerousParameter(model.AttachmentType, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.ExpireDate, out _))
                    //{
                    //    var dataBadReq = new
                    //    {
                    //        Status = "BadRequest",
                    //        Message = "Invalid Request.",
                    //        StatusCode = "500"
                    //    };
                    //    return BadRequest(dataBadReq);
                    //}



                    //commented -17-10-2022
                    var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                    var userid = GetCaseOwnerId(UserId.Value);

                    //=============================================
                    //var isValidUserForCase = CaseInUserId(int.Parse(model.CaseNo), userid);

                    //if (!isValidUserForCase)
                    //{
                    //    var dataerror = new
                    //    {
                    //        Status = "Invalid",
                    //        Message = "Not a Valid User For Case."
                    //    };
                    //    return Unauthorized(dataerror);
                    //}
                    //var isdocumentsubmitted = CheckInDocumentUploaded(int.Parse(model.CaseNo), userid);
                    //if (isdocumentsubmitted)
                    //{
                    //    var validdata = new
                    //    {
                    //        Status = "Invalid",
                    //        Message = "Document already submitted."
                    //    };

                    //    return BadRequest(validdata);
                    //}
                    var isclosedorcancelled = CheckInCloseOrCancelled(int.Parse(model.CaseNo), userid);
                    if (isclosedorcancelled)
                    {
                        var validdata = new
                        {
                            Status = "Invalid",
                            Message = "Error Not a Valid Case or case closed/cancelled."
                        };

                        return BadRequest(validdata);
                    }
                    if (model.File == null)
                    {
                        var data = new
                        {
                            Status = "file Not found",
                        };
                        logged = _tenantRepository.InsertErrorLog("no file selected");
                        return Ok(data);
                    }

                    else if (model.File.Count > 0)
                    {
                        logged = _tenantRepository.InsertErrorLog("upload start");
                        InsertedPhotoVM selected = new InsertedPhotoVM();
                        var constring = _settings.GetConnectionString();
                        constring = constring.ToString();
                        SqlConnection conn = new SqlConnection(constring);

                        var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                        if (!Directory.Exists(GetFilePath))
                        {
                            Directory.CreateDirectory(GetFilePath);
                        }
                        //var ServiceRequestThumbnail = _config["Path:ServiceRequestThumbnail"].ToString();
                        //ConfigValues values = new ConfigValues();
                        //var Array = values.ParameterNotIn;
                        //var LowerAddress = model.AttachmentType.ToLower();
                        //if (Array.Any(x => x == LowerAddress))
                        //{
                        //    var record = new
                        //    {
                        //        statusCode = "401",
                        //        Status = "not ok",
                        //        message = "Something went wrong"
                        //    };
                        //    return Ok(record);
                        //}
                        //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };//point no 10
                        foreach (var item in model.File)
                        {

                            var ext = Path.GetExtension(item.FileName).Substring(1);
                            //var log = $@"INSERT INTO [CMS].[ErrorLogs]
                            //        ([Date]
                            //        ,[Description])
                            //       VALUES
                            //        (GetDate()
                            //    ,'Image Type: {model.CaseNo} {ext}')";
                            //try
                            //{
                            //    var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, log);

                            //}
                            //catch (Exception ex)
                            //{
                            //    logged = _tenantRepository.InsertErrorLog(ex.Message);
                            //}
                            try
                            {
                                logged = _tenantRepository.InsertErrorLog("beforeGetBytes");
                                var bytes = item.GetBytes();
                                logged = _tenantRepository.InsertErrorLog("afterGetBytes");
                                var extention = Path.GetExtension(item.FileName);
                                logged = _tenantRepository.InsertErrorLog("afterfilExt");
                                if (!MimeTypes.IsValidMimeType(bytes, item.ContentType, extention))
                                {
                                    var dataError = new
                                    {
                                        Status = "Error",
                                        Message = "Invalid File " + extention,
                                        StatusCode = "500"
                                    };
                                    return BadRequest(dataError);
                                }

                                //if (!MimeTypes.IsValidMimeType(bytes, item.ContentType))
                                //{
                                //    var Invalid = new
                                //    {
                                //        Status = "Invalid file " + ext,
                                //    };
                                //    return NotFound(Invalid);
                                //}
                            }
                            catch (Exception ex)
                            {
                                logged = _tenantRepository.InsertErrorLog(ex.Message);
                            }

                            logged = _tenantRepository.InsertErrorLog("beforeFilename");
                            string Filename = DateTime.UtcNow.AddHours(4).ToString("yyyyMMddHHmmssffff") + item.FileName;
                            var path = GetFilePath + Filename;
                            logged = _tenantRepository.InsertErrorLog("pathFilename");
                            using (var stream = new FileStream(path, FileMode.Create))
                            {
                                logged = _tenantRepository.InsertErrorLog("FileStream");

                                await item.CopyToAsync(stream);
                            }

                            //security api updated
                            logged = _tenantRepository.InsertErrorLog("afterFileStream");

                            var AddFileInfoQuery = $@"
                    INSERT INTO CMS.Attachment (
	                    Description
	                    ,AttachmentUrl
	                    ,BaseUrl
	                    ,Extentsion
	                    ,AttachmentDate
	                    ,UploadedCaseId
	                    ,RoleTypeId
	                    ,AttachmentType
	                    ,ExpireDate
	                    ,FileSize
	                    ,DocumentTypeId
	                    ,IsRejected
	                    )
                    VALUES (
	                    ''
	                    ,@Filename
	                    ,'-'
	                    ,@Ext
	                    ,'{DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd HH:mm:ss.fff")}'
	                    ,{model.CaseNo}
	                    ,1
	                    ,@AttachmentType
	                    ,@ExpireDate
	                    ,'{item.Length.ToString()}'
	                    ,{model.DocumentTypeId}
	                    ,0
	                    )

                    SELECT SCOPE_IDENTITY() AS SelectedId";
                            logged = _tenantRepository.InsertErrorLog("SCOPE_IDENTITY");
                            //point no 3 --
                            if (string.IsNullOrWhiteSpace(model.ExpireDate) || model.ExpireDate == "default")
                            {
                                model.ExpireDate = DateTime.Now.AddYears(1).ToString("dd-MM-yyyy");
                            }

                            var extParam = new SqlParameter("@Ext", ext);
                            var filenameParam = new SqlParameter("@Filename", Filename);
                            if (string.IsNullOrWhiteSpace(Filename))
                            {
                                filenameParam = new SqlParameter("@Filename", DBNull.Value);
                            }
                            var attachmentTypeParam = new SqlParameter("@AttachmentType", model.AttachmentType);
                            if (string.IsNullOrWhiteSpace(model.AttachmentType))
                            {
                                attachmentTypeParam = new SqlParameter("@AttachmentType", DBNull.Value);
                            }
                            var expireDateParam = new SqlParameter("@ExpireDate", expirydate);

                            var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, AddFileInfoQuery, filenameParam, attachmentTypeParam, expireDateParam, extParam);
                            if (RequestDetail.Rows.Count > 0)
                            {
                                logged = _tenantRepository.InsertErrorLog("RequestDetail.Rows.Count");

                                var ListToBeSend = Conn.ToListof<InsertedPhotoVM>(RequestDetail);
                                selected = ListToBeSend.FirstOrDefault();
                            }


                            if (model.AttachmentType.ToLower() == "signature")
                            {
                                var CaseInfo = _tenantRepository.GetCaseInfo(int.Parse(model.CaseNo));
                                //var addNotification = _tenantRepository.prepareAndAddPushNotification(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 11, model.CaseNo, UserId.Value, "Tenant");
                            }

                        }

                        var data = new
                        {
                            Status = "file Uploaded",
                            PhotoId = selected.SelectedId
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "file Not found",
                        };
                        return NotFound(data);
                    }

                }
                else
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };

                    logged = _tenantRepository.InsertErrorLog("user identity issue");
                    return Ok(data);
                }
            }
            catch (Exception ex)
            {
                var dataError = new
                {
                    Status = "Error",
                    Message = ex.Message,
                    StatusCode = "500"
                };
                return BadRequest(dataError);
            }



        }

        [HttpPost]
        [Route("UploadServiceRequestFiles")]
        [RequestSizeLimit(15000000)]// 1073741824
        public async Task<IActionResult> UploadServiceRequestFiles([FromForm] ServiceRequestPhotosVm model)

        {
            try
            {
                var logged = _tenantRepository.InsertErrorLog("UploadServiceRequestFiles start");
                if (User.Identity.IsAuthenticated)
                {

                    //commented -17-10-2022
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
                    logged = _tenantRepository.InsertErrorLog("uploadservice request file after authentication");
                    model.AttachmentType = Decryption.Decrypt(model.AttachmentType);
                    var expirydate = "";
                    if (!model.ExpireDate.IsNullOrEmpty())
                    {
                        model.ExpireDate = Decryption.Decrypt(model.ExpireDate);
                        var expdate = DateTime.ParseExact(model.ExpireDate, "dd-MM-yyyy", null);
                        expirydate = expdate.ToString("dd-MM-yyyy");
                        _tenantRepository.InsertErrorLog(expirydate);
                    }
                    if (!model.DocumentTypeId.IsNullOrEmpty())
                    {
                        model.DocumentTypeId = Decryption.Decrypt(model.DocumentTypeId);
                    }
                    model.CaseNo = Decryption.Decrypt(model.CaseNo);
                    //if (CrossSiteScriptingValidation.IsDangerousParameter(model.AttachmentType, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.ExpireDate, out _))
                    //{
                    //    var dataBadReq = new
                    //    {
                    //        Status = "BadRequest",
                    //        Message = "Invalid Request.",
                    //        StatusCode = "500"
                    //    };
                    //    return BadRequest(dataBadReq);
                    //}



                    //commented -17-10-2022
                    var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                    var userid = GetCaseOwnerId(UserId.Value);

                    //=============================================
                    //var isValidUserForCase = CaseInUserId(int.Parse(model.CaseNo), userid);

                    //if (!isValidUserForCase)
                    //{
                    //    var dataerror = new
                    //    {
                    //        Status = "Invalid",
                    //        Message = "Not a Valid User For Case."
                    //    };
                    //    return Unauthorized(dataerror);
                    //}
                    //var isdocumentsubmitted = CheckInDocumentUploaded(int.Parse(model.CaseNo), userid);
                    //if (isdocumentsubmitted)
                    //{
                    //    var validdata = new
                    //    {
                    //        Status = "Invalid",
                    //        Message = "Document already submitted."
                    //    };

                    //    return BadRequest(validdata);
                    //}
                    var isclosedorcancelled = CheckInCloseOrCancelled(int.Parse(model.CaseNo), userid);
                    if (isclosedorcancelled)
                    {
                        var validdata = new
                        {
                            Status = "Invalid",
                            Message = "Error Not a Valid Case or case closed/cancelled."
                        };

                        return BadRequest(validdata);
                    }
                    if (model.File == null)
                    {
                        var data = new
                        {
                            Status = "file Not found",
                        };
                        logged = _tenantRepository.InsertErrorLog("no file selected");
                        return Ok(data);
                    }

                    else if (model.File.Count > 0)
                    {
                        logged = _tenantRepository.InsertErrorLog("upload start");
                        InsertedPhotoVM selected = new InsertedPhotoVM();
                        var constring = _settings.GetConnectionString();
                        constring = constring.ToString();
                        SqlConnection conn = new SqlConnection(constring);

                        var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                        if (!Directory.Exists(GetFilePath))
                        {
                            Directory.CreateDirectory(GetFilePath);
                        }
                        //var ServiceRequestThumbnail = _config["Path:ServiceRequestThumbnail"].ToString();
                        //ConfigValues values = new ConfigValues();
                        //var Array = values.ParameterNotIn;
                        //var LowerAddress = model.AttachmentType.ToLower();
                        //if (Array.Any(x => x == LowerAddress))
                        //{
                        //    var record = new
                        //    {
                        //        statusCode = "401",
                        //        Status = "not ok",
                        //        message = "Something went wrong"
                        //    };
                        //    return Ok(record);
                        //}
                        //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };//point no 10
                        foreach (var item in model.File)
                        {

                            var ext = Path.GetExtension(item.FileName).Substring(1);
                            //var log = $@"INSERT INTO [CMS].[ErrorLogs]
                            //        ([Date]
                            //        ,[Description])
                            //       VALUES
                            //        (GetDate()
                            //    ,'Image Type: {model.CaseNo} {ext}')";
                            //try
                            //{
                            //    var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, log);

                            //}
                            //catch (Exception ex)
                            //{
                            //    logged = _tenantRepository.InsertErrorLog(ex.Message);
                            //}
                            try
                            {
                                logged = _tenantRepository.InsertErrorLog("beforeGetBytes");
                                var bytes = item.GetBytes();
                                logged = _tenantRepository.InsertErrorLog("afterGetBytes");
                                var extention = Path.GetExtension(item.FileName);
                                logged = _tenantRepository.InsertErrorLog("afterfilExt");
                                if (!MimeTypes.IsValidMimeType(bytes, item.ContentType, extention))
                                {
                                    var dataError = new
                                    {
                                        Status = "Error",
                                        Message = "Invalid File " + extention,
                                        StatusCode = "500"
                                    };
                                    return BadRequest(dataError);
                                }

                                //if (!MimeTypes.IsValidMimeType(bytes, item.ContentType))
                                //{
                                //    var Invalid = new
                                //    {
                                //        Status = "Invalid file " + ext,
                                //    };
                                //    return NotFound(Invalid);
                                //}
                            }
                            catch (Exception ex)
                            {
                                logged = _tenantRepository.InsertErrorLog(ex.Message);
                            }

                            logged = _tenantRepository.InsertErrorLog("beforeFilename");
                            string Filename = DateTime.UtcNow.AddHours(4).ToString("yyyyMMddHHmmssffff") + item.FileName;
                            var path = GetFilePath + Filename;
                            logged = _tenantRepository.InsertErrorLog("pathFilename");
                            using (var stream = new FileStream(path, FileMode.Create))
                            {
                                logged = _tenantRepository.InsertErrorLog("FileStream");

                                await item.CopyToAsync(stream);
                            }

                            //security api updated
                            logged = _tenantRepository.InsertErrorLog("afterFileStream");

                            var AddFileInfoQuery = $@"
                    INSERT INTO CMS.Attachment (
	                    Description
	                    ,AttachmentUrl
	                    ,BaseUrl
	                    ,Extentsion
	                    ,AttachmentDate
	                    ,UploadedCaseId
	                    ,RoleTypeId
	                    ,AttachmentType
	                    ,ExpireDate
	                    ,FileSize
	                    ,DocumentTypeId
	                    ,IsRejected
	                    )
                    VALUES (
	                    ''
	                    ,@Filename
	                    ,'-'
	                    ,@Ext
	                    ,'{DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd HH:mm:ss.fff")}'
	                    ,{model.CaseNo}
	                    ,1
	                    ,@AttachmentType
	                    ,@ExpireDate
	                    ,'{item.Length.ToString()}'
	                    ,{model.DocumentTypeId}
	                    ,0
	                    )

                    SELECT SCOPE_IDENTITY() AS SelectedId";
                            logged = _tenantRepository.InsertErrorLog("SCOPE_IDENTITY");
                            //point no 3 --
                            if (string.IsNullOrWhiteSpace(model.ExpireDate) || model.ExpireDate == "default")
                            {
                                model.ExpireDate = DateTime.Now.AddYears(1).ToString("dd-MM-yyyy");
                            }

                            var extParam = new SqlParameter("@Ext", ext);
                            var filenameParam = new SqlParameter("@Filename", Filename);
                            if (string.IsNullOrWhiteSpace(Filename))
                            {
                                filenameParam = new SqlParameter("@Filename", DBNull.Value);
                            }
                            var attachmentTypeParam = new SqlParameter("@AttachmentType", model.AttachmentType);
                            if (string.IsNullOrWhiteSpace(model.AttachmentType))
                            {
                                attachmentTypeParam = new SqlParameter("@AttachmentType", DBNull.Value);
                            }
                            var expireDateParam = new SqlParameter("@ExpireDate", expirydate);

                            var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, AddFileInfoQuery, filenameParam, attachmentTypeParam, expireDateParam, extParam);
                            if (RequestDetail.Rows.Count > 0)
                            {
                                logged = _tenantRepository.InsertErrorLog("RequestDetail.Rows.Count");

                                var ListToBeSend = Conn.ToListof<InsertedPhotoVM>(RequestDetail);
                                selected = ListToBeSend.FirstOrDefault();
                            }


                            if (model.AttachmentType.ToLower() == "signature")
                            {
                                var CaseInfo = _tenantRepository.GetCaseInfo(int.Parse(model.CaseNo));
                                //var addNotification = _tenantRepository.prepareAndAddPushNotification(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 11, model.CaseNo, UserId.Value, "Tenant");
                            }

                        }

                        var data = new
                        {
                            Status = "file Uploaded",
                            PhotoId = selected.SelectedId
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "file Not found",
                        };
                        return NotFound(data);
                    }

                }
                else
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };

                    logged = _tenantRepository.InsertErrorLog("user identity issue");
                    return Ok(data);
                }
            }
            catch (Exception ex)
            {
                var dataError = new
                {
                    Status = "Error",
                    Message = ex.Message,
                    StatusCode = "500"
                };
                return BadRequest(dataError);
            }



        }
        //added on 10-12-2022 for only emirate ID
        [HttpPost]
        [Route("UploadServiceRequestEmirateId")]
        [RequestSizeLimit(15000000)]// 1073741824
        public async Task<IActionResult> UploadServiceRequestEmirateId([FromForm] ServiceRequestPhotosVm model)

        {
            try
            {
                var logged = _tenantRepository.InsertErrorLog("UploadServiceRequestEmirateId start");
                if (User.Identity.IsAuthenticated)
                {

                    //commented -17-10-2022
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
                    logged = _tenantRepository.InsertErrorLog("UploadServiceRequestEmirateId after authentication");
                    model.AttachmentType = Decryption.Decrypt(model.AttachmentType);
                    var expirydate = "";
                    if (!model.ExpireDate.IsNullOrEmpty())
                    {
                        _tenantRepository.InsertErrorLog(model.ExpireDate);
                        model.ExpireDate = Decryption.Decrypt(model.ExpireDate);
                        var expdate = DateTime.ParseExact(model.ExpireDate, "dd-MM-yyyy", null);
                        expirydate = expdate.ToString("dd-MM-yyyy");
                    }
                    if (!model.DocumentTypeId.IsNullOrEmpty())
                    {
                        model.DocumentTypeId = Decryption.Decrypt(model.DocumentTypeId);
                    }
                    model.CaseNo = Decryption.Decrypt(model.CaseNo);

                    //add extra paramters for emirateId
                    var issuedate = "";
                    if (!model.IssueDate.IsNullOrEmpty())
                    {
                        _tenantRepository.InsertErrorLog(model.IssueDate);
                        model.IssueDate = Decryption.Decrypt(model.IssueDate);
                        var issdate = DateTime.ParseExact(model.IssueDate, "dd-MM-yyyy", null);
                        issuedate = issdate.ToString("dd-MM-yyyy");
                    }
                    var dateob = "";
                    if (!model.DOB.IsNullOrEmpty())
                    {
                        _tenantRepository.InsertErrorLog(model.DOB);
                        model.DOB = Decryption.Decrypt(model.DOB);
                        var dob = DateTime.ParseExact(model.DOB, "dd-MM-yyyy", null);
                        dateob = dob.ToString("dd-MM-yyyy");
                    }
                    if (!model.EmirateIdNumber.IsNullOrEmpty())
                    {
                        model.EmirateIdNumber = Decryption.Decrypt(model.EmirateIdNumber);
                    }
                    if (!model.NameEng.IsNullOrEmpty())
                    {
                        model.NameEng = Decryption.Decrypt(model.NameEng);
                    }
                    if (!model.NameAr.IsNullOrEmpty())
                    {
                        model.NameAr = Decryption.Decrypt(model.NameAr);
                    }
                    if (!model.Nationality.IsNullOrEmpty())
                    {
                        model.Nationality = Decryption.Decrypt(model.Nationality);
                    }
                    //if (CrossSiteScriptingValidation.IsDangerousParameter(model.AttachmentType, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.ExpireDate, out _))
                    //{
                    //    var dataBadReq = new
                    //    {
                    //        Status = "BadRequest",
                    //        Message = "Invalid Request.",
                    //        StatusCode = "500"
                    //    };
                    //    return BadRequest(dataBadReq);
                    //}



                    //commented -17-10-2022
                    var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                    var userid = GetCaseOwnerId(UserId.Value);

                    //=============================================
                    //var isValidUserForCase = CaseInUserId(int.Parse(model.CaseNo), userid);

                    //if (!isValidUserForCase)
                    //{
                    //    var dataerror = new
                    //    {
                    //        Status = "Invalid",
                    //        Message = "Not a Valid User For Case."
                    //    };
                    //    return Unauthorized(dataerror);
                    //}
                    //var isdocumentsubmitted = CheckInDocumentUploaded(int.Parse(model.CaseNo), userid);
                    //if (isdocumentsubmitted)
                    //{
                    //    var validdata = new
                    //    {
                    //        Status = "Invalid",
                    //        Message = "Document already submitted."
                    //    };

                    //    return BadRequest(validdata);
                    //}
                    var isclosedorcancelled = CheckInCloseOrCancelled(int.Parse(model.CaseNo), userid);
                    if (isclosedorcancelled)
                    {
                        var validdata = new
                        {
                            Status = "Invalid",
                            Message = "Error Not a Valid Case or case closed/cancelled."
                        };

                        return BadRequest(validdata);
                    }
                    if (model.File == null)
                    {
                        var data = new
                        {
                            Status = "file Not found",
                        };
                        logged = _tenantRepository.InsertErrorLog("no file selected");
                        return Ok(data);
                    }

                    else if (model.File.Count > 0)
                    {
                        logged = _tenantRepository.InsertErrorLog("upload start");
                        InsertedPhotoVM selected = new InsertedPhotoVM();
                        var constring = _settings.GetConnectionString();
                        constring = constring.ToString();
                        SqlConnection conn = new SqlConnection(constring);

                        var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                        if (!Directory.Exists(GetFilePath))
                        {
                            Directory.CreateDirectory(GetFilePath);
                        }
                        //var ServiceRequestThumbnail = _config["Path:ServiceRequestThumbnail"].ToString();
                        //ConfigValues values = new ConfigValues();
                        //var Array = values.ParameterNotIn;
                        //var LowerAddress = model.AttachmentType.ToLower();
                        //if (Array.Any(x => x == LowerAddress))
                        //{
                        //    var record = new
                        //    {
                        //        statusCode = "401",
                        //        Status = "not ok",
                        //        message = "Something went wrong"
                        //    };
                        //    return Ok(record);
                        //}
                        //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };//point no 10
                        foreach (var item in model.File)
                        {

                            var ext = Path.GetExtension(item.FileName).Substring(1);
                            //var log = $@"INSERT INTO [CMS].[ErrorLogs]
                            //        ([Date]
                            //        ,[Description])
                            //       VALUES
                            //        (GetDate()
                            //    ,'Image Type: {model.CaseNo} {ext}')";
                            //try
                            //{
                            //    var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, log);

                            //}
                            //catch (Exception ex)
                            //{
                            //    logged = _tenantRepository.InsertErrorLog(ex.Message);
                            //}
                            try
                            {
                                logged = _tenantRepository.InsertErrorLog("beforeGetBytes");
                                var bytes = item.GetBytes();
                                logged = _tenantRepository.InsertErrorLog("afterGetBytes");
                                var extention = Path.GetExtension(item.FileName);
                                logged = _tenantRepository.InsertErrorLog("afterfilExt");
                                if (!MimeTypes.IsValidMimeType(bytes, item.ContentType, extention))
                                {
                                    var dataError = new
                                    {
                                        Status = "Error",
                                        Message = "Invalid File " + extention,
                                        StatusCode = "500"
                                    };
                                    return BadRequest(dataError);
                                }

                                //if (!MimeTypes.IsValidMimeType(bytes, item.ContentType))
                                //{
                                //    var Invalid = new
                                //    {
                                //        Status = "Invalid file " + ext,
                                //    };
                                //    return NotFound(Invalid);
                                //}
                            }
                            catch (Exception ex)
                            {
                                logged = _tenantRepository.InsertErrorLog(ex.Message);
                            }

                            logged = _tenantRepository.InsertErrorLog("beforeFilename");
                            string Filename = DateTime.UtcNow.AddHours(4).ToString("yyyyMMddHHmmssffff") + item.FileName;
                            var path = GetFilePath + Filename;
                            logged = _tenantRepository.InsertErrorLog("pathFilename");
                            using (var stream = new FileStream(path, FileMode.Create))
                            {
                                logged = _tenantRepository.InsertErrorLog("FileStream");

                                await item.CopyToAsync(stream);
                            }

                            //security api updated
                            logged = _tenantRepository.InsertErrorLog("afterFileStream");

                            var AddFileInfoQuery = $@"
                    INSERT INTO CMS.Attachment (
	                    Description
	                    ,AttachmentUrl
	                    ,BaseUrl
	                    ,Extentsion
	                    ,AttachmentDate
	                    ,UploadedCaseId
	                    ,RoleTypeId
	                    ,AttachmentType
	                    ,ExpireDate
	                    ,FileSize
	                    ,DocumentTypeId
	                    ,IsRejected,EmirateIdNumber,Nationality,NameEng,NameAr,IssueDate,DOB
	                    )
                    VALUES (
	                    ''
	                    ,@Filename
	                    ,'-'
	                    ,@Ext
	                    ,'{DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd HH:mm:ss.fff")}'
	                    ,{model.CaseNo}
	                    ,1
	                    ,@AttachmentType
	                    ,@ExpireDate
	                    ,'{item.Length.ToString()}'
	                    ,{model.DocumentTypeId}
	                    ,0,
                        @pEMID,@pNationality,@pNameEng,@pNameAR,@pIssueDate,@pDOB
	                    )

                    SELECT SCOPE_IDENTITY() AS SelectedId";
                            logged = _tenantRepository.InsertErrorLog("SCOPE_IDENTITY");
                            //point no 3 --
                            if (string.IsNullOrWhiteSpace(model.ExpireDate) || model.ExpireDate == "default")
                            {
                                model.ExpireDate = DateTime.Now.AddYears(1).ToString("dd-MM-yyyy");
                            }

                            var extParam = new SqlParameter("@Ext", ext);
                            var filenameParam = new SqlParameter("@Filename", Filename);
                            if (string.IsNullOrWhiteSpace(Filename))
                            {
                                filenameParam = new SqlParameter("@Filename", DBNull.Value);
                            }
                            var attachmentTypeParam = new SqlParameter("@AttachmentType", model.AttachmentType);
                            if (string.IsNullOrWhiteSpace(model.AttachmentType))
                            {
                                attachmentTypeParam = new SqlParameter("@AttachmentType", DBNull.Value);
                            }
                            var expireDateParam = new SqlParameter("@ExpireDate", expirydate);
                            //added new columns 10-12-2022
                            var EMIDParam = new SqlParameter("@pEMID", model.EmirateIdNumber);
                            if (string.IsNullOrWhiteSpace(model.EmirateIdNumber))
                            {
                                EMIDParam = new SqlParameter("@pEMID", DBNull.Value);
                            }
                            var NationalityParam = new SqlParameter("@pNationality", model.Nationality);
                            if (string.IsNullOrWhiteSpace(model.Nationality))
                            {
                                NationalityParam = new SqlParameter("@pNationality", DBNull.Value);
                            }
                            var NameEngParam = new SqlParameter("@pNameEng", model.NameEng);
                            if (string.IsNullOrWhiteSpace(model.NameEng))
                            {
                                NameEngParam = new SqlParameter("@pNameEng", DBNull.Value);
                            }
                            var NameArParam = new SqlParameter("@pNameAr", model.NameAr);
                            if (string.IsNullOrWhiteSpace(model.NameAr))
                            {
                                NameArParam = new SqlParameter("@pNameAr", DBNull.Value);
                            }
                            var IssueDateParam = new SqlParameter("@pIssueDate", model.IssueDate);
                            if (string.IsNullOrWhiteSpace(model.IssueDate))
                            {
                                IssueDateParam = new SqlParameter("@pIssueDate", DBNull.Value);
                            }
                            var DOBParam = new SqlParameter("@pDOB", model.DOB);
                            if (string.IsNullOrWhiteSpace(model.DOB))
                            {
                                DOBParam = new SqlParameter("@pDOB", DBNull.Value);
                            }

                            var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, AddFileInfoQuery, filenameParam, attachmentTypeParam, expireDateParam, extParam, EMIDParam, NationalityParam, NameEngParam, NameArParam, IssueDateParam, DOBParam);
                            if (RequestDetail.Rows.Count > 0)
                            {
                                logged = _tenantRepository.InsertErrorLog("RequestDetail.Rows.Count");

                                var ListToBeSend = Conn.ToListof<InsertedPhotoVM>(RequestDetail);
                                selected = ListToBeSend.FirstOrDefault();
                            }

                            //remove 10-12-2022 don't need
                            //if (model.AttachmentType.ToLower() == "signature")
                            //{
                            //    var CaseInfo = _tenantRepository.GetCaseInfo(int.Parse(model.CaseNo));
                            //    //var addNotification = _tenantRepository.prepareAndAddPushNotification(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 11, model.CaseNo, UserId.Value, "Tenant");
                            //}

                        }

                        var data = new
                        {
                            Status = "file Uploaded",
                            PhotoId = selected.SelectedId
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "file Not found",
                        };
                        return NotFound(data);
                    }

                }
                else
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };

                    logged = _tenantRepository.InsertErrorLog("user identity issue");
                    return Unauthorized(data);
                }
            }
            catch (Exception ex)
            {
                _tenantRepository.InsertErrorLog(ex.Message + " " + ex.InnerException);

                var dataError = new
                {
                    Status = "Error",
                    Message = ex.Message,
                    StatusCode = "500"
                };
                return BadRequest(dataError);
            }
        }

        [HttpPost]
        [RequestSizeLimit(15000000)]
        [Route("LargeFileUpload")]
        public async Task<IActionResult> LargeFileUpload([FromForm] ServiceRequestPhotosVm model)
        {
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
                model.AttachmentType = Decryption.Decrypt(model.AttachmentType);
                var expirydate = "";
                if (!model.ExpireDate.IsNullOrEmpty())
                {
                    model.ExpireDate = Decryption.Decrypt(model.ExpireDate);
                    var expdate = DateTime.ParseExact(model.ExpireDate, "dd-MM-yyyy", null);
                    expirydate = expdate.ToString("dd-MM-yyyy");
                    _tenantRepository.InsertErrorLog(expirydate);
                }
                if (CrossSiteScriptingValidation.IsDangerousParameter(model.AttachmentType, out _) || CrossSiteScriptingValidation.IsDangerousParameter(model.ExpireDate, out _))
                {
                    var dataBadReq = new
                    {
                        Status = "BadRequest",
                        Message = "Invalid Request.",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
                var logged = _tenantRepository.InsertErrorLog("LargeFileUpload");
                if (model.File == null || !model.File.Any())
                {
                    var data = new
                    {
                        Status = "file Not found",
                    };
                    return Ok(data);
                }

                var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                if (!Directory.Exists(GetFilePath))
                {
                    Directory.CreateDirectory(GetFilePath);
                }
                //var ServiceRequestThumbnail = _config["Path:ServiceRequestThumbnail"].ToString();
                var length = 0D;
                //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };//point no 10
                foreach (var item in model.File)
                {
                    length = item.Length / 1024.0 / 1024.0;

                    string Filename = DateTime.UtcNow.AddHours(4).ToString("yyyyMMddHHmmssffff") + item.FileName;
                    var path = GetFilePath + Filename;
                    var ext = Path.GetExtension(item.FileName);
                    var bytes = item.GetBytes();
                    if (MimeTypes.IsValidMimeType(bytes, item.ContentType, ext))
                    {
                        //var Thumbnailpath = ServiceRequestThumbnail + Filename;
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await item.CopyToAsync(stream);
                            //GenerateTumbnail.ThumbNail(320, 240, stream, Thumbnailpath);
                        }

                    }
                    else
                    {
                        Filename = "";
                        var dataError = new
                        {
                            Status = "Error",
                            Message = "Invalid File " + ext,
                            StatusCode = "500"
                        };
                        return BadRequest(dataError);
                    }

                }

                var data2 = new
                {
                    Status = $"{length} Mb file Uploaded"
                };
                return Ok(data2);
            }
            else
            {
                var dataerror = new
                {
                    Status = $"401",
                    Message = "Invalid Session"
                };
                return Unauthorized(dataerror);
            }
        }
        private int GetTenantId(int UserId)
        {
            int TenantId = 0;
            TenantId = _tenantRepository.GetTenantId(UserId);
            return TenantId;
        }
        [HttpPost]
        [Route("RemoveServiceRequestFiles")]
        [ValidateActionParameters]
        public async Task<IActionResult> RemoveServiceRequestFiles(RemoveServiceRequestVm modelRequest)
        {
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

                if (modelRequest.PhotoId == 0)
                {
                    var data = new
                    {
                        Status = "Invalid Data Id",
                    };
                    return Ok(data);
                }
                RemoveServiceRequestVm model = new RemoveServiceRequestVm();
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var SelectFileInfoQuery = $@"select a.AttachmentId as PhotoId from CMS.Attachment a inner join cms.Cases c on c.CaseNo = a.UploadedCaseId inner join cms.Customer cu on cu.CustomerId = c.CaseOwnerId  where a.AttachmentId=@pPhotoId and cu.UserId = @UserId";
                var photoSQLParm = new SqlParameter("@pPhotoId", modelRequest.PhotoId);
                if (modelRequest.PhotoId == 0)
                {
                    photoSQLParm = new SqlParameter("@pPhotoId", DBNull.Value);
                }
                var userSQLParm = new SqlParameter("@UserId", UserId);
                if (UserId == null)
                {
                    userSQLParm = new SqlParameter("@UserId", DBNull.Value);
                }
                var GetRequestDetail = Conn.ExecuteDataTableSqlDA(conn, SelectFileInfoQuery, photoSQLParm, userSQLParm);

                if (GetRequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<RemoveServiceRequestVm>(GetRequestDetail);
                    model = ListToBeSend.FirstOrDefault();
                    if (model == null)
                    {
                        var dataError = new
                        {
                            Status = "BadRequest",
                            StatusCode = "500",
                            message = "File does not exist"
                        };
                        return BadRequest(dataError);
                    }
                }
                else
                {
                    var dataError = new
                    {
                        Status = "BadRequest",
                        StatusCode = "500",
                        message = "File does not exist"
                    };
                    return BadRequest(dataError);
                }

                if (model.PhotoId != 0)
                {
                    modelRequest.PhotoId = model.PhotoId;
                    var DeleteFileInfoQuery = $@"delete from CMS.Attachment where AttachmentId={modelRequest.PhotoId}";
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, DeleteFileInfoQuery);
                    if (RequestDetail != null)
                    {
                        var data = new
                        {
                            Status = "file Deleted Successfully",
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "Record Not found",
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Record Not found",
                    };
                    return NotFound(data);
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


        [Route("ReopenServiceRequest")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> ReopenServiceRequest([FromBody] CaseNoBody requestModel)
        {

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
                //security api update start
                // add caseownerid to get cases
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
                var isclosedorcancelled = CheckInCloseOrCancelled(int.Parse(requestModel.CaseNo), userid);
                if (!isclosedorcancelled)
                {
                    var validdata = new
                    {
                        Status = "Invalid",
                        Message = "Error Not a Valid Case or case already open."
                    };

                    return BadRequest(validdata);
                }
                //security api update end
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                AddServiceRequestDetailVm addServiceRequestDetailVm = new AddServiceRequestDetailVm();
                var GetUserValueQuery = $@"select CaseNo from cms.Cases where CaseNo={int.Parse(requestModel.CaseNo)}";
                constring = constring.ToString();
                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetUserValueQuery);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<AddServiceRequestDetailVm>(RequestDetail);
                    addServiceRequestDetailVm = ListToBeSend.FirstOrDefault();
                }
                if (addServiceRequestDetailVm.CaseNo != 0)
                {

                    ConfigValues configValues = new ConfigValues();
                    var AddRequestQuery = $@"update cms.Cases set StatusId={configValues.ReOpenedStatus} where CaseNo={int.Parse(requestModel.CaseNo)}";
                    var UpdateRecord = Conn.ExecuteDataTableSqlDA(conn, AddRequestQuery);
                    //added 22-12-2022
                    var MessageAr = "تم إعادة فتح طلب الخدمة الخاص بك";
                    var AddNotification = _tenantRepository.SendPushNotification(UserId.Value, "Your service request has been re-opened", "Service request " + int.Parse(requestModel.CaseNo), "Your service request has been re-opened", MessageAr, MessageAr, "Tenant", int.Parse(requestModel.CaseNo), "case");

                    AddServiceRequestDetailVm CheckserviceRequest = new AddServiceRequestDetailVm();
                    var GetCaseNoQuery = $@"select CaseNo from cms.Cases where CaseNo={int.Parse(requestModel.CaseNo)}";

                    var CheckCase = Conn.ExecuteDataTableSqlDA(conn, GetCaseNoQuery);

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
                            Message = "Request reopened successfully"
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        Message = "No Data Found."
                    };
                    return NotFound(data);
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



        [HttpPost]
        [Route("UploadTicketFiles")]
        [RequestSizeLimit(15000000)]
        public async Task<IActionResult> UploadTicketFiles([FromForm] TicketFilesVm model)
        {
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
                var isValidUserForCase = CaseInUserId(model.CaseId, userid);

                if (!isValidUserForCase)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "Not a Valid User For Case."
                    };
                    return Unauthorized(data);
                }
                var isclosedorcancelled = CheckInCloseOrCancelled(model.CaseId, userid);
                if (isclosedorcancelled)
                {
                    var validdata = new
                    {
                        Status = "Invalid",
                        Message = "Error Not a Valid Case or case closed/cancelled."
                    };

                    return BadRequest(validdata);
                }
                var loggederr = _tenantRepository.InsertErrorLog("Tieket Add start");
                if (model.File == null)
                {
                    var data = new
                    {
                        Status = "file Not found",
                    };
                    return NotFound(data);
                }
                else if (model.File.Count > 0)
                {
                    loggederr = _tenantRepository.InsertErrorLog("Tieket Add start 1");
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    if (!Directory.Exists(GetFilePath))
                    {
                        Directory.CreateDirectory(GetFilePath);
                    }
                    //var ServiceRequestThumbnail = _config["Path:ServiceRequestThumbnail"].ToString();
                    loggederr = _tenantRepository.InsertErrorLog("Tieket Add start 2");
                    foreach (var item in model.File)
                    {
                        string Filname = DateTime.UtcNow.AddHours(4).ToString("yyyyMMddHHmmssffff") + item.FileName;
                        var path = GetFilePath + Filname;
                        //var Thumbnailpath = ServiceRequestThumbnail + Filname;
                        var extention = Path.GetExtension(item.FileName);
                        //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };//point no 10
                        loggederr = _tenantRepository.InsertErrorLog("Tieket Add start Mime 2");
                        var bytes = item.GetBytes();
                        if (!MimeTypes.IsValidMimeType(bytes, item.ContentType, extention))
                        {
                            loggederr = _tenantRepository.InsertErrorLog("Tieket Add start Mime 3");
                            var errodata = new
                            {
                                Status = "InvalidFile",
                                Message = "file uploadig failed invalid format."
                            };
                            loggederr = _tenantRepository.InsertErrorLog("Tieket Add start Mime 4");
                            return BadRequest(errodata);
                        }

                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            try
                            {
                                await item.CopyToAsync(stream);
                                loggederr = _tenantRepository.InsertErrorLog("File copied");
                            }
                            catch (Exception ex)
                            {
                                loggederr = _tenantRepository.InsertErrorLog(ex.Message);

                            }

                            //GenerateTumbnail.ThumbNail(320, 240, stream, Thumbnailpath);

                        }
                        var AddFileInfoQuery = $@"INSERT INTO CMS.TicketFiles(FileSource,UploadOn, CaseId) VALUES (@File,'{DateTime.UtcNow.AddHours(4)}',{model.CaseId});";
                        var filSqlParm = new SqlParameter("@File", item.FileName);
                        if (item.FileName == null)
                        {
                            filSqlParm = new SqlParameter("@File", DBNull.Value);
                        }
                        var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, AddFileInfoQuery, filSqlParm);

                        loggederr = _tenantRepository.InsertErrorLog("Ticket added");
                    }

                    var data = new
                    {
                        Status = "file Uploaded",
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "file Not found",
                    };
                    return NotFound(data);
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

        [Route("GetServiceRequestImages")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetServiceRequestImages(int CaseNo, int RoleId)
        {
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
                //security api update start
                // add caseownerid to get cases
                var userid = GetCaseOwnerId(UserId.Value);
                var isValidUserForCase = CaseInUserId(CaseNo, userid);

                if (!isValidUserForCase)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "Not a Valid User For Case."
                    };
                    return Unauthorized(data);
                }
                //security api update end
                List<ServiceRequestFiles> model = new List<ServiceRequestFiles>();
                var ImageQuery = $@"select AttachmentId as photoId,AttachmentUrl as PhotoSource, RoleTypeId from cms.Attachment where UploadedCaseId={CaseNo} and RoleTypeId={RoleId} and AttachmentType='Images'";

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ServiceRequestFiles>(Image);
                    model = ListToBeSend.ToList();
                }

                ServiceRequestFileResponse Response = new ServiceRequestFileResponse();
                if (model != null && model.Count() > 0)
                {
                    var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    if (!Directory.Exists(GetFilePath))
                    {
                        Directory.CreateDirectory(GetFilePath);
                    }
                    string imageName = null;
                    string MainDiectory = GetFilePath;
                    foreach (var item in model)
                    {

                        imageName = item.PhotoSource;
                        try
                        {

                            var path = MainDiectory + imageName;
                            if (System.IO.File.Exists(path))
                            {
                                byte[] newImage = null;
                                {
                                    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                                    using (BinaryReader reader = new BinaryReader(fileStream))
                                    {

                                        newImage = new byte[reader.BaseStream.Length];
                                        for (int i = 0; i < reader.BaseStream.Length; i++)
                                            newImage[i] = reader.ReadByte();


                                    }
                                }
                                Response.imageByteArray.Add(newImage);
                                Response.photoId.Add(item.photoId);
                            }


                        }
                        catch (Exception ex)
                        {
                            var Exception = new
                            {
                                Status = ex.Message,
                                Path = ""
                            };
                            return NotFound(Exception);
                        }
                    }
                    var data = new
                    {
                        Status = "200",
                        Path = Response.imageByteArray,
                        PhotoId = Response.photoId,
                    };
                    return Ok(data);
                }

                else
                {
                    var data = new
                    {
                        Status = "Image Name not found",
                        Path = "",
                        PhotoId = ""
                    };
                    return NotFound(data);
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


        [HttpPost]
        [Route("SaveSRFeedback")]
        public async Task<IActionResult> SaveSRFeedback(SaveSRFeedbackVm model)
        {

            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                    int? UserId = _settings.GetUserIdByToken(_bearer_token);
                    if (UserId == null)
                    {
                        var ExpireSession = new
                        {
                            Status = "Invalid",
                            Message = "InvalidSession."
                        };
                        return Unauthorized(ExpireSession);
                    }
                    //security api update start
                    // add caseownerid to get cases
                    var userid = GetCaseOwnerId(UserId.Value);
                    var isValidUserForCase = CaseInUserId(model.CaseId, userid);

                    if (!isValidUserForCase)
                    {
                        var dataerror = new
                        {
                            Status = "Invalid",
                            Message = "Not a Valid User For Case."
                        };
                        return Unauthorized(dataerror);
                    }
                    //security api update end
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
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    //Regex rRemScript = new Regex(@"<script[^>]*>[\s\S]*?</script>");
                    //model.Description = rRemScript.Replace(model.Description, "");

                    var AddFileInfoQuery = $@"INSERT INTO CMS.ServiceRequestFeedback(CaseNo,Description, Rating, UploadOn) VALUES ({model.CaseId},N'{model.Description}',@Rating,'{DateTime.UtcNow.AddHours(4)}');";

                    //var sqlparm = new SqlParameter("@Description",SqlDbType.NVarChar,200, model.Description);

                    //if (string.IsNullOrWhiteSpace(model.Description))
                    //{
                    //    sqlparm = new SqlParameter("@Description", DBNull.Value);
                    //}
                    var ratingsqlparm = new SqlParameter("@Rating", model.Rating);
                    if (model.Rating == null)
                    {
                        ratingsqlparm = new SqlParameter("@Rating", DBNull.Value);
                    }
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, AddFileInfoQuery, ratingsqlparm);

                    var data = new
                    {
                        Status = "Ok",
                        Message = "Feedback Submit successfully"
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

        // add something new  
        [Route("GetSRFeedback")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetSRFeedback([FromBody] CaseNoBody requestModel)
        {
            try
            {
                int CaseNo = int.Parse(requestModel.CaseNo);
                GetSRFeedbackVm FeedbackVm = new GetSRFeedbackVm();
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

                    //security api update start
                    // add caseownerid to get cases
                    var userid = GetCaseOwnerId(UserId.Value);
                    var isValidUserForCase = CaseInUserId(CaseNo, userid);

                    if (!isValidUserForCase)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            Message = "Not a Valid User For Case."
                        };
                        return Unauthorized(data);
                    }
                    //security api update end
                    var FaqQuery = $@"select CaseNo,Description, Rating from  CMS.ServiceRequestFeedback where CaseNo={CaseNo}";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, FaqQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<GetSRFeedbackVm>(RequestDetail);
                        FeedbackVm = ListToBeSend.FirstOrDefault();
                    }
                    if (FeedbackVm.CaseNo != 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            Feedback = FeedbackVm,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            Feedback = FeedbackVm,
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

        [Route("GetServiceRequestDocuments")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetServiceRequestDocuments([FromBody] GetServiceRequestDocuments requestModel)
        {
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
                //security api update start
                // add caseownerid to get cases
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
                //security api update end
                List<ServiceRequestFiles> model = new List<ServiceRequestFiles>();
                var ImageQuery = $@"select AttachmentId as photoId,AttachmentUrl as PhotoSource, RoleTypeId,AttachmentType from cms.Attachment where UploadedCaseId={int.Parse(requestModel.CaseNo)} and RoleTypeId={int.Parse(requestModel.RoleId)} ";

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ServiceRequestFiles>(Image);
                    model = ListToBeSend.ToList();
                }

                ServiceRequestFileResponse Response = new ServiceRequestFileResponse();
                if (model != null)
                {
                    var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    if (!Directory.Exists(GetFilePath))
                    {
                        Directory.CreateDirectory(GetFilePath);
                    }
                    string imageName = null;
                    string MainDiectory = GetFilePath;
                    foreach (var item in model)
                    {

                        imageName = item.PhotoSource;
                        try
                        {

                            var path = MainDiectory + imageName;
                            if (System.IO.File.Exists(path))
                            {
                                byte[] newImage = null;
                                {
                                    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                                    using (BinaryReader reader = new BinaryReader(fileStream))
                                    {

                                        newImage = new byte[reader.BaseStream.Length];
                                        for (int i = 0; i < reader.BaseStream.Length; i++)
                                            newImage[i] = reader.ReadByte();


                                    }
                                }
                                Response.imageByteArray.Add(newImage);
                                Response.photoId.Add(item.photoId);
                                Response.AttachmentType.Add(item.AttachmentType);
                            }
                            //else
                            //{

                            //}

                        }
                        catch (Exception ex)
                        {
                            var Exception = new
                            {
                                Status = ex.Message,
                                Path = ""
                            };
                            return NotFound(Exception);
                        }
                    }
                    var data = new
                    {
                        Status = "200",
                        Path = Response.imageByteArray,
                        PhotoId = Response.photoId,
                        AttachmentType = Response.AttachmentType,
                    };
                    return Ok(data);
                }

                else
                {
                    var data = new
                    {
                        Status = "Image Name not found",
                        Path = "",
                        PhotoId = ""
                    };
                    return NotFound(data);
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


        [Route("GetServiceRequestDocumentsPdf")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetServiceRequestDocumentsPdf([FromBody] GetServiceRequestDocuments requestModel)
        {
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
                //security api update start
                // add caseownerid to get cases
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
                //security api update end
                List<ServiceRequestFiles> model = new List<ServiceRequestFiles>();
                var ImageQuery = $@"select AttachmentId as photoId,AttachmentUrl as PhotoSource, RoleTypeId,AttachmentType from cms.Attachment where UploadedCaseId={int.Parse(requestModel.CaseNo)} and RoleTypeId={int.Parse(requestModel.RoleId)} and AttachmentType='Document'";

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ServiceRequestFiles>(Image);
                    model = ListToBeSend.ToList();
                }

                ServiceRequestFileResponse Response = new ServiceRequestFileResponse();
                if (model != null)
                {
                    var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    if (!Directory.Exists(GetFilePath))
                    {
                        Directory.CreateDirectory(GetFilePath);
                    }
                    string imageName = null;
                    string MainDiectory = GetFilePath;
                    foreach (var item in model)
                    {

                        imageName = item.PhotoSource;
                        try
                        {

                            var path = MainDiectory + imageName;
                            if (System.IO.File.Exists(path))
                            {
                                byte[] newImage = null;
                                {
                                    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                                    using (BinaryReader reader = new BinaryReader(fileStream))
                                    {

                                        newImage = new byte[reader.BaseStream.Length];
                                        for (int i = 0; i < reader.BaseStream.Length; i++)
                                            newImage[i] = reader.ReadByte();


                                    }
                                }
                                Response.imageByteArray.Add(newImage);
                                Response.photoId.Add(item.photoId);
                                Response.AttachmentType.Add(item.AttachmentType);
                            }
                            //else
                            //{

                            //}

                        }
                        catch (Exception ex)
                        {
                            var Exception = new
                            {
                                Status = ex.Message,
                                Path = ""
                            };
                            return NotFound(Exception);
                        }
                    }
                    var data = new
                    {
                        Status = "200",
                        Path = Response.imageByteArray,
                        PhotoId = Response.photoId,
                        AttachmentType = Response.AttachmentType,
                    };
                    return Ok(data);
                }

                else
                {
                    var data = new
                    {
                        Status = "Image Name not found",
                        Path = "",
                        PhotoId = ""
                    };
                    return NotFound(data);
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

        [Route("GetServiceRequestDocumentsByType")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetServiceRequestDocumentsByType([FromBody] GetServiceRequestDocumentsByType requestModel)
        {
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
                //security api update start
                // add caseownerid to get cases
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
                //security api update end
                requestModel.Code = _tenantRepository.GetCategoryCode(int.Parse(requestModel.CaseNo)).ToString();
                List<AttachmentFileVm> model = new List<AttachmentFileVm>();

                var ImageQuery = $@"
                SELECT dt.DocumentTypeId
	                    ,dt.Name,
                        dt.NameAr
	                    ,(
                            SELECT AttachmentUrl
                            FROM cms.Attachment att

                            WHERE dt.DocumentTypeId = att.DocumentTypeId

                                AND RoleTypeId = @pRoleId

                                AND UploadedCaseId = @pCaseId
		                    ) AttachmentUrl
	                    ,(
                            SELECT AttachmentId
                            FROM cms.Attachment att

                            WHERE dt.DocumentTypeId = att.DocumentTypeId

                                AND RoleTypeId = @pRoleId

                                AND UploadedCaseId = @pCaseId
		                    ) AttachmentId
	                    ,(
                            SELECT Extentsion
                            FROM cms.Attachment att

                            WHERE dt.DocumentTypeId = att.DocumentTypeId

                                AND RoleTypeId = @pRoleId

                                AND UploadedCaseId = @pCaseId
		                    ) Extentsion
	                    ,(
                            SELECT ExpireDate
                            FROM cms.Attachment att

                            WHERE dt.DocumentTypeId = att.DocumentTypeId

                                AND RoleTypeId = @pRoleId

                                AND UploadedCaseId = @pCaseId
		                    ) ExpireDate
	                    ,(
                            SELECT FORMAT(AttachmentDate, 'dd-MM-yyyy')

                            FROM cms.Attachment att

                            WHERE dt.DocumentTypeId = att.DocumentTypeId

                                AND RoleTypeId = @pRoleId

                                AND UploadedCaseId = @pCaseId
		                    ) AttachmentDate
	                    ,(
                            SELECT IsRejected
                            FROM cms.Attachment att

                            WHERE dt.DocumentTypeId = att.DocumentTypeId

                                AND RoleTypeId = @pRoleId

                                AND UploadedCaseId = @pCaseId
		                    ) IsRejected
                    FROM cms.DocumentContentType dct
                    LEFT JOIN cms.DocumentType dt ON dct.DocumentTypeId = dt.DocumentTypeId

                    WHERE contentid = @pCode";



                /* var ImageQuery = $@"select dt.DocumentTypeId,dt.Name, (select AttachmentUrl from cms.Attachment att where dt.DocumentTypeId=att.DocumentTypeId and RoleTypeId=@pRoleId and UploadedCaseId=@pCaseId)AttachmentUrl,(select AttachmentId from cms.Attachment att where dt.DocumentTypeId=att.DocumentTypeId and RoleTypeId=@pRoleId and UploadedCaseId=@pCaseId)AttachmentId, (select Extentsion from cms.Attachment att where dt.DocumentTypeId=att.DocumentTypeId and RoleTypeId=@pRoleId and UploadedCaseId=@pCaseId)Extentsion,(select ExpireDate from cms.Attachment att where dt.DocumentTypeId=att.DocumentTypeId and RoleTypeId=@pRoleId and UploadedCaseId=@pCaseId)ExpireDate,(select FORMAT(AttachmentDate,'dd-MM-yyyy') from cms.Attachment att where dt.DocumentTypeId=att.DocumentTypeId and RoleTypeId=@pRoleId and UploadedCaseId=@pCaseId)AttachmentDate,

                 (select IsRejected from cms.Attachment att where dt.DocumentTypeId=att.DocumentTypeId and RoleTypeId=@pRoleId and UploadedCaseId=@pCaseId)IsRejected
                 from cms.DocumentContentType dct
                 left join cms.DocumentType dt on dct.DocumentTypeId=dt.DocumentTypeId where contentid=@pCode";
                */
                var caseParm = new SqlParameter("@pCaseId", int.Parse(requestModel.CaseNo));
                var roleidparm = new SqlParameter("@pRoleId", int.Parse(requestModel.RoleId));
                var codeParm = new SqlParameter("@pCode", int.Parse(requestModel.Code));
                if (int.Parse(requestModel.Code) == null)
                {
                    codeParm = new SqlParameter("@pCode", DBNull.Value);
                }
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery, caseParm, roleidparm, codeParm);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<AttachmentFileVm>(Image);
                    model = ListToBeSend.ToList();
                }


                if (model != null)
                {
                    var data = new
                    {
                        Status = "200",
                        Record = model,
                        ResponseMessage = _settings.PaymentMessage,
                        CaseStageInfo = _tenantRepository.CaseStageInfo(int.Parse(requestModel.CaseNo)),
                    };
                    return Ok(data);
                }

                else
                {
                    var data = new
                    {
                        Status = "Not found",
                        Record = model,
                    };
                    return NotFound(data);
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

        [Route("DownloadServiceRequestDocuments")]
        [HttpPost]
        public ActionResult<string> DownloadServiceRequestDocuments([FromBody] DownloadServiceRequestDocuments req)
        {
            int CaseNo = int.Parse(req.CaseNo);
            int RoleId = int.Parse(req.RoleId);
            int PhotoId = int.Parse(req.PhotoId);

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
                //security api update start
                // add caseownerid to get cases
                var userid = GetCaseOwnerId(UserId.Value);
                var isValidUserForCase = CaseInUserId(CaseNo, userid);

                if (!isValidUserForCase)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "Not a Valid User For Case."
                    };
                    return Unauthorized(data);
                }
                //security api update end
                List<ServiceRequestFiles> model = new List<ServiceRequestFiles>();
                var ImageQuery = $@"select AttachmentId as photoId,AttachmentUrl as PhotoSource,AttachmentType from cms.Attachment where UploadedCaseId=@pCaseId and RoleTypeId=@pRoleId and AttachmentId=@PhotoId";
                var caseParm = new SqlParameter("@pCaseId", CaseNo);
                var roleidparm = new SqlParameter("@pRoleId", RoleId);
                var PhotoIdParm = new SqlParameter("@PhotoId", PhotoId);

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery, caseParm, roleidparm, PhotoIdParm);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ServiceRequestFiles>(Image);
                    model = ListToBeSend.ToList();
                }

                ServiceRequestFileResponse Response = new ServiceRequestFileResponse();
                if (model != null)
                {
                    string imageName = null;
                    var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    if (!Directory.Exists(GetFilePath))
                    {
                        Directory.CreateDirectory(GetFilePath);
                    }
                    string MainDiectory = GetFilePath;
                    foreach (var item in model)
                    {

                        imageName = item.PhotoSource;
                        try
                        {
                            var path = System.IO.Path.Combine(MainDiectory, imageName);
                            if (System.IO.File.Exists(path))
                            {
                                byte[] newImage = null;
                                {
                                    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                                    using (BinaryReader reader = new BinaryReader(fileStream))
                                    {
                                        newImage = new byte[reader.BaseStream.Length];
                                        for (int i = 0; i < reader.BaseStream.Length; i++)
                                            newImage[i] = reader.ReadByte();
                                    }
                                }
                                Response.imageByteArray.Add(newImage);
                            }


                        }
                        catch (Exception ex)
                        {
                            var Exception = new
                            {
                                Status = ex.Message,
                                Path = ""
                            };
                            return NotFound(Exception);
                        }
                    }
                    var data = new
                    {
                        Status = "200",
                        Path = Response.imageByteArray
                    };
                    return Ok(data);
                }

                else
                {
                    var data = new
                    {
                        Status = "Image Name not found",
                        Path = "",
                    };
                    return NotFound(data);
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

        [Route("DownloadTicketFile")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> DownloadTicketFile([FromBody] DownloadTicketFile requestModel)
        {
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
                List<ServiceRequestFiles> model = new List<ServiceRequestFiles>();
                int ticketReplyId = int.Parse(requestModel.TicketReplyId);
                var ImageQuery = $@"select Id as photoId,FileName as PhotoSource from cms.TicketReply tr 
inner join CMS.Cases c On c.CaseNo = tr.CaseId inner join cms.Customer cu
on cu.CustomerId = c.CaseOwnerId where tr.Id={ticketReplyId} and cu.UserId = {UserId}";

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ServiceRequestFiles>(Image);
                    model = ListToBeSend.ToList();
                }

                ServiceRequestFileResponse Response = new ServiceRequestFileResponse();
                if (model != null)
                {
                    string imageName = null;
                    var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    if (!Directory.Exists(GetFilePath))
                    {
                        Directory.CreateDirectory(GetFilePath);
                    }
                    string MainDiectory = GetFilePath;
                    foreach (var item in model)
                    {

                        imageName = item.PhotoSource;
                        try
                        {
                            var path = System.IO.Path.Combine(MainDiectory, imageName);
                            if (System.IO.File.Exists(path))
                            {
                                byte[] newImage = null;
                                {
                                    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                                    using (BinaryReader reader = new BinaryReader(fileStream))
                                    {
                                        newImage = new byte[reader.BaseStream.Length];
                                        for (int i = 0; i < reader.BaseStream.Length; i++)
                                            newImage[i] = reader.ReadByte();
                                    }
                                }
                                Response.imageByteArray.Add(newImage);
                            }
                            else
                            {

                            }

                        }
                        catch (Exception ex)
                        {
                            var Exception = new
                            {
                                Status = ex.Message,
                                Path = ""
                            };
                            return NotFound(Exception);
                        }
                    }
                    var data = new
                    {
                        Status = "200",
                        Path = Response.imageByteArray
                    };
                    return Ok(data);
                }

                else
                {
                    var data = new
                    {
                        Status = "Image Name not found",
                        Path = "",
                    };
                    return NotFound(data);
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

        [HttpPost]
        [Route("UpdateContractDocumentStage")]
        [ValidateActionParameters]
        public async Task<IActionResult> UpdateContractDocumentStage([FromBody] UpdateContractDocumentStage requestModel)
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
            if (int.Parse(requestModel.DueActionId) != 0)
            {
                var ContractStage = _tenantRepository.ContractStageInfo(int.Parse(requestModel.DueActionId));
                if (ContractStage != null)
                {
                    if (ContractStage.CaseNo != 0)
                    {
                        var caseownerid = GetCaseOwnerId(UserId.Value);
                        var validateCase = CaseInUserId(ContractStage.CaseNo, caseownerid);
                        if (!validateCase)
                        {
                            var dataInvalid = new
                            {
                                Status = "Unauthorized",
                                Message = "Unauthorized Request."
                            };
                            return Unauthorized(dataInvalid);
                        }
                    }
                    var caseOnfo = _tenantRepository.GetCaseInfo(ContractStage.CaseNo);
                    if (caseOnfo != null)
                    {
                        var updateStage = _tenantRepository.UpdateStageId(int.Parse(requestModel.DueActionId), 3);
                        var categoryInfo = _tenantRepository.CategoryInfo(caseOnfo.CaseCategouryId);
                        if (categoryInfo == "PM")
                        {
                            /* case stage update will happen from cms for 5 and 6 . no need to chage any assignee aprt from 5&6*/
                            //var UpdateUser = _tenantRepository.AssignPMAssineee(ContractStage.CaseNo, "Update", true);
                        }
                    }
                    else
                    {
                        var dataError = new
                        {
                            Status = "BadRequest",
                            statusCode = "400"
                        };
                        return BadRequest(dataError);
                    }
                }
                var data = new
                {
                    Status = "Stages updated successfully",
                    statusCode = "200"
                };
                return Ok(data);
            }
            else
            {
                var data = new
                {
                    Status = "Not Found",
                    statusCode = "404"
                };
                return NotFound(data);
            }
        }


        [Route("GetServiceRequestThumbnailList")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetServiceRequestThumbnailList([FromBody] GetServiceRequestDocuments requestModel)
        {
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
                //security api update start
                // add caseownerid to get cases
                var userid = GetCaseOwnerId(UserId.Value);
                var isValidUserForCase = CaseInUserId(int.Parse(requestModel.CaseNo), userid);

                if (!isValidUserForCase)
                {
                    var dataerror = new
                    {
                        Status = "Invalid",
                        Message = "Not a Valid User For Case."
                    };
                    return Unauthorized(dataerror);
                }
                //security api update end
                List<ServiceRequestFiles> model = new List<ServiceRequestFiles>();
                var image = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}{_httpContextAccessor.HttpContext.Request.PathBase}" + "/tenant01/Images/imagefound.jpg";
                var ImageQuery = $@"select AttachmentId as photoId,'{image}' as PhotoSource, RoleTypeId,AttachmentType from cms.Attachment where UploadedCaseId={int.Parse(requestModel.CaseNo)} and RoleTypeId={int.Parse(requestModel.RoleId)} ";

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ServiceRequestFiles>(Image);
                    model = ListToBeSend.ToList();
                    var data = new
                    {
                        Status = "200",
                        Data = model,
                    };
                    return Ok(data);

                }
                else
                {
                    var data = new
                    {
                        Status = "No record found",
                        Path = "",
                        PhotoId = ""
                    };
                    return NotFound(data);
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
        //make nabeel host
        [HttpPost]
        [Route("UpdateCaseAttachment")]
        //[RequestSizeLimit(15000000)]
        public async Task<IActionResult> UpdateCaseAttachment([FromForm] UpdateCaseVm model)
        {
            var loggederr = _tenantRepository.InsertErrorLog("Update Case Attacchment Funtion Call");

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
                if (CrossSiteScriptingValidation.IsDangerousParameter(Decryption.Decrypt(model.ExpireDate), out _))
                {
                    var dataBadReq = new
                    {
                        Status = "BadRequest",
                        Message = "Invalid Request.",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
                model.AttachmentId = Decryption.Decrypt(model.AttachmentId);
                var expirydate = "";
                if (!model.ExpireDate.IsNullOrEmpty())
                {
                    model.ExpireDate = Decryption.Decrypt(model.ExpireDate);
                    var expdate = DateTime.ParseExact(model.ExpireDate, "dd-MM-yyyy", null);
                    expirydate = expdate.ToString("dd-MM-yyyy");
                    _tenantRepository.InsertErrorLog(expirydate);
                }
                var validAttachment = IsvalidAttachment(int.Parse(model.AttachmentId), UserId.Value);
                if (!validAttachment)
                {
                    var dataBadReq = new
                    {
                        Status = "Invalid",
                        Message = "Invalid Attachment.",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
                loggederr = _tenantRepository.InsertErrorLog("File Update case 1");
                if (model.File == null)
                {
                    var data = new
                    {
                        Status = "file Not found",
                    };
                    loggederr = _tenantRepository.InsertErrorLog("file not found");
                    return NotFound(data);
                }
                else if (model.File.Count > 0)
                {
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    if (!Directory.Exists(GetFilePath))
                    {
                        Directory.CreateDirectory(GetFilePath);
                    }
                    loggederr = _tenantRepository.InsertErrorLog("File upload 1");
                    //var ServiceRequestThumbnail = _config["Path:ServiceRequestThumbnail"].ToString();
                    //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };
                    foreach (var item in model.File)
                    {
                        string Filname = DateTime.UtcNow.AddHours(4).ToString("yyyyMMddHHmmssffff") + item.FileName;
                        var path = GetFilePath + Filname;
                        //var Thumbnailpath = ServiceRequestThumbnail + Filname;
                        var extention = Path.GetExtension(item.FileName);
                        loggederr = _tenantRepository.InsertErrorLog("before mimetype " + extention);
                        var bytes = item.GetBytes();
                        if (!MimeTypes.IsValidMimeType(bytes, item.ContentType, extention))
                        {
                            var dataerror = new
                            {
                                Status = "Error",
                                Message = "Invalid file extention."
                            };
                            loggederr = _tenantRepository.InsertErrorLog("Invalid file extention.");
                            return BadRequest(dataerror);

                        }
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await item.CopyToAsync(stream);
                            //GenerateTumbnail.ThumbNail(320, 240, stream, Thumbnailpath);
                        }
                        //check attachment belong to same tenant


                        //end verfication
                        var AddFileInfoQuery = $@"update cms.Attachment set AttachmentUrl='{Filname}',ExpireDate='{expirydate}', isRejected=0, AttachmentDate='{DateTime.UtcNow}'  where AttachmentId={int.Parse(model.AttachmentId)}";
                        var sqlParm = new SqlParameter("@expireDate", expirydate);
                        if (model.ExpireDate == null || string.IsNullOrWhiteSpace(model.ExpireDate))
                        {
                            sqlParm = new SqlParameter("@expireDate", DateTime.UtcNow.AddYears(1));

                        }
                        var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, AddFileInfoQuery, sqlParm);
                        loggederr = _tenantRepository.InsertErrorLog("Attachment Upoaded");

                        //Work Here
                        var caseid = _tenantRepository.GetCaseNobyAttachmentid(int.Parse(model.AttachmentId));
                        var getCaseInfo = _tenantRepository.GetCaseDetailInfo(caseid);
                        if (getCaseInfo != null)
                        {
                            var assignee = getCaseInfo.Assignee;
                            var email = getCaseInfo.WorkGroupEmail;
                            var category = getCaseInfo.CategoryName;
                            var title = "Reupload Rejected document";
                            var subject = "Reupload Rejected document";
                            var body = $@"Dear Team, <br/> Case no: {getCaseInfo.CaseNo} with category {category} and is assigned to {assignee}. Rejected document has been reuploaded by tenant <br/>  
                            //    <br/> System Generated Notification.";

                            var sendworkGroupEmail = _tenantRepository.AddNewEmailNotification(body, UserId.Value, subject, email, title, getCaseInfo.CaseNo);
                        }
                    }

                    var data = new
                    {
                        Status = "file Uploaded",
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "file Not found",
                    };
                    return NotFound(data);
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
        private bool IsvalidAttachment(int AttachmentId, int UserId)
        {
            var query = $@"select 1 as IsvalidAttachment From cms.Attachment a  inner join cms.Cases c on c.CaseNo = a.UploadedCaseId inner join cms.Customer cu on cu.CustomerId = c.CaseOwnerId inner join cms.CaseStatuses cs on cs.Id = c.StatusId  where AttachmentId = {AttachmentId} and cu.UserId = {UserId} and (cs.Name not like '%Close%' and cs.Name not like '%Cancel%')";
            //change or to and in above qury
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Contracts = Conn.ExecuteDataTableSqlDA(conn, query);
            return Contracts.Rows.Count > 0;

        }
    }

    public class DownloadServiceRequestDocuments
    {
        public string CaseNo { get; set; }
        public string RoleId { get; set; }
        public string PhotoId { get; set; }
    }

    public class GetTenantContractUnit
    {
        public string Tenantid { get; set; }
    }

    public class CaseSubCategoryModel
    {
        public string CategoryId { get; set; }
    }

    public class DownloadTicketFile
    {
        public string TicketReplyId { get; set; }
    }

    public class GetServiceRequestDocumentsByType
    {
        public string CaseNo { get; set; }
        public string RoleId { get; set; }
        public string? Code { get; set; }
    }

    public class GetServiceRequestDocuments
    {
        public string CaseNo { get; set; }
        public string RoleId { get; set; }
    }

    public class GetTicketReplies
    {
        public string CaseId { get; set; }
    }

    public class GetTenantServiceRequests
    {
        public string type { get; set; }
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public string search { get; set; }

    }

    public class CaseNoBody
    {
        public string CaseNo { get; set; }
    }
    public class UpdateContractDocumentStage
    {
        public string DueActionId { get; set; }
    }
}