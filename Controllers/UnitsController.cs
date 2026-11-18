using FABTenantApi.Classes;
using FABTenantApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantApi.Controllers
{
    [Route("api/Tenant/[controller]")]
    [ApiController]
    public class UnitsController : ControllerBase
    {
        private IConfiguration _config;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        public UnitsController(IConfiguration config, ITokenService tokenService, ITenantRepository tenantRepository)
        {
            _config = config;
            _tenantRepository = tenantRepository;
            _tokenService = tokenService;
            _settings = new Settings();
        }
        private int GetTenantId(int UserId)
        {
            int TenantId = 0;
            TenantId = _tenantRepository.GetTenantId(UserId);
            return TenantId;
        }
        [HttpPost]
        [Route("GetUnits")]
        public ActionResult<string> GetUnits()
        {
            var TenantId = 0;
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
            TenantId = GetTenantId(UserId.Value);
            if (TenantId == 0)
            {
                var data = new
                {
                    Status = "Invalid",
                    units = new List<UnitVm>(),
                    Message = "Invalid User please try with valid user."
                };
                return Ok(data);
            }
            List<UnitVm> unitsData = new List<UnitVm>();
            if (User.Identity.IsAuthenticated)
            {
                string DateFormat = _settings.DateFormat();

                var unitQuery = $@"Select Top 20 c.ContractNo as ReferenceNo, p.PropertyName, p.PropertyImage, Format(c.FromDate, '{DateFormat}') as StartDate, Format(c.ToDate, '{DateFormat}') as EndDate, (Select Top 1 u.UnitNo from Pms_Unit u Where u.PropertyID = c.PropertyID and u.Suspend=0) as UnitNo  From Pms_Contract c LEFT OUTER JOIN Pms_Property p on p.PropertyID = c.PropertyID  Where   c.suspend=0 and p.suspend=0 and  c.TenantID = {TenantId}";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var units = Conn.ExecuteDataTableSqlDA(conn, unitQuery);

                if (units.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<UnitVm>(units);
                    unitsData = ListToBeSend;
                }
                if (unitsData.Count() > 0)
                {
                    var data = new
                    {
                        Status = "Ok",
                        units = unitsData,
                        Message = "Data Found."
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        Units = unitsData,
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
                    Message = "Invalid Session."
                };
                return Ok(data);
            }
        }

        [HttpPost]
        [Route("GetUnitDetails")]
        public ActionResult<string> GetUnitDetails([FromBody] GetUnitDetails requestModel)
        {
            try
            {

                int UnitId = int.Parse(requestModel.UnitId);
            var TenantId = 0;
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
            TenantId = GetTenantId(UserId.Value);
            if (TenantId == 0)
            {
                var data = new
                {
                    Status = "Invalid",
                    unit = new UnitVm(),
                    Message = "Invalid User please try with valid user."
                };
                return Ok(data);
            }
            List<UnitVm> unitsData = new List<UnitVm>();
            if (User.Identity.IsAuthenticated)
            {
     //           var unitQuery = $@"SELECT unitrefno as UnitName, UnitImage as UnitPicture, ut.UnitType as UnitType, p.PropertyName, p.PropertyImage
					//FROM pms_unit u
					//	, pms_contractunits cu
					//	, Pms_Contract c
					//	, Pms_UnitType ut
					//	, Pms_UnitView uv
					//	,Pms_Property  p
					//WHERE u.SUSPEND = 0
					//	AND cu.SUSPEND = 0
					//	AND ut.SUSPEND = 0
					//	AND uv.SUSPEND = 0
					//	AND c.Suspend=0
					//	AND ut.UnitTypeID = u.UnitTypeID
					//	AND uv.UnitViewID =  u.UnitViewID
					//	AND u.unitid = cu.unitid
					//	AND cu.CONTRACTID = c.ContractID
					//AND c.unitid  ={UnitId}
     //               AND c.TenantId =  {TenantId}    ";
     //correctin c.unitid and added tenantid clasue
                    var unitQuery = $@"  SELECT unitrefno as UnitName, UnitImage as UnitPicture, ut.UnitType as UnitType, p.PropertyName, p.PropertyImage
                    FROM pms_unit u
                        , pms_contractunits cu
						, Pms_Contract c
                        , Pms_UnitType ut
						, Pms_UnitView uv
                        , Pms_Property  p
                    WHERE u.SUSPEND = 0

                        AND cu.SUSPEND = 0

                        AND ut.SUSPEND = 0

                        AND uv.SUSPEND = 0

                        AND c.Suspend = 0

                        AND ut.UnitTypeID = u.UnitTypeID

                        AND uv.UnitViewID = u.UnitViewID

                        AND u.unitid = cu.unitid

                        AND cu.CONTRACTID = c.ContractID

                    AND u.unitid = { UnitId}
                    AND c.TenantId = { TenantId}

                         ";

                    var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var units = Conn.ExecuteDataTableSqlDA(conn, unitQuery);

                if (units.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<UnitVm>(units);
                    unitsData = ListToBeSend;
                }
                if (unitsData.Count() > 0)
                {
                    var data = new
                    {
                        Status = "Ok",
                        units = unitsData,
                        Message = "Data Found."
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        Units = unitsData,
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
                    Message = "Invalid Session."
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
    }
}
