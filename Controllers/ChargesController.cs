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
    public class ChargesController : ControllerBase
    {
        private IConfiguration _config;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        public ChargesController(IConfiguration config, ITokenService tokenService, ITenantRepository tenantRepository)
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

        [Route("GetCharges")]
        [HttpPost]
        public ActionResult<string> GetCharges(int ContractId)
        {
            try { 
            string DateFormat = _settings.DateFormat();
            List<ChargeVm> ChargesData = new List<ChargeVm>();
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
                var TenantId = GetTenantId(UserId.Value);
                if (TenantId == 0)
                {
                    var dataInvalid = new
                    {
                        Status = "Invalid",
                        Charges = ChargesData,
                        Message = "Invalid User please try with valid user."
                    };
                    return Ok(dataInvalid);
                }
                var ChargeQuery = $@"select  pt.PaymentFor as ChargesFor,  cc.ContractchargeID as ChargesNumber,cc.ChargeName , cc.Amount as ChargesAmount,cc.ContractID as ContractId, Isnull(cc.VATAmount, 0) as VATAmount, Format(IsNull(cc.CreationDate, GETDATE()), '{DateFormat}') as [Date] from  Pms_Contract c, Pms_ContractCharges cc, Pms_ReasonOfPayment pt where  c.Suspend=0 and  cc.Suspend=0 and  pt.Suspend=0 and c.ContractType=1 and c.ContractID=cc.ContractID and cc.ChargesTypeID = pt.ReasonofPaymentID and c.TenantID = {TenantId} and cc.ContractID = {ContractId}";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery);

                if (Charges.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ChargeVm>(Charges);
                    ChargesData = ListToBeSend;
                }
                if (ChargesData.Count() > 0)
                {
                    var data = new
                    {
                        Status = "Ok",
                        Charges = ChargesData,
                        Message = "Data Found."
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        Charges = ChargesData,
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
        [Route("GetPaymentDetails")]
        [HttpPost]
        public ActionResult<string> GetPaymentDetails(int ContractId)
        {
            try { 
            List<PaymentInfoVm> paymentdata = new List<PaymentInfoVm>();
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
                var TenantId = GetTenantId(UserId.Value);
                if (TenantId == 0)
                {
                    var dataInvalid = new
                    {
                        Status = "Invalid",
                        paymentInfo = paymentdata,
                        Message = "Invalid User please try with valid user."
                    };
                    return Ok(dataInvalid);
                }
                var payementQuery = $@"SELECT
	b.TransactionNo AS TransactionNo
	, convert(CHAR(10), b.transactiondate, 103) AS DATE	
	, isnull((
			SELECT paymentfor
			FROM pms_reasonofpayment pr
			WHERE pr.reasonofpaymentid = b.paymentfor
			), '') AS ReasonofPayment
		, ChequeNo
			, convert(CHAR(10), a.ChequeDate, 103) AS ChequeDate
		, isnull((
			SELECT bankname
			FROM pms_banknames pb
			WHERE pb.banknameid = a.banknameid
			), '') AS Bank
	, ChequeAmount
	, isnull((
			SELECT chequestatus
			FROM pms_chequestatus pr
			WHERE pr.chequestatusid = a.ChequeStatusid
			), '') AS ChequeStatus
, a.Remarks AS Description
FROM pms_cheques a
	, pms_transactions b
	, Pms_Contract c
WHERE a.suspend = 0
	AND b.suspend = 0
	AND c.Suspend=0
	AND a.transactionid = b.transactionid
   and b.status='Confirmed' and transactiontypeid=1  
   and b.ContractID=c.ContractID
   and c.TenantID ={TenantId}
   and c.ContractID= {ContractId}";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Charges = Conn.ExecuteDataTableSqlDA(conn, payementQuery);

                if (Charges.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<PaymentInfoVm>(Charges);
                    paymentdata = ListToBeSend;
                }
                if (paymentdata.Count() > 0)
                {
                    var data = new
                    {
                        Status = "Ok",
                        paymentInfo = paymentdata,
                        Message = "Data Found."
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        Charges = paymentdata,
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
