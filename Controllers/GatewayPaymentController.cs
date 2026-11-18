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
    public class GatewayPaymentController : ControllerBase
    {
        private IConfiguration _config;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        private readonly ConfigValues configValues;
        private readonly PaymentGatway _gatewayLog;
        private readonly PaymentApi _paymentApi;
        public GatewayPaymentController(IConfiguration config, ITokenService tokenService, ITenantRepository tenantRepository)
        {
            _config = config;
            _tenantRepository = tenantRepository;
            _tokenService = tokenService;
            _settings = new Settings();
            configValues = new ConfigValues();
            _gatewayLog = new PaymentGatway();
            _paymentApi = new PaymentApi();
        }


        [Route("RegisterPaymentNew")]
        [HttpPost]
        public IActionResult RegisterPaymentNew(RegisterPaymentVm model)
        {

            if (User.Identity.IsAuthenticated)
            {
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");

                //commented on 14/10/2022
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
                SqlTransaction sqlTran = null;
                try
                {
                    using (var conn = new SqlConnection(_settings.GetConnectionString()))
                    {
                        conn.Open();
                        sqlTran = conn.BeginTransaction();

                        model.UserId = UserId.Value;
                        //commented temporary 12/10/2022
                        //if (CrossSiteScriptingValidation.IsDangerousParameter(model.ContractNo, out _))
                        //{
                        //    var dataBadReq = new
                        //    {
                        //        Status = "BadRequest",
                        //        Message = "Invalid Request.",
                        //        StatusCode = "500"
                        //    };
                        //    return BadRequest(dataBadReq);
                        //}
                        //Add First Log
                        string Title = "Register payment for the contact id " + model.ContractId;
                        string Description = "Register payment for the contact id " + model.ContractId + " & contact no " + model.ContractNo + " & Amount " + model.TotalAmount + " & UserId " + model.UserId;
                        // var addLog = _gatewayLog.AddCmsLog(UserId.Value, Title, Description);
                        var addLog = _gatewayLog.AddCmsLogTransaction(sqlTran, conn, 1, Title, Description);
                        // End here
                        //tsst 
                        //Insert data in payment Registration
                        string GWRegRefNo = "Not Assigned Yet";
                        // var RegisterPaymentId = _gatewayLog.RegisterPayment(UserId.Value, Title, Description, model.ContractId, GWRegRefNo, model.TotalAmount);
                        var RegisterPaymentId = _gatewayLog.RegisterPaymentTransaction(sqlTran, conn, model.UserId, Title, Description, model.ContractId, GWRegRefNo, model.TotalAmount);
                        var AddPaymantItem = _gatewayLog.AddPaymentDetailItemTransaction(sqlTran, conn, RegisterPaymentId, model.DetailList);

                        //commented on 12/10/2022 111222
                        //if(AddPaymantItem.Contains("BadRequest"))
                        //{
                        //    var dataBadReq = new
                        //    {
                        //        Status = "BadRequest",
                        //        Message = "Invalid Request.",
                        //        StatusCode = "500"
                        //    };
                        //    return BadRequest(dataBadReq);
                        //}
                        //if (AddPaymantItem.Contains("Invalid"))
                        //{
                        //    var dataBadReq = new
                        //    {
                        //        Status = "Invalid",
                        //        Message = "Invalid Request.",
                        //        StatusCode = "500"
                        //    };
                        //    return BadRequest(dataBadReq);
                        //}
                        //Securty updated
                        // 28-Aug 
                        if (string.IsNullOrWhiteSpace(AddPaymantItem))
                        {
                            var data2 = new
                            {
                                Status = "Invalid",
                                Message = "Amount Mismatch"
                            };
                            return Ok(data2);
                        }

                        List<string> OrderArray = new List<string>();
                        foreach (var item in model.DetailList)
                        {

                            //commneted on 12/11/2022
                            //if (CrossSiteScriptingValidation.IsDangerousParameter(item.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(item.Title, out _))
                            //{
                            //    var dataBadReq = new
                            //    {
                            //        Status = "BadRequest",
                            //        Message = "Invalid Request.",
                            //        StatusCode = "500"
                            //    };
                            //    return BadRequest(dataBadReq);
                            //}
                            OrderArray.Add(item.Title);
                        }

                        string FinalOrderInfo = string.Join(", ", OrderArray);
                        FinalOrderInfo = "Contract No: " + model.ContractNo + " items: " + FinalOrderInfo;
                        // Other Api Call here 

                        //var RegisterPayment = _paymentApi.Registration(model.TotalAmount, RegisterPaymentId,UserId.Value, FinalOrderInfo);
                        // var RegisterPayment = _paymentApi.RegistrationSSL(model.TotalAmount, RegisterPaymentId, UserId, FinalOrderInfo);

                        var RegisterPayment = _paymentApi.RegistrationSSLTransaction(sqlTran, conn, model.TotalAmount, RegisterPaymentId, model.UserId, FinalOrderInfo);
                        // Update Transaction Id
                        var updateTranactionId = _gatewayLog.UpdateTransactionIdWithSqlTransaction(sqlTran, conn, RegisterPaymentId, RegisterPayment.TransactionID);

                        var data = new
                        {
                            OrderId = RegisterPaymentId,
                            TransactionID = RegisterPayment.TransactionID,
                            Url = RegisterPayment.PaymentPortal,
                            status = "Ok",
                        };
                        sqlTran.Commit();
                        return Ok(data);
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

        [Route("RegisterPayment")]
        [HttpPost]
        public IActionResult RegisterPayment(RegisterPaymentVm model)
        {
           
            if (User.Identity.IsAuthenticated)
            {
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");

                //commented on 14/10/2022
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
                SqlTransaction sqlTran = null;
                try
                {
                    using (var conn = new SqlConnection(_settings.GetConnectionString()))
                    {
                        conn.Open();
                        sqlTran = conn.BeginTransaction();

                        model.UserId = UserId.Value;
                        //commented temporary 12/10/2022
                        //if (CrossSiteScriptingValidation.IsDangerousParameter(model.ContractNo, out _))
                        //{
                        //    var dataBadReq = new
                        //    {
                        //        Status = "BadRequest",
                        //        Message = "Invalid Request.",
                        //        StatusCode = "500"
                        //    };
                        //    return BadRequest(dataBadReq);
                        //}
                        //Add First Log
                        string Title = "Register payment for the contact id " + model.ContractId;
                        string Description = "Register payment for the contact id " + model.ContractId + " & contact no " + model.ContractNo + " & Amount " + model.TotalAmount + " & UserId " + model.UserId;
                        // var addLog = _gatewayLog.AddCmsLog(UserId.Value, Title, Description);
                        var addLog = _gatewayLog.AddCmsLogTransaction(sqlTran,conn,1, Title, Description);
                        // End here
                        //tsst 
                        //Insert data in payment Registration
                        string GWRegRefNo = "Not Assigned Yet";
                        // var RegisterPaymentId = _gatewayLog.RegisterPayment(UserId.Value, Title, Description, model.ContractId, GWRegRefNo, model.TotalAmount);
                        var RegisterPaymentId = _gatewayLog.RegisterPaymentTransaction(sqlTran,conn,model.UserId, Title, Description, model.ContractId, GWRegRefNo, model.TotalAmount);
                        var AddPaymantItem = _gatewayLog.AddPaymentDetailItemTransaction(sqlTran,conn,RegisterPaymentId, model.DetailList);

                        //commented on 12/10/2022 111222
                        //if(AddPaymantItem.Contains("BadRequest"))
                        //{
                        //    var dataBadReq = new
                        //    {
                        //        Status = "BadRequest",
                        //        Message = "Invalid Request.",
                        //        StatusCode = "500"
                        //    };
                        //    return BadRequest(dataBadReq);
                        //}
                        //if (AddPaymantItem.Contains("Invalid"))
                        //{
                        //    var dataBadReq = new
                        //    {
                        //        Status = "Invalid",
                        //        Message = "Invalid Request.",
                        //        StatusCode = "500"
                        //    };
                        //    return BadRequest(dataBadReq);
                        //}
                        //Securty updated
                        // 28-Aug 
                        if (string.IsNullOrWhiteSpace(AddPaymantItem))
                        {
                            var data2 = new
                            {
                                Status = "Invalid",
                                Message = "Amount Mismatch"
                            };
                            return Ok(data2);
                        }

                        List<string> OrderArray = new List<string>();
                        foreach (var item in model.DetailList)
                        {

                            //commneted on 12/11/2022
                            //if (CrossSiteScriptingValidation.IsDangerousParameter(item.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(item.Title, out _))
                            //{
                            //    var dataBadReq = new
                            //    {
                            //        Status = "BadRequest",
                            //        Message = "Invalid Request.",
                            //        StatusCode = "500"
                            //    };
                            //    return BadRequest(dataBadReq);
                            //}
                            OrderArray.Add(item.Title);
                        }

                        string FinalOrderInfo = string.Join(", ", OrderArray);
                        FinalOrderInfo = "Contract No: " + model.ContractNo + " items: " + FinalOrderInfo;
                        // Other Api Call here 

                        //var RegisterPayment = _paymentApi.Registration(model.TotalAmount, RegisterPaymentId,UserId.Value, FinalOrderInfo);
                        // var RegisterPayment = _paymentApi.RegistrationSSL(model.TotalAmount, RegisterPaymentId, UserId, FinalOrderInfo);

                        var RegisterPayment = _paymentApi.RegistrationSSLTransaction(sqlTran,conn,model.TotalAmount, RegisterPaymentId, model.UserId, FinalOrderInfo);
                        // Update Transaction Id
                        var updateTranactionId = _gatewayLog.UpdateTransactionIdWithSqlTransaction(sqlTran,conn,RegisterPaymentId, RegisterPayment.TransactionID);

                        var data = new
                        {
                            OrderId = RegisterPaymentId,
                            TransactionID = RegisterPayment.TransactionID,
                            Url = RegisterPayment.PaymentPortal,
                            status = "Ok",
                        };
                        sqlTran.Commit();
                        return Ok(data);
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




        [Route("RegisterPaymentCopy")]
        [HttpPost]
        public IActionResult RegisterPaymentCopy(RegisterPaymentVm model)
        {
            if (User.Identity.IsAuthenticated)
            {
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");

                //commented on 14/10/2022
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
                model.UserId = UserId.Value;
                //commented temporary 12/10/2022
                //if (CrossSiteScriptingValidation.IsDangerousParameter(model.ContractNo, out _))
                //{
                //    var dataBadReq = new
                //    {
                //        Status = "BadRequest",
                //        Message = "Invalid Request.",
                //        StatusCode = "500"
                //    };
                //    return BadRequest(dataBadReq);
                //}
                //Add First Log
                string Title = "Register payment for the contact id " + model.ContractId;
                string Description = "Register payment for the contact id " + model.ContractId + " & contact no " + model.ContractNo + " & Amount " + model.TotalAmount + " & UserId " + model.UserId;
                // var addLog = _gatewayLog.AddCmsLog(UserId.Value, Title, Description);
                var addLog = _gatewayLog.AddCmsLog(1, Title, Description);
                // End here
                //tsst 
                //Insert data in payment Registration
                string GWRegRefNo = "Not Assigned Yet";
                // var RegisterPaymentId = _gatewayLog.RegisterPayment(UserId.Value, Title, Description, model.ContractId, GWRegRefNo, model.TotalAmount);
                var RegisterPaymentId = _gatewayLog.RegisterPayment(model.UserId, Title, Description, model.ContractId, GWRegRefNo, model.TotalAmount);
                var AddPaymantItem = _gatewayLog.AddPaymentDetailItem(RegisterPaymentId, model.DetailList);

                //commented on 12/10/2022 111222
                //if(AddPaymantItem.Contains("BadRequest"))
                //{
                //    var dataBadReq = new
                //    {
                //        Status = "BadRequest",
                //        Message = "Invalid Request.",
                //        StatusCode = "500"
                //    };
                //    return BadRequest(dataBadReq);
                //}
                //if (AddPaymantItem.Contains("Invalid"))
                //{
                //    var dataBadReq = new
                //    {
                //        Status = "Invalid",
                //        Message = "Invalid Request.",
                //        StatusCode = "500"
                //    };
                //    return BadRequest(dataBadReq);
                //}
                //Securty updated
                // 28-Aug 
                if (string.IsNullOrWhiteSpace(AddPaymantItem))
                {
                    var data2 = new
                    {
                        Status = "Invalid",
                        Message = "Amount Mismatch"
                    };
                    return Ok(data2);
                }

                List<string> OrderArray = new List<string>();
                foreach (var item in model.DetailList)
                {

                    //commneted on 12/11/2022
                    //if (CrossSiteScriptingValidation.IsDangerousParameter(item.Description, out _) || CrossSiteScriptingValidation.IsDangerousParameter(item.Title, out _))
                    //{
                    //    var dataBadReq = new
                    //    {
                    //        Status = "BadRequest",
                    //        Message = "Invalid Request.",
                    //        StatusCode = "500"
                    //    };
                    //    return BadRequest(dataBadReq);
                    //}
                    OrderArray.Add(item.Title);
                }

                string FinalOrderInfo = string.Join(", ", OrderArray);
                FinalOrderInfo = "Contract No: " + model.ContractNo + " items: " + FinalOrderInfo;
                // Other Api Call here 

                //var RegisterPayment = _paymentApi.Registration(model.TotalAmount, RegisterPaymentId,UserId.Value, FinalOrderInfo);
                // var RegisterPayment = _paymentApi.RegistrationSSL(model.TotalAmount, RegisterPaymentId, UserId, FinalOrderInfo);

                var RegisterPayment = _paymentApi.RegistrationSSL(model.TotalAmount, RegisterPaymentId, model.UserId, FinalOrderInfo);
                // Update Transaction Id
                var updateTranactionId = _gatewayLog.UpdateTransactionId(RegisterPaymentId, RegisterPayment.TransactionID);

                var data = new
                {
                    OrderId = RegisterPaymentId,
                    TransactionID = RegisterPayment.TransactionID,
                    Url = RegisterPayment.PaymentPortal,
                    status = "Ok",
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

        [Route("UnVerfiedAllPayment")]
        [HttpPost]
        public ActionResult<string> UnVerfiedAllPayment()
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

                List<ContractPaymantVm> ContractPaymentList = new List<ContractPaymantVm>();

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);

                var GetInfo = $@"select gp.GWRegRefNo as ReferenceNo,gpd.PaymentDetailId,gpd.Title,gp.ResponseCode,UserId,gp.PaymentId, gpd.Description,gpd.Amount,gp.Status,pc.Contractno,gpd.CreatedOn, ps.Cheque, pm.Name as PaymentType, ps.ChequeNo from cms.GatewayPaymantDetail gpd
                    join cms.GatewayPaymant gp on gp.PaymentId=gpd.PaymentId
                    join Pms_Contract pc on pc.ContractID=gp.ContractId 
					left join cms.PaymentSetting ps on ps.PaymentSettingId=gpd.PaymentSettingId
					left join cms.PaymentMethod pm on ps.PaymentMethodId=pm.PaymentMethodId  where UserId={UserId} and gp.ResponseCode in(0,000,200) and Reconciled=0  order by PaymentId desc";

                var AddRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetInfo);
                if (AddRequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ContractPaymantVm>(AddRequestDetail);
                    ContractPaymentList = ListToBeSend.ToList();
                }
                var data = new
                {
                    status = "ok",
                    ContractPayments = ContractPaymentList,
                    Note = "Online Payments will be updated against the contract within 7~10 days"
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
                return Ok(data);
            }
        }
        private bool ContractInTenantId(int contractId, int tenantId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Contracts = Conn.ExecuteDataTableSqlDA(conn, $"SELECT 1 FROM Pms_Contract WHERE (TenantID = {tenantId}) AND (ContractID = {contractId})");
            return Contracts.Rows.Count > 0;
        }
        private int GetTenantId(int UserId)
        {
            int TenantId = 0;
            TenantId = _tenantRepository.GetTenantId(UserId);
            return TenantId;
        }

        [Route("UnVerfiedContractPayment")]
        [HttpPost]
        public ActionResult<string> UnVerfiedContractPayment([FromBody] UnVerfiedContractPayment requestModel)
        {
            int ContractId = int.Parse(requestModel.ContractId);
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

                //commented on 14/10/2022
                //var contractTenantId = ContractInTenantId(ContractId, TenantID);

                //if (!contractTenantId)
                //{
                //    var data1 = new
                //    {
                //        Status = "Invalid",
                //        Message = "Contract not found for this user; please try with valid user."
                //    };
                //    return NotFound(data1);
                //}

                List<ContractPaymantVm> ContractPaymentList = new List<ContractPaymantVm>();
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                //24/12/2022 replace reconcile qurey
                //           var GetInfo = $@"select gp.GWRegRefNo as ReferenceNo,gpd.PaymentDetailId,gpd.Title,gp.ResponseCode,UserId,gp.PaymentId, gpd.Description,gpd.Amount,gp.Status,pc.Contractno,gpd.CreatedOn, ps.Cheque, pm.Name as PaymentType, ps.ChequeNo from cms.GatewayPaymantDetail gpd
                //               join cms.GatewayPaymant gp on gp.PaymentId=gpd.PaymentId
                //               join Pms_Contract pc on pc.ContractID=gp.ContractId 
                //left join cms.PaymentSetting ps on ps.PaymentSettingId=gpd.PaymentSettingId
                //left join cms.PaymentMethod pm on ps.PaymentMethodId=pm.PaymentMethodId  where UserId={UserId} and gp.ResponseCode in(0,000,200) and gp.ContractId={ContractId} and Reconciled=0  order by PaymentId desc";

                //           var GetInfo = $@"select gp.GWRegRefNo as ReferenceNo,gpd.PaymentDetailId,gpd.Title,gp.ResponseCode,UserId,gp.PaymentId, gpd.Description,gpd.Amount,gp.Status,pc.Contractno,gpd.CreatedOn, ps.Cheque, pm.Name as PaymentType, ps.ChequeNo from cms.GatewayPaymantDetail gpd
                //             join cms.GatewayPaymant gp on gp.PaymentId=gpd.PaymentId
                //               join Pms_Contract pc on pc.ContractID=gp.ContractId 
                //left join cms.PaymentSetting ps on ps.PaymentSettingId=gpd.PaymentSettingId
                //left join cms.PaymentMethod pm on ps.PaymentMethodId=pm.PaymentMethodId  where UserId={UserId} and gp.ResponseCode in(0,000,200) and gp.ContractId={ContractId} and not exists(select * from Pms_Transactions where OnlinePaymentRefNumber=gp.GWRegRefNo and TransactionTypeID=1 and Status='Confirmed' and Suspend=0)  order by PaymentId desc";
               
                //ar added 10-01-2023
                var GetInfo = $@"select gp.GWRegRefNo as ReferenceNo,gpd.PaymentDetailId,ps.Title,ps.TitleAR,gp.ResponseCode,UserId,gp.PaymentId, gpd.Description,gpd.Amount,gp.Status,pc.Contractno,gpd.CreatedOn, ps.Cheque, pm.Name as PaymentType,pm.NameAR as PaymentTypeAR, ps.ChequeNo from cms.GatewayPaymantDetail gpd
                  join cms.GatewayPaymant gp on gp.PaymentId=gpd.PaymentId
                    join Pms_Contract pc on pc.ContractID=gp.ContractId 
					left join cms.PaymentSetting ps on ps.PaymentSettingId=gpd.PaymentSettingId
					left join cms.PaymentMethod pm on ps.PaymentMethodId=pm.PaymentMethodId  where UserId={UserId} and gp.ResponseCode in(0,000,200) and gp.ContractId={ContractId} and not exists(select * from Pms_Transactions where OnlinePaymentRefNumber=gp.GWRegRefNo and TransactionTypeID=1 and Status='Confirmed' and Suspend=0)  order by PaymentId desc";

                var AddRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetInfo);
                if (AddRequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ContractPaymantVm>(AddRequestDetail);
                    ContractPaymentList = ListToBeSend.ToList();
                }
                var data = new
                {
                    status = "ok",
                    ContractPayments = ContractPaymentList,
                    Note = ""
                };
                //

                return Ok(data);
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
    }

    public class UnVerfiedContractPayment
    {
        public string ContractId { get; set; }
    }
}