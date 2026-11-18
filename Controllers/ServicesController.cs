using FABTenantApi.Classes;
using FABTenantApi.Models;
using Microsoft.AspNetCore.Authorization;
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

namespace FABTenantApi.Controllers
{
    [Route("api/Tenant/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private IConfiguration _config;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        public ServicesController(IConfiguration config, ITokenService tokenService, ITenantRepository tenantRepository)
        {
            _config = config;
            _tenantRepository = tenantRepository;
            _tokenService = tokenService;
            _settings = new Settings();
        }
     
        private List<CaseServiceVm> ServiceRequests(int TenantId)
        {
            var services = new List<CaseServiceVm>();
            services = _tenantRepository.GetServiceRequests(TenantId);
            return services;
        }
        
        private int GetTenantId(int UserId)
        {
            int TenantId = 0;
            TenantId = _tenantRepository.GetTenantId(UserId);
            return TenantId;
        }

        [HttpPost]
        [Route("GetRequests")]
        public ActionResult<string> GetRequests()
        {
            try { 
            var services = new List<CaseServiceVm>();
            if (User.Identity.IsAuthenticated)
            {
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                if (UserId == null)
                {
                    var response = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(response);
                }
                var TenantId = GetTenantId(UserId.Value);
                if (TenantId == 0)
                {
                    var dataInvalid = new
                    {
                        Status = "NotFound",
                        data = services,
                        statusCode = "404",
                        Message = "Invalid User please try with valid user."
                    };
                    return NotFound(dataInvalid);
                }
                var getservices = ServiceRequests(TenantId);
                if (getservices != null && getservices.Count() > 0)
                {
                    services = getservices;
                }

                var data = new
                {
                    data = services,
                    message = services.Count() + " Services Found.",
                    statusCode = "Ok",
                    status = "Ok"
                };
                return Ok(data);
            }
            else
            {
                var data = new
                {

                    message = "Invalid Session",
                    statusCode = "401",
                    status = "Unauthorized",
                    data = services
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
}
