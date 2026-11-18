using FABModels.Model;
using FABTenantApi.Classes;
using FABTenantApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Rotativa.AspNetCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using static FABTenantD2Api.Classes.ValidateQueryParam;
using System.Globalization;
using System.Data;

namespace FABTenantApi.Controllers
{
    [Route("api/Tenant/[controller]")]
    [ApiController]
    public class ContractsController : ControllerBase
    {


        private IConfiguration _config;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly FABAppDbContext _dbContext;
        private readonly Settings _settings;
        private readonly IHostingEnvironment _hostEnv;
        private IWebHostEnvironment _env;

        public ContractsController(IConfiguration config, ITokenService tokenService, ITenantRepository tenantRepository, FABAppDbContext dbContext, IHostingEnvironment hostingEnvironment,
            IWebHostEnvironment webHostEnvironment)
        {
            _config = config;
            _tenantRepository = tenantRepository;
            _tokenService = tokenService;
            _settings = new Settings();
            _dbContext = dbContext;
            _hostEnv = hostingEnvironment;
            _env = webHostEnvironment;
        }

        FABTenantD2Api.Classes.Decryption decrypt = new FABTenantD2Api.Classes.Decryption();

        private int GetTenantId(int UserId)
        {
            int TenantId = 0;
            TenantId = _tenantRepository.GetTenantId(UserId);
            return TenantId;
        }
        private PaymentVm GetPaymentData(int TenantId, string ContractId)
        {
            PaymentVm paymentBalanceVm = new PaymentVm();
            var contractId = Convert.ToInt32(ContractId);
            var paymentBalQuery = $@"SELECT IsNull(Sum( AmountToBeReceived), 0) Total, IsNull(Sum(  Received),0) AS Paid  from (

	SELECT 
	( 
		a.Rentforstay +  isnull(a.VATAmount,0)+ isnull(a.VATCharges,0) + isnull((
				SELECT sum(PaymentAmount)
				FROM pms_transactions
				WHERE suspend = 0
					AND STATUS = 'Confirmed'
					AND ContractID = a.contractid
					AND transactiontypeid = 2
			--		AND PaymentFor IN (1)
				), 0) 
				 + isnull((
				SELECT sum(Amount)
				FROM Pms_ContractCharges 
				WHERE suspend = 0
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
			--		AND PaymentFor IN (1)
					AND paymentkind IN (1)
				), 0) + isnull((
				SELECT sum(ChequeAmount)
				FROM pms_cheques sqta
					,pms_transactions sqtb
				WHERE sqta.suspend = 0
					AND sqtb.suspend = 0
					AND sqta.transactionid = sqtb.transactionid
					AND chequestatusid NOT IN (	5,6,7,11,12	)
					AND ContractID = a.contractid
					AND STATUS = 'Confirmed'
					AND transactiontypeid = 1
				--	AND PaymentFor IN (1)
				), 0)
		) Received
		FROM pms_contract a
	
		WHERE a.suspend = 0
	
		AND a.TenantID = 	{TenantId}
		AND a.ContractID = 	{contractId}
			) as report";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var payment = Conn.ExecuteDataTableSqlDA(conn, paymentBalQuery);

            if (payment.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<PaymentVm>(payment);
                paymentBalanceVm = ListToBeSend.FirstOrDefault();
            }
            return paymentBalanceVm;

        }


        [HttpPost]
        [Route("GetContracts")]
        public async Task<ActionResult<string>> GetContracts()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    string DateFormat = _settings.DateFormat();
                    List<ContractVm> ContractsData = new List<ContractVm>();
                    int TenantId = 0;
                    var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");

                    int? UserId = _settings.GetUserIdByToken(_bearer_token);
                    var logged1 = _tenantRepository.InsertErrorLog("User Id from JWT By getContracts in Tenant" + UserId);
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
                    var logged = _tenantRepository.InsertErrorLog("Tenant Id from JWT By getContracts in Tenant" + TenantId);

                    if (TenantId == 0)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            contracts = ContractsData,
                            Message = "Invalid User please try with valid user."
                        };
                        return Ok(data);
                    }
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);

                    SqlCommand cmd = new SqlCommand("FAB_PROC_CONTRACT_GET_CONTRACTS", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TenantId", TenantId);
                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    adp.Fill(ds);
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractVm>(ds.Tables[0]);
                        ContractsData = ListToBeSend;
                        var data = new
                        {
                            Status = "Ok",
                            Contracts = ContractsData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }


                    else
                    {
                        var logged11 = _tenantRepository.InsertErrorLog(" Data  not found By getContracts in Tenant");
                        var data = new
                        {
                            Status = "NotFound",
                            Contracts = ContractsData,
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
                    Message = ex.Message,

                };
                return BadRequest(data);
            }
        }

        [HttpPost]
        [Route("GetContractDetails")]
        public ActionResult<string> GetContractDetails([FromBody] GetOutstandingPayments requestModel)
        {
            //try
            //{

            int ContractId = int.Parse(requestModel.ContractId);
            if (User.Identity.IsAuthenticated)
            {
                string DateFormat = _settings.DateFormat();
                List<ContractVm> ContractsData = new List<ContractVm>();
                CaseExistVm caseExistVm = new CaseExistVm();
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
                        contract = new ContractVm(),
                        Message = "Invalid User please try with valid user."
                    };
                    return Ok(data);
                }

                //security api update start
                var contractTenantId = ContractInTenantId(ContractId, TenantId);

                if (!contractTenantId)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data);
                }
                //security api update end
                var UpdatePayment = _tenantRepository.UpdateChequePayments(ContractId);
                var ContractQuery = $@"SELECT c.ContractID,CAST(c.ToDate as datetime)ToDate,c.ContractPosition,pcp.ContractPosition as ContractPositionName,PreCon.Contractno as previousContactNo,

					(case when c.ToDate <GETDATE() then 1 else 0 end) as IsExpire,
					(case when CAST(DATEDIFF(day,30,c.ToDate) as datetime)<  getdate() then 1 else 0 end) as IsreadyToExpire

            , CONCAT((CASE 
		    When p.PropertyAddress = '' OR p.PropertyAddress IS NULL
		    THEN ''
		    ELSE p.PropertyAddress+ ', ' END),  pE.EmirateName) as address
			, CONCAT((CASE 
		    When p.PropertyAddressAR = '' OR p.PropertyAddressAR IS NULL
		    THEN ''
		    ELSE p.PropertyAddressAR+ ', ' END),  pE.EmirateNameAR) as addressAR,c.Contractno, Format(c.ContractDate, '{DateFormat}') as ContractDate, Format(c.fromDate, '{DateFormat}')  as ContractStartDate,  Format(c.ToDate, '{DateFormat}') as ContractEndDate ,IsNull(c.Rentforstay, 0)  as Rentforstay , IsNull(c.Period, 0) as NoOfDays, IsNull(c.GracePeriod, 0)  as GracePeriod, Format(c.GraceStartDate, '{DateFormat}') as GraceStartDate, Format(c.GraceEndDate, '{DateFormat}') as GraceEndDate, IsNull(c.NoOfContractYears,0) as NoOfContractYears, IsNull(c.Installments, 0) as Installments , c.Retention, IsNull(c.OtherCharges, 0) as OtherCharges, IsNull(c.VATCharges, 0) as VATCharges ,IsNull(c.VATAmount, 0) as  VATAmount, p.PropertyName,p.PropertyNameAR, p.PropertyImage, (select Top 1 ut.UnitType as UnitType from  Pms_ContractUnits pmu, Pms_Unit u, Pms_UnitType ut Where pmu.Suspend=0 AND u.Suspend=0 AND ut.Suspend=0 AND pmu.UnitID=u.UnitID AND u.UnitTypeID=ut.UnitTypeID AND pmu.ContractID = c.ContractID) as UnitType, (select Top 1 u.UnitNo as UnitType from  Pms_ContractUnits pmu, Pms_Unit u, Pms_UnitType ut
Where pmu.Suspend=0 AND u.Suspend=0 AND ut.Suspend=0 AND pmu.UnitID=u.UnitID AND u.UnitTypeID=ut.UnitTypeID AND pmu.ContractID = c.ContractID) as UnitNo, (SELECT IsNull(Sum( AmountToBeReceived), 0) Total from (SELECT  (  a.Rentforstay +  isnull(a.VATAmount,0)+ isnull(a.VATCharges,0) + isnull(( SELECT sum(PaymentAmount) FROM pms_transactions WHERE suspend = 0 AND STATUS = 'Confirmed' AND ContractID = a.contractid AND transactiontypeid = 2 ), 0)  + isnull(( SELECT sum(Amount) FROM Pms_ContractCharges  WHERE suspend = 0 AND ContractID = a.contractid	 ), 0)  ) AmountToBeReceived, ( isnull(( SELECT sum(PaymentAmount) FROM pms_transactions WHERE suspend = 0 AND STATUS = 'Confirmed' AND ContractID = a.contractid AND transactiontypeid = 1 AND paymentkind IN (1) ), 0) + isnull(( SELECT sum(ChequeAmount) FROM pms_cheques sqta ,pms_transactions sqtb WHERE sqta.suspend = 0 AND sqtb.suspend = 0 AND sqta.transactionid = sqtb.transactionid AND chequestatusid NOT IN (	1,3,4	) AND ContractID = a.contractid AND STATUS = 'Confirmed' AND transactiontypeid = 1 ), 0) ) Received FROM pms_contract a WHERE a.suspend = 0 AND a.TenantID = c.TenantID AND a.ContractID = c.ContractID) as report) Total, (SELECT IsNull(Sum(  Received),0) AS Paid  from (SELECT  (  a.Rentforstay +  isnull(a.VATAmount,0)+ isnull(a.VATCharges,0) + isnull(( SELECT sum(PaymentAmount) FROM pms_transactions WHERE suspend = 0 AND STATUS = 'Confirmed' AND ContractID = a.contractid AND transactiontypeid = 2 ), 0)  + isnull(( SELECT sum(Amount) FROM Pms_ContractCharges  WHERE suspend = 0 AND ContractID = a.contractid	 ), 0)  ) AmountToBeReceived, ( isnull(( SELECT sum(PaymentAmount) FROM pms_transactions WHERE suspend = 0 AND STATUS = 'Confirmed' AND ContractID = a.contractid AND transactiontypeid = 1 AND paymentkind IN (1) ), 0) + isnull(( SELECT sum(ChequeAmount) FROM pms_cheques sqta ,pms_transactions sqtb WHERE sqta.suspend = 0 AND sqtb.suspend = 0 AND sqta.transactionid = sqtb.transactionid AND chequestatusid NOT IN ( 5,6,7,11,12) AND ContractID = a.contractid AND STATUS = 'Confirmed' AND transactiontypeid = 1 ), 0) ) Received FROM pms_contract a WHERE a.suspend = 0 AND a.TenantID = c.TenantID AND a.ContractID = c.ContractID) as report) as Paid, 
            (case when c.ToDate < GETDATE() and c.ContractType=1  then 'Expired'  WHEN c.ContractType=1 THEN 'Active'  ELSE  ct.ContractType END)  as ContractStatus,
               (case when c.ToDate < GETDATE() and c.ContractType=1  then N'منتهي'  
                WHEN c.ContractType=1 THEN N'فعال'  ELSE  ct.ContractTypeAR END)  as ContractStatusAR,
            ct.ContractTypeID as ContractTypeID  from  Pms_Contract c Left Outer Join Pms_Property p On p.PropertyID = c.PropertyID Left Outer Join Pms_Emirate pE On p.EmirateID = pE.EmirateID Left Outer Join Pms_ContractType ct On ct.ContractTypeID = c.ContractType

			left join Pms_ContractPosition  pcp on c.ContractPosition=pcp.ContractPositionId
			left join Pms_Contract  PreCon on PreCon.PrevContractID=c.ContractID
			
			where c.Suspend=0  and c.ContractID = {ContractId}
					
			--and ct.ContractType != 'Draft' and ct.ContractType != 'Under Approval'  and ct.ContractType != 'Rejected'";

                /// var data = _dbContext.ExecuteQuery<ContractDetailVm>(ContractQuery);
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Contracts = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);

                if (Contracts.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ContractVm>(Contracts);
                    ContractsData = ListToBeSend;
                }
                int? CaseNo = 0;
                int? SignatureUploaded = 0;// when signature is not uploaded or contract not signed
                int? IsCaseCheckIn = 0;
                int? CheckInCaseNo = 0;
                int? LegalCaseNo = 0;



                var ImageQuery = $@"select AttachmentId as photoId,AttachmentUrl as PhotoSource, RoleTypeId,AttachmentType,con.CaseNo  from cms.CaseContractDetail con
                        join cms.Attachment att on att.UploadedCaseId=con.CaseNo
                        where  att.AttachmentType='Signature' and ContractId={ContractId} ";
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);
                RequestSignatureVM model = new RequestSignatureVM();
                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<RequestSignatureVM>(Image);
                    SignatureUploaded = 1;//when signature is  uploaded or contract is signed
                    model = ListToBeSend.FirstOrDefault();
                }
                string CaseType = "";
                //      var ContractExistinCase = $@"select cas.caseNo as CaseNo,csr.Description from cms.Cases cas
                //                              join cms.CaseContractDetail as cd on cd.CaseNo=cas.CaseNo
                //join cms.CaseServiceRequest as csr on csr.CaseId=cas.CaseNo
                //join cms.CaseSubCategory cuc on cuc.Id=csr.CaseSubCatagouryId
                //                              join cms.Customer cus on cus.CustomerId=cas.CaseOwnerId
                //                              where cas.StatusId not  in (20) and cuc.code in (411,412,421) and cd.ContractId={ContractId} and cus.UserId={UserId}";
                var CheckCancel = $@"select top 1  cas.CaseNo from cms.Cases cas
                                   join cms.ContractDueAction cda on cda.CaseId=cas.CaseNo
                                    where cas.StatusId in (20)  and COALESCE(cda.RenewalContractid,cda.ContractId)={ContractId}";
                var CancelOrNot = Conn.ExecuteDataTableSqlDA(conn, CheckCancel);
                int IsCanceled = 0;
                if (CancelOrNot.Rows.Count > 0)
                {
                    IsCanceled = 1;
                }
                var ContractExistinCase = $@"select cas.caseNo as CaseNo,csr.Description from cms.Cases cas
                                        join cms.CaseContractDetail as cd on cd.CaseNo=cas.CaseNo
										join cms.CaseServiceRequest as csr on csr.CaseId=cas.CaseNo
										join cms.CaseSubCategory cuc on cuc.Id=csr.CaseSubCatagouryId
                                        join cms.Customer cus on cus.CustomerId=cas.CaseOwnerId
                                        join cms.ContractDueAction cda on cda.CaseId=cas.CaseNo
                                        where cas.StatusId not in (20) and cuc.code in (411,412,421) and cda.RenewalContractid={ContractId} and cus.UserId={UserId}";
                var existOrNot = Conn.ExecuteDataTableSqlDA(conn, ContractExistinCase);
                if (existOrNot.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseExistVm>(existOrNot);
                    caseExistVm = ListToBeSend.FirstOrDefault();
                    CaseNo = caseExistVm.CaseNo;
                    CaseType = caseExistVm.Description;

                    //added on 20/01/2023 to check whether feedback added already

                    var CheckFeedbackExist = $@"
                                        SELECT CaseNo 
                                          FROM [CMS].[ServiceRequestFeedback]
                                          where   [CaseNo] = {CaseNo}";
                    var existOrNotFeedback = Conn.ExecuteDataTableSqlDA(conn, CheckFeedbackExist);
                    if (existOrNotFeedback.Rows.Count > 0)
                    {
                        CaseNo = 0;
                    }
                }
                /* commented by ifti on 20/01/2023 -- not required
                var CheckCheckInContractExist = $@"select cas.caseNo as CaseNo from cms.Cases cas
                                        join cms.CaseContractDetail as cd on cd.CaseNo=cas.CaseNo
										join cms.CaseServiceRequest as csr on csr.CaseId=cas.CaseNo
										join cms.CaseSubCategory cuc on cuc.Id=csr.CaseSubCatagouryId
                                        join cms.Customer cus on cus.CustomerId=cas.CaseOwnerId
                                        where cas.StatusId not in (20) and cuc.code in (451) and cd.ContractId={ContractId}";
                var existOrNotCheckInContract = Conn.ExecuteDataTableSqlDA(conn, CheckCheckInContractExist);
                if (existOrNotCheckInContract.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseExistVm>(existOrNotCheckInContract);
                    caseExistVm = ListToBeSend.FirstOrDefault();
                    CheckInCaseNo = caseExistVm.CaseNo;

                }

                var CheckLegalContractExist = $@"select cas.caseNo as CaseNo from cms.Cases cas
                                        join cms.CaseContractDetail as cd on cd.CaseNo=cas.CaseNo
										join cms.CaseServiceRequest as csr on csr.CaseId=cas.CaseNo
										join cms.CaseSubCategory cuc on cuc.Id=csr.CaseSubCatagouryId
                                        join cms.Customer cus on cus.CustomerId=cas.CaseOwnerId
                                        where cas.StatusId not  in (20) and cuc.code in (452) and cd.ContractId={ContractId}";
                var existOrNotLegalContract = Conn.ExecuteDataTableSqlDA(conn, CheckLegalContractExist);
                if (existOrNotLegalContract.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseExistVm>(existOrNotLegalContract);
                    caseExistVm = ListToBeSend.FirstOrDefault();
                    LegalCaseNo = caseExistVm.CaseNo;
                }
                */
                int PaymentDone = 0;
                var PaymentDoneQuery = $@"select sum(PaymentLeft) Amount from Pms_ContractPayments where  ContractID={ContractId} GROUP BY PaymentLeft having sum(PaymentLeft)>0";
                var IsPaymentDone = Conn.ExecuteDataTableSqlDA(conn, PaymentDoneQuery);
                if (IsPaymentDone.Rows.Count > 0)
                {
                    PaymentDone = 1; // if payment left return  1
                }


                ContractDetailVm contractVm = new ContractDetailVm();
                contractVm = ContractsData
                    .Select(p => new ContractDetailVm
                    {
                        IsExpire = p.IsExpire,
                        IsreadyToExpire = p.IsreadyToExpire,
                        ContractPosition = p.ContractPosition,
                        ContractPositionName = p.ContractPositionName,
                        ContractID = p.ContractID,
                        address = p.address,
                        addressAR = p.addressAR,
                        ContractDate = p.ContractDate,
                        ContractEndDate = p.ContractEndDate,
                        Contractno = p.Contractno,
                        ContractStartDate = p.ContractStartDate,
                        GracePeriod = p.GracePeriod,
                        Installments = p.Installments,
                        NoOfContractYears = p.NoOfContractYears,
                        NoOfDays = p.NoOfDays,
                        OtherCharges = p.OtherCharges,
                        Rentforstay = p.Rentforstay,
                        Retention = p.Retention,
                        VATAmount = p.VATAmount,
                        UnitName = p.PropertyName,
                        UnitNameAR = p.PropertyNameAR,
                        UnitPicture = p.PropertyImage,
                        UnitType = p.UnitType,
                        UnitTypeAR = p.UnitTypeAR,
                        VATCharges = p.VATCharges,
                        ContractStatus = p.ContractStatus,
                        ContractTypeID = p.ContractTypeID,
                        UnitNo = p.UnitNo,
                        ContractStatusAR = p.ContractStatusAR,
                        IsCanceled = IsCanceled,
                        payment = new PaymentVm
                        {
                            Paid = p.Paid,
                            Total = p.Total
                        }
                    }).FirstOrDefault();
                if (contractVm != null)
                {
                    if (contractVm.ContractTypeID == 1)
                    {
                        contractVm.IsExpire = 0;
                        contractVm.IsreadyToExpire = 0;
                    }
                    else
                    {
                        contractVm.IsExpire = 1;
                        contractVm.IsreadyToExpire = 1;
                    }

                    var data = new
                    {
                        Status = "Ok",
                        Contract = contractVm,
                        Message = "Data Found.",
                        CaseExist = CaseNo,
                        CanDownload = 1,
                        CanPayment = 1,
                        Signed = SignatureUploaded,
                        CanUploadPicture = 0,
                        SignedCaseNo = model.CaseNo,
                        CheckInCaseNo = CheckInCaseNo,
                        LegalCaseNo = LegalCaseNo,
                        CaseType = CaseType,
                        PaymentDone = PaymentDone,
                        CanCheckin = 1,  // contract postition =1 & checkin case doesnt exist or case status is not cancelled & contract renewal case doesnt exit,
                        CaseStageInfo = _tenantRepository.GetContractcaseIInfo(ContractId)
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "NotFound",
                        Contract = contractVm,
                        Message = "No Data Found.",
                        CaseExist = CaseNo,
                        CanDownload = 0,
                        CanPayment = 0,
                        Signed = 0,
                        CanUploadPicture = 0,
                        SignedCaseNo = model.CaseNo,
                        CheckInCaseNo = 0,
                        LegalCaseNo = 0,
                        CaseType = "",
                        PaymentDone = 0
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

        private bool ContractInTenantId(int contractId, int tenantId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Contracts = Conn.ExecuteDataTableSqlDA(conn, $"SELECT 1 FROM Pms_Contract WHERE (TenantID = {tenantId}) AND (ContractID = {contractId})");
            return Contracts.Rows.Count > 0;
        }

        private bool ContractInUserId(int contractId, int userId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Contracts = Conn.ExecuteDataTableSqlDA(conn, $@"SELECT TOP (1) 1
                            FROM            Pms_Contract INNER JOIN
                            Pms_Tenant ON Pms_Contract.TenantID = Pms_Tenant.TenantID INNER JOIN
                            App.UserRoles ON Pms_Tenant.TenantID = App.UserRoles.ProfileId
                            WHERE        (App.UserRoles.UserId = {userId}) AND (App.UserRoles.RoleId = 1) AND (Pms_Contract.ContractID = {contractId})");
            return Contracts.Rows.Count > 0;
        }

        [HttpPost]
        [Route("GetContractCharges")]
        [ValidateActionParameters]
        public ActionResult<string> GetContractCharges([FromBody] GetOutstandingPayments requestModel)
        {
            try
            {
                int ContractId = int.Parse(requestModel.ContractId);
                List<ContractChargesVm> contractchargeData = new List<ContractChargesVm>();
                PaidCharges paid = new PaidCharges();
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
                    string DateFormat = _settings.DateFormat();
                    var TenantId = 0;
                    TenantId = GetTenantId(UserId.Value);
                    if (TenantId == 0)
                    {
                        var data = new
                        {
                            Status = "Invalid",

                            Message = "Invalid User please try with valid user."
                        };
                        return Unauthorized(data);
                    }
                    //security api update start
                    var contractTenantId = ContractInTenantId(ContractId, TenantId);

                    if (!contractTenantId)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data);
                    }
                    //security api update end
                    var ContractQuery = $@"
                            --Contract Charges query
                            Select c.ContractchargeID as ChargeNo, c.ContractID, c.ChargeName,

                            Isnull(c.TenancyCodeNumber, 0) as TenancyCodeNumber, c.Amount,ISNULL(c.VATAmount,0) as VATAmount,

                            ISNULL(c.VATAmount,0)+ISNULL(c.Amount,0) as TotalAmount ,

                            ISNULL( Format(c.CreationDate, '{DateFormat}'),' ')  as CreatedOn, Isnull(ct.PaymentFor, '') as ChargesType,

                            c.ChargesTypeID  
                            From Pms_ContractCharges c
                            Left outer Join Pms_ReasonOfPayment ct ON ct.ReasonofPaymentID = c.ChargesTypeID
                            Left outer Join Pms_Contract pc ON pc.ContractID = c.ContractID 
                            Where c.ContractId = {ContractId} 
                            and pc.Occupant={TenantId} 
                            AND C.Suspend=0 
                            and isnull(c.Amount,0)>0 
                            and (ChargesTypeID * IsSDPaid <> 2 )

                            --and isnull(pc.IsSDPaid,0)=0

                            order by c.ContractchargeID desc";

                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Contracts = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);

                    //var PaidChargesQuery = $@"select isnull(sum(paid),0)as Paid from(SELECT isnull(sum( Pms_Transactions.paymentAmount), 0) AS Paid
                    //    FROM dbo.Pms_Transactions
                    //    INNER JOIN Pms_ContractCharges ON Pms_Transactions.PaymentFor = Pms_ContractCharges.ChargesTypeID
                    //    WHERE 
                    // Pms_Transactions.ContractID =  {ContractId}	AND
                    // TransactionTypeID = 1
                    // AND Pms_Transactions.Suspend = 0
                    // AND Pms_ContractCharges.Suspend = 0
                    // AND Pms_Transactions.STATUS = 'Confirmed'
                    // AND Pms_ContractCharges.contractid = pms_transactions.contractid
                    // AND PaymentKind = 1 --CASH
                    //    union 
                    //    ---RECEIVED OTHER THEN CASH
                    //    SELECT isnull(sum(Pms_Cheques.ChequeAmount), 0) AS Paid
                    //    FROM dbo.Pms_Transactions
                    //    INNER JOIN Pms_ContractCharges ON Pms_Transactions.PaymentFor = Pms_ContractCharges.ChargesTypeID
                    //    INNER JOIN Pms_Cheques ON Pms_Transactions.TransactionID = Pms_Cheques.TransactionID
                    //    WHERE 
                    // Pms_Transactions.ContractID = {ContractId} AND 
                    // TransactionTypeID = 1
                    // AND Pms_Transactions.Suspend = 0
                    // AND Pms_ContractCharges.Suspend = 0
                    // AND Pms_Cheques.Suspend=0
                    // AND Pms_Transactions.STATUS = 'Confirmed'
                    // AND Pms_ContractCharges.contractid = pms_transactions.contractid
                    // AND PaymentKind in (2,3,4,5,6 ) -- Other then CASH
                    // AND Pms_Cheques.ChequeStatusID IN (1,3,4) --Received, Deposited, Cleared
                    // ) Record";
                    // TransactionTypeID = 1 means receipts only 2 menas refunds
                    var PaidChargesQuery = $@"select isnull(sum(paid),0)as Paid from(SELECT isnull(sum( Pms_Transactions.paymentAmount), 0) AS Paid
                        FROM dbo.Pms_Transactions
                        INNER JOIN Pms_ContractCharges ON Pms_Transactions.PaymentFor = Pms_ContractCharges.ChargesTypeID
                        WHERE 
                           Pms_Transactions.ContractID = {ContractId}      AND
                           TransactionTypeID = 1
                           AND Pms_Transactions.Suspend = 0
                           AND Pms_ContractCharges.Suspend = 0
                           AND Pms_Transactions.STATUS = 'Confirmed'
                           AND Pms_ContractCharges.contractid = pms_transactions.contractid
                           AND PaymentKind = 1 --CASH

                           UNION ALL

                        ---RECEIVED OTHER THEN CASH
                        SELECT isnull(sum(Pms_Cheques.ChequeAmount), 0) AS Paid
                        FROM dbo.Pms_Transactions
                        INNER JOIN Pms_ContractCharges ON Pms_Transactions.PaymentFor = Pms_ContractCharges.ChargesTypeID
                        INNER JOIN Pms_Cheques ON Pms_Transactions.TransactionID = Pms_Cheques.TransactionID
                        WHERE 
                           Pms_Transactions.ContractID =  {ContractId} AND TransactionTypeID = 1
                           AND Pms_Transactions.Suspend = 0
                           AND Pms_ContractCharges.Suspend = 0
                           AND Pms_Cheques.Suspend=0
                           AND Pms_Transactions.STATUS = 'Confirmed'
                           AND Pms_ContractCharges.contractid = pms_transactions.contractid
                           AND PaymentKind in (2,3,4,5,6 ) -- Other then CASH
                           AND Pms_Cheques.ChequeStatusID IN (1,3,4) --Received, Deposited, Cleared

                                                UNION ALL  ---VAT ON CHARGES PAID

                                         SELECT isnull(sum( Pms_Transactions.paymentAmount), 0) AS Paid
                        FROM dbo.Pms_Transactions
                        WHERE 
                           Pms_Transactions.ContractID =   {ContractId}    AND TransactionTypeID = 1  
                           AND Pms_Transactions.Suspend = 0
                           AND Pms_Transactions.STATUS = 'Confirmed'
                                         AND Pms_Transactions.PaymentFor = 42 and Pms_Transactions.VATROPID <> 1
                           AND PaymentKind = 1 --CASH

                                         UNION ALL
                                         ---RECEIVED OTHER THEN CASH
                        SELECT isnull(sum(Pms_Cheques.ChequeAmount), 0) AS Paid
                        FROM dbo.Pms_Transactions
                        INNER JOIN Pms_Cheques ON Pms_Transactions.TransactionID = Pms_Cheques.TransactionID
                        WHERE 
                           Pms_Transactions.ContractID =  {ContractId} AND 
                           TransactionTypeID = 1
                           AND Pms_Transactions.Suspend = 0
                         
                           AND Pms_Cheques.Suspend=0
                           AND Pms_Transactions.STATUS = 'Confirmed'
                           AND PaymentKind in (2,3,4,5,6 ) -- Other then CASH
                           AND Pms_Cheques.ChequeStatusID IN (1,3,4) --Received, Deposited, Cleared
                                         AND Pms_Transactions.PaymentFor = 42 and Pms_Transactions.VATROPID <> 1

                           ) Record";
                    var GetPaidCharges = Conn.ExecuteDataTableSqlDA(conn, PaidChargesQuery);

                    if (GetPaidCharges.Rows.Count > 0)
                    {
                        var ListToBeSendPaidCharges = Conn.ToListof<PaidCharges>(GetPaidCharges);
                        paid = ListToBeSendPaidCharges.FirstOrDefault();
                    }


                    if (Contracts.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractChargesVm>(Contracts);
                        contractchargeData = ListToBeSend;
                    }

                    if (contractchargeData.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            StatusCode = "200",
                            ContractCharges = contractchargeData,
                            TotalCharges = contractchargeData.Sum(p => p.TotalAmount),
                            PaidCharges = paid.Paid,
                            OutstandingCharges = contractchargeData.Sum(p => p.TotalAmount) - paid.Paid,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            StatusCode = "404",
                            ContractCharges = contractchargeData,
                            TotalCharges = 0,
                            PaidCharges = 0,
                            OutstandingCharges = 0,
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }

                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session.",
                        TotalCharges = 0,
                        PaidCharges = 0,
                        OutstandingCharges = 0,
                        ContractCharges = contractchargeData,
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

        [HttpPost]
        [Route("GetContractUnits")]
        [ValidateActionParameters]
        public ActionResult<string> GetContractUnits([FromBody] GetOutstandingPayments requestModel)
        {
            try
            {
                List<TenantContractUnitVm> contractUnits = new List<TenantContractUnitVm>();

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
                    var TenantId = 0;
                    TenantId = GetTenantId(UserId.Value);


                    if (TenantId == 0)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            Message = "Invalid User please try with valid user."
                        };
                        return Ok(data);
                    }
                    //security api update start
                    var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantId);

                    if (!contractTenantId)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data);
                    }
                    //security api update end
                    contractUnits = ContractUnits(int.Parse(requestModel.ContractId), TenantId);
                    if (contractUnits.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            StatusCode = "200",
                            ContractUnits = contractUnits,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            StatusCode = "404",
                            ContractUnits = contractUnits,
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session.",
                        ContractUnits = contractUnits,
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

        private List<TenantContractUnitVm> ContractUnits(int ContractId, int TenantID)
        {
            List<TenantContractUnitVm> unitsData = new List<TenantContractUnitVm>();
            var unitQuery = $@"SELECT        Pms_ContractUnits.Amount, Pms_Unit.UnitName AS unitRefnumber, Pms_Property.PropertyName, Pms_Property.PropertyNameAR,

Pms_Property.PropertyImage, Pms_Unit.UnitRefNo, Pms_Unit.UnitID, Pms_PropertyType.PropertyType,Pms_PropertyType.PropertyTypeAR,

                         Pms_UnitType.UnitType, Pms_UnitType.UnitTypeAR

FROM            Pms_Property INNER JOIN

                         Pms_Unit ON Pms_Property.PropertyID = Pms_Unit.PropertyID INNER JOIN

                         Pms_PropertyType ON Pms_Property.PropertyTypeID = Pms_PropertyType.PropertyTypeID INNER JOIN

                         Pms_UnitType ON Pms_UnitType.UnitTypeID = Pms_Unit.UnitTypeID inner  JOIN

                         Pms_ContractUnits ON Pms_Unit.UnitID = Pms_ContractUnits.UnitID inner  JOIN

                         Pms_Contract ON Pms_Contract.ContractID = Pms_ContractUnits.ContractID WHERE        (Pms_ContractUnits.Suspend = 0) AND (Pms_Contract.Occupant = {TenantID})

AND (Pms_Contract.ContractID = {ContractId})";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var units = Conn.ExecuteDataTableSqlDA(conn, unitQuery);

            if (units.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<TenantContractUnitVm>(units);
                unitsData = ListToBeSend;
            }
            return unitsData;
        }
        [HttpPost]
        [Route("GetUnits")]
        [ValidateActionParameters]
        public ActionResult<string> GetUnits(int ContractId)
        {
            try
            {
                List<ContractUnitVm> contractUnits = new List<ContractUnitVm>();

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
                    var TenantId = 0;
                    TenantId = GetTenantId(UserId.Value);
                    //security api update start
                    var contractTenantId = ContractInTenantId(ContractId, TenantId);
                    if (!contractTenantId)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data);
                    }
                    //security api update end
                    contractUnits = GetUnitsByContract(ContractId, TenantId);
                    if (contractUnits.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            StatusCode = "200",
                            ContractUnits = contractUnits,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            StatusCode = "404",
                            ContractUnits = contractUnits,
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }

                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session.",
                        ContractUnits = contractUnits,
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

        private List<ContractUnitVm> GetUnitsByContract(int ContractId, int TenantID)
        {
            var data = new List<ContractUnitVm>();
            var query = $@"Select u.UnitID, p.PropertyImage, p.PropertyName, p.PropertyNameAR, u.UnitName, u.UnitNo, uc.UnitCategoryName as UnitCategory,uc.UnitCategoryNameAR as UnitCategoryAR, ut.UnitType,ut.UnitTypeAR, ll.LandlordName as Landlord,ll.LandlordNameAR as LandlordAR, uv.UnitViewName as UnitView,uv.UnitViewNameAR as UnitViewAR, IsNull(u.CurrentRent, 0) as CurrentRent, fm.FloorNo, Isnull(u.NoofRooms, 0) as BedRooms, Isnull(u.NoofBalconies,0) as Balconies, Isnull(u.NoofKitchens,0) AS Kitchens, Isnull(u.NoofLivingRooms,0) as LivingRooms, Isnull(u.NoofWashrooms, 0) as Washrooms, Isnull(u.MaidRooms, 0) as MaidRooms, Isnull(u.DriverRooms,0) as DriverRooms, u.ContractID From Pms_Unit u 
                left outer join Pms_Property p on p.PropertyID = u.PropertyID  
                Left Outer Join Pms_Contract c on c.ContractID = u.ContractID left outer join Pms_UnitCategory uc on uc.UnitCategoryID = u.UnitCategoryID left outer join Pms_UnitType ut on ut.UnitTypeID = u.UnitTypeID left outer join Pms_LandLord ll on ll.LandlordID = p.LandlordID left outer join Pms_UnitView uv on uv.UnitViewID = u.UnitViewID left outer join Pms_FloorMaster fm on fm.FloorID = u.Floor Where c.TenantID={TenantID} and u.ContractID = {ContractId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var units = Conn.ExecuteDataTableSqlDA(conn, query);
            if (units.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ContractUnitVm>(units);
                data = ListToBeSend;
            }
            return data;
        }


        [HttpPost]
        [Route("GetUnitDetails")]
        [ValidateActionParameters]
        public ActionResult<string> GetUnitDetails([FromBody] GetUnitDetails requestModel)
        {
            try
            {
                ContractUnitVm contractUnits = new ContractUnitVm();

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
                    var TenantId = 0;
                    TenantId = GetTenantId(UserId.Value);
                    contractUnits = GetUnitDetail(int.Parse(requestModel.UnitId), TenantId);
                    if (contractUnits != null)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            StatusCode = "200",
                            ContractUnit = contractUnits,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            StatusCode = "404",
                            ContractUnit = contractUnits,
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session.",
                        ContractUnit = contractUnits,
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
        private ContractUnitVm GetUnitDetail(int UnitId, int TenantId)
        {
            var data = new ContractUnitVm();
            // add wherefor tenant 
            //var query = $@"Select u.UnitID,ISNULL(u.MeasurementType,' ')as MeasurementType, ISNULL(u.AreaSizeSqm,0) as AreaSizeSqm,ISNULL(u.Areasize,'0.0') as Areasize , p.PropertyImage, p.PropertyName,p.PropertyNameAR, u.UnitName, u.UnitNo, uc.UnitCategoryName as UnitCategory,uc.UnitCategoryNameAR as UnitCategoryAR, ut.UnitType, ut.UnitTypeAR, ll.LandlordName as Landlord,ll.LandlordNameAR as LandlordAR, uv.UnitViewName as UnitView,'' as UnitViewAR, IsNull(u.CurrentRent, 0) as CurrentRent, fm.FloorNo, Isnull(u.BedRooms, 0) as BedRooms, Isnull(u.NoofBalconies,0) as Balconies, Isnull(u.NoofKitchens,0) AS Kitchens, Isnull(u.NoofLivingRooms,0) as LivingRooms, Isnull(u.NoofWashrooms, 0) as Washrooms, Isnull(u.MaidRooms, 0) as MaidRooms, Isnull(u.DriverRooms,0) as DriverRooms, u.ContractID From Pms_Unit u left outer join Pms_Property p on p.PropertyID = u.PropertyID Left Outer Join Pms_Contract c on c.ContractID = u.ContractID left outer join Pms_UnitCategory uc on uc.UnitCategoryID = u.UnitCategoryID left outer join Pms_UnitType ut on ut.UnitTypeID = u.UnitTypeID left outer join Pms_LandLord ll on ll.LandlordID = u.LandlordID left outer join Pms_UnitView uv on uv.UnitViewID = u.UnitViewID left outer join Pms_FloorMaster fm on fm.FloorID = u.Floor  Where u.UnitID = {UnitId}";
            var query = $@"Select u.UnitID,ISNULL(u.MeasurementType,' ')as MeasurementType, ISNULL(u.AreaSizeSqm,0) as AreaSizeSqm,ISNULL(u.Areasize,'0.0') as Areasize , p.PropertyImage, p.PropertyName,p.PropertyNameAR, u.UnitName, u.UnitNo, uc.UnitCategoryName as UnitCategory,uc.UnitCategoryNameAR as UnitCategoryAR, ut.UnitType, ut.UnitTypeAR, ll.LandlordName as Landlord,ll.LandlordNameAR as LandlordAR, uv.UnitViewName as UnitView,'' as UnitViewAR, IsNull(u.CurrentRent, 0) as CurrentRent, fm.FloorNo, Isnull(u.BedRooms, 0) as BedRooms, Isnull(u.NoofBalconies,0) as Balconies, Isnull(u.NoofKitchens,0) AS Kitchens, Isnull(u.NoofLivingRooms,0) as LivingRooms, Isnull(u.NoofWashrooms, 0) as Washrooms, Isnull(u.MaidRooms, 0) as MaidRooms, Isnull(u.DriverRooms,0) as DriverRooms, u.ContractID
            From Pms_Unit u inner join Pms_Property p on p.PropertyID = u.PropertyID inner join Pms_Contract c on c.ContractID = u.ContractID inner join Pms_UnitCategory uc on uc.UnitCategoryID = u.UnitCategoryID inner join Pms_UnitType ut on ut.UnitTypeID = u.UnitTypeID left outer join Pms_LandLord ll on ll.LandlordID = p.LandlordID left outer join Pms_UnitView uv on uv.UnitViewID = u.UnitViewID left outer join Pms_FloorMaster fm on fm.FloorID = u.Floor Where u.UnitID = {UnitId} and c.TenantID = {TenantId} ";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var units = Conn.ExecuteDataTableSqlDA(conn, query);
            if (units.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ContractUnitVm>(units);
                data = ListToBeSend.FirstOrDefault();
            }
            ContractUnitVm record = new ContractUnitVm
            {
                UnitID = data.UnitID,
                MeasurementType = data.MeasurementType,
                AreaSizeSqm = data.AreaSizeSqm,
                Areasize = data.Areasize,
                UnitCategoryAR = data.UnitCategoryAR,
                LandlordAR = data.LandlordAR,
                PropertyImage = data.PropertyImage,
                //PropertyImageInByte = ConvertImageToByteArray("1.jpg"),
                PropertyName = data.PropertyName,
                PropertyNameAR = data.PropertyNameAR,
                UnitName = data.UnitName,
                UnitNo = data.UnitNo,
                UnitCategory = data.UnitCategory,
                UnitType = data.UnitType,
                Landlord = data.Landlord,
                UnitView = data.UnitView,
                CurrentRent = data.CurrentRent,
                FloorNo = data.FloorNo,
                BedRooms = data.BedRooms,
                Balconies = data.Balconies,
                Kitchens = data.Kitchens,
                LivingRooms = data.LivingRooms,
                Washrooms = data.Washrooms,
                MaidRooms = data.MaidRooms,
                DriverRooms = data.DriverRooms,
                ContractID = data.ContractID,


            };

            return data;
        }

        [HttpPost]
        [Route("GetContractPayments")]

        public ActionResult<string> GetContractPayments([FromBody] GetOutstandingPayments requestModel)
        {
            try
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
                string DateFormat = _settings.DateFormat();
                List<TransactionVm> ChargesData = new List<TransactionVm>();
                if (User.Identity.IsAuthenticated)
                {
                    var TenantId = 0;
                    TenantId = GetTenantId(UserId.Value);
                    //security api update start
                    var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantId);

                    if (!contractTenantId)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data);
                    }
                    //security api update end
                    var ChargeQuery = $@"SELECT t.ContractID
	                ,t.TransactionId
	                ,Format(t.TransactionDate, 'dd-MM-yyyy') AS PaymentDate
	                ,p.PropertyName
	                ,p.PropertyNameAR
	                ,case when status.ContractType='Posted' then 'Active' else status.ContractType end Status
	                ,case when status.ContractType='Posted' then N'فعال' else status.ContractTypeAR end StatusAr
	                ,Isnull(p.BuildingNumber, '') AS PropertyNumber
	                ,IsNull(t.TransactionNo, '') AS ReceiptNo
	                ,IsNull(t.AccountNo, '') AS AccountNo
	                ,IsNull(t.AccountTitle, '') AS AccountTitle
	                ,IsNull(t.PaymentAmount, 0) AS Amount
	                ,Isnull(pk.PaymentKind, '') AS PaymentType
	                ,Isnull(pk.PaymentKindAR, '') AS PaymentTypeAR
	                ,Format(c.ContractDate, '{DateFormat}') AS ContractDate
	                ,isnull(tt.FirstName, '') + ' ' + isnull(tt.FullName, '') AS Tenant
	                ,isnull(tt.FirstNameAR, '') + ' ' + isnull(tt.FullNameAR, '') AS TenantAR
	                ,(
		                SELECT STRING_AGG([Pms_Unit].[UnitRefNo], ',')
		                FROM [Pms_ContractUnits]
			                ,[Pms_Unit]
		                WHERE [Pms_ContractUnits].[ContractID] = c.ContractID
			                AND [Pms_ContractUnits].[UnitID] = [Pms_Unit].[UnitID]
			                AND [Pms_ContractUnits].[Suspend] = 0
		                ) AS UnitNo
	                ,Isnull(c.Contractno, '') AS ContractNo
	                ,Isnull(land.LandlordName, '') AS LandLord
	                ,IsNull(e.EmirateName, '') AS City
	                ,IsNull(e.EmirateNameAR, '') AS CityAR
	                ,Isnull(pt.PaymentFor, '') AS PaymentFor
	                ,Isnull(pt.PaymentForAR, '') AS PaymentForAR
                FROM [dbo].[Pms_Transactions] AS t
                INNER JOIN Pms_Contract c ON c.ContractID = t.ContractID
                INNER JOIN Pms_ContractType status ON status.ContractTypeID = c.ContractType
                INNER JOIN Pms_Tenant tt ON tt.TenantID = c.Occupant
                INNER JOIN Pms_Property p ON p.PropertyID = t.PropertyID
                INNER JOIN Pms_LandLord land ON land.LandlordID = p.LandlordID
                INNER JOIN Pms_PaymentKinds pk ON pk.PaymentKindID = t.PaymentKind
                INNER JOIN Pms_ReasonOfPayment pt ON pt.ReasonofPaymentID = t.PaymentFor
                INNER JOIN Pms_Emirate e ON e.EmirateID = p.EmirateID Where c.ContractID  = {requestModel.ContractId} AND t.TransactionTypeId = 1 and t.Status='Confirmed'  AND c.Suspend = 0 and t.Suspend=0 and c.Occupant={TenantId} ORDER BY cast(t.TransactionDate as date) DESC";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery);

                    if (Charges.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<TransactionVm>(Charges);
                        ChargesData = ListToBeSend;
                    }
                    if (ChargesData.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            StatusCode = "200",
                            Payments = ChargesData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            StatusCode = "404",
                            Payments = ChargesData,
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }
                }


                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Payments = ChargesData,
                        Message = "Invalid Session."
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

        [HttpPost]
        [Route("GetContractCheques")]
        [ValidateActionParameters]
        public ActionResult<string> GetContractCheques([FromBody] GetContractCheques requestModel)
        {
            try
            {
                int TransactionID = int.Parse(requestModel.TransactionID);
                string DateFormat = _settings.DateFormat();
                List<TransactionCheque> Cheque = new List<TransactionCheque>();
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
                            Status = "Invalid",
                            Message = "InvalidSession."
                        };
                        return Unauthorized(data);
                    }

                    var ChequeQuery = $@"SELECT Pms_Cheques.TransactionID,Pms_Transactions.Suspend, Pms_Cheques.Suspend,Pms_Cheques.ChequeNo, Format(Pms_Cheques.ChequeDate, '{DateFormat}') as ChequeDate, Pms_BankNames.BankName,Pms_BankNames.BankNameAR, Pms_Cheques.ChequeAmount, Pms_ChequeStatus.ChequeStatus, Pms_ChequeStatus.ChequeStatusAR
                    FROM Pms_Transactions LEFT OUTER JOIN
                    Pms_Cheques ON Pms_Transactions.TransactionID = Pms_Cheques.TransactionID INNER JOIN
                    Pms_BankNames ON Pms_Cheques.BankNameID = Pms_BankNames.BankNameID INNER JOIN
                    Pms_ChequeStatus ON Pms_Cheques.ChequeStatusID = Pms_ChequeStatus.ChequeStatusID INNER JOIN
                    Pms_ReasonOfPayment ON Pms_Transactions.PaymentFor = Pms_ReasonOfPayment.ReasonofPaymentID INNER JOIN
                    Pms_PaymentKinds ON Pms_Transactions.PaymentKind = Pms_PaymentKinds.PaymentKindID
					INNER JOIN Pms_Contract ON Pms_Contract.ContractID = Pms_Transactions.ContractID
					where  
					Pms_Cheques.TransactionID={TransactionID} and 
                    Pms_Cheques.Suspend=0 
                    and Pms_Transactions.Suspend=0 
                    and ChequeStatus!='Replaced'
					and Pms_Contract.TenantID = {TenantId}
                    AND TransactionTypeID = 1
					ORDER BY Pms_Cheques.ChequeDate ";

                    //                    var ChequeQuery = $@"SELECT Pms_Cheques.TransactionID,Pms_Transactions.Suspend, Pms_Cheques.Suspend,Pms_Cheques.ChequeNo, Format(Pms_Cheques.ChequeDate, '{DateFormat}') as ChequeDate, Pms_BankNames.BankName,Pms_BankNames.BankNameAR, Pms_Cheques.ChequeAmount, Pms_ChequeStatus.ChequeStatus, Pms_ChequeStatus.ChequeStatusAR
                    //                FROM Pms_Transactions LEFT OUTER JOIN
                    //                Pms_Cheques ON Pms_Transactions.TransactionID = Pms_Cheques.TransactionID INNER JOIN
                    //                Pms_BankNames ON Pms_Cheques.BankNameID = Pms_BankNames.BankNameID INNER JOIN
                    //                Pms_ChequeStatus ON Pms_Cheques.ChequeStatusID = Pms_ChequeStatus.ChequeStatusID INNER JOIN
                    //                Pms_ReasonOfPayment ON Pms_Transactions.PaymentFor = Pms_ReasonOfPayment.ReasonofPaymentID INNER JOIN
                    //                Pms_PaymentKinds ON Pms_Transactions.PaymentKind = Pms_PaymentKinds.PaymentKindID 
                    //Inner Join Pms_Contract on Pms_Contract.ContractID = Pms_Transactions.ContractID
                    //where  Pms_Cheques.TransactionID={TransactionID} and 
                    //                Pms_Cheques.Suspend=0 and Pms_Transactions.Suspend=0 and ChequeStatus!='Replaced' and Pms_Contract.TenantID={TenantId}";

                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Contracts = Conn.ExecuteDataTableSqlDA(conn, ChequeQuery);

                    if (Contracts.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<TransactionCheque>(Contracts);
                        Cheque = ListToBeSend;
                    }

                    if (Cheque.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            StatusCode = "200",
                            TransactionCheque = Cheque,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            StatusCode = "404",
                            TransactionCheque = Cheque,
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }

                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session.",
                        TransactionCheque = Cheque,
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

        [HttpPost]
        [Route("GetPropertyTypes")]

        public ActionResult<string> GetPropertyTypes()
        {
            try
            {
                List<ContractTypesVm> PropertyTypes = new List<ContractTypesVm>();
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
                    var PropertyTypeQuery = $@"select PropertyType, PropertyTypeID, PropertyTypeAR from dbo.Pms_PropertyType where Suspend=0";

                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);

                    var PropertyType = Conn.ExecuteDataTableSqlDA(conn, PropertyTypeQuery);

                    if (PropertyType.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractTypesVm>(PropertyType);
                        PropertyTypes = ListToBeSend;
                    }

                    if (PropertyTypes.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            StatusCode = "200",
                            PropertyTypes = PropertyTypes,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            StatusCode = "404",
                            PropertyTypes = PropertyTypes,
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }

                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session.",
                        PropertyTypes = PropertyTypes,
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

        [HttpPost]
        [Route("GetContractStatus")]
        public ActionResult<string> GetContractStatus()
        {
            try
            {
                List<ContractStatusVm> ContractStatusResult = new List<ContractStatusVm>();
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
                    var ContractStatusQuery = $@"select  ContractTypeID, 
                                                (case when ContractType='Posted' then 'Active' else ContractType end) as ContractType,
                                                (case when ContractType='Posted' then N'فعال' else ContractTypeAR end) as  ContractTypeAR  from dbo.Pms_ContractType  
                                                where Suspend=0 and ContractTypeID in (1,3,4,6,7,8,10)";

                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);

                    var ContractStatus = Conn.ExecuteDataTableSqlDA(conn, ContractStatusQuery);

                    if (ContractStatus.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractStatusVm>(ContractStatus);
                        ContractStatusResult = ListToBeSend;
                    }

                    if (ContractStatusResult.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            StatusCode = "200",
                            ContractStatus = ContractStatusResult,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            StatusCode = "404",
                            ContractStatus = ContractStatusResult,
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session.",
                        ContractStatus = ContractStatusResult,
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
                    Message = ex.Message,// "Internal Server Error",
                };
                return BadRequest(data);
            }
        }

        [HttpPost]
        [Route("GetContractswithFilter")]
        //[ValidateActionParameters]
        public ActionResult<string> GetContractswithFilter([FromBody] GetConTractSwithFilter requestBody)
        {
            try
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
                if (FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(requestBody.PropertyName, out _))
                {
                    var dataBadReq = new
                    {
                        Status = "BadRequest",
                        Message = "Invalid Request.",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
                string DateFormat = _settings.DateFormat();
                List<ContractVm> ContractsData = new List<ContractVm>();
                var TenantId = 0;
                TenantId = GetTenantId(UserId.Value);
                if (TenantId == 0)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        contracts = ContractsData,
                        Message = "Invalid User please try with valid user."
                    };
                    return Ok(data);
                }

                if (User.Identity.IsAuthenticated)
                {
                    string WhereClause = "";
                    string Params = "";

                    //security api update start
                    var propertyNameParam = new SqlParameter("@PropertyName", DBNull.Value);

                    if (requestBody.PropertyName != null && requestBody.PropertyName != "")
                    {
                        WhereClause += $@" and p.PropertyName like '%'+ @PropertyName +'%'";
                        propertyNameParam = new SqlParameter("@PropertyName", requestBody.PropertyName);
                    }
                    //if (PropertyName != null && PropertyName != "")
                    //{
                    //    Params = $@" , @PropertyName nvarchar(500) = '{PropertyName}'";
                    //    WhereClause += $@" and p.PropertyName like '%'+ @PropertyName +'%'";
                    //}
                    //security api update end

                    if (int.Parse(requestBody.PropertyTypeId) != null && int.Parse(requestBody.PropertyTypeId) != -1)
                    {
                        Params += $@" , @PropertyTypeId int={int.Parse(requestBody.PropertyTypeId)}";
                        WhereClause += $@" and p.PropertyTypeID=@PropertyTypeId";
                    }

                    if (int.Parse(requestBody.PropertyStausId) != null && int.Parse(requestBody.PropertyStausId) != -1)
                    {
                        Params += $@" , @ContractTypeID int={int.Parse(requestBody.PropertyStausId)}";
                        WhereClause += $@" and ct.ContractTypeID=@ContractTypeID";
                    }

                    var ContractQuery = $@"DECLARE @ContractDate NVARCHAR(100) = '{DateFormat}' {Params}

                  SELECT c.ContractID
	,c.Contractno
	,Format(ContractDate, '{DateFormat}') AS ContractDate
    , Format(fromDate, '{DateFormat}') AS ContractStartDate
    , Format(ToDate, '{DateFormat}') AS ContractEndDate
    , IsNull(Rentforstay, 0) AS Rentforstay
    , IsNull(Period, 0) AS NoOfDays
    , IsNull(GracePeriod, 0) AS GracePeriod
    , Format(GraceStartDate, '{DateFormat}') AS GraceStartDate
    , Format(GraceEndDate, '{DateFormat}') AS GraceEndDate
    , IsNull(NoOfContractYears, 0) AS NoOfContractYears
    , IsNull(Installments, 0) AS Installments
    , Retention
    , IsNull(OtherCharges, 0) AS OtherCharges
    , IsNull(VATCharges, 0) AS VATCharges
    , IsNull(VATAmount, 0) AS VATAmount
    , p.PropertyName
	,p.PropertyNameAR
	,p.PropertyImage
	,(
        SELECT TOP 1 ut.UnitType AS UnitType
        FROM Pms_ContractUnits pmu
            , Pms_Unit u
			,Pms_UnitType ut

        WHERE pmu.Suspend = 0

            AND u.Suspend = 0

            AND ut.Suspend = 0

            AND pmu.UnitID = u.UnitID

            AND u.UnitTypeID = ut.UnitTypeID

            AND pmu.ContractID = c.ContractID
		) AS UnitType
    ,(
		SELECT TOP 1 ut.UnitTypeAR AS UnitTypeAR

        FROM Pms_ContractUnits pmu
            , Pms_Unit u
            , Pms_UnitType ut

        WHERE pmu.Suspend = 0

            AND u.Suspend = 0

            AND ut.Suspend = 0

            AND pmu.UnitID = u.UnitID

            AND u.UnitTypeID = ut.UnitTypeID

            AND pmu.ContractID = c.ContractID
		) AS UnitTypeAR
    ,(
		SELECT TOP 1 u.UnitNo AS UnitNo

        FROM Pms_ContractUnits pmu
            , Pms_Unit u
            , Pms_UnitType ut

        WHERE pmu.Suspend = 0

            AND u.Suspend = 0

            AND ut.Suspend = 0

            AND pmu.UnitID = u.UnitID

            AND u.UnitTypeID = ut.UnitTypeID

            AND pmu.ContractID = c.ContractID
		) AS UnitNo

            ,(
		SELECT STRING_AGG(u.UnitRefNo, ',') AS UnitRefNo

        FROM Pms_ContractUnits pmu
            , Pms_Unit u
            , Pms_UnitType ut

        WHERE pmu.Suspend = 0

            AND u.Suspend = 0

            AND ut.Suspend = 0

            AND pmu.UnitID = u.UnitID

            AND u.UnitTypeID = ut.UnitTypeID

            AND pmu.ContractID = c.ContractID
		) AS UnitRefNo
  --  ,(
		--SELECT IsNull(Sum(AmountToBeReceived), 0) Total

  --      FROM(
  --          SELECT(
  --                  a.Rentforstay + isnull(a.VATAmount, 0) + isnull(a.VATCharges, 0) + isnull((
  --                          SELECT sum(PaymentAmount)

  --                          FROM pms_transactions

  --                          WHERE suspend = 0

  --                              AND STATUS = 'Confirmed'

  --                              AND ContractID = a.contractid

  --                              AND transactiontypeid = 2
  --                          ), 0) + isnull((
  --                          SELECT sum(Amount)

  --                          FROM Pms_ContractCharges

  --                          WHERE suspend = 0

  --                              AND ContractID = a.contractid
  --                          ), 0)
		--			) AmountToBeReceived
		--		,(
  --                  isnull((
  --                          SELECT sum(PaymentAmount)

  --                          FROM pms_transactions

  --                          WHERE suspend = 0

  --                              AND STATUS = 'Confirmed'

  --                              AND ContractID = a.contractid

  --                              AND transactiontypeid = 1

  --                              AND paymentkind IN(1)
  --                          ), 0) + isnull((
  --                          SELECT sum(ChequeAmount)

  --                          FROM pms_cheques sqta
  --                              , pms_transactions sqtb

  --                          WHERE sqta.suspend = 0

  --                              AND sqtb.suspend = 0

  --                              AND sqta.transactionid = sqtb.transactionid

  --                              AND chequestatusid NOT IN(
  --                                  5
  --                                  , 6
  --                                  , 7
  --                                  , 11
  --                                  , 12
  --                                  )

  --                              AND ContractID = a.contractid

  --                              AND STATUS = 'Confirmed'

  --                              AND transactiontypeid = 1
  --                          ), 0)
		--			) Received
  --          FROM pms_contract a

  --          WHERE a.suspend = 0

  --              AND a.TenantID = c.TenantID

  --              AND a.ContractID = c.ContractID
		--	) AS report
		--) Total
	--,(
 --       SELECT IsNull(Sum(Received), 0) AS Paid

 --       FROM(
 --           SELECT(
 --                   a.Rentforstay + isnull(a.VATAmount, 0) + isnull(a.VATCharges, 0) + isnull((
 --                           SELECT sum(PaymentAmount)

 --                           FROM pms_transactions

 --                           WHERE suspend = 0

 --                               AND STATUS = 'Confirmed'

 --                               AND ContractID = a.contractid

 --                               AND transactiontypeid = 2
 --                           ), 0) + isnull((
 --                           SELECT sum(Amount)

 --                           FROM Pms_ContractCharges

 --                           WHERE suspend = 0

 --                               AND ContractID = a.contractid
 --                           ), 0)
	--				) AmountToBeReceived
	--			,(
 --                   isnull((
 --                           SELECT sum(PaymentAmount)

 --                           FROM pms_transactions

 --                           WHERE suspend = 0

 --                               AND STATUS = 'Confirmed'

 --                               AND ContractID = a.contractid

 --                               AND transactiontypeid = 1

 --                               AND paymentkind IN(1)
 --                           ), 0) + isnull((
 --                           SELECT sum(ChequeAmount)

 --                           FROM pms_cheques sqta
 --                               , pms_transactions sqtb

 --                           WHERE sqta.suspend = 0

 --                               AND sqtb.suspend = 0

 --                               AND sqta.transactionid = sqtb.transactionid

 --                               AND chequestatusid NOT IN(
 --                                   5
 --                                   , 6
 --                                   , 7
 --                                   , 11
 --                                   , 12
 --                                   )

 --                               AND ContractID = a.contractid

 --                               AND STATUS = 'Confirmed'

 --                               AND transactiontypeid = 1
 --                           ), 0)
	--				) Received
 --           FROM pms_contract a

 --           WHERE a.suspend = 0

 --               AND a.TenantID = c.TenantID

 --               AND a.ContractID = c.ContractID
 --		) AS report
 --	) AS Paid
    , CASE

        WHEN c.ToDate < GETDATE()

            AND ct.ContractTypeID = 1

            THEN 'Expired'

        WHEN ct.ContractTypeID = 1

            THEN 'Active'

        ELSE ct.ContractType
        END AS ContractStatus
 , CASE

        WHEN c.ToDate < GETDATE()

            AND ct.ContractTypeID = 1

            THEN N'منتهي'

        WHEN ct.ContractTypeID = 1

            THEN N'فعال'

        ELSE ct.ContractTypeAR
        END AS ContractStatusAR

FROM Pms_Contract c
LEFT OUTER JOIN Pms_Property p ON p.PropertyID = c.PropertyID
LEFT OUTER JOIN Pms_ContractType ct ON ct.ContractTypeID = c.ContractType
WHERE c.Suspend = 0    {WhereClause}";

                    /*
                     * var ContractQuery = $@"Declare @ContractDate nvarchar(100) =  '{DateFormat}' {Params} SELECT c.ContractID, c.Contractno, Format(ContractDate, '{DateFormat}') as ContractDate, Format(fromDate, '{DateFormat}')  as ContractStartDate,  Format(ToDate, '{DateFormat}') as ContractEndDate ,IsNull(Rentforstay, 0)  as Rentforstay , IsNull(Period, 0) as NoOfDays, IsNull(GracePeriod, 0)  as GracePeriod, Format(GraceStartDate, '{DateFormat}') as GraceStartDate, Format(GraceEndDate, '{DateFormat}') as GraceEndDate, IsNull(NoOfContractYears,0) as NoOfContractYears, IsNull(Installments, 0) as Installments , Retention, IsNull(OtherCharges, 0) as OtherCharges, IsNull(VATCharges, 0) as VATCharges ,IsNull(VATAmount, 0) as  VATAmount, p.PropertyName, p.PropertyNameAR, p.PropertyImage, (select Top 1 ut.UnitType as UnitType from  Pms_ContractUnits pmu, Pms_Unit u, Pms_UnitType ut Where pmu.Suspend=0 AND u.Suspend=0 AND ut.Suspend=0 AND pmu.UnitID=u.UnitID AND u.UnitTypeID=ut.UnitTypeID AND pmu.ContractID = c.ContractID) as UnitType,(select Top 1 ut.UnitTypeAR as UnitTypeAR from  Pms_ContractUnits pmu, Pms_Unit u, Pms_UnitType ut Where pmu.Suspend=0 AND u.Suspend=0 AND ut.Suspend=0 AND pmu.UnitID=u.UnitID AND u.UnitTypeID=ut.UnitTypeID AND pmu.ContractID = c.ContractID) as UnitTypeAR, (select Top 1 u.UnitNo as UnitType from  Pms_ContractUnits pmu, Pms_Unit u, Pms_UnitType ut Where pmu.Suspend=0 AND u.Suspend=0 AND ut.Suspend=0 AND pmu.UnitID=u.UnitID AND u.UnitTypeID=ut.UnitTypeID AND pmu.ContractID = c.ContractID) as UnitNo, (SELECT IsNull(Sum( AmountToBeReceived), 0) Total from (SELECT  (  a.Rentforstay +  isnull(a.VATAmount,0)+ isnull(a.VATCharges,0) + isnull(( SELECT sum(PaymentAmount) FROM pms_transactions WHERE suspend = 0 AND STATUS = 'Confirmed' AND ContractID = a.contractid AND transactiontypeid = 2 ), 0)  + isnull(( SELECT sum(Amount) FROM Pms_ContractCharges  WHERE suspend = 0 AND ContractID = a.contractid	 ), 0)  ) AmountToBeReceived, ( isnull(( SELECT sum(PaymentAmount) FROM pms_transactions WHERE suspend = 0 AND STATUS = 'Confirmed' AND ContractID = a.contractid AND transactiontypeid = 1 AND paymentkind IN (1) ), 0) + isnull(( SELECT sum(ChequeAmount) FROM pms_cheques sqta ,pms_transactions sqtb WHERE sqta.suspend = 0 AND sqtb.suspend = 0 AND sqta.transactionid = sqtb.transactionid AND chequestatusid NOT IN (	5,6,7,11,12	) AND ContractID = a.contractid AND STATUS = 'Confirmed' AND transactiontypeid = 1 ), 0) ) Received FROM pms_contract a WHERE a.suspend = 0 AND a.TenantID = c.TenantID AND a.ContractID = c.ContractID) as report) Total, (SELECT IsNull(Sum(  Received),0) AS Paid  from (SELECT  (  a.Rentforstay +  isnull(a.VATAmount,0)+ isnull(a.VATCharges,0) + isnull(( SELECT sum(PaymentAmount) FROM pms_transactions WHERE suspend = 0 AND STATUS = 'Confirmed' AND ContractID = a.contractid AND transactiontypeid = 2 ), 0)  + isnull(( SELECT sum(Amount) FROM Pms_ContractCharges  WHERE suspend = 0 AND ContractID = a.contractid	 ), 0)  ) AmountToBeReceived, ( isnull(( SELECT sum(PaymentAmount) FROM pms_transactions WHERE suspend = 0 AND STATUS = 'Confirmed' AND ContractID = a.contractid AND transactiontypeid = 1 AND paymentkind IN (1) ), 0) + isnull(( SELECT sum(ChequeAmount) FROM pms_cheques sqta ,pms_transactions sqtb WHERE sqta.suspend = 0 AND sqtb.suspend = 0 AND sqta.transactionid = sqtb.transactionid AND chequestatusid NOT IN (	5,6,7,11,12	) AND ContractID = a.contractid AND STATUS = 'Confirmed' AND transactiontypeid = 1 ), 0) ) Received FROM pms_contract a WHERE a.suspend = 0 AND a.TenantID = c.TenantID AND a.ContractID = c.ContractID) as report) as Paid, case when c.ToDate < GETDATE() and ct.ContractTypeID=1  then 'Expired'  WHEN ct.ContractTypeID=1 THEN 'Active'  ELSE  ct.ContractType END as ContractStatus from  Pms_Contract c Left Outer Join Pms_Property p On p.PropertyID = c.PropertyID Left Outer Join Pms_ContractType ct On ct.ContractTypeID = c.ContractType  where c.Suspend=0  {WhereClause}";
                   */
                    DateTime? ContractDateFrom = null;
                    DateTime? ContractDateTo = null;

                    string pattern = "MM-dd-yyyy";
                    CultureInfo provider = CultureInfo.InvariantCulture;
                    if (!string.IsNullOrWhiteSpace(requestBody.ContractDateTo))
                    {
                        ContractDateTo = DateTime.ParseExact(requestBody.ContractDateTo, pattern, provider);
                    }
                    if (!string.IsNullOrWhiteSpace(requestBody.ContractDateFrom))
                    {
                        ContractDateFrom = DateTime.ParseExact(requestBody.ContractDateFrom, pattern, provider);
                    }

                    //if (DateTime.TryParseExact(requestBody.ContractDateFrom, pattern, null,
                    //                 DateTimeStyles.None, out ContractDateFrom)) ;
                    //if (DateTime.TryParseExact(requestBody.ContractDateTo, pattern, null,
                    //                 DateTimeStyles.None, out ContractDateTo)) ;


                    // if (requestBody.ContractDateFrom != null && requestBody.ContractDateTo != null)

                    if (ContractDateFrom != null && ContractDateTo != null)
                    {
                        ContractQuery += $@" and  CAST(ContractDate AS DATE) >= @ContractDateFrom and   CAST(ContractDate AS DATE) <= @ContractDateTo";
                    }
                    ContractQuery += $@" and c.Occupant =  {TenantId} and ct.ContractTypeID in (1,3,4,6,7,8,10) ORDER BY CAST(contractno AS int) DESC";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);

                    //27-Aug-2022
                    var ContractDateFromParam = new SqlParameter("@ContractDateFrom", ContractDateFrom); //requestBody.ContractDateFrom);//add paramter
                    if (ContractDateFrom == null)
                    {
                        ContractDateFromParam = new SqlParameter("@ContractDateFrom", DBNull.Value);
                    }
                    var ContractDateToParam = new SqlParameter("@ContractDateTo", ContractDateTo);//requestBody.ContractDateTo);//add paramter
                    if (ContractDateTo == null)
                    {
                        ContractDateToParam = new SqlParameter("@ContractDateTo", DBNull.Value);
                    }
                    var Contracts = Conn.ExecuteDataTableSqlDA(conn, ContractQuery, ContractDateFromParam, ContractDateToParam, propertyNameParam);

                    if (Contracts.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractVm>(Contracts);
                        ContractsData = ListToBeSend;
                    }

                    if (ContractsData.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            Contracts = ContractsData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            Contracts = ContractsData,
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
                    //Message = "Internal Server Error",
                    Message = ex.Message + " " + ex.InnerException.Message,

                };
                return BadRequest(data);
            }
        }

        [HttpPost]
        [Route("GetUnitAditionalDetails")]
        //[ValidateActionParameters]
        public ActionResult<string> GetUnitAditionalDetails([FromBody] GetUnitDetails requestModel)
        {
            int UnitID = int.Parse(requestModel.UnitId);
            try
            {
                List<UnitAdditionalInfo> AdditionInfosData = new List<UnitAdditionalInfo>();
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
                            Status = "Invalid",
                            Message = "InvalidSession."
                        };
                        return Unauthorized(data);
                    }
                    var AdditionInfoQuery = $@"select PmsFac.FacilityDescription,PmsFac.FacilityDescriptionAR from Pms_UnitFacilitiy as Unitfac join pms_facilities as PmsFac on Unitfac.FacilityID=PmsFac.FacilityID join Pms_Unit u on u.UnitID = Unitfac.UnitID inner join Pms_Contract c on c.ContractID = u.ContractID where Unitfac.Suspend=0 and PmsFac.Suspend=0 and c.TenantID = {TenantId} and Unitfac.UnitID={UnitID}";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var AdditionInfos = Conn.ExecuteDataTableSqlDA(conn, AdditionInfoQuery);

                    if (AdditionInfos.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<UnitAdditionalInfo>(AdditionInfos);
                        AdditionInfosData = ListToBeSend;
                    }
                    if (AdditionInfosData.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            StatusCode = "200",
                            AdditionalInfo = AdditionInfosData,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            StatusCode = "404",
                            AdditionalInfo = AdditionInfosData,
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }
                }


                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        AdditionalInfo = AdditionInfosData,
                        Message = "Invalid Session."
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


        [HttpPost]
        [Route("ContractChargeReceipts")]
        [ValidateActionParameters]
        public ActionResult<string> ContractChargeReceipts([FromBody] ConractChargeRecepts requestModel)
        {
            try
            {
                string DateFormat = _settings.DateFormat();
                List<ContractChargeReceipts> Receipts = new List<ContractChargeReceipts>();
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
                    var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantId);

                    if (!contractTenantId)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data);
                    }
                    //                   var ReceiptQuery = $@"SELECT	Pms_Transactions.TransactionNo AS ReceiptNo, 
                    //	Format(Pms_Transactions.TransactionDate, '{DateFormat}')  AS TransactionDate,
                    //	Pms_PaymentKinds.PaymentKind AS PaymentType,
                    //	Pms_Transactions.paymentAmount AS PaymentAmount
                    //       FROM dbo.Pms_Transactions
                    //       INNER JOIN Pms_ContractCharges ON Pms_Transactions.PaymentFor = Pms_ContractCharges.ChargesTypeID
                    //       INNER JOIN Pms_PaymentKinds ON Pms_PaymentKinds.PaymentKindID = Pms_Transactions.PaymentKind
                    //       WHERE 
                    //    Pms_Transactions.ContractID =  {int.Parse(requestModel.ContractId)} AND 
                    //    TransactionTypeID = 1 AND Pms_Transactions.Suspend = 0
                    //    AND Pms_ContractCharges.Suspend = 0
                    //    AND Pms_PaymentKinds.Suspend = 0
                    //    AND Pms_Transactions.STATUS = 'Confirmed'
                    //    AND Pms_ContractCharges.contractid = pms_transactions.contractid
                    //    AND Pms_Transactions.PaymentKind = 1 
                    //    AND Pms_Transactions.PaymentFor = {int.Parse(requestModel.ChargesTypeID)}

                    //   union 
                    //   SELECT Pms_Transactions.TransactionNo AS ReceiptNo, 
                    //	Format(Pms_Transactions.TransactionDate, '{DateFormat}') AS TransactionDate,
                    //	Pms_PaymentKinds.PaymentKind AS PaymentType,
                    //	Pms_Transactions.paymentAmount AS PaymentAmount
                    //   FROM dbo.Pms_Transactions
                    //   INNER JOIN Pms_ContractCharges ON Pms_Transactions.PaymentFor = Pms_ContractCharges.ChargesTypeID
                    //   INNER JOIN Pms_Cheques ON Pms_Transactions.TransactionID = Pms_Cheques.TransactionID
                    //   INNER JOIN Pms_PaymentKinds ON Pms_PaymentKinds.PaymentKindID = Pms_Transactions.PaymentKind
                    //   WHERE 
                    //   Pms_Transactions.ContractID = {int.Parse(requestModel.ContractId)} AND 
                    //   TransactionTypeID = 1
                    //AND Pms_Transactions.Suspend = 0
                    //AND Pms_ContractCharges.Suspend = 0
                    //AND Pms_Cheques.Suspend=0
                    //AND Pms_PaymentKinds.Suspend = 0
                    //AND Pms_Transactions.STATUS = 'Confirmed'
                    //AND Pms_ContractCharges.contractid = pms_transactions.contractid
                    //AND Pms_Transactions.PaymentKind in (2,3,4,5,6 ) -- Other then CASH
                    //AND Pms_Cheques.ChequeStatusID IN (1,3,4) --Received, Deposited, Cleared
                    //AND Pms_Transactions.PaymentFor = {int.Parse(requestModel.ChargesTypeID)}";
                    var ReceiptQuery = $@"SELECT	Pms_Transactions.TransactionNo AS ReceiptNo, 
		                        Format(Pms_Transactions.TransactionDate, '{DateFormat}')  AS TransactionDate,
		                        Pms_PaymentKinds.PaymentKind AS PaymentType,
		                        Pms_Transactions.paymentAmount AS PaymentAmount
                                FROM dbo.Pms_Transactions
                                INNER JOIN Pms_ContractCharges ON Pms_Transactions.PaymentFor = Pms_ContractCharges.ChargesTypeID
                                INNER JOIN Pms_PaymentKinds ON Pms_PaymentKinds.PaymentKindID = Pms_Transactions.PaymentKind
                                WHERE 
	                            Pms_Transactions.ContractID = {int.Parse(requestModel.ContractId)} 
		                        AND TransactionTypeID = 1 AND Pms_Transactions.Suspend = 0
	                            AND Pms_ContractCharges.Suspend = 0
	                            AND Pms_PaymentKinds.Suspend = 0
	                            AND Pms_Transactions.STATUS = 'Confirmed'
	                            AND Pms_ContractCharges.contractid = pms_transactions.contractid
	                            AND Pms_Transactions.PaymentKind = 1 
	                            AND Pms_Transactions.PaymentFor = {int.Parse(requestModel.ChargesTypeID)}

                             UNION ALL 
                            SELECT Pms_Transactions.TransactionNo AS ReceiptNo, 
		                        Format(Pms_Transactions.TransactionDate, '{DateFormat}') AS TransactionDate,
		                        Pms_PaymentKinds.PaymentKind AS PaymentType,
		                        Pms_Transactions.paymentAmount AS PaymentAmount
                            FROM dbo.Pms_Transactions
                            INNER JOIN Pms_ContractCharges ON Pms_Transactions.PaymentFor = Pms_ContractCharges.ChargesTypeID
                            INNER JOIN Pms_Cheques ON Pms_Transactions.TransactionID = Pms_Cheques.TransactionID
                            INNER JOIN Pms_PaymentKinds ON Pms_PaymentKinds.PaymentKindID = Pms_Transactions.PaymentKind
                            WHERE 
                            Pms_Transactions.ContractID = {int.Parse(requestModel.ContractId)}  
                            AND TransactionTypeID = 1
	                        AND Pms_Transactions.Suspend = 0
	                        AND Pms_ContractCharges.Suspend = 0
	                        AND Pms_Cheques.Suspend=0
	                        AND Pms_PaymentKinds.Suspend = 0
	                        AND Pms_Transactions.STATUS = 'Confirmed'
	                        AND Pms_ContractCharges.contractid = pms_transactions.contractid
	                        AND Pms_Transactions.PaymentKind in (2,3,4,5,6 ) -- Other then CASH
	                        AND Pms_Cheques.ChequeStatusID IN (1,3,4) --Received, Deposited, Cleared
	                        AND Pms_Transactions.PaymentFor = {int.Parse(requestModel.ChargesTypeID)}
	                          UNION ALL  ---VAT ON CHARGES PAID
                          SELECT Pms_Transactions.TransactionNo AS ReceiptNo, 
		                        Format(Pms_Transactions.TransactionDate, '{DateFormat}') AS TransactionDate,
		                        Pms_PaymentKinds.PaymentKind AS PaymentType,
		                        Pms_Transactions.paymentAmount AS PaymentAmount
                                                FROM dbo.Pms_Transactions
						                          INNER JOIN Pms_PaymentKinds ON Pms_PaymentKinds.PaymentKindID = Pms_Transactions.PaymentKind
                                                WHERE 
                                                   Pms_Transactions.ContractID =  {int.Parse(requestModel.ContractId)}     
						                           AND TransactionTypeID = 1  
                                                   AND Pms_Transactions.Suspend = 0
                                                   AND Pms_Transactions.STATUS = 'Confirmed'
                                                   AND Pms_Transactions.PaymentFor = 42 
						                           AND Pms_Transactions.vatropid = {int.Parse(requestModel.ChargesTypeID)}
                                                   AND Pms_Transactions.PaymentKind = 1 --CASH

                                                                 UNION ALL
                                                                 ---RECEIVED OTHER THEN CASH
                                                SELECT Pms_Transactions.TransactionNo AS ReceiptNo, 
		                        Format(Pms_Transactions.TransactionDate, '{DateFormat}') AS TransactionDate,
		                        Pms_PaymentKinds.PaymentKind AS PaymentType,
		                        Pms_Transactions.paymentAmount AS PaymentAmount
                                                FROM dbo.Pms_Transactions
                                                INNER JOIN Pms_Cheques ON Pms_Transactions.TransactionID = Pms_Cheques.TransactionID
						                        INNER JOIN Pms_PaymentKinds ON Pms_PaymentKinds.PaymentKindID = Pms_Transactions.PaymentKind
                                                WHERE 
                                                   Pms_Transactions.ContractID = {int.Parse(requestModel.ContractId)}  
						                           AND TransactionTypeID = 1
                                                   AND Pms_Transactions.Suspend = 0
                         
                                                   AND Pms_Cheques.Suspend=0
                                                   AND Pms_Transactions.STATUS = 'Confirmed'
                                                   AND Pms_Transactions.PaymentKind in (2,3,4,5,6 ) -- Other then CASH
                                                   AND Pms_Cheques.ChequeStatusID IN (1,3,4) --Received, Deposited, Cleared
                                                   AND Pms_Transactions.PaymentFor = 42
						                           AND Pms_Transactions.vatropid =  {int.Parse(requestModel.ChargesTypeID)}
";

                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Contracts = Conn.ExecuteDataTableSqlDA(conn, ReceiptQuery);

                    if (Contracts.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractChargeReceipts>(Contracts);
                        Receipts = ListToBeSend;
                    }

                    if (Receipts.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            StatusCode = "200",
                            Receipts = Receipts,
                            Message = "Data Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "NotFound",
                            StatusCode = "404",
                            Receipts = Receipts,
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session.",
                        Receipts = Receipts,
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

        [Route("GetPropertyImage")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetPropertyImage([FromBody] GetPropertyImage request)
        {
            int PropertyId = int.Parse(request.PropertyId);
            PropertyImageVm model = new PropertyImageVm();
            var ImageQuery = $@"select PropertyID, PropertyName,PropertyImage from Pms_Property where PropertyID={PropertyId}";

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);

            if (Image.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<PropertyImageVm>(Image);
                model = ListToBeSend.FirstOrDefault();
            }

            string imageName = model.PropertyImage;
            if (imageName != null)
            {
                var GetFilePath = _config["Path:PropertyImages"].ToString();
                string MainDirectory = GetFilePath;
                imageName = MainDirectory + imageName;

                byte[] imageByteArray = null;

                try
                {
                    var path = System.IO.Path.Combine(MainDirectory, imageName);
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
                                    Description = "Image Name: " + model.PropertyImage + " Property Id: " + model.PropertyID + " Property Name: " + model.PropertyName,
                                    Path = path
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
                            Description = "Image Name: " + model.PropertyImage + " Property Id: " + model.PropertyID + " Property Name: " + model.PropertyName,
                            Path = path
                        };
                        return NotFound(data);
                    }
                }
                catch (Exception ex)
                {
                    var data = new
                    {
                        Status = ex.Message,
                        Description = "Image Name: " + model.PropertyImage + " Property Id: " + model.PropertyID + " Property Name: " + model.PropertyName,
                        Path = ""
                    };
                    return NotFound(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Image Name not found",
                    Description = "Image Name: " + model.PropertyImage + " Property Id: " + model.PropertyID + " Property Name: " + model.PropertyName,
                    Path = ""
                };
                return NotFound(data);
            }
        }

        [Route("GetUnitImage")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetUnitImage([FromBody] GetUnitDetails requestModel)
        {
            int UnitId = int.Parse(requestModel.UnitId);
            UnitImageVm model = new UnitImageVm();
            var ImageQuery = $@"select UnitName, UnitID ,UnitImage from Pms_Unit where UnitID={UnitId}";

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);

            if (Image.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<UnitImageVm>(Image);
                model = ListToBeSend.FirstOrDefault();
            }


            string imageName = model.UnitImage;
            //string imageName = "2.jpg";
            if (imageName != null)
            {
                var GetFilePath = _config["Path:UnitImages"].ToString();
                string MainDirectory = GetFilePath;
                imageName = MainDirectory + imageName;

                byte[] imageByteArray = null;

                try
                {
                    var path = System.IO.Path.Combine(MainDirectory, imageName);
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
                                    Description = "Image Name: " + model.UnitImage + " Unit Id: " + model.UnitID + " Unit Name: " + model.UnitImage,
                                    Path = path
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
                            Description = "Image Name: " + model.UnitImage + " Unit Id: " + model.UnitID + " Unit Name: " + model.UnitImage,
                            Path = path
                        };
                        return NotFound(data);
                    }
                }
                catch (Exception ex)
                {
                    var data = new
                    {
                        Status = ex.Message,
                        Description = "Image Name: " + model.UnitImage + " Unit Id: " + model.UnitID + " Unit Name: " + model.UnitImage,
                        Path = ""
                    };
                    return NotFound(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Image Name not found",
                    Description = "Image Name: " + model.UnitImage + " Unit Id: " + model.UnitID + " Unit Name: " + model.UnitImage,
                    Path = ""
                };
                return NotFound(data);
            }
        }


        [HttpPost]
        [Route("ContractPayables")]
        //[ValidateActionParameters]
        public ActionResult<string> ContractPayables([FromBody] GetOutstandingPayments requestModel)
        {
            try
            {
                List<ContractPayableVM> contractPayables = new List<ContractPayableVM>();
                List<ContractPayableVM> Charges = new List<ContractPayableVM>();
                List<ContractPayableVM> VatOnRent = new List<ContractPayableVM>();
                List<ContractPayableVM> VatCharges = new List<ContractPayableVM>();


                VATOnRentVM vATOnRent = new VATOnRentVM();
                VatonRentVm VatonRent1 = new VatonRentVm();
                VatonRentVm VatonRent2 = new VatonRentVm();
                VatRecordVm VatRecord = new VatRecordVm();
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
                    var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantId);

                    if (!contractTenantId)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data);
                    }
                    string DateFormat = _settings.DateFormat();

                    var query = $@"SELECT PaymentID
	                                ,cp.ContractID
	                                ,FORMAT(cp.PaymentDate, 'dd-MM-yyyy') AS PaymentDate
	                                ,pc.Contractno
	                                ,ISNULL(cp.PaymentLeft,0) as PaymentValue
	                                ,ISNULL(cp.PaymentLeft,0) Balance
	                                ,cp.PaymentNo
	                                ,cp.PaidAmount
	                                ,pc.Installments
	                                ,'Rental' AS Type
                                FROM Pms_ContractPayments AS cp
                                LEFT JOIN Pms_Contract pc ON pc.ContractID = cp.ContractID
                                WHERE cp.Suspend = 0
	                                AND pc.Suspend = 0
	                                AND cp.ContractID = {requestModel.ContractId}
									and  PaymentLeft > 0
	                                AND cp.PaymentID NOT IN (
		                                SELECT gpd.ContractPaymentID
		                                FROM cms.GatewayPaymant gp
		                                JOIN cms.GatewayPaymantDetail gpd ON gp.PaymentId = gpd.PaymentId
		                                WHERE gp.ContractId = {requestModel.ContractId} AND 
										gpd.ContractPaymentID IS NOT NULL and  gp.responseCode in(0, 000, 200) and gp.Reconciled=0
		                                )";

                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var dashboardData = Conn.ExecuteDataTableSqlDA(conn, query);
                    if (dashboardData.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractPayableVM>(dashboardData);
                        contractPayables = ListToBeSend;
                    }

                    var ChargesQuery = $@"
--Contract Charges
SELECT ChargesTypeID
	,ContractID
	,ISNULL(PaymentValue,0) PaymentValue
	,Type
	,TxnPaymentAmount
	,ISNULL(ChequeAmount,0) ChequeAmount
	,ISNULL(PaymentValue - TxnPaymentAmount - ChequeAmount,0) AS Balance
	,ContractchargeID
    ,ISNULL(TxnPaymentAmount + ChequeAmount,0)  as PaidAmount
FROM (
	SELECT cc.ChargesTypeID
	,cc.ContractchargeID
		,cc.ContractID
		,ISNULL(cc.Amount,0) as PaymentValue
		,rop.PaymentFor as Type
		
		,isnull((
				SELECT SUM(PaymentAmount)
				FROM pms_transactions
				WHERE suspend = 0
					AND STATUS = 'Confirmed'
					AND ContractID = cc.ContractID --CONTRACT_ID
					AND transactiontypeid = 1
					AND paymentkind IN (1)
					AND PaymentFor =  cc.ChargesTypeID
				), 0) AS TxnPaymentAmount
		,ISNULL((
				SELECT SUM(sqta.ChequeAmount) AS Expr1
				FROM Pms_Cheques AS sqta
				INNER JOIN Pms_Transactions AS sqtb ON sqta.TransactionID = sqtb.TransactionID
				WHERE (sqta.Suspend = 0)
					AND (sqtb.Suspend = 0)
					AND (
						sqta.ChequeStatusID NOT IN (
							5
							,6
							,7
							,11
							,12
							)
						)
					AND (sqtb.ContractID = cc.ContractID)
					AND (sqtb.STATUS = 'Confirmed')
					AND (sqtb.TransactionTypeID = 1)
					AND (sqtb.PaymentFor = cc.ChargesTypeID)
				), 0) AS ChequeAmount
	FROM Pms_ContractCharges AS cc
	INNER JOIN Pms_ReasonOfPayment AS rop ON cc.ChargesTypeID = rop.ReasonofPaymentID
	left join Pms_Contract pc on pc.ContractID=cc.ContractID
	WHERE (cc.Suspend = 0)
		AND (rop.Suspend = 0) AND (cc.ContractID = {requestModel.ContractId})
		AND cc.ContractchargeID NOT IN (
		                        SELECT gpd.ContractchargeID
		                        FROM cms.GatewayPaymant gp
		                        JOIN cms.GatewayPaymantDetail gpd ON gp.PaymentId = gpd.PaymentId
		                        WHERE gp.ContractId = {requestModel.ContractId}
			                        AND gpd.ContractchargeID IS NOT NULL and  gp.responseCode in(0, 000, 200)
		                        )
and (ChargesTypeID * IsSDPaid <> 2 )
	) l

WHERE PaymentValue - TxnPaymentAmount - ChequeAmount > 0



--select * from Pms_ContractCharges";
                    var chargesData = Conn.ExecuteDataTableSqlDA(conn, ChargesQuery);
                    if (chargesData.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractPayableVM>(chargesData);
                        Charges = ListToBeSend;
                    }

                    var VATOnRENTQuery = $@"
--Total VAT on Rent
select ISNULL(sum(VATCharges),0) VATOnRent from Pms_Contract where ContractID = {requestModel.ContractId}";
                    var VatOnRentData = Conn.ExecuteDataTableSqlDA(conn, VATOnRENTQuery);
                    if (VatOnRentData.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<VATOnRentVM>(VatOnRentData);
                        vATOnRent = ListToBeSend.FirstOrDefault();
                    }

                    var RentReciveable1Query = $@"
                --Cash Payment for VAT on Rent
                SELECT ISNULL(SUM(PaymentAmount),0) as PaymentAmount
				FROM pms_transactions
				WHERE suspend = 0
					AND STATUS = 'Confirmed'
					AND ContractID = {requestModel.ContractId} 
					AND transactiontypeid = 1
					AND paymentkind IN (1) --CASH
					AND PaymentFor = 42
					AND VATROPID=1";
                    var RentReciveable1 = Conn.ExecuteDataTableSqlDA(conn, RentReciveable1Query);
                    if (RentReciveable1.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<VatonRentVm>(RentReciveable1);
                        VatonRent1 = ListToBeSend.FirstOrDefault();
                    }

                    var RentReciveable2Query = $@"
                --Cheque Payment for VAT on Rent
                SELECT SUM(sqta.ChequeAmount) AS ChequeAmount
				FROM Pms_Cheques AS sqta
				INNER JOIN Pms_Transactions AS sqtb ON sqta.TransactionID = sqtb.TransactionID
				WHERE (sqta.Suspend = 0)
					AND (sqtb.Suspend = 0)
					AND (
						sqta.ChequeStatusID NOT IN (
							5
							,6
							,7
							,11
							,12
							)
						)
					AND (sqtb.ContractID ={requestModel.ContractId})
					AND (sqtb.STATUS = 'Confirmed')
					AND (sqtb.TransactionTypeID = 1)
					AND (sqtb.PaymentFor = 42)
					AND VATROPID=1";

                    var RentReciveable2 = Conn.ExecuteDataTableSqlDA(conn, RentReciveable2Query);
                    if (RentReciveable2.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<VatonRentVm>(RentReciveable2);
                        VatonRent2 = ListToBeSend.FirstOrDefault();
                    }
                    var SumVatOnRent = vATOnRent.VATOnRent ?? 0 - (VatonRent1.PaymentAmount ?? 0 + VatonRent2.ChequeAmount ?? 0);
                    var CHeckRentValueExistQuery = $@"select gpd.PaymentId from cms.GatewayPaymantDetail gpd
join cms.GatewayPaymant gp on gp.ContractId=gp.ContractId where gp.ContractId={requestModel.ContractId} and gpd.VatOnRentContractId={requestModel.ContractId} and gp.ResponseCode in (0,000,200)";
                    var CHeckRentValueExist = Conn.ExecuteDataTableSqlDA(conn, CHeckRentValueExistQuery);
                    if (CHeckRentValueExist.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<VatRecordVm>(RentReciveable2);
                        VatRecord = ListToBeSend.FirstOrDefault();
                        SumVatOnRent = 0;
                    }
                    var VatChargesQuery = $@"SELECT ChargesTypeID
	,ContractID
    ,ContractchargeID
	,ISNULL(VATAmount,0) as PaymentValue
	, 'VAT On ' + PaymentFor  As Type
	,TxnPaymentAmount
	,ISNULL(ChequeAmount,0) as ChequeAmount	
	,ISNULL(VATAmount - TxnPaymentAmount - ChequeAmount,0) AS Balance
    ,ISNULL(TxnPaymentAmount + ChequeAmount,0) AS PaidAmount
FROM (
	SELECT cc.ChargesTypeID
    ,ContractchargeID
		,cc.ContractID
		,ISNULL(cc.VATAmount,0) VATAmount
		,rop.PaymentFor

		,isnull((
				SELECT SUM(PaymentAmount)
				FROM pms_transactions
				WHERE suspend = 0
					AND STATUS = 'Confirmed'
					AND ContractID = cc.ContractID --CONTRACT_ID
					AND transactiontypeid = 1
					AND paymentkind IN (1)
					AND PaymentFor = 42
					AND VATROPID= cc.ChargesTypeID
				), 0) AS TxnPaymentAmount
		,ISNULL((
				SELECT SUM(sqta.ChequeAmount) AS Expr1
				FROM Pms_Cheques AS sqta
				INNER JOIN Pms_Transactions AS sqtb ON sqta.TransactionID = sqtb.TransactionID
				WHERE (sqta.Suspend = 0)
					AND (sqtb.Suspend = 0)
					AND (
						sqta.ChequeStatusID NOT IN (
							5
							,6
							,7
							,11
							,12
							)
						)
					AND (sqtb.ContractID = cc.ContractID)
					AND (sqtb.STATUS = 'Confirmed')
					AND (sqtb.TransactionTypeID = 1)
					AND PaymentFor = 42
					AND VATROPID= cc.ChargesTypeID
				), 0) AS ChequeAmount
	FROM Pms_ContractCharges AS cc
	INNER JOIN Pms_ReasonOfPayment AS rop ON cc.ChargesTypeID = rop.ReasonofPaymentID
	WHERE (cc.Suspend = 0)
		AND (rop.Suspend = 0)
		AND (cc.ContractID = {requestModel.ContractId})
		AND cc.ContractchargeID NOT IN (
		                        SELECT gpd.VATOnChargeId
		                        FROM cms.GatewayPaymant gp
		                        JOIN cms.GatewayPaymantDetail gpd ON gp.PaymentId = gpd.PaymentId
		                        WHERE gp.ContractId = {requestModel.ContractId}
			                        AND gpd.VATOnChargeId IS NOT NULL and  gp.responseCode in(0, 000, 200)
		                        )
	) l

WHERE VATAmount - TxnPaymentAmount - ChequeAmount > 0";
                    var VatChargesList = Conn.ExecuteDataTableSqlDA(conn, VatChargesQuery);
                    if (VatChargesList.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractPayableVM>(VatChargesList);
                        VatCharges = ListToBeSend.ToList();
                    }



                    if (SumVatOnRent > 0)
                    {
                        ContractPayableVM contractPayableVM = new ContractPayableVM();
                        contractPayableVM.Type = "Vat on Rent.";
                        contractPayableVM.PaidAmount = (VatonRent1.PaymentAmount ?? 0 + VatonRent2.ChequeAmount ?? 0);
                        contractPayableVM.PaymentValue = vATOnRent.VATOnRent ?? 0;
                        contractPayableVM.Balance = SumVatOnRent;
                        contractPayableVM.ContractchargeID = int.Parse(requestModel.ContractId);
                        contractPayableVM.ContractID = int.Parse(requestModel.ContractId);
                        VatOnRent.Add(contractPayableVM);
                    }


                    if (contractPayables != null && contractPayables.Count() > 0 || Charges != null && Charges.Count > 0)
                    {

                        var data = new
                        {
                            statusCode = "200",
                            Status = "Ok",
                            Message = "Dashboard data found.",
                            contractPayable = contractPayables,
                            additionalCharges = Charges,
                            VatOnRent = VatOnRent,
                            VatOnCharges = VatCharges,

                        };
                        return Ok(data);
                    }
                    else
                    {
                        var dataError = new
                        {
                            statusCode = "404",
                            Status = "NotFound",
                            Message = "No Record Found.",
                            contractPayable = contractPayables,
                            additionalCharges = Charges
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
                        contractPayable = contractPayables,
                        additionalCharges = Charges
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

        [HttpPost]
        [Route("DownloadContractWithSignature")]
        [ValidateActionParameters]
        public async Task<ActionResult<string>> DownloadContractWithSignature(int ContractID)
        {
            try
            {
                DownloadContractVm invoice = new DownloadContractVm();
                string DateFormat = _settings.DateFormat();
                ContractsVM ContractData = new ContractsVM();
                List<UnitRefVm> UnitRefVm = new List<UnitRefVm>();
                List<UnitRefVm> UnitStoreVm = new List<UnitRefVm>();
                TanentVm tanentVm = new TanentVm();
                TransactionDateVm TransactionDate = new TransactionDateVm();
                PreparedByVm PreparedBy = new PreparedByVm();
                List<ParkingRefVm> parking = new List<ParkingRefVm>();


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
                    var contractTenantId = ContractInTenantId(ContractID, TenantId);

                    if (!contractTenantId)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data);
                    }
                    var ContractQuery = $@"select Contractno, pc.TenantID, UserDefinedNo,ContractPosition, format(ContractDate,'{DateFormat}') as ContractDate, PropertyName +' /UBS#: '+BuildingNumber as Property, CityName as PropertyArea, Sector, PlotNumber as PlotNo, CONCAT(ISNULL(FORMAT(TenantShare,'#,###,##0'),0),'.00') as TenantShare, CONCAT(ISNULL(FORMAT(OccupantShare,'#,###,##0'),0),'.00') as OccupantShare, Subject, AdditionalTerms,Concat(tc.FirstName,' ', tc.MiddleName,' ', tc.FullName) as OccupiedBy ,FORMAT(fromDate,'dd-MM-yyy') as fromDate, FORMAT(ToDate,'{DateFormat}')as ToDate, format(GraceStartDate,'{DateFormat}') as GraceStartDate,CONCAT(ISNULL(FORMAT(AnnualMinRent,'#,###,##0'),0),'.00') as AnnualMinRent,  FORMAT(GraceEndDate, '{DateFormat}') as GraceEndDate , CONCAT(ISNULL(FORMAT(Rentforstay,'#,###,##0'),0),'.00') Rentforstay, CONCAT(ISNULL(FORMAT(SecurtiyDeposits,'#,###,##0'),0),'.00') SecurtiyDeposits,SDReceivedContractiD from Pms_Contract pc
                    left join Pms_Property pp on pc.PropertyID=pp.PropertyID
                    left join Pms_City pcity on pcity.CityID=pp.CityID
                    left join Pms_Tenant tc on pc.TenantID=Occupant
                    where pc.ContractID={ContractID} and pc.Suspend=0";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Contract = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);

                    if (Contract.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractsVM>(Contract);
                        ContractData = ListToBeSend.FirstOrDefault();
                    }

                    var TenantQuery = $@"select CONCAT(FirstName , MiddleName , FullName) as Name, FirstNameAR + MiddleNameAR + FullNameAR as NameAR , PostBoxno,  CONCAT(PhoneCountryCode, PhoneAreaCode, Phone) as TelNo,CONCAT(FaxCountryCode, FaxAreaCode, Fax) as FaxNo ,Concat(pt.MobileCountryCode, pt.MobileAreaCode, pt.Mobile) as MobileNo, pe.EmirateName, Email, TenantTypeID from Pms_Tenant pt
left join Pms_Emirate pe on pe.EmirateID= pt.EmirateId
where pe.Suspend=0 and TenantID={ContractData.TenantID}";

                    var Tenant = Conn.ExecuteDataTableSqlDA(conn, TenantQuery);

                    if (Tenant.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<TanentVm>(Tenant);
                        tanentVm = ListToBeSend.FirstOrDefault();
                    }
                    string Position = "New";
                    if (ContractData.ContractPosition == 2)
                        Position = "Renewed";
                    if (ContractData.ContractPosition == 3)
                        Position = "Extended";
                    invoice.Position = Position;

                    var UnitQuery = $@"select pu.UnitRefNo, pu.BedRooms, pu.MeasurementType , AreaSizeSqm, Areasize from Pms_ContractUnits pc
                join Pms_Unit pu on pc.UnitID=pu.UnitID
                where pu.Suspend=0 and pc.ContractID={ContractID} and pu.UnitTypeID!=2";

                    var Unit = Conn.ExecuteDataTableSqlDA(conn, UnitQuery);

                    if (Unit.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<UnitRefVm>(Unit);
                        UnitRefVm = ListToBeSend.ToList();
                    }


                    var UnitStoreQuery = $@"select pu.UnitRefNo, pu.BedRooms, pu.MeasurementType , AreaSizeSqm, Areasize from Pms_ContractUnits pc
                join Pms_Unit pu on pc.UnitID=pu.UnitID
                where pu.Suspend=0 and pc.ContractID={ContractID} and pu.UnitTypeID=2";

                    var UnitStore = Conn.ExecuteDataTableSqlDA(conn, UnitStoreQuery);

                    if (UnitStore.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<UnitRefVm>(UnitStore);
                        UnitStoreVm = ListToBeSend.ToList();
                    }

                    var StoreParkingQuery = $@"select pp.ParkingRefNo from Pms_ContractParkings pc
				join Pms_ParkingDetail pp on pp.ParkingId=pc.ParkingId where pc.suspend=0 and pc.ContractID={ContractID}";

                    var Parking = Conn.ExecuteDataTableSqlDA(conn, StoreParkingQuery);

                    if (Parking.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ParkingRefVm>(Parking);
                        parking = ListToBeSend.ToList();
                    }
                    int? SDRecdContractID = ContractData.SDReceivedContractiD;
                    if (SDRecdContractID != null)
                    {
                        if (SDRecdContractID != 0)
                        {
                            var TransactionDateQuery = $@"select FORMAT(TransactionDate,'{DateFormat}') as TransactionDate from Pms_Transactions where TransactionTypeID=1 and ContractID={SDRecdContractID} and Suspend=0 and Status='Confirmed' and PaymentFor=2";

                            var GetTransactionDate = Conn.ExecuteDataTableSqlDA(conn, TransactionDateQuery);

                            if (Parking.Rows.Count > 0)
                            {
                                var ListToBeSend = Conn.ToListof<TransactionDateVm>(GetTransactionDate);
                                TransactionDate = ListToBeSend.FirstOrDefault();
                            }

                        }
                        else
                        {
                            var TransactionDateQuery = $@"select FORMAT(TransactionDate,'{DateFormat}') as TransactionDate from Pms_Transactions where TransactionTypeID=1 and ContractID={ContractID} and Suspend=0 and Status='Confirmed' and PaymentFor=2";

                            var GetTransactionDate = Conn.ExecuteDataTableSqlDA(conn, TransactionDateQuery);

                            if (Parking.Rows.Count > 0)
                            {
                                var ListToBeSend = Conn.ToListof<TransactionDateVm>(GetTransactionDate);
                                TransactionDate = ListToBeSend.FirstOrDefault();
                            }
                        }
                    }
                    else
                    {
                        var TransactionDateQuery = $@"select FORMAT(TransactionDate,'{DateFormat}') as TransactionDate from Pms_Transactions where TransactionTypeID=1 and ContractID={ContractID} and Suspend=0 and Status='Confirmed' and PaymentFor=2";

                        var GetTransactionDate = Conn.ExecuteDataTableSqlDA(conn, TransactionDateQuery);

                        if (Parking.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<TransactionDateVm>(GetTransactionDate);
                            TransactionDate = ListToBeSend.FirstOrDefault();
                        }
                    }

                    var PreparedByQuery = $@"select FORMAT(TransactionDate,'{DateFormat}') as TransactionDate from Pms_Transactions where TransactionTypeID=1 and ContractID={ContractID} and Suspend=0 and Status='Confirmed' and PaymentFor=2";

                    var GetPreparedBy = Conn.ExecuteDataTableSqlDA(conn, PreparedByQuery);

                    if (Parking.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<PreparedByVm>(GetPreparedBy);
                        PreparedBy = ListToBeSend.FirstOrDefault();
                    }
                    invoice.ContractsVM = ContractData;
                    invoice.TanentVm = tanentVm;
                    invoice.UnitRefVm = UnitRefVm;
                    invoice.UnitStoreVm = UnitStoreVm;
                    invoice.PreparedBy = PreparedBy.Name;
                    invoice.parking = parking;
                    invoice.TransactionDate = TransactionDate.TransactionDate;

                    // Signature and path Get
                    var ImageQuery = $@"select AttachmentId as photoId,AttachmentUrl as PhotoSource, RoleTypeId,AttachmentType  
from cms.CaseContractDetail con join cms.Attachment att on att.UploadedCaseId=con.CaseNo
                        where ContractId={ContractID} and att.AttachmentType='Signature' ";
                    var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);
                    RequestSignatureVM model = new RequestSignatureVM();
                    if (Image.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<RequestSignatureVM>(Image);
                        model = ListToBeSend.FirstOrDefault();
                        var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                        string imageName = null;
                        string MainDiectory = GetFilePath;
                        imageName = model.PhotoSource;
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
                            invoice.ImageByte = newImage;
                        }
                    }


                    if (invoice.ContractsVM.Contractno != null)
                    {
                        var pdf = new ViewAsPdf("~/Views/Payments/DownloadContractWithSignature.cshtml", invoice)
                        {
                            FileName = invoice.ContractsVM.Contractno + ".pdf",
                            CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
                        };

                        try
                        {
                            byte[] pdfData = await pdf.BuildFile(ControllerContext);
                            //var path = _hostEnv.WebRootPath + "\\Downloads\\Contracts\\" + pdf.FileName;
                            //System.IO.File.WriteAllBytes(path, pdfData);
                            var PdfByte = pdfData;
                            var data = new
                            {
                                Path = pdfData,
                                Status = "200"
                            };
                            return Ok(data);
                        }
                        catch (Exception ex)
                        {
                            var data = new
                            {
                                Path = "",
                                Status = "400",
                                message = ex.Message
                            };
                            return Ok(data);
                        }


                    }
                    else
                    {
                        var data = new
                        {
                            Path = "",
                            PdfByte = "",
                            Status = "404",
                        };
                        return NotFound(data);
                    }

                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session."
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

        [HttpPost]
        [Route("DownloadContractTermsold")]
        [ValidateActionParameters]
        public async Task<ActionResult<string>> DownloadContractTermsold([FromBody] FABTenantD2Api.Controllers.DownloadContractTerms requestbody)
        {
            //try
            //{
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
                var contractTenantId = ContractInTenantId(int.Parse(requestbody.ContractId), TenantId);

                if (!contractTenantId)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data);
                }
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);

                var ContractTermQuery = $@"select pt.TenantTypeID,pc.Contractno from Pms_Contract pc
                join Pms_Tenant pt on pc.TenantID=pt.TenantID
                join Pms_TenantType ptt on ptt.TenantTypeID=pt.TenantTypeID where pc.ContractID={int.Parse(requestbody.ContractId)}";
                var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, ContractTermQuery);
                ContractTermsVm contractTerms = new ContractTermsVm();
                if (ContractTerm.Rows.Count > 0)
                {
                    var ListToBeSend1 = Conn.ToListof<ContractTermsVm>(ContractTerm);
                    contractTerms = ListToBeSend1.FirstOrDefault();
                    string Type = "Commercial";
                    if (contractTerms.TenantTypeID == 7)
                    {
                        Type = "Residential";
                    }

                    // Get Term and Condition
                    var TermConditionQuery = $@"SELECT Id,Residential,Commercial FROM Pms_TermsTemplate";
                    var TermCondition = Conn.ExecuteDataTableSqlDA(conn, TermConditionQuery);
                    TermConditionVM term = new TermConditionVM();
                    if (TermCondition.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<TermConditionVM>(TermCondition);
                        term = ListToBeSend.FirstOrDefault();
                    }

                    if (term.Id != null)
                    {
                        term.Type = Type;
                        var pdf = new ViewAsPdf("~/Views/Payments/TermAndConditions.cshtml", term)
                        {
                            FileName = Type + ".pdf",
                            CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
                        };

                        try
                        {
                            byte[] pdfData = await pdf.BuildFile(ControllerContext);
                            //var path = _hostEnv.WebRootPath + "\\Downloads\\Contracts\\" + pdf.FileName;
                            //System.IO.File.WriteAllBytes(path, pdfData);
                            var PdfByte = pdfData;
                            var data = new
                            {
                                Path = pdfData,
                                Status = "200"
                            };
                            return Ok(data);
                        }
                        catch (Exception ex)
                        {
                            var data = new
                            {
                                Path = "",
                                Status = "400",
                                message = ex.Message
                            };
                            return Ok(data);
                        }


                    }
                    else
                    {
                        var data = new
                        {
                            Path = "",
                            PdfByte = "",
                            Status = "404",
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Path = "",
                        PdfByte = "",
                        Status = "404",
                    };
                    return NotFound(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
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

        [HttpPost]
        [Route("DownloadContractTermsNew")]
        //[ValidateActionParameters]
        public async Task<ActionResult<string>> DownloadContractTermsNew()
        {
            //try
            //{
            if (User.Identity.IsAuthenticated)
            {

                //InsertErrorLog("DownloadContractTerms IS called");

                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                //InsertErrorLog("_settings.GetUserIdByToken(_bearer_token) IS called");
                if (UserId == null)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data);
                }
                //var TenantId = GetTenantId(UserId.Value);
                //var contractTenantId = ContractInTenantId(int.Parse(requestbody.ContractId), TenantId);

                //if (!contractTenantId)
                //{
                //    var data = new
                //    {
                //        Status = "Invalid",
                //        contract = new ContractVm(),
                //        Message = "Contract not found for this user; please try with valid user."
                //    };
                //    return Ok(data);
                //}
                ////InsertErrorLog("_settings.GetConnectionString(); IS called");
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                //InsertErrorLog("SqlConnection(constring); IS called");

                // Commented Becuse we dont need commercial. 05-08-2023
                ////var ContractTermQuery = $@"select pt.TenantTypeID,pc.Contractno from Pms_Contract pc
                ////join Pms_Tenant pt on pc.TenantID=pt.TenantID
                ////join Pms_TenantType ptt on ptt.TenantTypeID=pt.TenantTypeID where pc.ContractID={int.Parse(requestbody.ContractId)}";
                //var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, ContractTermQuery);
                //ContractTermsVm contractTerms = new ContractTermsVm();
                //if (ContractTerm.Rows.Count > 0)
                //{
                //var ListToBeSend1 = Conn.ToListof<ContractTermsVm>(ContractTerm);
                /// contractTerms = ListToBeSend1.FirstOrDefault();
                string Type = "Residential";
                //if (contractTerms.TenantTypeID == 7)
                //{
                //    Type = "Residential";
                //}

                // Get Term and Condition

                //InsertErrorLog("Befofre Query:SELECT top 1 Id,Residential,Commercial FROM Pms_TermsTemplate");
                var TermConditionQuery = $@"SELECT top 1 Id,Residential,Commercial FROM Pms_TermsTemplate";
                //InsertErrorLog("After Query:SELECT top 1 Id,Residential,Commercial FROM Pms_TermsTemplate");
                var TermCondition = Conn.ExecuteDataTableSqlDA(conn, TermConditionQuery);

                //InsertErrorLog("Query Result");
                TermConditionVM term = new TermConditionVM();
                if (TermCondition.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<TermConditionVM>(TermCondition);
                    term = ListToBeSend.FirstOrDefault();
                }

                if (term.Id != null)
                {
                    //InsertErrorLog("Before Reading File:~/Views/Payments/TermAndConditions.cshtml");
                    term.Type = Type;
                    var pdf = new ViewAsPdf("~/Views/Payments/TermAndConditions.cshtml", term)
                    {
                        FileName = Type + ".pdf",
                        CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
                    };
                    //InsertErrorLog("After Reading File:~/Views/Payments/TermAndConditions.cshtml");
                    try
                    {
                        byte[] pdfData = await pdf.BuildFile(ControllerContext);
                        //InsertErrorLog("Build PDF :pdf.BuildFile(ControllerContext)");
                        //var path = _hostEnv.WebRootPath + "\\Downloads\\Contracts\\" + pdf.FileName;
                        //System.IO.File.WriteAllBytes(path, pdfData);
                        var PdfByte = pdfData;
                        //InsertErrorLog(" var PdfByte = pdfData;");
                        var data = new
                        {
                            Path = pdfData,
                            Status = "200"
                        };
                        InsertErrorLog("OK");
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        InsertErrorLog("Exception:" + ex.Message);
                        var data = new
                        {
                            Path = "",
                            Status = "400",
                            message = ex.Message
                        };
                        return Ok(data);
                    }


                }
                else
                {

                    var data = new
                    {
                        Path = "",
                        PdfByte = "",
                        Status = "404",
                    };
                    return NotFound(data);
                }
                //}
                //else
                //{
                //    var data = new
                //    {
                //        Path = "",
                //        PdfByte = "",
                //        Status = "404",
                //    };
                //    return NotFound(data);
                //}
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
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





        [HttpPost]
        [Route("DownloadContractTerms")]
        //[ValidateActionParameters]
        public async Task<ActionResult<string>> DownloadContractTerms()
        {
            //try
            //{

            if (!User.Identity.IsAuthenticated)
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
                    Message = "Invalid Session."
                };
                return Unauthorized(data);
            }

            CompanyDetails companyDetails = new CompanyDetails()
            {
                CompanyName = _config["CompanyDetail:CompanyName"].ToString(),
                CompanyNameAR = _config["CompanyDetail:CompanyNameAR"].ToString(),
                CompanyShortName = _config["CompanyDetail:CompanyShortName"].ToString()
            };

            if (companyDetails.CompanyShortName == "COLLIERS")
            {
                DownloadContractVm invoice = new DownloadContractVm();

                invoice.Res2 = ConvertImageToByteArray("res-2.jpg");
                invoice.Res3 = ConvertImageToByteArray("res-3.jpg");
                invoice.Res4 = ConvertImageToByteArray("res-4.jpg");
                invoice.Res5 = ConvertImageToByteArray("res-5.jpg");

                var pdf = new ViewAsPdf("~/Views/Payments/CollierTermAndConditions.cshtml", invoice)
                {
                    FileName = "CollierTermAndCondition" + ".pdf",
                    CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
                };
                try
                {
                    byte[] pdfData = await pdf.BuildFile(ControllerContext);
                    var PdfByte = pdfData;
                    var data = new
                    {
                        Path = pdfData,
                        Status = "200"
                    };
                    InsertErrorLog("OK");
                    return Ok(data);
                }
                catch (Exception ex)
                {
                    InsertErrorLog("Exception:" + ex.Message);
                    var data = new
                    {
                        Path = "",
                        Status = "400",
                        message = ex.Message
                    };
                    return Ok(data);
                }

            }




            if (User.Identity.IsAuthenticated)
            {

                //InsertErrorLog("DownloadContractTerms IS called");

                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                //InsertErrorLog("_settings.GetUserIdByToken(_bearer_token) IS called");
                if (UserId == null)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data);
                }
                //var TenantId = GetTenantId(UserId.Value);
                //var contractTenantId = ContractInTenantId(int.Parse(requestbody.ContractId), TenantId);

                //if (!contractTenantId)
                //{
                //    var data = new
                //    {
                //        Status = "Invalid",
                //        contract = new ContractVm(),
                //        Message = "Contract not found for this user; please try with valid user."
                //    };
                //    return Ok(data);
                //}
                ////InsertErrorLog("_settings.GetConnectionString(); IS called");
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                //InsertErrorLog("SqlConnection(constring); IS called");

                // Commented Becuse we dont need commercial. 05-08-2023
                ////var ContractTermQuery = $@"select pt.TenantTypeID,pc.Contractno from Pms_Contract pc
                ////join Pms_Tenant pt on pc.TenantID=pt.TenantID
                ////join Pms_TenantType ptt on ptt.TenantTypeID=pt.TenantTypeID where pc.ContractID={int.Parse(requestbody.ContractId)}";
                //var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, ContractTermQuery);
                //ContractTermsVm contractTerms = new ContractTermsVm();
                //if (ContractTerm.Rows.Count > 0)
                //{
                //var ListToBeSend1 = Conn.ToListof<ContractTermsVm>(ContractTerm);
                /// contractTerms = ListToBeSend1.FirstOrDefault();
                string Type = "Residential";
                //if (contractTerms.TenantTypeID == 7)
                //{
                //    Type = "Residential";
                //}

                // Get Term and Condition

                //InsertErrorLog("Befofre Query:SELECT top 1 Id,Residential,Commercial FROM Pms_TermsTemplate");
                var TermConditionQuery = $@"SELECT top 1 Id,Residential,Commercial FROM Pms_TermsTemplate";
                //InsertErrorLog("After Query:SELECT top 1 Id,Residential,Commercial FROM Pms_TermsTemplate");
                var TermCondition = Conn.ExecuteDataTableSqlDA(conn, TermConditionQuery);

                //InsertErrorLog("Query Result");
                TermConditionVM term = new TermConditionVM();
                if (TermCondition.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<TermConditionVM>(TermCondition);
                    term = ListToBeSend.FirstOrDefault();
                }

                if (term.Id != null)
                {
                    //InsertErrorLog("Before Reading File:~/Views/Payments/TermAndConditions.cshtml");
                    term.Type = Type;
                    var pdf = new ViewAsPdf("~/Views/Payments/TermAndConditions.cshtml", term)
                    {
                        FileName = Type + ".pdf",
                        CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
                    };
                    //InsertErrorLog("After Reading File:~/Views/Payments/TermAndConditions.cshtml");
                    try
                    {
                        byte[] pdfData = await pdf.BuildFile(ControllerContext);
                        //InsertErrorLog("Build PDF :pdf.BuildFile(ControllerContext)");
                        //var path = _hostEnv.WebRootPath + "\\Downloads\\Contracts\\" + pdf.FileName;
                        //System.IO.File.WriteAllBytes(path, pdfData);
                        var PdfByte = pdfData;
                        //InsertErrorLog(" var PdfByte = pdfData;");
                        var data = new
                        {
                            Path = pdfData,
                            Status = "200"
                        };
                        InsertErrorLog("OK");
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        InsertErrorLog("Exception:" + ex.Message);
                        var data = new
                        {
                            Path = "",
                            Status = "400",
                            message = ex.Message
                        };
                        return Ok(data);
                    }


                }
                else
                {

                    var data = new
                    {
                        Path = "",
                        PdfByte = "",
                        Status = "404",
                    };
                    return NotFound(data);
                }
                //}
                //else
                //{
                //    var data = new
                //    {
                //        Path = "",
                //        PdfByte = "",
                //        Status = "404",
                //    };
                //    return NotFound(data);
                //}
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
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

        [HttpPost]
        [Route("DownloadNewContractTerms")]
        //[ValidateActionParameters]
        public async Task<ActionResult<string>> DownloadNewContractTerms()
        {
            //try
            //{

            if (!User.Identity.IsAuthenticated)
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
                    Message = "Invalid Session."
                };
                return Unauthorized(data);
            }

            CompanyDetails companyDetails = new CompanyDetails()
            {
                CompanyName = _config["CompanyDetail:CompanyName"].ToString(),
                CompanyNameAR = _config["CompanyDetail:CompanyNameAR"].ToString(),
                CompanyShortName = _config["CompanyDetail:CompanyShortName"].ToString()
            };

            if (companyDetails.CompanyShortName == "COLLIERS")
            {
                DownloadContractVm invoice = new DownloadContractVm();

                invoice.Res2 = ConvertImageToByteArray("res-2.jpg");
                invoice.Res3 = ConvertImageToByteArray("res-3.jpg");
                invoice.Res4 = ConvertImageToByteArray("res-4.jpg");
                invoice.Res5 = ConvertImageToByteArray("res-5.jpg");

                var pdf = new ViewAsPdf("~/Views/Payments/CollierTermAndConditions_NewContract.cshtml", invoice)
                {
                    FileName = "CollierTermAndCondition" + ".pdf",
                    CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
                };
                try
                {
                    byte[] pdfData = await pdf.BuildFile(ControllerContext);
                    var PdfByte = pdfData;
                    var data = new
                    {
                        Path = pdfData,
                        Status = "200"
                    };
                    InsertErrorLog("OK");
                    return Ok(data);
                }
                catch (Exception ex)
                {
                    InsertErrorLog("Exception:" + ex.Message);
                    var data = new
                    {
                        Path = "",
                        Status = "400",
                        message = ex.Message
                    };
                    return Ok(data);
                }

            }




            if (User.Identity.IsAuthenticated)
            {

                //InsertErrorLog("DownloadContractTerms IS called");

                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                //InsertErrorLog("_settings.GetUserIdByToken(_bearer_token) IS called");
                if (UserId == null)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data);
                }
                //var TenantId = GetTenantId(UserId.Value);
                //var contractTenantId = ContractInTenantId(int.Parse(requestbody.ContractId), TenantId);

                //if (!contractTenantId)
                //{
                //    var data = new
                //    {
                //        Status = "Invalid",
                //        contract = new ContractVm(),
                //        Message = "Contract not found for this user; please try with valid user."
                //    };
                //    return Ok(data);
                //}
                ////InsertErrorLog("_settings.GetConnectionString(); IS called");
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                //InsertErrorLog("SqlConnection(constring); IS called");

                // Commented Becuse we dont need commercial. 05-08-2023
                ////var ContractTermQuery = $@"select pt.TenantTypeID,pc.Contractno from Pms_Contract pc
                ////join Pms_Tenant pt on pc.TenantID=pt.TenantID
                ////join Pms_TenantType ptt on ptt.TenantTypeID=pt.TenantTypeID where pc.ContractID={int.Parse(requestbody.ContractId)}";
                //var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, ContractTermQuery);
                //ContractTermsVm contractTerms = new ContractTermsVm();
                //if (ContractTerm.Rows.Count > 0)
                //{
                //var ListToBeSend1 = Conn.ToListof<ContractTermsVm>(ContractTerm);
                /// contractTerms = ListToBeSend1.FirstOrDefault();
                string Type = "Residential";
                //if (contractTerms.TenantTypeID == 7)
                //{
                //    Type = "Residential";
                //}

                // Get Term and Condition

                //InsertErrorLog("Befofre Query:SELECT top 1 Id,Residential,Commercial FROM Pms_TermsTemplate");
                var TermConditionQuery = $@"SELECT top 1 Id,Residential,Commercial FROM Pms_TermsTemplate";
                //InsertErrorLog("After Query:SELECT top 1 Id,Residential,Commercial FROM Pms_TermsTemplate");
                var TermCondition = Conn.ExecuteDataTableSqlDA(conn, TermConditionQuery);

                //InsertErrorLog("Query Result");
                TermConditionVM term = new TermConditionVM();
                if (TermCondition.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<TermConditionVM>(TermCondition);
                    term = ListToBeSend.FirstOrDefault();
                }

                if (term.Id != null)
                {
                    //InsertErrorLog("Before Reading File:~/Views/Payments/TermAndConditions.cshtml");
                    term.Type = Type;
                    var pdf = new ViewAsPdf("~/Views/Payments/TermAndConditions_NewContract.cshtml", term)
                    {
                        FileName = Type + ".pdf",
                        CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
                    };
                    //InsertErrorLog("After Reading File:~/Views/Payments/TermAndConditions.cshtml");
                    try
                    {
                        byte[] pdfData = await pdf.BuildFile(ControllerContext);
                        //InsertErrorLog("Build PDF :pdf.BuildFile(ControllerContext)");
                        //var path = _hostEnv.WebRootPath + "\\Downloads\\Contracts\\" + pdf.FileName;
                        //System.IO.File.WriteAllBytes(path, pdfData);
                        var PdfByte = pdfData;
                        //InsertErrorLog(" var PdfByte = pdfData;");
                        var data = new
                        {
                            Path = pdfData,
                            Status = "200"
                        };
                        InsertErrorLog("OK");
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        InsertErrorLog("Exception:" + ex.Message);
                        var data = new
                        {
                            Path = "",
                            Status = "400",
                            message = ex.Message
                        };
                        return Ok(data);
                    }


                }
                else
                {

                    var data = new
                    {
                        Path = "",
                        PdfByte = "",
                        Status = "404",
                    };
                    return NotFound(data);
                }
                //}
                //else
                //{
                //    var data = new
                //    {
                //        Path = "",
                //        PdfByte = "",
                //        Status = "404",
                //    };
                //    return NotFound(data);
                //}
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
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



        public bool InsertErrorLog(string error)
        {

            bool logged = true;
            var datecurrent = DateTime.UtcNow.AddHours(4);
            var query = $@"INSERT INTO [CMS].[ErrorLogs]
                       ([Date]
                       ,[Description])
                        VALUES
                       ('{datecurrent}'
                       ,@Error)

                        SELECT SCOPE_IDENTITY() AS SelectedId
            ";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var errorParam = new SqlParameter("@Error", error);
            var excutequery = Conn.ExecuteDataTableSqlDA(conn, query, errorParam);
            if (excutequery != null)
            {
                logged = true;
            }
            return logged;
        }




        [HttpPost]
        [Route("ContractExpire30Days")]
        [ValidateActionParameters]
        public async Task<ActionResult<string>> ContractExpire30Days()
        {
            //try
            //{
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
                var TenanatId = _tenantRepository.GetTenantId(UserId.Value);

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);

                var ContractTermQuery = $@" select  ContractID, FORMAT(ContractDate,'dd-MM-yyyy') as ContractDate, FORMAT(fromDate,'dd-MM-yyyy') Fromdate,FORMAT(toDate,'dd-MM-yyyy') Todate, Installments, RentAmount, Rentforstay, pp.PropertyName,pp.PropertyNameAR, Contractno, 'Active' Status,  N'فعال' StatusAR from Pms_Contract 
  join Pms_Property pp on pp.PropertyID=Pms_Contract.PropertyID
  where pp.Suspend=0 and Pms_Contract.Suspend=0 and ToDate between CAST( GETDATE() AS Date ) AND  DATEADD(DD,30,( CAST( GETDATE() AS Date ))) AND ContractType= 1 AND Occupant = {TenanatId}";
                var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, ContractTermQuery);
                List<ContractExpireVm> contractExpire = new List<ContractExpireVm>();
                if (ContractTerm.Rows.Count > 0)
                {
                    var ListToBeSend1 = Conn.ToListof<ContractExpireVm>(ContractTerm);
                    contractExpire = ListToBeSend1.ToList();

                    var data = new
                    {
                        Record = contractExpire,
                        Status = "200"
                    };
                    return Ok(data);

                }
                else
                {
                    var data = new
                    {
                        Record = "",
                        Status = "404",
                    };
                    return NotFound(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
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

        [HttpPost]
        [Route("ChequeDueWithIn30Days")]
        [ValidateActionParameters]
        public async Task<ActionResult<string>> ChequeDueWithIn30Days()
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
                    var TenanatId = _tenantRepository.GetTenantId(UserId.Value);

                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);

                    var ContractTermQuery = $@"SELECT chq.AccountTitle,chq.TransactionID, chq.ChequeNo,chq.ChequeAmount, chq.check_bank FROM pms_cheques chq ,pms_transactions trans ,pms_contract tc WHERE tc.suspend = 0 AND chq.suspend = 0 AND trans.suspend = 0 AND chq.transactionid = trans.transactionid 	AND chequestatusid IN (	1,3,9	)AND trans.ContractID = tc.contractid AND STATUS = 'Confirmed'	AND transactiontypeid = 1 AND chq.ChequeDate between   CAST( GETDATE() AS Date ) AND  DATEADD(DD,30,( CAST( GETDATE() AS Date )))  AND tc.TenantID = {TenanatId}";




                    var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, ContractTermQuery);
                    List<ChequeDue30DaysVm> ChequeDue = new List<ChequeDue30DaysVm>();
                    if (ContractTerm.Rows.Count > 0)
                    {
                        var ListToBeSend1 = Conn.ToListof<ChequeDue30DaysVm>(ContractTerm);
                        ChequeDue = ListToBeSend1.ToList();

                        var data = new
                        {
                            Record = ChequeDue,
                            Status = "200"
                        };
                        return Ok(data);

                    }
                    else
                    {
                        var data = new
                        {
                            Record = "",
                            Status = "404",
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session."
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

        [HttpPost]
        [Route("CanCheckIn")]
        [ValidateActionParameters]
        public ActionResult CanCheckIn([FromBody] GetOutstandingPayments requestModel)
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

                var TenantId = GetTenantId(UserId.Value);
                var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantId);

                if (!contractTenantId)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data1);
                }
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var TenanatId = _tenantRepository.GetTenantId(UserId.Value);

                OtherCheckInContract record = new OtherCheckInContract();
                var ContractDays = $@"select ContractPosition, DATEDIFF(day, ContractDate,getdate()) as ContractDays, ContractDate from Pms_Contract  where contractid={requestModel.ContractId}";
                var CheckInContract = Conn.ExecuteDataTableSqlDA(conn, ContractDays);
                if (CheckInContract.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<OtherCheckInContract>(CheckInContract);
                    record = ListToBeSend.FirstOrDefault();
                    if (record.ContractPosition == 2)// 2 means renewals- renewals are not suppossed to be checkin
                    {
                        var data1 = new
                        {
                            CheckIn = false,
                            caseNo = 0
                        };
                        return Ok(data1);
                    }
                    if (record.ContractPosition == 1 && record.ContractDays > 30)
                    {
                        var data1 = new
                        {
                            CheckIn = false,
                            caseNo = 0
                        };
                        return Ok(data1);
                    }
                }

                // getCheck In Case No
                int? CheckInCaseNo = 0;

                CaseExistVm caseExistVm = new CaseExistVm();

                var CheckCheckInContractExist = $@"select cas.caseNo as CaseNo from cms.Cases cas
                                        join cms.CaseContractDetail as cd on cd.CaseNo=cas.CaseNo
										join cms.CaseServiceRequest as csr on csr.CaseId=cas.CaseNo
										join cms.CaseSubCategory cuc on cuc.Id=csr.CaseSubCatagouryId
                                        join cms.Customer cus on cus.CustomerId=cas.CaseOwnerId
                                        where cas.StatusId not in (20) and cuc.code in (451) and cd.ContractId={requestModel.ContractId}";
                var existOrNotCheckInContract = Conn.ExecuteDataTableSqlDA(conn, CheckCheckInContractExist);
                if (existOrNotCheckInContract.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseExistVm>(existOrNotCheckInContract);
                    caseExistVm = ListToBeSend.FirstOrDefault();
                    CheckInCaseNo = caseExistVm.CaseNo;

                }


                // check
                var checkin = _tenantRepository.CanCheckin(int.Parse(requestModel.ContractId));
                var data = new
                {
                    CheckIn = checkin,
                    caseNo = CheckInCaseNo
                };
                return Ok(data);
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
                    Message = "Invalid Session."
                };
                return Unauthorized(data);
            }
        }

        [HttpPost]
        [Route("CanSubmitLegalCase")]
        [ValidateActionParameters]
        public ActionResult CanSubmitLegalCase(int ContractId)
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
                var TenantId = GetTenantId(UserId.Value);
                var contractTenantId = ContractInTenantId(ContractId, TenantId);

                if (!contractTenantId)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data1);
                }
                // get Legal Case No
                int? LegalCaseNo = 0;
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                CaseExistVm caseExistVm = new CaseExistVm();


                var CheckLegalContractExist = $@"select cas.caseNo as CaseNo from cms.Cases cas
                                        join cms.CaseContractDetail as cd on cd.CaseNo=cas.CaseNo
										join cms.CaseServiceRequest as csr on csr.CaseId=cas.CaseNo
										join cms.CaseSubCategory cuc on cuc.Id=csr.CaseSubCatagouryId
                                        join cms.Customer cus on cus.CustomerId=cas.CaseOwnerId
                                        where cas.StatusId not  in (20) and cuc.code in (452) and cd.ContractId={ContractId}";
                var existOrNotLegalContract = Conn.ExecuteDataTableSqlDA(conn, CheckLegalContractExist);
                if (existOrNotLegalContract.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<CaseExistVm>(existOrNotLegalContract);
                    caseExistVm = ListToBeSend.FirstOrDefault();
                    LegalCaseNo = caseExistVm.CaseNo;
                }



                var data = new
                {
                    status = "Ok",
                    caseNo = LegalCaseNo
                };
                return Ok(data);
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
                    Message = "Invalid Session."
                };
                return Unauthorized(data);
            }
        }

        [HttpPost]
        [Route("CanRenewContract")]
        [ValidateActionParameters]
        public ActionResult CanRenewContract(int ContractId)
        {
            if (User.Identity.IsAuthenticated)
            {
                string DateFormat = _settings.DateFormat();
                RenewalCaseVm ContractsData = new RenewalCaseVm();
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
                var TenantId = GetTenantId(UserId.Value);
                var contractTenantId = ContractInTenantId(ContractId, TenantId);

                if (!contractTenantId)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data1);
                }
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                CaseExistVm caseExistVm = new CaseExistVm();

                var ContractQuery = $@"SELECT (case when ToDate <GETDATE() then 1 else 0 end) as IsExpire, (case when CAST(DATEDIFF(day,30,ToDate) as datetime)<  getdate() then 1 else 0 end) as IsreadyToExpire, ContractTypeID from  Pms_Contract c Left Outer Join Pms_Property p On p.PropertyID = c.PropertyID Left Outer Join Pms_Emirate pE On p.EmirateID = pE.EmirateID Left Outer Join Pms_ContractType ct On ct.ContractTypeID = c.ContractType join Pms_ContractPosition  pcp on c.ContractPosition=pcp.ContractPositionId where c.Suspend=0 and contractid={ContractId}";

                var Contracts = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);
                if (Contracts.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<RenewalCaseVm>(Contracts);
                    ContractsData = ListToBeSend.FirstOrDefault();

                    if (ContractsData.ContractTypeID == 1)
                    {
                        ContractsData.IsExpire = 0;
                        ContractsData.IsreadyToExpire = 0;
                    }
                    else
                    {
                        ContractsData.IsExpire = 1;
                        ContractsData.IsreadyToExpire = 1;
                    }
                }


                var data = new
                {
                    isExpire = ContractsData.IsExpire,
                    IsreadyToExpire = ContractsData.IsreadyToExpire,
                    status = "Ok",
                };
                return Ok(data);
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
                    Message = "Invalid Session."
                };
                return Unauthorized(data);
            }
        }

        //[HttpPost]
        //[Route("NewContractActions")]
        //public ActionResult<string> NewContractActions()
        //{
        //    try
        //    {
        //        List<ContractRenewalActionVm> Data = new List<ContractRenewalActionVm>();
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
        //            string DateFormat = _settings.DateFormat();

        //            var TenantId = GetTenantId(UserId.Value);
        //            if (TenantId == 0)
        //            {
        //                var dataInvalid = new
        //                {
        //                    Status = "Invalid",
        //                    StatusCode = "404",
        //                    Charges = Data,
        //                    Message = "Invalid User please try with valid user."
        //                };
        //                return NotFound(dataInvalid);
        //            }
        //            var ChargeQuery = $@"
        //                        select ContractId ,Contractno, 
        //                        format(pmsC.ContractDate, 'dd-MM-yyyy') As ContractDate,
        //                        format(pmsC.fromDate, 'dd-MM-yyyy') as FromDate,
        //                        format(pmsC.toDate, 'dd-MM-yyyy') as ToDate,
        //                        pmsP.PropertyName , pmsP.PropertyNameAR , pmsE.EmirateName , pmsE.EmirateNameAR , pmsE.Address,
        //                        pmcCT.ContractType , pmcCT.ContractTypeAR
        //                        from
        //                        Pms_Contract pmsC left join Pms_Property pmsP On pmsC.propertyID = pmsP.PropertyID
        //                        left join Pms_Emirate pmsE on  pmsP.emirateId = pmsE.EmirateID
        //                        left join Pms_ContractType pmcCT on pmsC.ContractType = pmcCT.ContractTypeID
        //                        where pmsC.contractType = 3  and TenantID = {TenantId}";

        //            var constring = _settings.GetConnectionString();
        //            constring = constring.ToString();
        //            SqlConnection conn = new SqlConnection(constring);
        //            var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery);

        //            if (Charges.Rows.Count > 0)
        //            {
        //                var ListToBeSend = Conn.ToListof<ContractRenewalActionVm>(Charges);
        //                Data = ListToBeSend;
        //            }
        //            if (Data.Count() > 0)
        //            {
        //                var data = new
        //                {
        //                    Status = "Ok",
        //                    StatusCode = "200",
        //                    Record = Data,
        //                    Message = "Data Found.",
        //                    ResponseMessage = _settings.PaymentMessage
        //                };
        //                return Ok(data);
        //            }
        //            else
        //            {
        //                var data = new
        //                {
        //                    Status = "NotFound",
        //                    StatusCode = "404",
        //                    Record = Data,
        //                    Message = "No Data Found."
        //                };
        //                return NotFound(data);
        //            }
        //        }


        //        else
        //        {
        //            var data = new
        //            {
        //                Status = "Unauthorized",
        //                StatusCode = "401",
        //                Record = Data,
        //                Message = "Invalid Session."
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

        [HttpPost]
        [Route("ContractRenewalActions")]
        public ActionResult<string> ContractRenewalActions()
        {
            try
            {
                List<ContractRenewalActionVm> Data = new List<ContractRenewalActionVm>();
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
                    string DateFormat = _settings.DateFormat();

                    var TenantId = GetTenantId(UserId.Value);
                    if (TenantId == 0)
                    {
                        var dataInvalid = new
                        {
                            Status = "Invalid",
                            StatusCode = "404",
                            Charges = Data,
                            Message = "Invalid User please try with valid user."
                        };
                        return NotFound(dataInvalid);
                    }
                    var ChargeQuery = $@"
SELECT DueActionid
	,CaseId
      ,iif(CaseId is null,0,iif((select c.statusId from cms.Cases c where c.CaseNo=CaseId)=20,1,0)) IsCanceled

    ,iif(CaseId is null,0,iif((select c.statusId from cms.Cases c where c.CaseNo=CaseId)=17,1,0)) IsClosed
	,ExpiringingDays
	,cda.StageId
	,sta.Name AS StageName
	,con.Contractno AS PreviousContractNo
	,COALESCE(RenewalContractid, cda.Contractid) AS 'Contractid'
	,COALESCE(cont.Contractno, con.Contractno) AS 'Contractno'
	,COALESCE(propt.PropertyName, prop.PropertyName) AS 'PropertyName'
	,COALESCE(propt.PropertyNameAr, prop.PropertyNameAr) AS 'PropertyNameAr'
	,COALESCE(emit.EmirateName, emi.EmirateName) AS 'EmirateName'
	,CASE 
		WHEN COALESCE(emit.EmirateName, emi.EmirateName) = 'Abu Dhabi'
			AND (
				con.RentPaidBy = 2
				OR con.RentPaidBy = 3
				)
			THEN 1
		ELSE 0
		END AS ShowOfferLetter
	,COALESCE(format(cont.fromDate, 'dd-MM-yyyy'), format(con.fromDate, 'dd-MM-yyyy')) AS 'Fromdate'
	,COALESCE(format(cont.ToDate, 'dd-MM-yyyy'), format(con.ToDate, 'dd-MM-yyyy')) AS 'ToDate'	
	,COALESCE(
            CASE 
	 
				WHEN ttt.ContractType = 'Posted'  AND  cont.ToDate >= GETDATE()
					THEN 'Active'
				WHEN ttt.ContractType = 'Posted' AND  cont.ToDate < GETDATE()
					THEN 'Expired'		
				ELSE ttt.ContractType
				END,

                CASE 

                WHEN tt.ContractType = 'Posted'  AND  con.ToDate >= GETDATE()
					THEN 'Active'
				WHEN tt.ContractType = 'Posted' AND  con.ToDate < GETDATE()
					THEN 'Expired'		
				ELSE tt.ContractType
				END
		 	
				) AS 'Status'
	,COALESCE(CASE 
			WHEN tt.ContractType = 'Posted' AND  con.ToDate >= GETDATE()
				THEN N'فعال'
			ELSE tt.ContractTypeAr
			END, CASE 
			WHEN tt.ContractType = 'Posted' AND  con.ToDate < GETDATE()
				THEN N'منتهي'
			ELSE tt.ContractTypeAr
			END) AS 'StatusAr',
(select iif( (cda.RenewalContractid is not null and cda.RenewalContractid!=0 and (select count(*) from cms.PaymentSetting where ContractId=cda.RenewalContractid)>0), iif(Count(*)>0,0,1),0) IsAllPaid from cms.PaymentSetting where ContractId=cda.RenewalContractid and ((PaymentMethodId!=2 and (Paid=0 or Paid is null)) or (PaymentMethodId=2 and (ChequeNo=0 or ChequeNo is null) or IsRejected=1))) IsAllPaid
FROM cms.ContractDueAction cda
JOIN cms.stages sta ON sta.stageId = cda.StageId
LEFT JOIN Pms_Contract con ON con.ContractID = cda.Contractid
LEFT JOIN Pms_Property prop ON prop.PropertyID = con.PropertyID
LEFT JOIN Pms_ContractType tt ON tt.ContractTypeID = con.ContractType
LEFT JOIN Pms_Emirate emi ON emi.EmirateID = prop.EmirateID
--For renewal 
LEFT JOIN Pms_Contract cont ON cont.ContractID = cda.RenewalContractid
LEFT JOIN Pms_Property propt ON propt.PropertyID = cont.PropertyID
LEFT JOIN Pms_ContractType ttt ON ttt.ContractTypeID = cont.ContractType
LEFT JOIN Pms_Emirate emit ON propt.EmirateID = emit.EmirateID
WHERE cda.TennantId = {TenantId} and COALESCE(cont.ContractType,con.ContractType) not in(2,5,7) and
	 cda.StageId < 9
	AND COALESCE(tt.ContractTypeID, ttt.ContractTypeID) IN (1)
ORDER BY COALESCE(cont.Contractno, con.Contractno) DESC
";

                    // AND cont.ContractType not in(2,5,7) 
                    //              var ChargeQuery = $@"select DueActionid,CaseId,ExpiringingDays,cda.StageId,sta.Name as StageName,con.Contractno as PreviousContractNo,
                    //            COALESCE (RenewalContractid,cda.Contractid) as 'Contractid', 
                    //            COALESCE (cont.Contractno,con.Contractno) as 'Contractno',
                    //            COALESCE (propt.PropertyName,prop.PropertyName) as 'PropertyName',
                    //COALESCE (propt.PropertyNameAr,prop.PropertyNameAr) as 'PropertyNameAr',
                    //COALESCE (emit.EmirateName,emi.EmirateName) as 'EmirateName',
                    //CASE WHEN COALESCE (emit.EmirateName,emi.EmirateName)='Abu Dhabi' and (cont.RentPaidBy = 2 or cont.RentPaidBy = 3) THEN 1  ELSE 0 END as ShowOfferLetter,
                    //            COALESCE (format(cont.fromDate,'dd-MM-yyyy'),format(con.fromDate,'dd-MM-yyyy')) as 'Fromdate',
                    //            COALESCE (format(cont.ToDate,'dd-MM-yyyy'),format(con.ToDate,'dd-MM-yyyy')) as 'ToDate',
                    // COALESCE (case WHEN ttt.ContractType='Posted' THEN 'Active' ELSE  ttt.ContractType END, case WHEN tt.ContractType='Posted' THEN 'Active' ELSE  tt.ContractType END ) as 'Status',
                    // COALESCE (case WHEN ttt.ContractTypeAr='Posted' THEN 'Active' ELSE  ttt.ContractTypeAr END, case WHEN tt.ContractTypeAr='Posted' THEN 'Active' ELSE  tt.ContractTypeAr END ) as 'StatusAr'
                    //                from cms.ContractDueAction cda
                    //                  join cms.stages  sta on sta.stageId= cda.StageId
                    //                  left join Pms_Contract con on con.ContractID= cda.Contractid
                    //left join Pms_Property prop on prop.PropertyID= con.PropertyID
                    //left join Pms_ContractType tt on tt.ContractTypeID= con.ContractType
                    //left join Pms_Emirate emi on emi.EmirateID= prop.EmirateID
                    //--For renewal 
                    //left join Pms_Contract cont on cont.ContractID= cda.RenewalContractid
                    //left join Pms_Property propt on propt.PropertyID= cont.PropertyID
                    //left join Pms_ContractType ttt on ttt.ContractTypeID= cont.ContractType
                    //left join Pms_Emirate emit on propt.EmirateID= emit.EmirateID
                    //                  where cda.TennantId={TenantId} and cda.StageId !=11 and COALESCE (tt.ContractTypeID,ttt.ContractTypeID) in (1) order by COALESCE (cont.Contractno,con.Contractno) desc";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery);

                    if (Charges.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractRenewalActionVm>(Charges);
                        Data = ListToBeSend;
                    }
                    if (Data.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            StatusCode = "200",
                            Record = Data,
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
                            StatusCode = "404",
                            Record = Data,
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }
                }


                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Record = Data,
                        Message = "Invalid Session."
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
        [HttpPost]
        [Route("NewContractActions")]
        public ActionResult<string> NewContractActions()
        {
            try
            {
                  List<ContractRenewalActionVm> Data = new List<ContractRenewalActionVm>();
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
                    string DateFormat = _settings.DateFormat();

                    var TenantId = GetTenantId(UserId.Value);
                    if (TenantId == 0)
                    {
                        var dataInvalid = new
                        {
                            Status = "Invalid",
                            StatusCode = "404",
                            Charges = Data,
                            Message = "Invalid User please try with valid user."
                        };
                        return NotFound(dataInvalid);
                    }
                    var ChargeQuery = $@"Select DueActionid,
			    CaseId,
				ExpiringingDays,
				cda.StageId,
				sta.Name as StageName,
				con.Contractno as PreviousContractNo,
                cda.Contractid as 'Contractid', 
                con.Contractno as 'Contractno',
                prop.PropertyName as 'PropertyName',
				prop.PropertyNameAr as 'PropertyNameAr',
                format(con.fromDate,'dd-MM-yyyy') as 'Fromdate',
                format(con.ToDate,'dd-MM-yyyy') as 'ToDate',
				Case WHEN tt.ContractType='Posted' THEN 'Active' ELSE  tt.ContractType END  as 'Status',
                 Case WHEN tt.ContractType='Posted' THEN N'فعال' ELSE  tt.ContractTypeAr END as 'StatusAr'

 from cms.ContractDueAction cda
                 join cms.stages  sta on sta.stageId= cda.StageId
                 left join Pms_Contract con on con.ContractID= cda.Contractid
				left join Pms_Property prop on prop.PropertyID= con.PropertyID
				left join Pms_ContractType tt on tt.ContractTypeID= con.ContractType
                 where cda.TennantId={TenantId} and cda.StageId < 9 
                 AND tt.ContractTypeID IN (3)

 ORDER BY con.Contractno DESC
";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery);

                    if (Charges.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractRenewalActionVm>(Charges);
                        Data = ListToBeSend;
                    }
                    if (Data.Count() > 0)
                    {
                        var data = new
                        {
                            Status = "Ok",
                            StatusCode = "200",
                            Record = Data,
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
                            StatusCode = "404",
                            Record = Data,
                            Message = "No Data Found."
                        };
                        return NotFound(data);
                    }
                }


                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Record = Data,
                        Message = "Invalid Session."
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
       
        
        private int GetCaseOwnerId(int UserId)
        {
            int ownerid = 0;
            FABTenantD2Api.Models.CaseOwnerViewModal caseOwnerView = new FABTenantD2Api.Models.CaseOwnerViewModal();
            var ServiceRequestQuery = $@"Select CustomerId as CaseOwnerId From CMS.Customer Where UserId = {UserId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, ServiceRequestQuery);

            if (RequestDetail.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<FABTenantD2Api.Models.CaseOwnerViewModal>(RequestDetail);
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
            var Contracts = Conn.ExecuteDataTableSqlDA(conn, $"" +
                $"SELECT 1 FROM CMS.Cases WHERE (CaseOwnerId = {userId}) AND (CaseNo = {caseNo})");
            return Contracts.Rows.Count > 0;
        }

        [HttpPost]
        [Route("UpdateContractStageNew")]
        [ValidateActionParameters]
        public ActionResult<string> UpdateContractStageNew([FromBody] UpdateContractStage requestModel)
        {

            //try
            //{

            if (User.Identity.IsAuthenticated)
            {
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                if (UserId == null)
                {
                    var data1 = new
                    {
                        StatusCode = "200",
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data1);
                }
                string DateFormat = _settings.DateFormat();

                var TenantId = GetTenantId(UserId.Value);
                if (TenantId == 0)
                {
                    var dataInvalid = new
                    {
                        Status = "Invalid",
                        StatusCode = "404",
                        Message = "Invalid User please try with valid user."
                    };
                    return NotFound(dataInvalid);
                }
                try
                {
                    var logged = _tenantRepository.InsertErrorLog("Before Update Stage : " + requestModel.StageId);
                    var Update = _tenantRepository.UpdateStageId(int.Parse(requestModel.DueActionid), int.Parse(requestModel.StageId));
                    logged = _tenantRepository.InsertErrorLog("After Update Stage " + int.Parse(requestModel.StageId));

                }
                catch (Exception ex)
                {

                    var logged = _tenantRepository.InsertErrorLog(ex.Message);
                }
                var EmirateId = 0;
                var ContractStage = _tenantRepository.ContractStageInfo(int.Parse(requestModel.DueActionid));
                if (ContractStage != null)
                {

                    //security api update start
                    var userid = GetCaseOwnerId(UserId.Value);
                    var isValidUserForCase = CaseInUserId(ContractStage.CaseNo, userid);

                    if (!isValidUserForCase)
                    {
                        var invaliddata = new
                        {
                            Status = "Invalid",
                            Message = "Not a Valid User For Case."
                        };
                        return Unauthorized(invaliddata);
                    }
                    var logged = _tenantRepository.InsertErrorLog("Call For Casse Info");
                    var caseOnfo = _tenantRepository.GetCaseInfo(ContractStage.CaseNo);
                    if (caseOnfo != null)
                    {
                        logged = _tenantRepository.InsertErrorLog("Call For PM Assignee");

                        /* case stage update will happen from cms for 5 and 6 . no need to chage any assignee aprt from 5&6*/
                        var categoryInfo = _tenantRepository.CategoryInfo(caseOnfo.CaseCategouryId);
                        //on each contract stage in PM cases assignee must be changed as per policy
                        if (categoryInfo == "PM")
                        {
                            logged = _tenantRepository.InsertErrorLog("Call For PM Assignee to update");
                            if (int.Parse(requestModel.StageId) == 2)
                            {
                                var UpdateUser = _tenantRepository.AssignPMAssineee(ContractStage.CaseNo, "Update", true);
                                if (!string.IsNullOrWhiteSpace(UpdateUser))
                                {
                                    logged = _tenantRepository.InsertErrorLog(UpdateUser);
                                }
                                else
                                {
                                    logged = _tenantRepository.InsertErrorLog("Unable to assign user");
                                }
                            }
                        }
                    }
                }
                if (int.Parse(requestModel.StageId) == 3)
                {
                    var CaseStageInfo = _tenantRepository.ContractStageInfo(int.Parse(requestModel.DueActionid));
                    var CaseInfo = _tenantRepository.GetCaseInfo(CaseStageInfo.CaseNo);
                    if (CaseInfo.UserId != null)
                    {
                        string Email = _tenantRepository.CaseOwnerEmail(CaseInfo.UserId);
                        var AddNotification = _tenantRepository.prepareAndAddEmailNotification(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 7, CaseStageInfo.CaseNo, Email);

                    }
                }
                if (int.Parse(requestModel.StageId) == 7)
                {
                    var logged = _tenantRepository.InsertErrorLog("When Stage Id is 7 trying to send email");
                    try
                    {
                        var CaseStageInfo = _tenantRepository.ContractStageInfo(int.Parse(requestModel.DueActionid));
                        var CaseInfo = _tenantRepository.GetCaseInfo(CaseStageInfo.CaseNo);
                        EmirateId = (int)CaseInfo.EmirateId;
                        _tenantRepository.InsertErrorLog("When Stage Id is 7 trying to send email after geting case info.");
                        if (CaseInfo != null && CaseInfo.UserId != null)
                        {
                            logged = _tenantRepository.InsertErrorLog("workgroup email before ");
                            var workgroupemail = _tenantRepository.GetWorkGroupEmail(CaseInfo.WorkGroupId, CaseInfo.EmirateId);
                            logged = _tenantRepository.InsertErrorLog("workgroup email: " + workgroupemail);
                            if (!string.IsNullOrWhiteSpace(workgroupemail))
                            {
                                // string CMSurl = CMSLocationPath + CaseStageInfo.CaseNo;
                                //< a href ={ CMSurl}></a>
                                var body = $@"Dear Employee, <br/>
                                        Tenant with Case No:  {CaseStageInfo.CaseNo}, Assigned to: {CaseStageInfo.FullName} has Signed the Contract<br/>
                                        System generated email
                                        ";

                                //var body = $@"Dear Employee, .<br/>
                                //        Tenant with case No {CaseStageInfo.CaseNo} has signed the contract.<br/>
                                //        System Generated Email
                                //        ";
                                var subject = "Contract Signed By Tenant";
                                var title = "Contract Signed By Tenant";
                                var sendemailnotification = _tenantRepository.AddNewEmailNotification(body, CaseInfo.UserId.Value, subject, workgroupemail, title, CaseStageInfo.CaseNo);
                                logged = _tenantRepository.InsertErrorLog("When Stage Id is 7 email added to queue");
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                        logged = _tenantRepository.InsertErrorLog(ex.Message);
                    }
                }
                //                if (StageId == 8)
                //                {
                //                    var logged = _tenantRepository.InsertErrorLog("When Stage Id is 8 trying to send email");
                //                    // adding email notification to work group
                //                    try
                //                    {
                //                        var CaseStageInfo = _tenantRepository.ContractStageInfo(DueActionid);
                //                        var CaseInfo = _tenantRepository.GetCaseInfo(CaseStageInfo.CaseNo);
                //                        if (CaseInfo != null && CaseInfo.UserId != null)
                //                        {
                //                            var workgroupemail = _tenantRepository.GetWorkGroupEmail(CaseInfo.WorkGroupId, CaseInfo.EmirateId);
                //                            if (string.IsNullOrWhiteSpace(workgroupemail))
                //                            {
                //                                var body = $@"Dear Employee, .<br/>
                //Tenant with case No {CaseStageInfo.CaseNo} has completed Tawtheeq registration , kindly post the contract.<br/>
                //B. Regards,
                //";
                //                                var subject = "Tenant Tawtheeq registration";
                //                                var title = "Tenant Tawtheeq registration";
                //                                var sendemailnotification = _tenantRepository.AddNewEmailNotification(body, CaseInfo.UserId.Value, subject, workgroupemail, title, CaseStageInfo.CaseNo);
                //                                logged = _tenantRepository.InsertErrorLog("When Stage Id is 8 email added to queue");
                //                            }
                //                        }

                //                    }
                //                    catch (Exception ex)
                //                    {

                //                        logged = _tenantRepository.InsertErrorLog(ex.Message);
                //                    }
                //                }
                if (int.Parse(requestModel.StageId) == 9)
                {
                    var logged = _tenantRepository.InsertErrorLog("When Stage Id is 9 trying to send email");
                    var CaseStageInfo = _tenantRepository.ContractStageInfo(int.Parse(requestModel.DueActionid));
                    var CaseInfo = _tenantRepository.GetCaseInfo(CaseStageInfo.CaseNo);
                    try
                    {

                        if (CaseInfo != null && CaseInfo.UserId != null)
                        {
                            var workgroupemail = _tenantRepository.GetWorkGroupEmail(CaseInfo.WorkGroupId, CaseInfo.EmirateId);
                            if (!string.IsNullOrWhiteSpace(workgroupemail))
                            {
                                var body = $@"Dear Team, .<br/>
                            Tenant with case No {CaseStageInfo.CaseNo} has completed Tawtheeq registration, kindly post the contract.<br/>
                            System generated email.
                            ";
                                var subject = "Tenant Tawtheeq registration";
                                var title = "Tenant Tawtheeq registration";
                                var sendemailnotification = _tenantRepository.AddNewEmailNotification(body, CaseInfo.UserId.Value, subject, workgroupemail, title, CaseStageInfo.CaseNo);
                                logged = _tenantRepository.InsertErrorLog("When Stage Id is 8 email added to queue");
                            }
                        }

                    }
                    catch (Exception ex)
                    {

                        logged = _tenantRepository.InsertErrorLog(ex.Message);
                    }
                }

                var data = new
                {
                    Status = "ok",
                    StatusCode = "200",
                    Message = "Record updated",
                    EmirateId = EmirateId
                };
                return Ok(data);

            }


            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
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

        [HttpPost]
        [Route("UpdateContractStage")]
        [ValidateActionParameters]
        public ActionResult<string> UpdateContractStage([FromBody] UpdateContractStage requestModel)
        {

            //try
            //{

            if (User.Identity.IsAuthenticated)
            {
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                if (UserId == null)
                {
                    var data1 = new
                    {
                        StatusCode = "200",
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data1);
                }
                string DateFormat = _settings.DateFormat();

                var TenantId = GetTenantId(UserId.Value);
                if (TenantId == 0)
                {
                    var dataInvalid = new
                    {
                        Status = "Invalid",
                        StatusCode = "404",
                        Message = "Invalid User please try with valid user."
                    };
                    return NotFound(dataInvalid);
                }
                try
                {
                    var logged = _tenantRepository.InsertErrorLog("Before Update Stage : " + requestModel.StageId);
                    var Update = _tenantRepository.UpdateStageId(int.Parse(requestModel.DueActionid), int.Parse(requestModel.StageId));
                    logged = _tenantRepository.InsertErrorLog("After Update Stage " + int.Parse(requestModel.StageId));

                }
                catch (Exception ex)
                {

                    var logged = _tenantRepository.InsertErrorLog(ex.Message);
                }
                var EmirateId = 0;
                var ContractStage = _tenantRepository.ContractStageInfo(int.Parse(requestModel.DueActionid));
                if (ContractStage != null)
                {

                    //security api update start

                    //commneted by iftikhar on terporary basis 15/03/2024
                    /*
                    var userid = GetCaseOwnerId(UserId.Value);
                    var isValidUserForCase = CaseInUserId(ContractStage.CaseNo, userid);

                    if (!isValidUserForCase)
                    {
                        var invaliddata = new
                        {
                            Status = "Invalid",
                            Message = "Not a Valid User For Case."
                        };
                        return Unauthorized(invaliddata);
                    }
                    */



                    var logged = _tenantRepository.InsertErrorLog("Call For Casse Info");
                    var caseOnfo = _tenantRepository.GetCaseInfo(ContractStage.CaseNo);
                    if (caseOnfo != null)
                    {
                        logged = _tenantRepository.InsertErrorLog("Call For PM Assignee");

                        /* case stage update will happen from cms for 5 and 6 . no need to chage any assignee aprt from 5&6*/
                        var categoryInfo = _tenantRepository.CategoryInfo(caseOnfo.CaseCategouryId);
                        //on each contract stage in PM cases assignee must be changed as per policy
                        if (categoryInfo == "PM")
                        {
                            logged = _tenantRepository.InsertErrorLog("Call For PM Assignee to update");
                            if (int.Parse(requestModel.StageId) == 2)
                            {
                                var UpdateUser = _tenantRepository.AssignPMAssineee(ContractStage.CaseNo, "Update", true);
                                if (!string.IsNullOrWhiteSpace(UpdateUser))
                                {
                                    logged = _tenantRepository.InsertErrorLog(UpdateUser);
                                }
                                else
                                {
                                    logged = _tenantRepository.InsertErrorLog("Unable to assign user");
                                }
                            }
                        }
                    }
                }
                if (int.Parse(requestModel.StageId) == 3)
                {
                    var CaseStageInfo = _tenantRepository.ContractStageInfo(int.Parse(requestModel.DueActionid));
                    var CaseInfo = _tenantRepository.GetCaseInfo(CaseStageInfo.CaseNo);
                    if (CaseInfo.UserId != null)
                    {
                        string Email = _tenantRepository.CaseOwnerEmail(CaseInfo.UserId);
                        var AddNotification = _tenantRepository.prepareAndAddEmailNotification(CaseInfo.CaseTypeId, CaseInfo.CaseCategouryId, CaseInfo.EmirateId, 7, CaseStageInfo.CaseNo, Email);

                    }
                }
                if (int.Parse(requestModel.StageId) == 7)
                {
                    var logged = _tenantRepository.InsertErrorLog("When Stage Id is 7 trying to send email");
                    try
                    {
                        var CaseStageInfo = _tenantRepository.ContractStageInfo(int.Parse(requestModel.DueActionid));
                        var CaseInfo = _tenantRepository.GetCaseInfo(CaseStageInfo.CaseNo);
                        EmirateId = (int)CaseInfo.EmirateId;
                        _tenantRepository.InsertErrorLog("When Stage Id is 7 trying to send email after geting case info.");
                        if (CaseInfo != null && CaseInfo.UserId != null)
                        {
                            logged = _tenantRepository.InsertErrorLog("workgroup email before ");
                            var workgroupemail = _tenantRepository.GetWorkGroupEmail(CaseInfo.WorkGroupId, CaseInfo.EmirateId);
                            logged = _tenantRepository.InsertErrorLog("workgroup email: " + workgroupemail);
                            if (!string.IsNullOrWhiteSpace(workgroupemail))
                            {
                                // string CMSurl = CMSLocationPath + CaseStageInfo.CaseNo;
                                //< a href ={ CMSurl}></a>
                                var body = $@"Dear Employee, <br/>
                                        Tenant with Case No:  {CaseStageInfo.CaseNo}, Assigned to: {CaseStageInfo.FullName} has submitted the required documents<br/>
                                        System generated email
                                        ";

                                //var body = $@"Dear Employee, .<br/>
                                //        Tenant with case No {CaseStageInfo.CaseNo} has signed the contract.<br/>
                                //        System Generated Email
                                //        ";
                                var subject = "Contract Signed By Tenant";
                                var title = "Contract Signed By Tenant";
                                var sendemailnotification = _tenantRepository.AddNewEmailNotification(body, CaseInfo.UserId.Value, subject, workgroupemail, title, CaseStageInfo.CaseNo);
                                logged = _tenantRepository.InsertErrorLog("When Stage Id is 7 email added to queue");
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                        logged = _tenantRepository.InsertErrorLog(ex.Message);
                    }
                }
                //                if (StageId == 8)
                //                {
                //                    var logged = _tenantRepository.InsertErrorLog("When Stage Id is 8 trying to send email");
                //                    // adding email notification to work group
                //                    try
                //                    {
                //                        var CaseStageInfo = _tenantRepository.ContractStageInfo(DueActionid);
                //                        var CaseInfo = _tenantRepository.GetCaseInfo(CaseStageInfo.CaseNo);
                //                        if (CaseInfo != null && CaseInfo.UserId != null)
                //                        {
                //                            var workgroupemail = _tenantRepository.GetWorkGroupEmail(CaseInfo.WorkGroupId, CaseInfo.EmirateId);
                //                            if (string.IsNullOrWhiteSpace(workgroupemail))
                //                            {
                //                                var body = $@"Dear Employee, .<br/>
                //Tenant with case No {CaseStageInfo.CaseNo} has completed Tawtheeq registration , kindly post the contract.<br/>
                //B. Regards,
                //";
                //                                var subject = "Tenant Tawtheeq registration";
                //                                var title = "Tenant Tawtheeq registration";
                //                                var sendemailnotification = _tenantRepository.AddNewEmailNotification(body, CaseInfo.UserId.Value, subject, workgroupemail, title, CaseStageInfo.CaseNo);
                //                                logged = _tenantRepository.InsertErrorLog("When Stage Id is 8 email added to queue");
                //                            }
                //                        }

                //                    }
                //                    catch (Exception ex)
                //                    {

                //                        logged = _tenantRepository.InsertErrorLog(ex.Message);
                //                    }
                //                }
                if (int.Parse(requestModel.StageId) == 9)
                {
                    var logged = _tenantRepository.InsertErrorLog("When Stage Id is 9 trying to send email");
                    var CaseStageInfo = _tenantRepository.ContractStageInfo(int.Parse(requestModel.DueActionid));
                    var CaseInfo = _tenantRepository.GetCaseInfo(CaseStageInfo.CaseNo);
                    try
                    {

                        if (CaseInfo != null && CaseInfo.UserId != null)
                        {
                            var workgroupemail = _tenantRepository.GetWorkGroupEmail(CaseInfo.WorkGroupId, CaseInfo.EmirateId);
                            if (!string.IsNullOrWhiteSpace(workgroupemail))
                            {
                                var body = $@"Dear Team, .<br/>
                            Tenant with case No {CaseStageInfo.CaseNo} has completed Tawtheeq registration, kindly post the contract.<br/>
                            System generated email.
                            ";
                                var subject = "Tenant Tawtheeq registration";
                                var title = "Tenant Tawtheeq registration";
                                var sendemailnotification = _tenantRepository.AddNewEmailNotification(body, CaseInfo.UserId.Value, subject, workgroupemail, title, CaseStageInfo.CaseNo);
                                logged = _tenantRepository.InsertErrorLog("When Stage Id is 8 email added to queue");
                            }
                        }

                    }
                    catch (Exception ex)
                    {

                        logged = _tenantRepository.InsertErrorLog(ex.Message);
                    }
                }

                var data = new
                {
                    Status = "ok",
                    StatusCode = "200",
                    Message = "Record updated",
                    EmirateId = EmirateId
                };
                return Ok(data);

            }


            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
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


        [HttpPost]
        [Route("AddContractPayments")]
        public IActionResult AddContractPayments(RegisterContractPayment model)
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
                var TenantId = GetTenantId(UserId.Value);
                foreach (var item in model.DetailList)
                {
                    if (item.ContractId != null)
                    {
                        var contractTenantId = ContractInTenantId(item.ContractId.Value, TenantId);

                        if (!contractTenantId)
                        {
                            var data1 = new
                            {
                                Status = "Invalid",
                                contract = new ContractVm(),
                                Message = "Contract not found for this user; please try with valid user."
                            };
                            return Ok(data1);
                        }
                    }
                    else
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data1);
                    }
                    if (FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(item.Description, out _) || FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(item.AramexAddress, out _) || FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(item.Title, out _) || FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(item.SelfDelivery, out _) || FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(item.Type, out _))
                    {
                        var dataBadReq = new
                        {
                            Status = "BadRequest",
                            Message = "Invalid Request.",
                            StatusCode = "500"
                        };
                        return BadRequest(dataBadReq);
                    }
                }

                var add = _tenantRepository.RegisterContractPayment(model.DetailList);
                if (add == "Add successfully")
                {
                    var data = new
                    {
                        status = "ok",
                        statusCode = "200"
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        status = "Error",
                        statusCode = "500"
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
        }

        [HttpPost]
        [Route("OnlinePayments")]
        [ValidateActionParameters]
        public IActionResult OnlinePayments([FromBody] GetOutstandingPayments requestModel)
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

                    var TenantId = GetTenantId(UserId.Value);
                    var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantId);

                    if (!contractTenantId)
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data1);
                    }
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);

                    var ContractTermQuery = $@"select PaymentSettingId,Type, Title, TitleAr, ContractId, ps.PaymentMethodId,Description,Amount, ContractchargeID, ContractPaymentID, VatOnRentContractId ,VatOnChargeId,mode.Name as PaymentMode, Cheque,PaymentDate  from cms.PaymentSetting  ps
                        join cms.PaymentMethod  mode on mode.PaymentMethodId= ps.PaymentMethodId
                        where contractId={requestModel.ContractId} and ps.PaymentMethodId in(1,3)  
                        and PaymentSettingId not in 
                        (select gpd.PaymentSettingId from cms.GatewayPaymantDetail gpd left join cms.GatewayPaymant gp on gp.PaymentId=gpd.PaymentId where gp.ContractId={requestModel.ContractId} and gp.ResponseCode in (0, 000, 200)) and (Paid is null or paid=0)";
                    var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, ContractTermQuery);
                    List<ContractPaymentVM> contractPaymentts = new List<ContractPaymentVM>();
                    if (ContractTerm.Rows.Count > 0)
                    {
                        var ListToBeSend1 = Conn.ToListof<ContractPaymentVM>(ContractTerm);
                        contractPaymentts = ListToBeSend1.ToList();

                        var data = new
                        {
                            Record = contractPaymentts,
                            Status = "200"
                        };
                        return Ok(data);

                    }
                    else
                    {
                        var data = new
                        {
                            Record = "",
                            Status = "404",
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session."
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
                    Message = ex.Message,

                };
                return BadRequest(data);
            }
        }

        [HttpPost]
        [Route("OnlinePaymentsNew")]
        [ValidateActionParameters]
        public IActionResult OnlinePaymentsNew([FromBody] GetOutstandingPayments requestModel)
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

                    var TenantId = GetTenantId(UserId.Value);
                    var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantId);

                    if (!contractTenantId)
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data1);
                    }
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);

                    var ContractTermQuery = $@"select PaymentSettingId,Type, Title, TitleAr, ContractId, ps.PaymentMethodId,Description,Amount, ContractchargeID, ContractPaymentID, VatOnRentContractId ,VatOnChargeId,mode.Name as PaymentMode, Cheque,PaymentDate  from cms.PaymentSetting  ps
                        join cms.PaymentMethod  mode on mode.PaymentMethodId= ps.PaymentMethodId
                        where contractId={requestModel.ContractId} and ps.PaymentMethodId in(1,3)  
                        and PaymentSettingId not in 
                        (select gpd.PaymentSettingId from cms.GatewayPaymantDetail gpd left join cms.GatewayPaymant gp on gp.PaymentId=gpd.PaymentId where gp.ContractId={requestModel.ContractId} and gp.ResponseCode in (0, 000, 200)) and (Paid is null or paid=0)";
                    var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, ContractTermQuery);
                    List<ContractPaymentVM> contractPaymentts = new List<ContractPaymentVM>();
                    if (ContractTerm.Rows.Count > 0)
                    {
                        var ListToBeSend1 = Conn.ToListof<ContractPaymentVM>(ContractTerm);
                        contractPaymentts = ListToBeSend1.ToList();

                        var data = new
                        {
                            Record = contractPaymentts,
                            Status = "200"
                        };
                        return Ok(data);

                    }
                    else
                    {
                        var data = new
                        {
                            Record = "",
                            Status = "404",
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session."
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
                    Message = ex.Message,

                };
                return BadRequest(data);
            }
        }

        [HttpPost]
        [Route("GetOutstandingPaymentsNew")]
        public IActionResult GetOutstandingPaymentsNew([FromBody] GetOutstandingPayments requestModel)
        {

            //try
            //{
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
                _tenantRepository.InsertErrorLog("GetOutstandingPayments: after authentication");
                // security api update start
                // commented on 14 / 10 / 2022
                var contractTenantId = ContractInUserId(int.Parse(requestModel.ContractId), UserId.Value);
                _tenantRepository.InsertErrorLog("GetOutstandingPayments: after isvalid contract for user");
                if (!contractTenantId)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    _tenantRepository.InsertErrorLog("GetOutstandingPayments: invalid contract for user");
                    return Ok(data);
                }
                //security api update end

                var TenanatId = _tenantRepository.GetTenantId(UserId.Value);
                _tenantRepository.InsertErrorLog("GetOutstandingPayments: get tenantid");

                var AddPayments = _tenantRepository.GetContractPayablePayments(int.Parse(requestModel.ContractId));
                _tenantRepository.InsertErrorLog("GetOutstandingPayments: GetContractPayablePayments (" + AddPayments + ")");
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);

                var ContractTermQuery = $@"select PaymentSettingId,ps.Paid,Type, Title, ContractId,TitleAr, ps.PaymentMethodId,Description,Amount, ContractchargeID, ContractPaymentID, VatOnRentContractId ,VatOnChargeId,mode.Name as PaymentMode, Cheque,ChequeNo,PaymentDate,AramexAddress, SelfDelivery,ISNULL(Confirmed,0) as Confirmed, ISNULL(IsRejected,0) IsRejected, 
ISNULL(AcceptPaymentType,0) AS AcceptPaymentType  from cms.PaymentSetting  ps
                    join cms.PaymentMethod  mode on mode.PaymentMethodId= ps.PaymentMethodId
                    where contractId={requestModel.ContractId} and  (ps.Paid=0 or ps.Paid is null)";

                var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, ContractTermQuery);
                List<ContractPaymentVM> contractPaymentts = new List<ContractPaymentVM>();
                _tenantRepository.InsertErrorLog("GetOutstandingPayments: GetContractPayablePayments invalid data");
                if (ContractTerm.Rows.Count > 0)
                {
                    var ListToBeSend1 = Conn.ToListof<ContractPaymentVM>(ContractTerm);
                    contractPaymentts = ListToBeSend1.ToList();

                    var data = new
                    {
                        Record = contractPaymentts,
                        Status = "200"
                    };
                    return Ok(data);

                }
                else
                {
                    var data = new
                    {
                        Record = "",
                        Status = "404",
                    };
                    return NotFound(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
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

        [HttpPost]
        [Route("GetOutstandingPayments")]
        public IActionResult GetOutstandingPayments([FromBody] GetOutstandingPayments requestModel)
        {

            //try
            //{
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
                _tenantRepository.InsertErrorLog("GetOutstandingPayments: after authentication");
                // security api update start
                // commented on 14 / 10 / 2022
                var contractTenantId = ContractInUserId(int.Parse(requestModel.ContractId), UserId.Value);
                _tenantRepository.InsertErrorLog("GetOutstandingPayments: after isvalid contract for user");
                if (!contractTenantId)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    _tenantRepository.InsertErrorLog("GetOutstandingPayments: invalid contract for user");
                    return Ok(data);
                }
                //security api update end

                var TenanatId = _tenantRepository.GetTenantId(UserId.Value);
                _tenantRepository.InsertErrorLog("GetOutstandingPayments: get tenantid");

                var AddPayments = _tenantRepository.GetContractPayablePayments(int.Parse(requestModel.ContractId));
                _tenantRepository.InsertErrorLog("GetOutstandingPayments: GetContractPayablePayments (" + AddPayments + ")");
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);

                var ContractTermQuery = $@"select PaymentSettingId,ps.Paid,Type, Title, ContractId,TitleAr, ps.PaymentMethodId,Description,Amount, ContractchargeID, ContractPaymentID, VatOnRentContractId ,VatOnChargeId,mode.Name as PaymentMode, Cheque,ChequeNo,PaymentDate,AramexAddress, SelfDelivery,ISNULL(Confirmed,0) as Confirmed, ISNULL(IsRejected,0) IsRejected, ISNULL(AcceptPaymentType,0) AS AcceptPaymentType  from cms.PaymentSetting  ps
                    join cms.PaymentMethod  mode on mode.PaymentMethodId= ps.PaymentMethodId
                    where contractId={requestModel.ContractId} and  (ps.Paid=0 or ps.Paid is null)";

                var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, ContractTermQuery);
                List<ContractPaymentVM> contractPaymentts = new List<ContractPaymentVM>();
                _tenantRepository.InsertErrorLog("GetOutstandingPayments: GetContractPayablePayments invalid data");
                if (ContractTerm.Rows.Count > 0)
                {
                    var ListToBeSend1 = Conn.ToListof<ContractPaymentVM>(ContractTerm);
                    contractPaymentts = ListToBeSend1.ToList();

                    var data = new
                    {
                        Record = contractPaymentts,
                        Status = "200"
                    };
                    return Ok(data);

                }
                else
                {
                    var data = new
                    {
                        Record = "",
                        Status = "404",
                    };
                    return NotFound(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
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

        private bool validatePaymentSettingTransaction(SqlTransaction tran, SqlConnection conn, int PaymentSettingId, int UserId)
        {

            var query = $@"Select 1 From cms.PaymentSetting ps inner join Pms_Contract c on c.ContractID = ps.ContractId where ps.PaymentSettingId = {PaymentSettingId} and TenantID = {UserId}";
            var Contracts = Conn.ExecuteDataTableSqlDAWithTrnSaction(tran, conn, query);
            return Contracts.Rows.Count > 0;
        }


        private bool validatePaymentSetting(int PaymentSettingId, int UserId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var query = $@"Select 1 From cms.PaymentSetting ps inner join Pms_Contract c on c.ContractID = ps.ContractId where ps.PaymentSettingId = {PaymentSettingId} and TenantID = {UserId}";
            var Contracts = Conn.ExecuteDataTableSqlDA(conn, query);
            return Contracts.Rows.Count > 0;
        }

        [HttpPost]
        [Route("UpdateContractPaymentMethodNew")]
        [RequestSizeLimit(15000000)]
        public async Task<IActionResult> UpdateContractPaymentMethodNew([FromForm] ContractPay model)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrWhiteSpace(model.ChequeNo))
                {
                    model.ChequeNo = decrypt.Decrypt(model.ChequeNo);
                }
                model.PaymentSettingId = decrypt.Decrypt(model.PaymentSettingId);
                model.PaymentModeId = decrypt.Decrypt(model.PaymentModeId);
                if (!string.IsNullOrWhiteSpace(model.Address))
                {
                    model.Address = decrypt.Decrypt(model.Address);
                }
                //addbecause remove regex for validation
                if (FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(model.ChequeNo, out _) || FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousString(model.ChequeNo, out _) || FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(model.Address, out _) ||
                    FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousString(model.Address, out _)
                    )
                {
                    var dataBadReq = new
                    {
                        Status = "BadRequest",
                        Message = "Invalid Request.",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
                var logged = _tenantRepository.InsertErrorLog("Update Contrat Api Call");
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                if (UserId == null)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        StatusCode = "401",
                        Message = "InvalidSession."
                        //test
                    };
                    logged = _tenantRepository.InsertErrorLog("Unauthorize try Update Contrat Api Call");
                    return Unauthorized(data1);
                }

                SqlTransaction sqlTran = null;
                try
                {
                    using (var conn = new SqlConnection(_settings.GetConnectionString()))
                    {
                        conn.Open();
                        sqlTran = conn.BeginTransaction();
                        var tenantId = GetTenantId(UserId.Value);
                        if (tenantId == 0)
                        {
                            var data1 = new
                            {
                                Status = "Invalid",
                                StatusCode = "401",
                                Message = "InvalidSession."
                                //test
                            };

                            return Unauthorized(data1);
                        }
                        var validatePayment = validatePaymentSettingTransaction(sqlTran, conn, int.Parse(model.PaymentSettingId), tenantId);
                        if (!validatePayment)
                        {
                            var data1 = new
                            {
                                Status = "Invalid",
                                StatusCode = "401",
                                Message = "Invalid Attempt."
                                //test
                            };
                            _tenantRepository.InsertErrorLog("Unauthorize try Update Payment Api Call");
                            return Unauthorized(data1);

                        }

                        //checking payemntmethod confirm or not
                        var getConfirmPaymentStatus = _tenantRepository.GetPaymentMethodConfirmStatusTransaction(sqlTran, conn, int.Parse(model.PaymentSettingId), model.ChequeNo);
                        if (getConfirmPaymentStatus)
                        {
                            var dataerror = new
                            {
                                status = "Error",
                                statusCode = "500",
                                message = "Unable to update payment method please contact administrator."
                            };
                            return BadRequest(dataerror);
                        }
                        //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };//point no 10
                        logged = _tenantRepository.InsertErrorLog("Get File 1");
                        //var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                        var GetFilePath = Path.Combine(_env.WebRootPath, _config["Path:ServiceRequestFiles"].ToString());

                        if (!Directory.Exists(GetFilePath))
                        {
                            Directory.CreateDirectory(GetFilePath);
                        }
                        string Filename = null;

                        //28 Aug

                        logged = _tenantRepository.InsertErrorLog(" File bytes found");
                        //
                        if (model.File != null)
                        {
                            var bytes = await model.File.GetBytesAsync();
                            var ext = Path.GetExtension(model.File.FileName);
                            if (!MimeTypes.IsValidMimeType(bytes, model.File.ContentType, ext))
                            {
                                var data1 = new
                                {
                                    Status = "Invalid",
                                    StatusCode = "500",
                                    Message = "Invalid file type"
                                    //test
                                };
                                logged = _tenantRepository.InsertErrorLog("Invalid file type");
                                return BadRequest(data1);
                            }
                            logged = _tenantRepository.InsertErrorLog("Getting File Name");
                            Filename = DateTime.UtcNow.AddHours(4).ToString("yyyyMMddHHmmssffff") + model.File.FileName;
                            var path = GetFilePath + Filename;
                            logged = _tenantRepository.InsertErrorLog("File Copied to folder");
                            await System.IO.File.WriteAllBytesAsync(path, bytes);
                        }

                        //27-Aug-22 updated
                        var ContractQuery = $@"update cms.PaymentSetting set PaymentMethodId={model.PaymentModeId}, Cheque=@Filename, ChequeNo=@ChequeNo, IsRejected=0 where PaymentSettingId={model.PaymentSettingId}";


                        //15-March-2023 Add Confirmed = 1
                        //var ContractQuery = $@"update cms.PaymentSetting set PaymentMethodId ={model.PaymentModeId},Confirmed = 1, Cheque = @Filename, ChequeNo = @ChequeNo, IsRejected = 0 where PaymentSettingId = {model.PaymentSettingId}";

                        var chequeParam = new SqlParameter("@ChequeNo", model.ChequeNo);//add paramter
                        var FilenameParam = new SqlParameter("@Filename", Filename);//add paramter
                        logged = _tenantRepository.InsertErrorLog("Exicuting update query");

                        //db null check
                        if (!string.IsNullOrWhiteSpace(model.ChequeNo))
                        {
                            chequeParam.Value = model.ChequeNo;
                        }
                        else
                        {
                            chequeParam.Value = DBNull.Value;
                        }

                        if (!string.IsNullOrWhiteSpace(Filename))
                        {
                            FilenameParam.Value = Filename;
                        }
                        else
                        {
                            FilenameParam.Value = DBNull.Value;
                        }

                        // check payment is confimed then don't update
                        SqlParameter[] sqlParameters = new SqlParameter[]
                           {
                            chequeParam, FilenameParam
                           };
                        var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, conn, ContractQuery, sqlParameters);// include params
                        if (Properties != null)
                        {
                            logged = _tenantRepository.InsertErrorLog("Executed update query");
                        }

                        var data = new
                        {
                            status = "ok",
                            statusCode = "200"
                        };
                        sqlTran.Commit();
                        return Ok(data);
                        //}
                        //else
                        //{
                        //    var data = new
                        //    {
                        //        Status = "Unauthorized",
                        //        StatusCode = "401",
                        //        Message = "Invalid Session."
                        //    };
                        //    return Unauthorized(data);
                        //}


                    }
                }
                catch (Exception ex)
                {
                    sqlTran.Rollback();
                    return Ok(ex.Message);
                };
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
        [Route("UpdateContractPaymentMethod")]
        [RequestSizeLimit(15000000)]
        public async Task<IActionResult> UpdateContractPaymentMethod([FromForm] ContractPay model)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrWhiteSpace(model.ChequeNo))
                {
                    model.ChequeNo = decrypt.Decrypt(model.ChequeNo);
                }
                model.PaymentSettingId = decrypt.Decrypt(model.PaymentSettingId);
                model.PaymentModeId = decrypt.Decrypt(model.PaymentModeId);
                if (!string.IsNullOrWhiteSpace(model.Address))
                {
                    model.Address = decrypt.Decrypt(model.Address);
                }
                //addbecause remove regex for validation
                if (FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(model.ChequeNo, out _) ||
                    FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousString(model.ChequeNo, out _) ||
                    FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(model.Address, out _) ||
                    FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousString(model.Address, out _)
                    )
                {
                    var dataBadReq = new
                    {
                        Status = "BadRequest",
                        Message = "Invalid Request.",
                        StatusCode = "500"
                    };
                    return BadRequest(dataBadReq);
                }
                var logged = _tenantRepository.InsertErrorLog("Update Contrat Api Call");
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                if (UserId == null)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        StatusCode = "401",
                        Message = "InvalidSession."
                        //test
                    };
                    logged = _tenantRepository.InsertErrorLog("Unauthorize try Update Contrat Api Call");
                    return Unauthorized(data1);
                }

                SqlTransaction sqlTran = null;
                try
                {
                    using (var conn = new SqlConnection(_settings.GetConnectionString()))
                    {
                        conn.Open();
                        sqlTran = conn.BeginTransaction();
                        var tenantId = GetTenantId(UserId.Value);
                        if (tenantId == 0)
                        {
                            var data1 = new
                            {
                                Status = "Invalid",
                                StatusCode = "401",
                                Message = "InvalidSession."
                                //test
                            };

                            return Unauthorized(data1);
                        }
                        var validatePayment = validatePaymentSettingTransaction(sqlTran, conn, int.Parse(model.PaymentSettingId), tenantId);
                        if (!validatePayment)
                        {
                            var data1 = new
                            {
                                Status = "Invalid",
                                StatusCode = "401",
                                Message = "Invalid Attempt."
                                //test
                            };
                            _tenantRepository.InsertErrorLog("Unauthorize try Update Payment Api Call");
                            return Unauthorized(data1);

                        }

                        //checking payemntmethod confirm or not
                        var getConfirmPaymentStatus = _tenantRepository.GetPaymentMethodConfirmStatusTransaction(sqlTran, conn, int.Parse(model.PaymentSettingId), model.ChequeNo);
                        if (getConfirmPaymentStatus)
                        {
                            var dataerror = new
                            {
                                status = "Error",
                                statusCode = "500",
                                message = "Unable to update payment method please contact administrator."
                            };
                            return BadRequest(dataerror);
                        }
                        //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };//point no 10
                        logged = _tenantRepository.InsertErrorLog("Get File 1");
                        //var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                        var GetFilePath = Path.Combine(_env.WebRootPath, _config["Path:ServiceRequestFiles"].ToString());

                        if (!Directory.Exists(GetFilePath))
                        {
                            Directory.CreateDirectory(GetFilePath);
                        }
                        string Filename = null;

                        //28 Aug

                        logged = _tenantRepository.InsertErrorLog(" File bytes found");
                        //
                        if (model.File != null)
                        {
                            var bytes = await model.File.GetBytesAsync();
                            var ext = Path.GetExtension(model.File.FileName);
                            if (!MimeTypes.IsValidMimeType(bytes, model.File.ContentType, ext))
                            {
                                var data1 = new
                                {
                                    Status = "Invalid",
                                    StatusCode = "500",
                                    Message = "Invalid file type"
                                    //test
                                };
                                logged = _tenantRepository.InsertErrorLog("Invalid file type");
                                return BadRequest(data1);
                            }
                            logged = _tenantRepository.InsertErrorLog("Getting File Name");
                            Filename = DateTime.UtcNow.AddHours(4).ToString("yyyyMMddHHmmssffff") + model.File.FileName;
                            var path = GetFilePath + Filename;
                            logged = _tenantRepository.InsertErrorLog("File Copied to folder");
                            await System.IO.File.WriteAllBytesAsync(path, bytes);
                        }


                        bool IsExist = false;
                        var ExistsQuery = $@"Select top 1 * from CMS.PaymentSetting WHere IsRejected=1 AND PaymentSettingId={model.PaymentSettingId}";

                        var existsqueryResult = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, conn, ExistsQuery, null);
                        if (existsqueryResult.Rows.Count > 0)
                        {
                            IsExist = true;
                        }

                        //27-Aug-22 updated
                        var ContractQuery = $@"update cms.PaymentSetting set PaymentMethodId={model.PaymentModeId}, Cheque=@Filename, ChequeNo=@ChequeNo, IsRejected=0 where PaymentSettingId={model.PaymentSettingId}";


                        //15-March-2023 Add Confirmed = 1
                        //var ContractQuery = $@"update cms.PaymentSetting set PaymentMethodId ={model.PaymentModeId},Confirmed = 1, Cheque = @Filename, ChequeNo = @ChequeNo, IsRejected = 0 where PaymentSettingId = {model.PaymentSettingId}";

                        var chequeParam = new SqlParameter("@ChequeNo", model.ChequeNo);//add paramter
                        var FilenameParam = new SqlParameter("@Filename", Filename);//add paramter
                        logged = _tenantRepository.InsertErrorLog("Exicuting update query");

                        //db null check
                        if (!string.IsNullOrWhiteSpace(model.ChequeNo))
                        {
                            chequeParam.Value = model.ChequeNo;
                        }
                        else
                        {
                            chequeParam.Value = DBNull.Value;
                        }

                        if (!string.IsNullOrWhiteSpace(Filename))
                        {
                            FilenameParam.Value = Filename;
                        }
                        else
                        {
                            FilenameParam.Value = DBNull.Value;
                        }

                        // check payment is confimed then don't update
                        SqlParameter[] sqlParameters = new SqlParameter[]
                           {
                            chequeParam, FilenameParam
                           };
                        var Properties = Conn.ExecuteDataTableSqlDAWithTrnSaction(sqlTran, conn, ContractQuery, sqlParameters);// include params
                        if (Properties != null)
                        {
                            logged = _tenantRepository.InsertErrorLog("Executed update query");
                        }

                        if (IsExist)
                        {
                            int caseNo = _tenantRepository.GetCaseNoPaymentSettingid(sqlTran, conn, int.Parse(model.PaymentSettingId));
                            var getCaseInfo = _tenantRepository.GetCaseDetailInfoTransaction(sqlTran, conn, caseNo);
                            if (getCaseInfo != null)
                            {
                                var assignee = getCaseInfo.Assignee;
                                var email = getCaseInfo.WorkGroupEmail;
                                var category = getCaseInfo.CategoryName;
                                var title = "Reupload rejected Cheque";
                                var subject = "Reupload rejected Cheque";
                                var body = $@"Dear Team, <br/> Case no: {caseNo} with category {category} and is assigned to {assignee}. Rejected cheque has been reuploaded by tenant <br/>  <br/> System Generated Notification.";

                                var sendworkGroupEmail = _tenantRepository.AddNewEmailNotificationTransaction(sqlTran, conn, body, UserId.Value, subject, email, title, caseNo);
                            }
                        }

                        var data = new
                        {
                            status = "ok",
                            statusCode = "200"
                        };
                        sqlTran.Commit();
                        return Ok(data);
                        //}
                        //else
                        //{
                        //    var data = new
                        //    {
                        //        Status = "Unauthorized",
                        //        StatusCode = "401",
                        //        Message = "Invalid Session."
                        //    };
                        //    return Unauthorized(data);
                        //}


                    }
                }
                catch (Exception ex)
                {
                    sqlTran.Rollback();
                    return Ok(ex.Message);
                };
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
        [Route(" ")]
        [RequestSizeLimit(15000000)]
        public async Task<IActionResult> UpdateContractPaymentMethodCopy([FromForm] ContractPay model)
        {
            //if (User.Identity.IsAuthenticated)
            //{
            if (!string.IsNullOrWhiteSpace(model.ChequeNo))
            {
                model.ChequeNo = decrypt.Decrypt(model.ChequeNo);
            }
            model.PaymentSettingId = decrypt.Decrypt(model.PaymentSettingId);
            model.PaymentModeId = decrypt.Decrypt(model.PaymentModeId);
            if (!string.IsNullOrWhiteSpace(model.Address))
            {
                model.Address = decrypt.Decrypt(model.Address);
            }
            //addbecause remove regex for validation
            if (FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(model.ChequeNo, out _) || FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousString(model.ChequeNo, out _) || FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(model.Address, out _) ||
                FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousString(model.Address, out _)
                )
            {
                var dataBadReq = new
                {
                    Status = "BadRequest",
                    Message = "Invalid Request.",
                    StatusCode = "500"
                };
                return BadRequest(dataBadReq);
            }
            var logged = _tenantRepository.InsertErrorLog("Update Contrat Api Call");
            var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            int? UserId = _settings.GetUserIdByToken(_bearer_token);
            if (UserId == null)
            {
                var data1 = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                    //test
                };
                logged = _tenantRepository.InsertErrorLog("Unauthorize try Update Contrat Api Call");
                return Unauthorized(data1);
            }
            var tenantId = GetTenantId(UserId.Value);
            if (tenantId == 0)
            {
                var data1 = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                    //test
                };

                return Unauthorized(data1);
            }
            var validatePayment = validatePaymentSetting(int.Parse(model.PaymentSettingId), tenantId);
            if (!validatePayment)
            {
                var data1 = new
                {
                    Status = "Invalid",
                    Message = "Invalid Attempt."
                    //test
                };
                _tenantRepository.InsertErrorLog("Unauthorize try Update Payment Api Call");
                return Unauthorized(data1);

            }

            //checking payemntmethod confirm or not
            var getConfirmPaymentStatus = _tenantRepository.GetPaymentMethodConfirmStatus(int.Parse(model.PaymentSettingId), model.ChequeNo);
            if (getConfirmPaymentStatus)
            {
                var dataerror = new
                {
                    status = "Error",
                    statusCode = "500",
                    message = "Unable to update payment method please contact administrator."
                };
                return BadRequest(dataerror);
            }
            //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };//point no 10
            logged = _tenantRepository.InsertErrorLog("Get File 1");
            //var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();

            var GetFilePath = Path.Combine(_env.WebRootPath, _config["Path:ServiceRequestFiles"].ToString());

            if (!Directory.Exists(GetFilePath))
            {
                Directory.CreateDirectory(GetFilePath);
            }
            string Filename = null;

            //28 Aug

            logged = _tenantRepository.InsertErrorLog(" File bytes found");
            //
            if (model.File != null)
            {
                var bytes = await model.File.GetBytesAsync();
                var ext = Path.GetExtension(model.File.FileName);
                if (!MimeTypes.IsValidMimeType(bytes, model.File.ContentType, ext))
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        Message = "Invalid file type"
                        //test
                    };
                    logged = _tenantRepository.InsertErrorLog("Invalid file type");
                    return BadRequest(data1);
                }
                logged = _tenantRepository.InsertErrorLog("Getting File Name");
                Filename = DateTime.UtcNow.AddHours(4).ToString("yyyyMMddHHmmssffff") + model.File.FileName;
                var path = GetFilePath + Filename;
                logged = _tenantRepository.InsertErrorLog("File Copied to folder");
                await System.IO.File.WriteAllBytesAsync(path, bytes);
            }

            //27-Aug-22 updated
            var ContractQuery = $@"update cms.PaymentSetting set PaymentMethodId={model.PaymentModeId}, Cheque=@Filename, ChequeNo=@ChequeNo, IsRejected=0 where PaymentSettingId={model.PaymentSettingId}";


            //15-March-2023 Add Confirmed = 1
            //var ContractQuery = $@"update cms.PaymentSetting set PaymentMethodId ={model.PaymentModeId},Confirmed = 1, Cheque = @Filename, ChequeNo = @ChequeNo, IsRejected = 0 where PaymentSettingId = {model.PaymentSettingId}";

            var chequeParam = new SqlParameter("@ChequeNo", model.ChequeNo);//add paramter
            var FilenameParam = new SqlParameter("@Filename", Filename);//add paramter
            logged = _tenantRepository.InsertErrorLog("Exicuting update query");

            //db null check
            if (!string.IsNullOrWhiteSpace(model.ChequeNo))
            {
                chequeParam.Value = model.ChequeNo;
            }
            else
            {
                chequeParam.Value = DBNull.Value;
            }

            if (!string.IsNullOrWhiteSpace(Filename))
            {
                FilenameParam.Value = Filename;
            }
            else
            {
                FilenameParam.Value = DBNull.Value;
            }

            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            // check payment is confimed then don't update
            var Properties = Conn.ExecuteDataTableSqlDA(conn, ContractQuery, chequeParam, FilenameParam);// include params
            if (Properties != null)
            {
                logged = _tenantRepository.InsertErrorLog("Executed update query");
            }
            var data = new
            {
                status = "ok",
                statusCode = "200"
            };
            return Ok(data);
            //}
            //else
            //{
            //    var data = new
            //    {
            //        Status = "Unauthorized",
            //        StatusCode = "401",
            //        Message = "Invalid Session."
            //    };
            //    return Unauthorized(data);
            //}
        }
        [HttpPost]
        [Route("GetApproveMunicipalNew")]
        [ValidateActionParameters]
        public IActionResult GetApproveMunicipalNew([FromBody] GetOutstandingPayments requestModel)
        {
            try
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
                    var TenantId = GetTenantId(UserId.Value);
                    var contractTenantId = ContractInTenantId(ContractId, TenantId);

                    if (!contractTenantId)
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data1);
                    }
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);

                    var ApproveMunicipalQuery = $@"select link.Description from Pms_Contract con
                    left join Pms_Property prop on prop.PropertyID=con.PropertyID
                    join cms.EmirateMuncipalityLinks link on link.emirateid=prop.EmirateID
                    where ContractID={ContractId}";
                    var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, ApproveMunicipalQuery);
                    ApproveMunicipalVM Data = new ApproveMunicipalVM();
                    if (ContractTerm.Rows.Count > 0)
                    {
                        var ListToBeSend1 = Conn.ToListof<ApproveMunicipalVM>(ContractTerm);
                        Data = ListToBeSend1.FirstOrDefault();

                        var Message = Data.Description;


                        var data = new
                        {
                            Record = Message,
                            Status = "ok",
                            StatusCode = "200",
                            Message = "Record Found"
                        };
                        return Ok(data);

                    }
                    else
                    {
                        var data = new
                        {
                            Record = "",
                            Status = "ok",
                            StatusCode = "200",
                            Message = "Record Found"
                        };
                        return Ok(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Record = "",
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session."
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

        [HttpPost]
        [Route("GetApproveMunicipal")]
        [ValidateActionParameters]
        public IActionResult GetApproveMunicipal([FromBody] GetOutstandingPayments requestModel)
        {
            try
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
                    var TenantId = GetTenantId(UserId.Value);
                    var contractTenantId = ContractInTenantId(ContractId, TenantId);

                    if (!contractTenantId)
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data1);
                    }
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);

                    var ApproveMunicipalQuery = $@"select link.Description from Pms_Contract con
                    left join Pms_Property prop on prop.PropertyID=con.PropertyID
                    join cms.EmirateMuncipalityLinks link on link.emirateid=prop.EmirateID
                    where ContractID={ContractId}";
                    var ContractTerm = Conn.ExecuteDataTableSqlDA(conn, ApproveMunicipalQuery);
                    ApproveMunicipalVM Data = new ApproveMunicipalVM();
                    if (ContractTerm.Rows.Count > 0)
                    {
                        var ListToBeSend1 = Conn.ToListof<ApproveMunicipalVM>(ContractTerm);
                        Data = ListToBeSend1.FirstOrDefault();

                        var Message = Data.Description;


                        var data = new
                        {
                            Record = Message,
                            Status = "ok",
                            StatusCode = "200",
                            Message = "Record Found"
                        };
                        return Ok(data);

                    }
                    else
                    {
                        var data = new
                        {
                            Record = "",
                            Status = "ok",
                            StatusCode = "200",
                            Message = "Record Found"
                        };
                        return Ok(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Record = "",
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session."
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
        

        public byte[] ConvertImageToByteArray(string imageName)
        {
            if (imageName != null)
            {
                string wwwRootPath = _env.WebRootPath;
                string GetFilePath = Path.Combine(wwwRootPath + "/downloads/CollierContract/", imageName);
                imageName = GetFilePath;
                byte[] imageByteArray = null;
                try
                {
                    if (System.IO.File.Exists(imageName))
                    {
                        FileStream fileStream = new FileStream(imageName, FileMode.Open, FileAccess.Read);
                        using (BinaryReader reader = new BinaryReader(fileStream))
                        {
                            imageByteArray = new byte[reader.BaseStream.Length];
                            for (int i = 0; i < reader.BaseStream.Length; i++)
                                imageByteArray[i] = reader.ReadByte();
                            return imageByteArray;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }


        [HttpPost]
        [Route("DownloadContract")]
        [ValidateActionParameters]
        public async Task<ActionResult<string>> DownloadContract([FromBody] GetOutstandingPayments requestModel)
        {
            //try
            //{

            DownloadContractVm invoice = new DownloadContractVm();
            string DateFormat = _settings.DateFormat();
            ContractsVM ContractData = new ContractsVM();
            List<UnitRefVm> UnitRefVm = new List<UnitRefVm>();
            List<UnitRefVm> UnitStoreVm = new List<UnitRefVm>();
            TanentVm tanentVm = new TanentVm();
            TransactionDateVm TransactionDate = new TransactionDateVm();
            PreparedByVm PreparedBy = new PreparedByVm();
            List<ParkingRefVm> parking = new List<ParkingRefVm>();

            string UnitNoStr = @"";
            string BedroomStr = @"";
            string AreaStr = @"";
            string unitStoreStr = @"";


            #region collier contract 

            CompanyDetails companyDetails = new CompanyDetails()
            {
                CompanyName = _config["CompanyDetail:CompanyName"].ToString(),
                CompanyNameAR = _config["CompanyDetail:CompanyNameAR"].ToString(),
                CompanyShortName = _config["CompanyDetail:CompanyShortName"].ToString()
            };
            invoice.CompanyDetail = companyDetails;

            if (companyDetails.CompanyShortName == "COLLIERS")
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
                    var TenantId = GetTenantId(UserId.Value);

                    _tenantRepository.InsertErrorLog("tenant data");



                    var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantId);
                    _tenantRepository.InsertErrorLog("contract id");

                    if (!contractTenantId)
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data1);
                    }
                    var ContractQuery = $@"select top 1 Contractno, pc.TenantID, UserDefinedNo,ContractPosition, format(ContractDate,'{DateFormat}') as ContractDate, PropertyName +' /UBS#: '+BuildingNumber as Property, CityName as PropertyArea, Sector, PlotNumber as PlotNo, CONCAT(ISNULL(FORMAT(TenantShare,'#,###,##0'),0),'.00') as TenantShare, CONCAT(ISNULL(FORMAT(OccupantShare,'#,###,##0'),0),'.00') as OccupantShare, Subject, AdditionalTerms,Concat(tc.FirstName,' ', tc.MiddleName,' ', tc.FullName) as OccupiedBy ,FORMAT(fromDate,'dd-MM-yyy') as fromDate, FORMAT(ToDate,'{DateFormat}')as ToDate, format(GraceStartDate,'{DateFormat}') as GraceStartDate,CONCAT(ISNULL(FORMAT(AnnualMinRent,'#,###,##0'),0),'.00') as AnnualMinRent,  FORMAT(GraceEndDate, '{DateFormat}') as GraceEndDate , CONCAT(ISNULL(FORMAT(Rentforstay,'#,###,##0'),0),'.00') Rentforstay, CONCAT(ISNULL(FORMAT(SecurtiyDeposits,'#,###,##0'),0),'.00') SecurtiyDeposits,SDReceivedContractiD 
                             from Pms_Contract pc
                            left join Pms_Property pp on pc.PropertyID=pp.PropertyID
                            left join Pms_City pcity on pcity.CityID=pp.CityID
                            left join Pms_Tenant tc on tc.TenantID=pc.Occupant
                            where pc.ContractID={requestModel.ContractId} and pc.Suspend=0";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Contract = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);
                    _tenantRepository.InsertErrorLog("contactdata");

                    if (Contract.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractsVM>(Contract);
                        ContractData = ListToBeSend.FirstOrDefault();
                        _tenantRepository.InsertErrorLog("found contract data");

                    }

                    var TenantQuery = $@"select CONCAT(FirstName , MiddleName , FullName) as Name, FirstNameAR + MiddleNameAR + FullNameAR as NameAR , PostBoxno,  CONCAT(PhoneCountryCode, PhoneAreaCode, Phone) as TelNo,CONCAT(FaxCountryCode, FaxAreaCode, Fax) as FaxNo ,Concat(pt.MobileCountryCode, pt.MobileAreaCode, pt.Mobile) as MobileNo, pe.EmirateName, Email, TenantTypeID 
                        from Pms_Tenant pt
                        left join Pms_Emirate pe on pe.EmirateID= pt.EmirateId
                        where pe.Suspend=0 and TenantID={ContractData.TenantID}";

                    var Tenant = Conn.ExecuteDataTableSqlDA(conn, TenantQuery);
                    _tenantRepository.InsertErrorLog("tenant qurey data");


                    if (Tenant.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<TanentVm>(Tenant);
                        tanentVm = ListToBeSend.FirstOrDefault();
                        _tenantRepository.InsertErrorLog("found tenant qurey data");
                    }

                    try
                    {
                        invoice.logo = ConvertImageToByteArray("Logo.jpg");
                    }
                    catch (Exception)
                    {

                    }



                    string Position = "New";
                    if (ContractData.ContractPosition == 2)
                        Position = "Renewed";
                    if (ContractData.ContractPosition == 3)
                        Position = "Extended";
                    invoice.Position = Position;


                    #region Lesse Related Work

                    LesseDetails lesseDetails = new LesseDetails();


                    var lesseeQuery = $@"SELECT Isnull(t.FirstName,'') as FirstName,ISNULL(t.MiddleName,'') AS MiddleName,ISNULL(t.FullName,'') AS FullName,tp.MasterTenantTypeID,ISNULL(FirstNameAR,'') AS FirstNameAR, ISNULL(MiddleNameAR,'') AS MiddleNameAR, ISNULL(FullNameAR,'') AS FullNameAR
                                 FROM 
                                 Pms_Tenant t
                                 JOIN Pms_TenantType tp ON t.TenantTypeID=tp.TenantTypeID
                                 WHERE t.TenantID={TenantId}";

                    var lesseQueryResponse = Conn.ExecuteDataTableSqlDA(conn, lesseeQuery);

                    if (lesseQueryResponse.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<LesseDetails>(lesseQueryResponse);
                        lesseDetails = ListToBeSend.FirstOrDefault();
                        invoice.LesseDetails = lesseDetails;
                    }

                    #endregion



                    var UnitQuery = $@"select pu.UnitRefNo, pu.BedRooms, pu.MeasurementType , AreaSizeSqm, Areasize from Pms_ContractUnits pc
                        join Pms_Unit pu on pc.UnitID=pu.UnitID
                        where pu.Suspend=0 and pc.ContractID={requestModel.ContractId} and pu.UnitTypeID!=2 and pc.Suspend=0";

                    var Unit = Conn.ExecuteDataTableSqlDA(conn, UnitQuery);
                    _tenantRepository.InsertErrorLog("unit data");

                    if (Unit.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<UnitRefVm>(Unit);
                        UnitRefVm = ListToBeSend.ToList();
                        _tenantRepository.InsertErrorLog("unit  data found");
                    }

                    if (Unit.Rows.Count > 0)
                    {
                        foreach (var units in UnitRefVm)
                        {

                            UnitNoStr += units.UnitRefNo + ", ";

                            BedroomStr += Convert.ToString(Convert.ToInt32(units.BedRooms)) + ", ";

                            if (units.MeasurementType == "SQM")
                            {
                                AreaStr += (units.AreaSizeSqm) + " " + units.MeasurementType + ", ";
                            }
                            else
                                AreaStr += (units.Areasize) + " " + units.MeasurementType + ", ";
                        }
                    }

                    invoice.UnitNo = UnitNoStr;
                    invoice.Bedrooms = BedroomStr;
                    invoice.Area = AreaStr;




                    var UnitStoreQuery = $@"select pu.UnitRefNo, pu.BedRooms, pu.MeasurementType , AreaSizeSqm, Areasize from Pms_ContractUnits pc
                        join Pms_Unit pu on pc.UnitID=pu.UnitID
                        where pu.Suspend=0 and pc.ContractID={requestModel.ContractId} and pu.UnitTypeID=2 and pc.Suspend=0";

                    var UnitStore = Conn.ExecuteDataTableSqlDA(conn, UnitStoreQuery);
                    _tenantRepository.InsertErrorLog("UnitStoreQuery");

                    if (UnitStore.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<UnitRefVm>(UnitStore);
                        UnitStoreVm = ListToBeSend.ToList();
                        _tenantRepository.InsertErrorLog("UnitStoreQuery found");
                    }

                    if (Unit.Rows.Count > 0)
                    {
                        foreach (var store in UnitRefVm)
                        {
                            unitStoreStr += store.UnitRefNo + ",";
                        }
                    }
                    invoice.UnitStore = unitStoreStr;


                    var StoreParkingQuery = $@"select pp.ParkingRefNo from Pms_ContractParkings pc
        				join Pms_ParkingDetail pp on pp.ParkingId=pc.ParkingId where pc.suspend=0 and pc.ContractID={requestModel.ContractId}";

                    var Parking = Conn.ExecuteDataTableSqlDA(conn, StoreParkingQuery);
                    _tenantRepository.InsertErrorLog("StoreParkingQuery");

                    if (Parking.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ParkingRefVm>(Parking);
                        parking = ListToBeSend.ToList();
                        _tenantRepository.InsertErrorLog("StoreParkingQuery found");

                    }

                    string unitParkingStr = "";
                    if (Parking.Rows.Count > 0)
                    {
                        foreach (var item in parking)
                        {
                            unitParkingStr += item.ParkingRefNo + ",";
                        }
                    }
                    invoice.UnitParking = unitParkingStr;

                    #region LandLord name related work..here


                    var propertyNameQuery = $@"Select l.LandlordName,LandlordNameAR
                                        FROM  Pms_Contract  c 
                                        JOIN Pms_Property p on p.PropertyID=c.PropertyID
                                        join Pms_LandLord l on p.LandlordID=l.LandlordID
                                        WHERE c.ContractID={requestModel.ContractId}";

                    LandLordDetails landLordDetails = new LandLordDetails();

                    var propertyNameQueryResponse = Conn.ExecuteDataTableSqlDA(conn, propertyNameQuery);

                    if (propertyNameQueryResponse.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<LandLordDetails>(propertyNameQueryResponse);
                        landLordDetails = ListToBeSend.FirstOrDefault();
                        invoice.landLordDetails = landLordDetails;
                    }

                    #endregion


                    #region

                    var PurposeOfUseQuery = $@"Select c.ContractType, PurposeOfUseID as PurposeOfUseID,ct.ContractType AS ContractTypeName,ct.ContractTypeAR
                                         from Pms_Contract  c
                                            JOIN Pms_ContractType ct ON c.ContractType=ct.ContractTypeID
                                            Where ContractID={requestModel.ContractId}";
                    var response = Conn.ExecuteDataTableSqlDA(conn, PurposeOfUseQuery);

                    ContractTermsVm PurposeOfUseNm = new ContractTermsVm();

                    if (response.Rows.Count > 0)
                    {
                        var ListToBeSend1 = Conn.ToListof<ContractTermsVm>(response);
                        PurposeOfUseNm = ListToBeSend1.FirstOrDefault();
                        invoice.ContractTypeID = PurposeOfUseNm.ContractType;
                        invoice.ContractTypeName = PurposeOfUseNm.ContractTypeName;
                        invoice.ContractTypeAR = PurposeOfUseNm.ContractTypeAR;
                    }

                    #endregion

                    invoice.ContractsVM = ContractData;
                    invoice.TanentVm = tanentVm;
                    invoice.UnitRefVm = UnitRefVm;
                    invoice.UnitStoreVm = UnitStoreVm;
                    if (PreparedBy != null)
                    {
                        invoice.PreparedBy = PreparedBy.Name;
                    }

                    invoice.parking = parking;
                    if (TransactionDate != null)
                    {
                        invoice.TransactionDate = TransactionDate.TransactionDate;
                    }



                    if (invoice.ContractsVM.Contractno != null)
                    {
                        _tenantRepository.InsertErrorLog("invoice data");

                        var pdf = new ViewAsPdf("~/Views/Payments/DownloadCollierContract.cshtml", invoice)
                        {
                            FileName = invoice.ContractsVM.Contractno + ".pdf",
                            CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
                        };
                        _tenantRepository.InsertErrorLog("pdf data");

                        try
                        {
                            byte[] pdfData = await pdf.BuildFile(ControllerContext);
                            //var path = _hostEnv.WebRootPath + "\\Downloads\\Contracts\\" + pdf.FileName;
                            //System.IO.File.WriteAllBytes(path, pdfData);
                            var PdfByte = pdfData;
                            _tenantRepository.InsertErrorLog(PdfByte.ToString());

                            var data = new
                            {
                                Path = pdfData,
                                Status = "200"
                            };
                            _tenantRepository.InsertErrorLog("success");

                            return Ok(data);
                        }
                        catch (Exception ex)
                        {
                            _tenantRepository.InsertErrorLog(ex.Message + " " + ex.InnerException.ToString());

                            var data = new
                            {
                                Path = "",
                                Status = "400",
                                message = ex.Message
                            };
                            return Ok(data);
                        }


                    }
                    else
                    {
                        _tenantRepository.InsertErrorLog("data not found");

                        var data = new
                        {
                            Path = "",
                            PdfByte = "",
                            Status = "404",
                        };
                        return NotFound(data);
                    }

                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session."
                    };
                    return Unauthorized(data);
                }

                #endregion
            }

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
                var TenantId = GetTenantId(UserId.Value);
                _tenantRepository.InsertErrorLog("tenant data");

                var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantId);
                _tenantRepository.InsertErrorLog("contract id");


                if (!contractTenantId)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data1);
                }
                var ContractQuery = $@"select top 1 Contractno, pc.TenantID, UserDefinedNo,ContractPosition, format(ContractDate,'{DateFormat}') as ContractDate, PropertyName +' /UBS#: '+BuildingNumber as Property, CityName as PropertyArea, Sector, PlotNumber as PlotNo, CONCAT(ISNULL(FORMAT(TenantShare,'#,###,##0'),0),'.00') as TenantShare, CONCAT(ISNULL(FORMAT(OccupantShare,'#,###,##0'),0),'.00') as OccupantShare, Subject, AdditionalTerms,Concat(tc.FirstName,' ', tc.MiddleName,' ', tc.FullName) as OccupiedBy ,FORMAT(fromDate,'dd-MM-yyy') as fromDate, FORMAT(ToDate,'{DateFormat}')as ToDate, format(GraceStartDate,'{DateFormat}') as GraceStartDate,CONCAT(ISNULL(FORMAT(AnnualMinRent,'#,###,##0'),0),'.00') as AnnualMinRent,  FORMAT(GraceEndDate, '{DateFormat}') as GraceEndDate , CONCAT(ISNULL(FORMAT(Rentforstay,'#,###,##0'),0),'.00') Rentforstay, CONCAT(ISNULL(FORMAT(SecurtiyDeposits,'#,###,##0'),0),'.00') SecurtiyDeposits,SDReceivedContractiD 
                             from Pms_Contract pc
                            left join Pms_Property pp on pc.PropertyID=pp.PropertyID
                            left join Pms_City pcity on pcity.CityID=pp.CityID
                            left join Pms_Tenant tc on tc.TenantID=pc.Occupant
                            where pc.ContractID={requestModel.ContractId} and pc.Suspend=0";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Contract = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);
                _tenantRepository.InsertErrorLog("contactdata");

                if (Contract.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ContractsVM>(Contract);
                    ContractData = ListToBeSend.FirstOrDefault();
                    _tenantRepository.InsertErrorLog("found contract data");

                }

                var TenantQuery = $@"select CONCAT(FirstName , MiddleName , FullName) as Name, FirstNameAR + MiddleNameAR + FullNameAR as NameAR , PostBoxno,  CONCAT(PhoneCountryCode, PhoneAreaCode, Phone) as TelNo,CONCAT(FaxCountryCode, FaxAreaCode, Fax) as FaxNo ,Concat(pt.MobileCountryCode, pt.MobileAreaCode, pt.Mobile) as MobileNo, pe.EmirateName, Email, TenantTypeID 
                from Pms_Tenant pt
                left join Pms_Emirate pe on pe.EmirateID= pt.EmirateId
                where pe.Suspend=0 and TenantID={ContractData.TenantID}";

                var Tenant = Conn.ExecuteDataTableSqlDA(conn, TenantQuery);
                _tenantRepository.InsertErrorLog("tenant qurey data");


                if (Tenant.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<TanentVm>(Tenant);
                    tanentVm = ListToBeSend.FirstOrDefault();
                    _tenantRepository.InsertErrorLog("found tenant qurey data");

                }
                string Position = "New";
                if (ContractData.ContractPosition == 2)
                    Position = "Renewed";
                if (ContractData.ContractPosition == 3)
                    Position = "Extended";
                invoice.Position = Position;

                var UnitQuery = $@"select pu.UnitRefNo, pu.BedRooms, pu.MeasurementType , AreaSizeSqm, Areasize from Pms_ContractUnits pc
                        join Pms_Unit pu on pc.UnitID=pu.UnitID
                        where pu.Suspend=0 and pc.ContractID={requestModel.ContractId} and pu.UnitTypeID!=2 and pc.Suspend=0";

                var Unit = Conn.ExecuteDataTableSqlDA(conn, UnitQuery);
                _tenantRepository.InsertErrorLog("unit data");

                if (Unit.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<UnitRefVm>(Unit);
                    UnitRefVm = ListToBeSend.ToList();
                    _tenantRepository.InsertErrorLog("unit  data found");

                }


                var UnitStoreQuery = $@"select pu.UnitRefNo, pu.BedRooms, pu.MeasurementType , AreaSizeSqm, Areasize from Pms_ContractUnits pc
                        join Pms_Unit pu on pc.UnitID=pu.UnitID
                        where pu.Suspend=0 and pc.ContractID={requestModel.ContractId} and pu.UnitTypeID=2 and pc.Suspend=0";

                var UnitStore = Conn.ExecuteDataTableSqlDA(conn, UnitStoreQuery);
                _tenantRepository.InsertErrorLog("UnitStoreQuery");

                if (UnitStore.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<UnitRefVm>(UnitStore);
                    UnitStoreVm = ListToBeSend.ToList();
                    _tenantRepository.InsertErrorLog("UnitStoreQuery found");

                }

                var StoreParkingQuery = $@"select pp.ParkingRefNo from Pms_ContractParkings pc
        				join Pms_ParkingDetail pp on pp.ParkingId=pc.ParkingId where pc.suspend=0 and pc.ContractID={requestModel.ContractId}";

                var Parking = Conn.ExecuteDataTableSqlDA(conn, StoreParkingQuery);
                _tenantRepository.InsertErrorLog("StoreParkingQuery");

                if (Parking.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ParkingRefVm>(Parking);
                    parking = ListToBeSend.ToList();
                    _tenantRepository.InsertErrorLog("StoreParkingQuery found");

                }
                //25-12-2022 comment becasue not use
                //int? SDRecdContractID = ContractData.SDReceivedContractiD;
                //if (SDRecdContractID != null)
                //{
                //    if (SDRecdContractID != 0)
                //    {
                //        var TransactionDateQuery = $@"select FORMAT(TransactionDate,'{DateFormat}') as TransactionDate from Pms_Transactions where TransactionTypeID=1 and ContractID={SDRecdContractID} and Suspend=0 and Status='Confirmed' and PaymentFor=2";

                //        var GetTransactionDate = Conn.ExecuteDataTableSqlDA(conn, TransactionDateQuery);

                //        if (Parking.Rows.Count > 0)
                //        {
                //            var ListToBeSend = Conn.ToListof<TransactionDateVm>(GetTransactionDate);
                //            TransactionDate = ListToBeSend.FirstOrDefault();
                //        }

                //    }
                //    else
                //    {
                //        var TransactionDateQuery = $@"select FORMAT(TransactionDate,'{DateFormat}') as TransactionDate from Pms_Transactions where TransactionTypeID=1 and ContractID={requestModel.ContractId} and Suspend=0 and Status='Confirmed' and PaymentFor=2";

                //        var GetTransactionDate = Conn.ExecuteDataTableSqlDA(conn, TransactionDateQuery);

                //        if (Parking.Rows.Count > 0)
                //        {
                //            var ListToBeSend = Conn.ToListof<TransactionDateVm>(GetTransactionDate);
                //            TransactionDate = ListToBeSend.FirstOrDefault();
                //        }
                //    }
                //}
                //else
                //{
                //    var TransactionDateQuery = $@"select FORMAT(TransactionDate,'{DateFormat}') as TransactionDate from Pms_Transactions where TransactionTypeID=1 and ContractID={requestModel.ContractId} and Suspend=0 and Status='Confirmed' and PaymentFor=2";

                //    var GetTransactionDate = Conn.ExecuteDataTableSqlDA(conn, TransactionDateQuery);

                //    if (Parking.Rows.Count > 0)
                //    {
                //        var ListToBeSend = Conn.ToListof<TransactionDateVm>(GetTransactionDate);
                //        TransactionDate = ListToBeSend.FirstOrDefault();
                //    }
                //}

                //var PreparedByQuery = $@"select FORMAT(TransactionDate,'{DateFormat}') as TransactionDate from Pms_Transactions where TransactionTypeID=1 and ContractID={requestModel.ContractId} and Suspend=0 and Status='Confirmed' and PaymentFor=2";

                //var GetPreparedBy = Conn.ExecuteDataTableSqlDA(conn, PreparedByQuery);
                //_tenantRepository.InsertErrorLog("PreparedByQuery");

                //if (Parking.Rows.Count > 0)
                //{
                //    var ListToBeSend = Conn.ToListof<PreparedByVm>(GetPreparedBy);
                //    PreparedBy = ListToBeSend.FirstOrDefault();
                //    _tenantRepository.InsertErrorLog("PreparedByQuery found");

                //}
                invoice.ContractsVM = ContractData;
                invoice.TanentVm = tanentVm;
                invoice.UnitRefVm = UnitRefVm;
                invoice.UnitStoreVm = UnitStoreVm;
                if (PreparedBy != null)
                {
                    invoice.PreparedBy = PreparedBy.Name;
                }

                invoice.parking = parking;
                if (TransactionDate != null)
                {
                    invoice.TransactionDate = TransactionDate.TransactionDate;
                }

                if (invoice.ContractsVM.Contractno != null)
                {
                    _tenantRepository.InsertErrorLog("invoice data");

                    var pdf = new ViewAsPdf("~/Views/Payments/DownloadContract.cshtml", invoice)
                    {
                        FileName = invoice.ContractsVM.Contractno + ".pdf",
                        CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
                    };
                    _tenantRepository.InsertErrorLog("pdf data");

                    try
                    {
                        byte[] pdfData = await pdf.BuildFile(ControllerContext);
                        //var path = _hostEnv.WebRootPath + "\\Downloads\\Contracts\\" + pdf.FileName;
                        //System.IO.File.WriteAllBytes(path, pdfData);
                        var PdfByte = pdfData;
                        _tenantRepository.InsertErrorLog(PdfByte.ToString());

                        var data = new
                        {
                            Path = pdfData,
                            Status = "200"
                        };
                        _tenantRepository.InsertErrorLog("success");

                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        _tenantRepository.InsertErrorLog(ex.Message + " " + ex.InnerException.ToString());

                        var data = new
                        {
                            Path = "",
                            Status = "400",
                            message = ex.Message
                        };
                        return Ok(data);
                    }


                }
                else
                {
                    _tenantRepository.InsertErrorLog("data not found");

                    var data = new
                    {
                        Path = "",
                        PdfByte = "",
                        Status = "404",
                    };
                    return NotFound(data);
                }

            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
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


        [HttpPost]
        [Route("DownloadSignedContract")]
        [ValidateActionParameters]
        public async Task<ActionResult<string>> DownloadSignedContract([FromBody] GetOutstandingPayments requestModel)
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
                    var TenantId = GetTenantId(UserId.Value);
                    var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantId);

                    if (!contractTenantId)
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data1);
                    }
                    var GetFilePath = _config["Path:Contract"].ToString();
                    string imageName = requestModel.ContractId + ".pdf".ToString();
                    string MainDiectory = GetFilePath;
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
                        var data = new
                        {
                            Path = newImage,
                            Status = "200"
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Path = "",
                            Status = "404",
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session."
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

        [HttpPost]
        [Route("CanDownloadContract")]
        [ValidateActionParameters]
        public async Task<ActionResult<string>> CanDownloadContract([FromBody] GetOutstandingPayments requestModel)
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
                    var TenantId = GetTenantId(UserId.Value);
                    var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantId);

                    if (!contractTenantId)
                    {
                        var data1 = new
                        {
                            Status = "Invalid",
                            contract = new ContractVm(),
                            Message = "Contract not found for this user; please try with valid user."
                        };
                        return Ok(data1);
                    }
                    var GetFilePath = _config["Path:Contract"].ToString();
                    string imageName = requestModel.ContractId + ".pdf".ToString();

                    ContractDueActionStageVM model = new ContractDueActionStageVM();
                    var query = $@"select DueActionid from cms.ContractDueAction where RenewalContractid={requestModel.ContractId} and StageId>=9";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Contract = Conn.ExecuteDataTableSqlDA(conn, query);

                    if (Contract.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<ContractDueActionStageVM>(Contract);
                        model = ListToBeSend.FirstOrDefault();
                    }
                    else
                    {
                        var data = new
                        {
                            CanDownload = "0",
                            Message = "Your contract is being prepared",
                            MessageAR = "يتم تحضير عقدك",
                            Status = "404",
                            path = ""
                        };
                        return Ok(data);
                    }

                    string MainDiectory = GetFilePath;
                    var path = MainDiectory + imageName;
                    if (System.IO.File.Exists(path))
                    {
                        var data = new
                        {
                            Message = "Your contract is prepared please download",
                            MessageAR = "عقدك جاهز الرجاء التحميل",
                            CanDownload = "1",
                            Status = "200",
                            path = path
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            CanDownload = "0",
                            Message = "Your contract is being prepared",
                            MessageAR = "يتم تحضير عقدك",
                            Status = "404",
                            path = path
                        };
                        return Ok(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        StatusCode = "401",
                        Message = "Invalid Session."
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

        [Route("CaseCheckinContract")]
        [HttpPost]
        public ActionResult<string> CaseCheckinContract(CaseCheckInVM model)
        {
            //try
            //{

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
                var TenantId = GetTenantId(UserId.Value);
                var contractTenantId = ContractInTenantId(Convert.ToInt32(model.ContractId), TenantId);

                if (!contractTenantId)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data1);
                }
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                // Insert data to cms.CaseContractDetail
                var CaseServiceRequestQuery = $@"INSERT INTO CMS.CaseContractDetail(ContractId, CaseNo,CreatedOn,Type)
                    VALUES ({model.ContractId} ,  {model.CaseNo},'{DateTime.UtcNow.AddHours(4)}','Check in Contract')";
                var CaseServiceRequestQueryDetail = Conn.ExecuteDataTableSqlDA(conn, CaseServiceRequestQuery);

                var Notification = _tenantRepository.AddOtherNotificationNewSerivceRequestCase(model.CaseNo.Value, UserId);

                var Update = _tenantRepository.UpdateStageId(model.ActionDueId, 11);
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
        [Route("GeneratePMSContract")]
        [ValidateActionParameters]
        public async Task<ActionResult<string>> GeneratePMSContract(int ContractID, int TenantId)
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
                var TenantID = GetTenantId(UserId.Value);
                var contractTenantId = ContractInTenantId(ContractID, TenantID);

                if (!contractTenantId)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data1);
                }
                DownloadContractVm invoice = new DownloadContractVm();
                string DateFormat = _settings.DateFormat();
                ContractsVM ContractData = new ContractsVM();
                List<UnitRefVm> UnitRefVm = new List<UnitRefVm>();
                List<UnitRefVm> UnitStoreVm = new List<UnitRefVm>();
                TanentVm tanentVm = new TanentVm();
                TransactionDateVm TransactionDate = new TransactionDateVm();
                PreparedByVm PreparedBy = new PreparedByVm();
                List<ParkingRefVm> parking = new List<ParkingRefVm>();


                var ContractQuery = $@"select Contractno, pc.TenantID, UserDefinedNo,ContractPosition, format(ContractDate,'{DateFormat}') as ContractDate, PropertyName +' /UBS#: '+BuildingNumber as Property, CityName as PropertyArea, Sector, PlotNumber as PlotNo, CONCAT(ISNULL(FORMAT(TenantShare,'#,###,##0'),0),'.00') as TenantShare, CONCAT(ISNULL(FORMAT(OccupantShare,'#,###,##0'),0),'.00') as OccupantShare, Subject, AdditionalTerms,Concat(tc.FirstName,' ', tc.MiddleName,' ', tc.FullName) as OccupiedBy ,FORMAT(fromDate,'dd-MM-yyy') as fromDate, FORMAT(ToDate,'{DateFormat}')as ToDate, format(GraceStartDate,'{DateFormat}') as GraceStartDate,CONCAT(ISNULL(FORMAT(AnnualMinRent,'#,###,##0'),0),'.00') as AnnualMinRent,  FORMAT(GraceEndDate, '{DateFormat}') as GraceEndDate , CONCAT(ISNULL(FORMAT(Rentforstay,'#,###,##0'),0),'.00') Rentforstay, CONCAT(ISNULL(FORMAT(SecurtiyDeposits,'#,###,##0'),0),'.00') SecurtiyDeposits,SDReceivedContractiD from Pms_Contract pc
                    left join Pms_Property pp on pc.PropertyID=pp.PropertyID
                    left join Pms_City pcity on pcity.CityID=pp.CityID
                    left join Pms_Tenant tc on pc.TenantID=Occupant
                    where pc.ContractID={ContractID} and pc.Suspend=0";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Contract = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);

                if (Contract.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ContractsVM>(Contract);
                    ContractData = ListToBeSend.FirstOrDefault();

                    if (ContractData.TenantID != TenantId)
                    {
                        var data = new
                        {
                            Bytes = "",
                            Status = "404",
                            path = "",
                            message = "Unauthorized invalid tenant id",
                        };
                        return Unauthorized(data);
                    }
                }

                var TenantQuery = $@"select CONCAT(FirstName , MiddleName , FullName) as Name, FirstNameAR + MiddleNameAR + FullNameAR as NameAR , PostBoxno,  CONCAT(PhoneCountryCode, PhoneAreaCode, Phone) as TelNo,CONCAT(FaxCountryCode, FaxAreaCode, Fax) as FaxNo ,Concat(pt.MobileCountryCode, pt.MobileAreaCode, pt.Mobile) as MobileNo, pe.EmirateName, Email, TenantTypeID from Pms_Tenant pt
left join Pms_Emirate pe on pe.EmirateID= pt.EmirateId
where pe.Suspend=0 and TenantID={ContractData.TenantID}";

                var Tenant = Conn.ExecuteDataTableSqlDA(conn, TenantQuery);

                if (Tenant.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<TanentVm>(Tenant);
                    tanentVm = ListToBeSend.FirstOrDefault();
                }
                string Position = "New";
                if (ContractData.ContractPosition == 2)
                    Position = "Renewed";
                if (ContractData.ContractPosition == 3)
                    Position = "Extended";
                invoice.Position = Position;

                var UnitQuery = $@"select pu.UnitRefNo, pu.BedRooms, pu.MeasurementType , AreaSizeSqm, Areasize from Pms_ContractUnits pc
                join Pms_Unit pu on pc.UnitID=pu.UnitID
                where pu.Suspend=0 and pc.ContractID={ContractID} and pu.UnitTypeID!=2";

                var Unit = Conn.ExecuteDataTableSqlDA(conn, UnitQuery);

                if (Unit.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<UnitRefVm>(Unit);
                    UnitRefVm = ListToBeSend.ToList();
                }


                var UnitStoreQuery = $@"select pu.UnitRefNo, pu.BedRooms, pu.MeasurementType , AreaSizeSqm, Areasize from Pms_ContractUnits pc
                join Pms_Unit pu on pc.UnitID=pu.UnitID
                where pu.Suspend=0 and pc.ContractID={ContractID} and pu.UnitTypeID=2";

                var UnitStore = Conn.ExecuteDataTableSqlDA(conn, UnitStoreQuery);

                if (UnitStore.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<UnitRefVm>(UnitStore);
                    UnitStoreVm = ListToBeSend.ToList();
                }

                var StoreParkingQuery = $@"select pp.ParkingRefNo from Pms_ContractParkings pc
				join Pms_ParkingDetail pp on pp.ParkingId=pc.ParkingId where pc.suspend=0 and pc.ContractID={ContractID}";

                var Parking = Conn.ExecuteDataTableSqlDA(conn, StoreParkingQuery);

                if (Parking.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ParkingRefVm>(Parking);
                    parking = ListToBeSend.ToList();
                }
                int? SDRecdContractID = ContractData.SDReceivedContractiD;
                if (SDRecdContractID != null)
                {
                    if (SDRecdContractID != 0)
                    {
                        var TransactionDateQuery = $@"select FORMAT(TransactionDate,'{DateFormat}') as TransactionDate from Pms_Transactions where TransactionTypeID=1 and ContractID={SDRecdContractID} and Suspend=0 and Status='Confirmed' and PaymentFor=2";

                        var GetTransactionDate = Conn.ExecuteDataTableSqlDA(conn, TransactionDateQuery);

                        if (Parking.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<TransactionDateVm>(GetTransactionDate);
                            TransactionDate = ListToBeSend.FirstOrDefault();
                        }

                    }
                    else
                    {
                        var TransactionDateQuery = $@"select FORMAT(TransactionDate,'{DateFormat}') as TransactionDate from Pms_Transactions where TransactionTypeID=1 and ContractID={ContractID} and Suspend=0 and Status='Confirmed' and PaymentFor=2";

                        var GetTransactionDate = Conn.ExecuteDataTableSqlDA(conn, TransactionDateQuery);

                        if (Parking.Rows.Count > 0)
                        {
                            var ListToBeSend = Conn.ToListof<TransactionDateVm>(GetTransactionDate);
                            TransactionDate = ListToBeSend.FirstOrDefault();
                        }
                    }
                }
                else
                {
                    var TransactionDateQuery = $@"select FORMAT(TransactionDate,'{DateFormat}') as TransactionDate from Pms_Transactions where TransactionTypeID=1 and ContractID={ContractID} and Suspend=0 and Status='Confirmed' and PaymentFor=2";

                    var GetTransactionDate = Conn.ExecuteDataTableSqlDA(conn, TransactionDateQuery);

                    if (Parking.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<TransactionDateVm>(GetTransactionDate);
                        TransactionDate = ListToBeSend.FirstOrDefault();
                    }
                }

                var PreparedByQuery = $@"select FORMAT(TransactionDate,'{DateFormat}') as TransactionDate from Pms_Transactions where TransactionTypeID=1 and ContractID={ContractID} and Suspend=0 and Status='Confirmed' and PaymentFor=2";

                var GetPreparedBy = Conn.ExecuteDataTableSqlDA(conn, PreparedByQuery);

                if (Parking.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<PreparedByVm>(GetPreparedBy);
                    PreparedBy = ListToBeSend.FirstOrDefault();
                }
                invoice.ContractsVM = ContractData;
                invoice.TanentVm = tanentVm;
                invoice.UnitRefVm = UnitRefVm;
                invoice.UnitStoreVm = UnitStoreVm;
                invoice.PreparedBy = PreparedBy.Name;
                invoice.parking = parking;
                invoice.TransactionDate = TransactionDate.TransactionDate;

                // Signature and path Get
                var ImageQuery = $@"select AttachmentId as photoId,AttachmentUrl as PhotoSource, RoleTypeId,AttachmentType  
from cms.CaseContractDetail con join cms.Attachment att on att.UploadedCaseId=con.CaseNo
                        where ContractId={ContractID} and att.AttachmentType='Signature' ";
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);
                RequestSignatureVM model = new RequestSignatureVM();
                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<RequestSignatureVM>(Image);
                    model = ListToBeSend.FirstOrDefault();
                    var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    string imageName = null;
                    string MainDiectory = GetFilePath;
                    imageName = model.PhotoSource;
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
                        invoice.ImageByte = newImage;
                    }
                }


                if (invoice.ContractsVM.Contractno != null)
                {
                    var pdf = new ViewAsPdf("~/Views/Payments/DownloadContractWithSignature.cshtml", invoice)
                    {
                        FileName = ContractID.ToString() + ".pdf",
                        CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
                    };

                    try
                    {
                        byte[] pdfData = await pdf.BuildFile(ControllerContext);
                        var GetFilePath = _config["Path:Contract"].ToString();

                        var path = GetFilePath + pdf.FileName;
                        System.IO.File.WriteAllBytes(path, pdfData);

                        var PdfByte = pdfData;
                        var data = new
                        {
                            Bytes = pdfData,
                            Status = "200",
                            path = path
                        };
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        var data = new
                        {
                            Path = "",
                            Status = "400",
                            message = ex.Message
                        };
                        return Ok(data);
                    }


                }
                else
                {
                    var data = new
                    {
                        Path = "",
                        PdfByte = "",
                        Status = "404",
                    };
                    return NotFound(data);
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
        [Route("CheckPMSContract")]
        [ValidateActionParameters]
        public async Task<ActionResult<string>> CheckPMSContract(int ContractID, int TenantId)
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
                var TenantID = GetTenantId(UserId.Value);
                var contractTenantId = ContractInTenantId(ContractID, TenantID);

                if (!contractTenantId)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data1);
                }
                string DateFormat = _settings.DateFormat();
                ContractsVM ContractData = new ContractsVM();
                var ContractQuery = $@"select Contractno, pc.TenantID, UserDefinedNo,ContractPosition, format(ContractDate,'{DateFormat}') as ContractDate, PropertyName +' /UBS#: '+BuildingNumber as Property, CityName as PropertyArea, Sector, PlotNumber as PlotNo, CONCAT(ISNULL(FORMAT(TenantShare,'#,###,##0'),0),'.00') as TenantShare, CONCAT(ISNULL(FORMAT(OccupantShare,'#,###,##0'),0),'.00') as OccupantShare, Subject, AdditionalTerms,Concat(tc.FirstName,' ', tc.MiddleName,' ', tc.FullName) as OccupiedBy ,FORMAT(fromDate,'dd-MM-yyy') as fromDate, FORMAT(ToDate,'{DateFormat}')as ToDate, format(GraceStartDate,'{DateFormat}') as GraceStartDate,CONCAT(ISNULL(FORMAT(AnnualMinRent,'#,###,##0'),0),'.00') as AnnualMinRent,  FORMAT(GraceEndDate, '{DateFormat}') as GraceEndDate , CONCAT(ISNULL(FORMAT(Rentforstay,'#,###,##0'),0),'.00') Rentforstay, CONCAT(ISNULL(FORMAT(SecurtiyDeposits,'#,###,##0'),0),'.00') SecurtiyDeposits,SDReceivedContractiD from Pms_Contract pc
                    left join Pms_Property pp on pc.PropertyID=pp.PropertyID
                    left join Pms_City pcity on pcity.CityID=pp.CityID
                    left join Pms_Tenant tc on pc.TenantID=Occupant
                    where pc.ContractID={ContractID} and pc.Suspend=0";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Contract = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);

                if (Contract.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ContractsVM>(Contract);
                    ContractData = ListToBeSend.FirstOrDefault();

                    if (ContractData.TenantID != TenantId)
                    {
                        var data = new
                        {
                            Bytes = "",
                            Status = "404",
                            path = "",
                            message = "Unauthorized invalid tenant id",
                        };
                        return Unauthorized(data);
                    }
                }

                var GetFilePath = _config["Path:Contract"].ToString();
                string imageName = ContractID + ".pdf".ToString();


                string MainDiectory = GetFilePath;
                var path = MainDiectory + imageName;
                if (System.IO.File.Exists(path))
                {
                    var data = new
                    {
                        Message = "Your contract is prepared please download",
                        CanDownload = "1",
                        Status = "200",
                        path = path
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        CanDownload = "0",
                        Message = "Your contract is being prepared",
                        Status = "404",
                        path = path
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
        [Route("UpdateContractPaymentAddressNew")]
        public IActionResult UpdateContractPaymentAddressNew(ContractAddress model)
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
                var TenantId = GetTenantId(UserId.Value);
                var contractTenantId = ContractInTenantId(model.ContractId, TenantId);

                if (!contractTenantId)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data1);
                }

                //security api update start
                //   var addressParam = new SqlParameter("@Address", model.Address);

                var selfDeliveryParam = new SqlParameter("@SelfDelivery", model.SelfDelivery);

                var ContractQuery = $@"update cms.PaymentSetting set  AramexAddress=N'{model.Address}', SelfDelivery=@SelfDelivery,Confirmed=1, PaidOn=getdate()  where ContractId={model.ContractId}";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Properties = Conn.ExecuteDataTableSqlDA(conn, ContractQuery, selfDeliveryParam);
                var data = new
                {
                    status = "ok",
                    statusCode = "200"
                };
                return Ok(data);

                //security api update end

                //var ContractQuery = $@"update cms.PaymentSetting set  AramexAddress='{model.Address}', SelfDelivery='{model.SelfDelivery}',Confirmed=1 where ContractId={model.ContractId}";
                //var constring = _settings.GetConnectionString();
                //constring = constring.ToString();
                //SqlConnection conn = new SqlConnection(constring);
                //var Properties = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);
                //var data = new
                //{
                //    status = "ok",
                //    statusCode = "200"
                //};
                //return Ok(data);
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
                    Message = "Invalid Session."
                };
                return Unauthorized(data);
            }
        }


        [HttpPost]
        [Route("UpdateContractPaymentAddress")]
        public IActionResult UpdateContractPaymentAddress(ContractAddress model)
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
                var TenantId = GetTenantId(UserId.Value);
                var contractTenantId = ContractInTenantId(model.ContractId, TenantId);

                if (!contractTenantId)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data1);
                }

                //security api update start
                //   var addressParam = new SqlParameter("@Address", model.Address);

                var selfDeliveryParam = new SqlParameter("@SelfDelivery", model.SelfDelivery);

                var ContractQuery = $@"update cms.PaymentSetting set  AramexAddress=N'{model.Address}', SelfDelivery=@SelfDelivery,Confirmed=1, PaidOn=getdate()  where ContractId={model.ContractId}";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Properties = Conn.ExecuteDataTableSqlDA(conn, ContractQuery, selfDeliveryParam);
                var data = new
                {
                    status = "ok",
                    statusCode = "200"
                };
                return Ok(data);

                //security api update end

                //var ContractQuery = $@"update cms.PaymentSetting set  AramexAddress='{model.Address}', SelfDelivery='{model.SelfDelivery}',Confirmed=1 where ContractId={model.ContractId}";
                //var constring = _settings.GetConnectionString();
                //constring = constring.ToString();
                //SqlConnection conn = new SqlConnection(constring);
                //var Properties = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);
                //var data = new
                //{
                //    status = "ok",
                //    statusCode = "200"
                //};
                //return Ok(data);
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
                    Message = "Invalid Session."
                };
                return Unauthorized(data);
            }
        }

        [Route("DownloadChequeNew")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> DownloadChequeNew(RemoveContractCheque vm)
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
                var TenantId = GetTenantId(UserId.Value);
                if (TenantId == 0)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data1);
                }
                PaymentSettingVm model = new PaymentSettingVm();
                var ImageQuery = $@"select ps.PaymentSettingId, ps.Cheque from cms.PaymentSetting ps inner join Pms_Contract c on c.ContractID = ps.ContractId where PaymentSettingId={vm.PaymentSettingId} and c.TenantID ={TenantId}";

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<PaymentSettingVm>(Image);
                    model = ListToBeSend.FirstOrDefault();
                }
                if (model.Cheque != null)
                {
                    //var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    var GetFilePath = Path.Combine(_env.WebRootPath, _config["Path:ServiceRequestFiles"].ToString());

                    if (!Directory.Exists(GetFilePath))
                    {
                        Directory.CreateDirectory(GetFilePath);
                    }
                    string imageName = null;
                    string MainDiectory = GetFilePath;
                    byte[] Cheque = null;

                    imageName = model.Cheque;
                    try
                    {
                        var path = MainDiectory + imageName;
                        if (System.IO.File.Exists(path))
                        {
                            {
                                FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                                using (BinaryReader reader = new BinaryReader(fileStream))
                                {
                                    Cheque = new byte[reader.BaseStream.Length];
                                    for (int i = 0; i < reader.BaseStream.Length; i++)
                                        Cheque[i] = reader.ReadByte();
                                }
                            }

                        }
                        else
                        {
                            var data1 = new
                            {
                                Status = "Cheque not found",
                            };
                            return NotFound(data1);
                        }

                    }
                    catch (Exception ex)
                    {
                        var Exception = new
                        {
                            Status = ex.Message,
                            Path = ""
                        };
                        return BadRequest(Exception);
                    }

                    var data = new
                    {
                        Status = "200",
                        Cheque = Cheque,
                        ChequeName = model.Cheque
                    };
                    return Ok(data);
                }

                else
                {
                    var data = new
                    {
                        Status = "Cheque not found",
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


        [Route("DownloadCheque")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> DownloadCheque(RemoveContractCheque vm)
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
                var TenantId = GetTenantId(UserId.Value);
                if (TenantId == 0)
                {
                    var data1 = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(data1);
                }
                PaymentSettingVm model = new PaymentSettingVm();
                var ImageQuery = $@"select ps.PaymentSettingId, ps.Cheque from cms.PaymentSetting ps inner join Pms_Contract c on c.ContractID = ps.ContractId where PaymentSettingId={vm.PaymentSettingId} and c.TenantID ={TenantId}";

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<PaymentSettingVm>(Image);
                    model = ListToBeSend.FirstOrDefault();
                }
                if (model.Cheque != null)
                {
                    var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    string imageName = null;
                    string MainDiectory = GetFilePath;
                    byte[] Cheque = null;

                    imageName = model.Cheque;
                    try
                    {
                        var path = MainDiectory + imageName;
                        if (System.IO.File.Exists(path))
                        {
                            {
                                FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                                using (BinaryReader reader = new BinaryReader(fileStream))
                                {
                                    Cheque = new byte[reader.BaseStream.Length];
                                    for (int i = 0; i < reader.BaseStream.Length; i++)
                                        Cheque[i] = reader.ReadByte();
                                }
                            }

                        }
                        else
                        {
                            var data1 = new
                            {
                                Status = "Cheque not found",
                            };
                            return NotFound(data1);
                        }

                    }
                    catch (Exception ex)
                    {
                        var Exception = new
                        {
                            Status = ex.Message,
                            Path = ""
                        };
                        return BadRequest(Exception);
                    }

                    var data = new
                    {
                        Status = "200",
                        Cheque = Cheque,
                        ChequeName = model.Cheque
                    };
                    return Ok(data);
                }

                else
                {
                    var data = new
                    {
                        Status = "Cheque not found",
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
        public class ContractIdVm
        {
            public int ContractId { get; set; }
        }

        [Route("RemoveContractChequeNew")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> RemoveContractChequeNew([FromBody] RemoveContractCheque requestModel)
        {//int PaymentSettingId
            if (User.Identity.IsAuthenticated)
            {
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                if (UserId == null)
                {
                    var dataInvalid = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(dataInvalid);
                }
                var tenantId = GetTenantId(UserId.Value);
                var contractQuery = $@"Select ContractId From CMS.PaymentSetting where PaymentSettingId={requestModel.PaymentSettingId}";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var contract = Conn.ExecuteDataTableSqlDA(conn, contractQuery);
                ContractIdVm contractId = new ContractIdVm();
                if (contract != null && contract.Rows.Count > 0)
                {
                    var ListToBeSend1 = Conn.ToListof<ContractIdVm>(contract);
                    contractId = ListToBeSend1.FirstOrDefault();
                }
                if (contractId != null && contractId.ContractId > 0)
                {
                    var validateContract = ContractInUserId(contractId.ContractId, UserId.Value);
                    if (!validateContract)
                    {
                        var dataContract = new
                        {
                            Status = "Invalid",
                            Message = "Invalid Contract."
                        };
                        return BadRequest(dataContract);
                    }
                }
                else
                {
                    var dataContract = new
                    {
                        Status = "Invalid",
                        Message = "Invalid Contract."
                    };
                    return BadRequest(dataContract);
                }
                PaymentSettingVm model = new PaymentSettingVm();
                var ImageQuery = $@"update cms.PaymentSetting set Cheque = null where PaymentSettingId={requestModel.PaymentSettingId}";
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);
                var data = new
                {
                    Status = "200",
                    Message = "Cheque Deleted Successfully",
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

        [Route("RemoveContractCheque")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> RemoveContractCheque([FromBody] RemoveContractCheque requestModel)
        {//int PaymentSettingId
            if (User.Identity.IsAuthenticated)
            {
                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                int? UserId = _settings.GetUserIdByToken(_bearer_token);
                if (UserId == null)
                {
                    var dataInvalid = new
                    {
                        Status = "Invalid",
                        Message = "InvalidSession."
                    };
                    return Unauthorized(dataInvalid);
                }
                var tenantId = GetTenantId(UserId.Value);
                var contractQuery = $@"Select ContractId From CMS.PaymentSetting where PaymentSettingId={requestModel.PaymentSettingId}";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var contract = Conn.ExecuteDataTableSqlDA(conn, contractQuery);
                ContractIdVm contractId = new ContractIdVm();
                if (contract != null && contract.Rows.Count > 0)
                {
                    var ListToBeSend1 = Conn.ToListof<ContractIdVm>(contract);
                    contractId = ListToBeSend1.FirstOrDefault();
                }
                if (contractId != null && contractId.ContractId > 0)
                {
                    var validateContract = ContractInUserId(contractId.ContractId, UserId.Value);
                    if (!validateContract)
                    {
                        var dataContract = new
                        {
                            Status = "Invalid",
                            Message = "Invalid Contract."
                        };
                        return BadRequest(dataContract);
                    }
                }
                else
                {
                    var dataContract = new
                    {
                        Status = "Invalid",
                        Message = "Invalid Contract."
                    };
                    return BadRequest(dataContract);
                }
                PaymentSettingVm model = new PaymentSettingVm();
                var ImageQuery = $@"update cms.PaymentSetting set Cheque = null where PaymentSettingId={requestModel.PaymentSettingId}";
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);
                var data = new
                {
                    Status = "200",
                    Message = "Cheque Deleted Successfully",
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

        [HttpPost]
        [Route("DownloadOfferLetter")]
        [ValidateActionParameters]
        public async Task<ActionResult<string>> DownloadOfferLetter([FromBody] GetOutstandingPayments requestModel)
        {
            //try
            //{
            OfferLetterVm offer = new OfferLetterVm();
            string DateFormat = _settings.DateFormat();


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

                var TenantId = _tenantRepository.GetTenantId(UserId.Value);

                //security api update start
                //27-Aug-22
                var contractTenantId = ContractInTenantId(int.Parse(requestModel.ContractId), TenantId);

                if (!contractTenantId)
                {
                    var data = new
                    {
                        Status = "Invalid",
                        contract = new ContractVm(),
                        Message = "Contract not found for this user; please try with valid user."
                    };
                    return Ok(data);
                }

                var TenantProfile = _tenantRepository.GetProfile(TenantId);

                var ContractQuery = $@"SELECT Case When Isnull(Pms_Unit.ContractRenewalRentRate, 0) = 0 then IsNull(Pms_Unit.RentPerAnnumMin,0) ELSE  Isnull(Pms_Unit.ContractRenewalRentRate, 0) END as TotalPayable
	,Pms_Unit.UnitName AS UnitName
	,Pms_Contract.Contractno
,Pms_Contract.RentPaidByEntity
	,FORMAT(DATEADD(DAY, 1,ToDate) , 'dd-MM-yyyy')fromDate, FORMAT(DATEADD(year, 1,ToDate), 'dd-MM-yyyy')ToDate
	,CONCAT(PropertyName,'/UBS#: ', BuildingNumber)PropertyName
	,Pms_Property.PropertyNameAR
	,Pms_Property.PropertyImage
	,Pms_Unit.UnitRefNo
	,Pms_Unit.UnitID
	,Pms_PropertyType.PropertyType
	,Pms_PropertyType.PropertyTypeAR
	,Pms_UnitType.UnitType
	,Pms_UnitType.UnitTypeAR,
	Isnull(Pms_Unit.BedRooms, 0) as BedRooms
FROM Pms_Property
INNER JOIN Pms_Unit ON Pms_Property.PropertyID = Pms_Unit.PropertyID
INNER JOIN Pms_PropertyType ON Pms_Property.PropertyTypeID = Pms_PropertyType.PropertyTypeID
INNER JOIN Pms_UnitType ON Pms_UnitType.UnitTypeID = Pms_Unit.UnitTypeID
INNER JOIN Pms_ContractUnits ON Pms_Unit.UnitID = Pms_ContractUnits.UnitID
INNER JOIN Pms_Contract ON Pms_Contract.ContractID = Pms_ContractUnits.ContractID
WHERE (Pms_ContractUnits.Suspend = 0)
AND (Pms_Contract.ContractID = {int.Parse(requestModel.ContractId)})";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Contract = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);

                if (Contract.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<OfferLetterVm>(Contract);
                    offer = ListToBeSend.FirstOrDefault();
                    offer.TenantName = TenantProfile.Name;
                }

                if (offer.Contractno != null)
                {
                    var url = "~/Views/Payment/OfferLetter.cshtml";
                    if (offer.RentPaidByEntity == 3)
                    {
                        url = "~/Views/Payment/OfferLetterAdPolice.cshtml";
                    }
                    var pdf = new ViewAsPdf(url, offer)
                    {
                        FileName = offer.Contractno + ".pdf",
                        CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
                    };

                    try
                    {
                        byte[] pdfData = await pdf.BuildFile(ControllerContext);
                        //var path = _hostEnv.WebRootPath + "\\Downloads\\Contracts\\" + pdf.FileName;
                        //System.IO.File.WriteAllBytes(path, pdfData);
                        var PdfByte = pdfData;
                        var data = new
                        {
                            Path = pdfData,
                            Status = "200"
                        };
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        var data = new
                        {
                            Path = "",
                            Status = "400",
                            message = ex.Message
                        };
                        return Ok(data);
                    }


                }
                else
                {
                    var data = new
                    {
                        Path = "",
                        PdfByte = "",
                        Status = "404",
                    };
                    return NotFound(data);
                }

            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    StatusCode = "401",
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
    }

    public class GetPropertyImage
    {
        public string PropertyId { get; set; }
    }

    public class GetContractCheques
    {
        public string TransactionID { get; set; }
    }

    public class ConractChargeRecepts
    {
        public string ContractId { get; set; }
        public string ChargesTypeID { get; set; }
    }

    public class GetUnitDetails
    {
        public string UnitId { get; set; }
    }

    public class GetConTractSwithFilter
    {
        //  string PropertyName, int? PropertyTypeId, int? PropertyStausId, DateTime? ContractDateFrom, DateTime? ContractDateTo
        public string PropertyName { get; set; }
        public string? PropertyTypeId { get; set; }
        public string? PropertyStausId { get; set; }
        public string? ContractDateFrom { get; set; }
        public string? ContractDateTo { get; set; }

        //public DateTime? ContractDateFrom { get; set; }
        //public DateTime? ContractDateTo { get; set; }



    }

    public class RemoveContractCheque
    {
        public string PaymentSettingId { get; set; }
    }

    public class GetOutstandingPayments
    {
        public string ContractId { get; set; }
    }

    public class UpdateContractStage
    {
        public string? DueActionid { get; set; }
        public string StageId { get; set; }
    }
}