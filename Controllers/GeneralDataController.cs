using FABTenantApi.Classes;
using FABTenantApi.Models;
using FABTenantD2Api.Classes;
using FABTenantD2Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Rotativa.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantD2Api.Controllers
{
    [Route("api/Tenant/[controller]")]
    [ApiController]
    public class GeneralDataController : ControllerBase
    {
        private readonly IHostingEnvironment _hostEnv;
        private IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        public GeneralDataController(IConfiguration config, ITokenService tokenService, IHostingEnvironment hostingEnvironment)
        {
            _config = config;
            _hostEnv = hostingEnvironment;
            _tokenService = tokenService;
            _settings = new Settings();
        }
        [Route("GetServicesCategories")]
        [HttpPost]
        public ActionResult<string> GetServicesCategories()
        {
            try
            {
                List<ServicesCategoriesVm> ServiceCategoriesData = new List<ServicesCategoriesVm>();
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
                    var FaqCategoriesQuery = $@"select Title, CategoryId, TitleAR from cms.ServiceCategories where Active=1 and RoleId=3";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, FaqCategoriesQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ServicesCategoriesVm>(RequestDetail);
                        ServiceCategoriesData = ListToBeSend;
                    }
                    if (ServiceCategoriesData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            ServiceCategories = ServiceCategoriesData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            ServiceCategories = ServiceCategoriesData,
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

        [Route("GetServices")]
        [HttpPost]
        public ActionResult<string> GetServices(int CategoryId)
        {
            try
            {
                List<ServicesVm> ServiceData = new List<ServicesVm>();
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
                                                INNER JOIN CMS.ServiceCategories fc  ON faq.CategoryId = fc.CategoryId 
                                                INNER JOIN CMS.FAQRoleType frt ON fc.RoleId = frt.Id
                                                where fc.Active=1 and faq.Active=1 and faq.CategoryId={CategoryId}";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, FaqQuery);

                    if (RequestDetail.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ServicesVm>(RequestDetail);
                        ServiceData = ListToBeSend;
                    }
                    if (ServiceData != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            Services = ServiceData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            Services = ServiceData,
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

        [Route("GetConfigurationValue")]
        [HttpPost]
        public ActionResult<string> GetConfigurationValue([FromBody] GetConfigurationValueRM requestModel)
        {//string Key
            try
            {
                if (CrossSiteScriptingValidation.IsDangerousParameter(requestModel.Key, out _))
                {
                    var dataBadReq = new
                    {
                        Status = "BadRequest",
                        Message = "Invalid Request.",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
                ConfigurationValueVm Data = new ConfigurationValueVm();
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                //EnsureNewContractSettings(conn);
                //28 Aug
                var Query = $@"SELECT Settingid ,Name ,Value ,Title ,Type, ValueAr, TitleAr FROM [App].[Settings] where Name like '%'+@Key+'%'";

                var keysqlParam = new SqlParameter("@Key", requestModel.Key);
                if (string.IsNullOrEmpty(requestModel.Key))
                {
                    keysqlParam = new SqlParameter("@Key", DBNull.Value);

                }
                var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, Query, keysqlParam);

                if (RequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ConfigurationValueVm>(RequestDetail);
                    Data = ListToBeSend.FirstOrDefault();
                }
                if (Data != null)
                {
                    var data = new
                    {
                        Status = "Ok",
                        Data = Data,
                        Message = "Data Found."
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        Data = Data,
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
                    Message = "Internal Server Error",

                };
                return BadRequest(data);
            }
        }

        private bool EnsureNewContractSettings(SqlConnection conn)
        {
            string checkQuery = @"
                SELECT COUNT(*)
                FROM [App].[Settings]
                WHERE Name IN (
                    'PrivacyPolicyNewContract',
                    'RefundPolicyNewContract'
                    )";

            var result = Conn.ExecuteDataTableSqlDA(conn, checkQuery);

            int count = 0;

            if (result != null && result.Rows.Count > 0)
            {
                count = Convert.ToInt32(result.Rows[0][0]);
            }
            // Both already exist
            if (count == 2)
            {
                return true;
            }

            var privacPolicyValue = _config["NewContractDetail:privacPolicyValue"]?.ToString();
            var privacPolicyValueAR = _config["NewContractDetail:privacPolicyValueAR"]?.ToString();
            var refundPolicyValue = _config["NewContractDetail:refundPolicyValue"]?.ToString();
            var refundPolicyValueAR = _config["NewContractDetail:refundPolicyValueAR"]?.ToString();

            string insertQuery = @"
IF NOT EXISTS (
    SELECT 1 
    FROM [App].[Settings] 
    WHERE Name = 'PrivacyPolicyNewContract'
)
BEGIN
    INSERT INTO [App].[Settings]
        (Name, Value, Title, Type, ValueAr, TitleAr)
    VALUES
        (
            'PrivacyPolicyNewContract',
            @PrivacyPolicyValue,
            'Privacy Policy New Contract',
            'New Contract',
            @PrivacyPolicyValueAR,
            'سياسة الخصوصية للعقد الجديد'
        );
END

IF NOT EXISTS (
    SELECT 1 
    FROM [App].[Settings] 
    WHERE Name = 'RefundPolicyNewContract'
)
BEGIN
    INSERT INTO [App].[Settings]
        (Name, Value, Title, Type, ValueAr, TitleAr)
    VALUES
        (
            'RefundPolicyNewContract',
            @RefundPolicyValue,
            'Refund Policy New Contract',
            'New Contract',
            @RefundPolicyValueAR,
            'سياسة الاسترداد للعقد الجديد'
        );
END";

            Conn.ExecuteDataTableSqlDA(conn, insertQuery);

            return true;

        }

        public class GetConfigurationValueRM
        {
            public string Key { get; set; }
        }
    }
}
