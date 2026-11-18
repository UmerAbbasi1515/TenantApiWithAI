using FABModels.Model;
using FABTenantApi.Classes;
using FABTenantApi.Models;
using FABTenantD2Api.Models;
using FABTenantD2Api.Repository.DashboardRepo;
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
    //[Authorize]
    public class DashboardController : ControllerBase
    {
        private IConfiguration _config;
        private readonly FABAppDbContext _dbContext;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        private readonly IDashboardRepo _dashboardRepo;
        public DashboardController(IConfiguration config, ITokenService tokenService, ITenantRepository tenantRepository, IDashboardRepo dashboardRepo, FABAppDbContext dbContext)
        {
            _dbContext = dbContext;
            _config = config;
            _tenantRepository = tenantRepository;
            _tokenService = tokenService;
            _dashboardRepo = dashboardRepo;
            _settings = new Settings();
        }
        private string GetFullName(int TenantId)
        {
            var fullName = "";
            var nameQuery = $@"SELECT isnull(FirstName, '') + ' ' + isnull([FullName],'')  as FullName FROM [dbo].[Pms_Tenant] where Suspend=0 and IsBlockListed = 0 AND TenantID = {TenantId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var name = Conn.ExecuteDataTableSqlDA(conn, nameQuery);
            List<ProVm> proVms = new List<ProVm>();
            if (name.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ProVm>(name);
                proVms = ListToBeSend;
            }
            if (proVms.Count() > 0)
            {
                var proname = proVms.FirstOrDefault();
                fullName = proname.FullName;
            }
            return fullName;
        }
        private string GetFullNameAR(int TenantId)
        {
            var fullName = "";
            var nameQuery = $@"SELECT isnull(FirstNameAR, '') + ' ' + isnull([FullNameAR],'')  as FullNameAR FROM [dbo].[Pms_Tenant] where Suspend=0 and IsBlockListed = 0 AND TenantID = {TenantId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var name = Conn.ExecuteDataTableSqlDA(conn, nameQuery);
            List<ProVm> proVms = new List<ProVm>();
            if (name.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ProVm>(name);
                proVms = ListToBeSend;
            }
            if (proVms.Count() > 0)
            {
                var proname = proVms.FirstOrDefault();
                fullName = proname.FullName;
            }
            return fullName;
        }
        private List<CaseServiceVm> ServiceRequests(int TenantId)
        {
            var services = new List<CaseServiceVm>();
            services = _tenantRepository.GetServiceRequests(TenantId);
            return services;
        }
        private int GetContractsExpiring(int TenantId)
        {
            int contracts = 0;
            var contractQuery = $@"select  COUNT(*) as ContractsExpiring from Pms_Contract where Suspend=0 and ToDate between CAST( GETDATE() AS Date ) AND  DATEADD(DD,30,( CAST( GETDATE() AS Date ))) AND ContractType= 1 AND TenantID ={TenantId} -- contract expiring";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var contract = Conn.ExecuteDataTableSqlDA(conn, contractQuery);
            List<ContExpiringVm> conts = new List<ContExpiringVm>();
            if (contract.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ContExpiringVm>(contract);
                conts = ListToBeSend;
            }
            if (conts.Count() > 0)
            {
                var contExpiring = conts.FirstOrDefault();
                contracts = contExpiring.ContractsExpiring;
            }
            return contracts;
        }
        private int GetTenantId(int UserId)
        {
            int TenantId = 0;
            TenantId = _tenantRepository.GetTenantId(UserId);
            return TenantId;
        }
        private string GetPropertyImage(int TenantId)
        {
            var propertyImage = "";
            var imageQuery = $@"select PropertyImage from Pms_Property p, Pms_Contract c where p.Suspend=0 and p.HandOverDate is null and c.Suspend=0 and c.ContractType=1 and c.PropertyID=p.PropertyID and c.TenantID = {TenantId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var image = Conn.ExecuteDataTableSqlDA(conn, imageQuery);
            List<ImageVm> images = new List<ImageVm>();
            if (image.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ImageVm>(image);
                images = ListToBeSend;
            }
            if (images.Count() > 0)
            {
                var imagee = images.FirstOrDefault();
                propertyImage = imagee.PropertyImage;
            }
            return propertyImage;
        }
        private decimal GetThiryDayPayment(int TenantId)
        {
            decimal thiryDayPayment = 0;
            var paymentQuery = $@"SELECT isnull(sum(ChequeAmount),0) as ThiryDayPayment FROM pms_cheques chq ,pms_transactions trans ,pms_contract tc WHERE tc.suspend = 0 AND chq.suspend = 0 AND trans.suspend = 0 AND chq.transactionid = trans.transactionid 	AND chequestatusid IN (	1,3,9	)AND trans.ContractID = tc.contractid AND STATUS = 'Confirmed'	AND transactiontypeid = 1 AND chq.ChequeDate between   CAST( GETDATE() AS Date ) AND  DATEADD(DD,30,( CAST( GETDATE() AS Date ))) AND tc.TenantID =  {TenantId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var payment = Conn.ExecuteDataTableSqlDA(conn, paymentQuery);
            List<PayVm> payments = new List<PayVm>();
            if (payment.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<PayVm>(payment);
                payments = ListToBeSend;
            }
            if (payments.Count() > 0)
            {
                var pay = payments.FirstOrDefault();
                thiryDayPayment = pay.ThiryDayPayment;
            }
            return thiryDayPayment;
        }
        private int GetCheckDueInThirtyDay(int TenantId)
        {
            int checkDueIn30Days = 0;
            var paymentQuery = $@"SELECT isnull(Count(*),0) as CheckDueInThirtyDays FROM pms_cheques chq ,pms_transactions trans ,pms_contract tc WHERE tc.suspend = 0 AND chq.suspend = 0 AND trans.suspend = 0 AND chq.transactionid = trans.transactionid 	AND chequestatusid IN (	1,3,9	)AND trans.ContractID = tc.contractid AND STATUS = 'Confirmed'	AND transactiontypeid = 1 AND chq.ChequeDate between   CAST( GETDATE() AS Date ) AND  DATEADD(DD,30,( CAST( GETDATE() AS Date ))) AND tc.Occupant = {TenantId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var payment = Conn.ExecuteDataTableSqlDA(conn, paymentQuery);
            List<CheckDueVm> payments = new List<CheckDueVm>();
            if (payment.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<CheckDueVm>(payment);
                payments = ListToBeSend;
            }
            if (payments.Count() > 0)
            {
                var pay = payments.FirstOrDefault();
                checkDueIn30Days = pay.CheckDueInThirtyDays;
            }
            return checkDueIn30Days;
        }
        private List<ThirtyDaysPayments> GetThirtyDaysPayments(int TenantId)
        {
            string DateFormat = _settings.DateFormat();
            List<ThirtyDaysPayments> thirtyDaysPayments = new List<ThirtyDaysPayments>();
            var paymentQuery = $@"SELECT tc.Contractno as ContractID, isnull(ChequeAmount,0) as Amount, Format(chq.ChequeDate, '{DateFormat}') as TransactionDate, chq.ChequeNo as TransactionNo, tt.TransactionType  FROM pms_cheques chq ,pms_transactions trans ,pms_contract tc, Pms_TransactionTypes tt  WHERE tc.suspend = 0 AND chq.suspend = 0 AND trans.suspend = 0 AND chq.transactionid = trans.transactionid AND chequestatusid IN (1,3,9)AND trans.ContractID = tc.contractid AND trans.transactiontypeid = 1 AND chq.ChequeDate between   CAST( GETDATE() AS Date ) AND  DATEADD(DD,30,( CAST( GETDATE() AS Date ))) AND STATUS = 'Confirmed' and TransactionType='RECEIPT' AND tc.Occupant = {TenantId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var payment = Conn.ExecuteDataTableSqlDA(conn, paymentQuery);
            if (payment.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ThirtyDaysPayments>(payment);
                thirtyDaysPayments = ListToBeSend;
            }

            return thirtyDaysPayments;
        }

        private List<PaymentBalanceVm> GetPaymentBalanceVm(int TenantId)
        {
            List<PaymentBalanceVm> paymentBalanceVm = new List<PaymentBalanceVm>();

            var paymentBalQuery = $@"SELECT  AmountToBeReceived,Received , IIF((AmountToBeReceived -Received)<0,0,(AmountToBeReceived -Received)) as Balance   from (
              SELECT
              (
                     a.Rentforstay +  isnull(a.VATAmount,0)+ isnull(a.VATCharges,0) + isnull((

                                  SELECT sum(PaymentAmount)

                                  FROM pms_transactions

                                  WHERE suspend = 0

                                         AND STATUS = 'Confirmed'

                                         AND ContractID = a.contractid

                                         AND transactiontypeid = 2

                            --            AND PaymentFor IN (1)

                                  ), 0)

                                   + isnull((

                                  SELECT sum(Amount)

                                  FROM Pms_ContractCharges

                                  WHERE suspend = 0 and ChargesTypeID not in (2)

                                         AND ContractID = a.contractid                         

                                         ), 0)

              ) AmountToBeReceived,

              (

                                  isnull((

                                  SELECT sum(PaymentAmount)

                                  FROM pms_transactions

                                  WHERE suspend = 0

                                         AND STATUS = 'Confirmed'

                                         AND ContractID = a.contractid

                                         AND transactiontypeid = 1

                            --            AND PaymentFor IN (1)

                                         AND paymentkind IN (1)

                                  ), 0) + isnull((

                                  SELECT sum(ChequeAmount)

                                  FROM pms_cheques sqta

                                         ,pms_transactions sqtb

                                  WHERE sqta.suspend = 0

                                         AND sqtb.suspend = 0

                                         AND sqta.transactionid = sqtb.transactionid

                                         AND chequestatusid IN (4)

                                         AND ContractID = a.contractid

                                         AND STATUS = 'Confirmed'

                                         AND transactiontypeid = 1

                                  --     AND PaymentFor IN (1)

                                  ), 0)
                     ) Received

                     FROM pms_contract a
                     WHERE a.suspend = 0 and a.ContractType in (1,8,4)
                     AND a.Occupant = {TenantId}
            ) as report --Paymant balance";


            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var payment = Conn.ExecuteDataTableSqlDA(conn, paymentBalQuery);

            if (payment.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<PaymentBalanceVm>(payment);
                paymentBalanceVm = ListToBeSend;
            }
            return paymentBalanceVm;

        }

        [HttpPost]
        [Route("GetStats")]
        public ActionResult<string> GetStats()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {

                    var dashboardVm = new DashboardVm();
                    var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                    int? UserId = _settings.GetUserIdByToken(_bearer_token);

                    var TenantId = GetTenantId(UserId.Value);
                    if (TenantId == 0)
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            dashboard = dashboardVm,
                            statusCode = "404",
                            Message = "Invalid User please try with valid user."
                        };
                        return NotFound(data);
                    }

                    List<PaymentBalanceVm> paymentBalanceVm = GetPaymentBalanceVm(TenantId);//looks ok
                    var fullName = GetFullName(TenantId);//looks ok
                    var fullNameAR = GetFullNameAR(TenantId); //looks ok
                    var propertyImage = GetPropertyImage(TenantId);//looks ok
                    var contractsExpiring = GetContractsExpiring(TenantId);
                    var thiryDayPayment = GetThiryDayPayment(TenantId);

                    dashboardVm.ContractsExpiring = contractsExpiring;
                    dashboardVm.FullName = fullName;
                    dashboardVm.FullNameAR = fullNameAR;
                    dashboardVm.PaymentBalanceChart = paymentBalanceVm;
                    dashboardVm.PropertyImage = propertyImage;
                    dashboardVm.ToBePaidIn30Days = thiryDayPayment;
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        var data = new
                        {
                            Status = "Ok",
                            dashboard = dashboardVm,
                            statusCode = "200",
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            dashboard = dashboardVm,
                            statusCode = "404",
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
        //Rental Values
        private decimal GetRentalValues(int TenantId)
        {
            decimal RentalValue = 0;
            var paymentQuery = $@"select sum(rentforstay) as RentalValues from Pms_Contract where Suspend=0 and ContractType in (1,4,8) and Occupant={TenantId} --Rental Value";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var payment = Conn.ExecuteDataTableSqlDA(conn, paymentQuery);
            List<PayVm> payments = new List<PayVm>();
            if (payment.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<PayVm>(payment);
                payments = ListToBeSend;
            }
            if (payments.Count() > 0)
            {
                var pay = payments.FirstOrDefault();
                RentalValue = pay.RentalValues;
            }
            return RentalValue;
        }
        private List<PaymentBalanceVm> GetRentOutstanding(int TenantId)
        {
            List<PaymentBalanceVm> paymentBalanceVm = new List<PaymentBalanceVm>();

            var paymentBalQuery = $@"SELECT  RentToBeReceived as AmountToBeReceived,Received as Received, IIF((RentToBeReceived -Received)<0,0,(RentToBeReceived -Received)) as RentOutstanding   from (
       SELECT
       (
              a.Rentforstay  + isnull((

                           SELECT sum(PaymentAmount)

                           FROM pms_transactions

                           WHERE suspend = 0

                                  AND STATUS = 'Confirmed'

                                  AND ContractID = a.contractid

                                  AND transactiontypeid = 2

                                  AND PaymentFor IN (1)

                           ), 0)                     

       ) RentToBeReceived,

       (

                           isnull((

                           SELECT sum(PaymentAmount)

                           FROM pms_transactions

                           WHERE suspend = 0

                                  AND STATUS = 'Confirmed'

                                  AND ContractID = a.contractid

                                  AND transactiontypeid = 1

                                  AND PaymentFor IN (1)

                                  AND paymentkind IN (1)

                           ), 0) + isnull((

                           SELECT sum(ChequeAmount)

                           FROM pms_cheques sqta

                                  ,pms_transactions sqtb

                           WHERE sqta.suspend = 0

                                  AND sqtb.suspend = 0

                                  AND sqta.transactionid = sqtb.transactionid

                                  AND chequestatusid IN (4)

                                  AND ContractID = a.contractid

                                  AND STATUS = 'Confirmed'

                                  AND transactiontypeid = 1

                                  AND PaymentFor IN (1)

                           ), 0)

              ) Received

              FROM pms_contract a

      

              WHERE a.suspend = 0 and a.ContractType in (1,8,4)

      

              AND a.Occupant = {TenantId}
			  ) as report --rentoutstanding";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var payment = Conn.ExecuteDataTableSqlDA(conn, paymentBalQuery);

            if (payment.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<PaymentBalanceVm>(payment);
                paymentBalanceVm = ListToBeSend;
            }
            return paymentBalanceVm;

        }
        private int GetContractsActive(int TenantId)
        {
            int contracts = 0;
            var contractQuery = $@"select count(*) as ActiveContracts from Pms_Contract where Suspend=0 and ContractType in (1,4,8) and Occupant={TenantId} -- active contracts";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var contract = Conn.ExecuteDataTableSqlDA(conn, contractQuery);
            List<ContActiveVm> conts = new List<ContActiveVm>();
            if (contract.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ContActiveVm>(contract);
                conts = ListToBeSend;
            }
            if (conts.Count() > 0)
            {
                var contExpiring = conts.FirstOrDefault();
                contracts = contExpiring.ActiveContracts;
            }
            return contracts;
        }

        [HttpPost]
        [Route("GetData")]
        public async Task<ActionResult<string>> GetData()
        {
            //try { 
            if (User.Identity.IsAuthenticated)
            {
                var dashboardVm = new TenantDashboardVm();
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId =  _settings.GetUserIdByToken(_bearer_token);
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
                    var data = new
                    {
                        Status = "NotFound",
                        dashboard = dashboardVm,
                        statusCode = "404",
                        Message = "Invalid User please try with valid user."
                    };
                    return NotFound(data);
                }
                var balanceReport = GetPaymentBalanceVm(TenantId);
                if (balanceReport != null && balanceReport.Count() > 0)
                {
                    decimal PaymentBalanceAmount = 0;
                    decimal received = 0;
                    foreach (var item in balanceReport)
                    {
                        PaymentBalanceAmount += item.Balance;

                        received += item.Received;
                    }
                    //assign payment balance
                    dashboardVm.PaymentBalance = PaymentBalanceAmount;//amount;
                                                                      //dashboardVm.Paid = received;

                }
                var RentOutstanding = GetRentOutstanding(TenantId);
                if (RentOutstanding != null && RentOutstanding.Count() > 0)
                {

                    decimal balance = 0;
                    foreach (var item in RentOutstanding)
                    {
                        balance += item.RentOutstanding;
                    }
                    dashboardVm.RentOutstanding = balance;
                    //dashboardVm.Paid = received;

                }
                var fullname = GetFullName(TenantId);
                var fullnameAR = GetFullNameAR(TenantId);
                if (!string.IsNullOrWhiteSpace(fullname))
                {
                    dashboardVm.TenantName = fullname;
                }
                else
                {
                    dashboardVm.TenantName = "Name";
                }
                if (!string.IsNullOrWhiteSpace(fullnameAR))
                {
                    dashboardVm.TenantNameAR = fullnameAR;
                }
                else
                {
                    dashboardVm.TenantNameAR = "Name";
                }
                var RentalValues = GetRentalValues(TenantId);
                if (RentalValues > 0)
                {
                    dashboardVm.RentalValues = RentalValues;
                }

                var checkDueIn30Days = GetCheckDueInThirtyDay(TenantId);
                if (checkDueIn30Days > 0)
                {
                    dashboardVm.CheckDueIn30Days = checkDueIn30Days;
                }
                var contractsIn30Days = GetContractsExpiring(TenantId);
                if (contractsIn30Days > 0)
                {
                    dashboardVm.ContractExpiringIn30Days = contractsIn30Days;
                }
                var ActiveContract = GetContractsActive(TenantId);
                if (ActiveContract > 0)
                {
                    dashboardVm.ActiveContracts = ActiveContract;
                }
                dashboardVm.TenantId = TenantId;
                if (dashboardVm != null)
                {
                    var data = new
                    {
                        Status = "Ok",
                        dashboard = dashboardVm,
                        statusCode = "200",
                        Message = "Data Found.",
                        UnreadNotifications = _tenantRepository.CountUnreadNotifications(UserId.Value),
                        ContractRenewalAction = _tenantRepository.ContractRenewalActions(TenantId),
                        NewContractAction = _tenantRepository.NewContractActions(TenantId)
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        dashboard = dashboardVm,
                        statusCode = "404",
                        Message = "No Data Found.",
                        UnreadNotifications = 0,
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

        [HttpPost]
        [Route("GetDataV1")]
        public async Task<ActionResult<string>> GetDataV1()
        {
            TenantDashboardVmV1 dashboardVm = new TenantDashboardVmV1();
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
                var TenantId = GetTenantId(UserId.Value);
                if (TenantId == 0)
                {
                    var data = new
                    {
                        Status = "NotFound",
                        dashboard = dashboardVm,
                        statusCode = "404",
                        Message = "Invalid User please try with valid user."
                    };
                    return NotFound(data);
                }
                var paymentdata = await _dashboardRepo.GetOccupantOutstanding(TenantId);
                dashboardVm.RentOutstanding = paymentdata.ActiveTcRentBalance;
                dashboardVm.PaymentBalance = paymentdata.TotalLiability;
                dashboardVm.RentalValues = paymentdata.ActiveTcRent;
                
                var result = _dashboardRepo.GetDataV1(TenantId.ToString(), UserId.ToString());
                
                dashboardVm.ActiveContracts = result.Result.ActiveContracts;
               // dashboardVm.RentalValues = result.Result.RentalValues;
                dashboardVm.ContractExpiringIn30Days = result.Result.ContractExpiringIn30Days;
                dashboardVm.CheckDueIn30Days = result.Result.CheckDueIn30Days;
                dashboardVm.UnreadNotificationCount = result.Result.UnreadNotificationCount;
                dashboardVm.ContractRenewalActionCount = result.Result.ContractRenewalActionCount;
                dashboardVm.TenantNameAR=result.Result.TenantNameAR;
                dashboardVm.TenantName=result.Result.TenantName;

                dashboardVm.TenantId = TenantId;
                if (dashboardVm != null)
                {
                    var data = new
                    {
                        Status = "Ok",
                        dashboard = dashboardVm,
                        statusCode = "200",
                        Message = "Data Found.",
                        UnreadNotifications = result.Result.UnreadNotificationCount,
                        ContractRenewalAction = result.Result.ContractRenewalActionCount,
                    };
                    return await Task.FromResult(Ok(data));
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        dashboard = dashboardVm,
                        statusCode = "404",
                        Message = "No Data Found.",
                        UnreadNotifications = 0,
                    };
                    return await Task.FromResult(NotFound(data));
                }
            }
            else
            {
                var data = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                };
                return await Task.FromResult(Unauthorized(data));
            }
        }


        [HttpPost]
        [Route("GetServiceRequests")]
        public ActionResult<string> GetServiceRequests()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var services = new List<CaseServiceVm>();
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
                            statusCode = "404",
                            Status = "NotFound",
                            data = services,
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
                        statusCode = "200",
                        message = services.Count() + " Services Found.",
                        status = "Ok"
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
        [HttpPost]
        [Route("GetUnits")]
        public ActionResult<string> GetUnits()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var units = new List<UnitVm>();
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
                            statusCode = "404",
                            data = units,
                            Message = "Invalid User please try with valid user."
                        };
                        return NotFound(dataInvalid);
                    }
                    var getUnits = Units(TenantId);
                    if (getUnits != null && getUnits.Count() > 0)
                    {
                        units = getUnits;
                    }

                    var data = new
                    {
                        data = units,
                        statusCode = "200",
                        message = units.Count() + " Services Found.",
                        status = "Ok"
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

        private List<UnitVm> Units(int TenantId)
        {
            string DateFormat = _settings.DateFormat();
            List<UnitVm> unitsData = new List<UnitVm>();
            var unitQuery = $@"Select c.ContractNo as ReferenceNo, p.PropertyName, p.PropertyNameAR, p.PropertyImage, Format(c.FromDate, '{DateFormat}') as StartDate, Format(c.ToDate, '{DateFormat}') as EndDate, (Select Top 1 u.UnitNo from Pms_Unit u Where u.PropertyID = c.PropertyID) as UnitNo  From Pms_Contract c LEFT OUTER JOIN Pms_Property p on p.PropertyID = c.PropertyID  Where c.TenantID = {TenantId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var units = Conn.ExecuteDataTableSqlDA(conn, unitQuery);

            if (units.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<UnitVm>(units);
                unitsData = ListToBeSend;
            }
            return unitsData;
        }
        [HttpPost]
        [Route("ToBePaidIn30Days")]
        public ActionResult<string> ToBePaidIn30Days()
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

                    var thirtyDaysPayments = new List<ThirtyDaysPayments>();
                    var newData = new ThirtyDaysPayments();
                    decimal totalAmount = 0;

                    var TenantId = GetTenantId(UserId.Value);
                    if (TenantId == 0)
                    {
                        newData.Amount = 20;
                        newData.ContractID = "";
                        newData.TransactionDate = "";
                        newData.TransactionNo = "";
                        newData.TransactionType = "";

                        thirtyDaysPayments.Add(newData);
                        var dataInvalid = new
                        {
                            Status = "NotFound",
                            statusCode = "404",
                            data = thirtyDaysPayments,
                            Message = "No Data.",
                            totalAmount = totalAmount
                        };
                        return NotFound(dataInvalid);
                    }
                    var getThirtyDaysPayemnts = GetThirtyDaysPayments(TenantId);
                    if (getThirtyDaysPayemnts != null && getThirtyDaysPayemnts.Count() > 0)
                    {
                        thirtyDaysPayments = getThirtyDaysPayemnts;
                        totalAmount = thirtyDaysPayments.Sum(p => p.Amount);
                        var data = new
                        {

                            data = thirtyDaysPayments,
                            statusCode = "200",
                            message = thirtyDaysPayments.Count() + " Services Found.",
                            status = "Ok",
                            totalAmount = totalAmount
                        };
                        return Ok(data);
                    }
                    else
                    {
                        newData.Amount = 10;
                        newData.ContractID = "";
                        newData.TransactionDate = "";
                        newData.TransactionNo = "";
                        newData.TransactionType = "";

                        thirtyDaysPayments.Add(newData);
                        var dataInvalid = new
                        {
                            Status = "NotFound",
                            statusCode = "404",
                            data = thirtyDaysPayments,
                            Message = "No Data.",
                            totalAmount = totalAmount
                        };
                        return NotFound(dataInvalid);
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

        [HttpPost]
        [Route("VInfo")]
        public ActionResult<string> Info()
        {
            string Message = "Welcome to Tenant Api";
            return Message;
        }

        [Route("GetImage")]
        [HttpPost]
        public ActionResult<string> GetImage()
        {
            string imageName = "1.jpg";
            if (imageName != null)
            {
                string MainDiectory = @"/mnt/azure/Propertyimages/";
                //string MainDiectory = @"C:\workspace\UBS\FAB_App_Develop\FABApiD2\Tenant\wwwroot\assets\images\";
                imageName = MainDiectory + imageName;

                byte[] imageByteArray = null;

                try
                {
                    var path = System.IO.Path.Combine(MainDiectory, imageName);
                    if (System.IO.File.Exists(path))
                    {
                        {
                            FileStream fileStream = new FileStream(imageName, FileMode.Open, FileAccess.Read);
                            using (BinaryReader reader = new BinaryReader(fileStream))
                            {
                                imageByteArray = new byte[reader.BaseStream.Length];
                                for (int i = 0; i < reader.BaseStream.Length; i++)
                                    imageByteArray[i] = reader.ReadByte();
                                var data = new
                                {
                                    Status = "Found",
                                    Bytes = imageByteArray,
                                };
                                return Ok(data);
                            }
                        }
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "Image Not found",

                        };
                        return NotFound(data);
                    }
                }
                catch (Exception ex)
                {
                    var data = new
                    {
                        Status = ex.Message,

                    };
                    return NotFound(data);
                }
            }
            else
            {
                return null;
            }
        }

        [HttpPost]
        [Route("GetDashboardNotifications")]
        public async Task<ActionResult<string>> GetDashboardNotifications()
        {
            try
            {
                List<DashboardNotificationVm> notificationVms = new List<DashboardNotificationVm>();
                if (User.Identity.IsAuthenticated)
                {
                    var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                    int? UserId =  _settings.GetUserIdByToken(_bearer_token);
                    if (UserId == null)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            Message = "InvalidSession."
                        };
                        return Unauthorized(data);
                    }
                    string DateFormat = _settings.DateFormat();

                    var query = $@"--dashboard notifications 
                            select act.CaseId,cont.Contractno,StageId, FORMAT(fromDate,'dd-MM-yyyy')FromDate,FORMAT(ToDate,'dd-MM-yyyy') ToDate,NotificationId,Title,TitleAr, Description,DescriptionAr, FORMAT(CreatedOn,'dd-MM-yyyy') as CreatedOn, RecordId, RecordType, act.DueActionid from app.Notifications noti
                            left join cms.ContractDueAction act on noti.RecordId=act.Contractid
                            left join Pms_Contract cont on cont.ContractID=act.Contractid
                            where Popup=1 and UserId={UserId} and UserType='Tenant'  and RecordType='Contract' and act.RenewalContractid is null";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var dashboardData = Conn.ExecuteDataTableSqlDA(conn, query);
                    if (dashboardData.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<DashboardNotificationVm>(dashboardData);
                        notificationVms = ListToBeSend;
                        notificationVms = notificationVms.DistinctBy(p => p.Contractno).ToList();
                    }
                    if (notificationVms != null && notificationVms.Count() > 0)
                    {

                        var data = new
                        {
                            statusCode = "200",
                            Status = "Ok",
                            Message = "Dashboard data found.",
                            notifications = notificationVms
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var dataError = new
                        {
                            statusCode = "404",
                            Status = "NotFound",
                            Message = "Record not found.",
                            notifications = notificationVms
                        };
                        return NotFound(dataError);
                    }
                }
                else
                {
                    var data = new
                    {
                        statusCode = "401",
                        Status = "Unauthorized",
                        Message = "Invalid Session.",
                        notifications = notificationVms
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
