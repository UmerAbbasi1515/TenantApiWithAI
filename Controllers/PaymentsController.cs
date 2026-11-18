using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FABTenantApi.Classes;
using FABTenantApi.Models;
using FABTenantD2Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Rotativa.AspNetCore;

namespace FABTenantApi.Controllers
{
    [Route("api/Tenant/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private IConfiguration _config;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        private readonly IHostingEnvironment _hostEnv;
        private IWebHostEnvironment _env;

        public PaymentsController(IConfiguration config, ITokenService tokenService, ITenantRepository tenantRepository, IHostingEnvironment hostingEnvironment,

            IWebHostEnvironment webHostEnvironment)
        {
            _config = config;
            _tenantRepository = tenantRepository;
            _tokenService = tokenService;
            _settings = new Settings();
            _hostEnv = hostingEnvironment;
            _env= webHostEnvironment;
        }
        private int GetTenantId(int UserId)
        {
            int TenantId = 0;
            TenantId = _tenantRepository.GetTenantId(UserId);
            return TenantId;
        }



        [HttpPost]
        [Route("GetPaymentMethod")]
        public ActionResult<string> GetPaymentMethod()
        {
            try
            {
                List<PaymentMethodVm> ChargesData = new List<PaymentMethodVm>();
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
                            Charges = ChargesData,
                            Message = "Invalid User please try with valid user."
                        };
                        return NotFound(dataInvalid);
                    }
                    var ChargeQuery = $@"select PaymentMethodId,Name from cms.PaymentMethod";
                    var constring = _settings.GetConnectionString();
                    constring = constring.ToString();
                    SqlConnection conn = new SqlConnection(constring);
                    var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery);

                    if (Charges.Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<PaymentMethodVm>(Charges);
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
        [Route("Get")]
        public ActionResult<string> Get([FromBody] PaymentRequestModel paymentRequestModel)
        {


            //try
            //{
            List<TransactionVm> ChargesData = new List<TransactionVm>();
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
                if (FABTenantD2Api.Classes.CrossSiteScriptingValidation.IsDangerousParameter(paymentRequestModel.SearchParam, out _))
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

                var TenantId = GetTenantId(UserId.Value);
                if (TenantId == 0)
                {
                    var dataInvalid = new
                    {
                        Status = "Invalid",
                        StatusCode = "404",
                        Charges = ChargesData,
                        Message = "Invalid User please try with valid user."
                    };
                    return NotFound(dataInvalid);
                }
                //var ChargeQuery = $@"
                //            SELECT TOP 100 t.ContractID
                //             , Format(t.TransactionDate, '{DateFormat}') AS PaymentDate
                //             , p.PropertyName
                //             , Isnull(p.BuildingNumber, '') AS PropertyNumber
                //             , TransactionId
                //             , IsNull(t.TransactionNo, '') AS ReceiptNo
                //             , IsNull(t.AccountNo, '') AS AccountNo
                //             , IsNull(t.AccountTitle, '') AS AccountTitle
                //             , IsNull(t.PaymentAmount, 0) AS Amount
                //             , Isnull(pk.PaymentKind, '') AS PaymentType
                //             , Isnull(pk.PaymentKindAR, '') AS PaymentTypeAR
                //             , Format(c.ContractDate, '{DateFormat}') AS ContractDate
                //             , IsNull(tt.FullName, '') AS Tenant
                //             , Isnull((
                //               SELECT TOP 1 UnitNo
                //               FROM Pms_Unit u
                //               WHERE u.PropertyID = p.PropertyID
                //               ), '') AS UnitNo
                //             , Isnull(c.Contractno, '') AS ContractNo
                //             , Isnull(land.LandlordName, '') AS LandLord
                //             , IsNull(pc.CityName, '') AS City
                //             , Isnull(pt.PaymentFor, '') AS PaymentFor
                //             , Isnull(pt.PaymentForAR, '') AS PaymentForAR
                //            FROM [dbo].[Pms_Transactions] AS t
                //            LEFT JOIN Pms_Contract c ON c.ContractID = t.ContractID
                //            LEFT JOIN Pms_Tenant tt ON tt.TenantID = c.TenantID
                //            LEFT JOIN Pms_Property p ON p.PropertyID = t.PropertyID
                //            LEFT JOIN Pms_LandLord land ON land.LandlordID = p.PropertyID
                //            LEFT JOIN Pms_City pc ON pc.CityID = p.CityID
                //            LEFT JOIN Pms_PaymentKinds pk ON pk.PaymentKindID = t.PaymentKind
                //            LEFT JOIN Pms_ReasonOfPayment pt ON pt.ReasonofPaymentID = t.PaymentFor
                //            WHERE c.TenantID = {TenantId} AND
                //            c.Suspend = 0
                //             AND t.TransactionTypeID = 1
                //             AND pt.Suspend = 0
                //            ORDER BY t.TransactionDate DESC";


                int pageNo = int.Parse(paymentRequestModel.Page);
                int pageSize = int.Parse(paymentRequestModel.PageSize);

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                DataSet ds = new DataSet();
                using (SqlConnection conn = new SqlConnection(constring))
                {
                    //var Charges = Conn.ExecuteDataTableSqlDA(conn, ChargeQuery);
                    using (SqlCommand cmd = new SqlCommand("SP_GetPayments", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@page", pageNo);
                        cmd.Parameters.AddWithValue("@pageSize", pageSize);
                        cmd.Parameters.AddWithValue("@TenantId", TenantId);
                        cmd.Parameters.AddWithValue("@SearchText", paymentRequestModel.SearchParam);
                        SqlDataAdapter adp = new SqlDataAdapter(cmd);
                        adp.Fill(ds);
                    }
                }
                if (ds.Tables[0].Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<TransactionVm>(ds.Tables[0]);
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

        [HttpGet]
        [Route("Test")]
        public async Task<ActionResult<string>> Test()
        {
            return Ok();
        }


        public byte[] ConvertImageToByteArray(string imageName)
        {
            if (imageName != null)
            {
                string wwwRootPath = _env.WebRootPath;
                //var GetFilePath = _config["Path:ServiceRequestFile"].ToString();
                string GetFilePath = Path.Combine(wwwRootPath + "/Logo/", imageName);
                imageName = GetFilePath;

                byte[] imageByteArray = null;

                try
                {

                    if (System.IO.File.Exists(imageName))
                    {
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
        [Route("DownloadReceipt")]
        public async Task<ActionResult<string>> DownloadReceipt([FromBody] DownloadReceipt requestModel)
        {
            int transactionid = int.Parse(requestModel.transactionid);
            //try { 
            InvoiceVm invoice = new InvoiceVm();

            InvoiceTransactionVm InvoiceData = new InvoiceTransactionVm();
            InvoiceContractDate InvoiceContractData = new InvoiceContractDate();
            PaymentForVm PaymentVm = new PaymentForVm();
            List<ChequesVM> Cheques = new List<ChequesVM>();
            //var routeData = new RouteData();

            //security api update start
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
            // add 15-12-2022 because not need to check
            //var isValidUserForCase = TransactionInUserId(transactionid, UserId.Value);

            //if (!isValidUserForCase)
            //{
            //    var data = new
            //    {
            //        Status = "Invalid",
            //        Message = "Not a Valid User For Transaction."
            //    };
            //    return Unauthorized(data);
            //}

            //security api update end

            //var fakeControllerContext = new ControllerContext();
            if (User.Identity.IsAuthenticated)
            {
                //    var ReceiptsQuery = $@"select TransactionNo,PaymentAmount,FORMAT(TransactionDate,'dd-MM-yyyy') as TransactionDate, ptr.Description,ContractID, PayeeName, kind.PaymentKind,  plm.FirstName +' ' +plm.LastName as PreparedBy, TransactionTypeID, PaymentFor,ActionID, PrintStatus, pro.PropertyName, PropertyAddress  from Pms_Transactions ptr
                //     left join Pms_LoginMembers plm on plm.LoginMemberId=ptr.PreparedBy
                //     left join Pms_PaymentKinds kind on ptr.PaymentKind=kind.PaymentKindID
                //left join Pms_Property pro on pro.PropertyID=ptr.PropertyID
                //     where ptr.Suspend=0 and plm.suspend=0 and TransactionID={transactionid}";




                var ReceiptsQuery = $@"select TransactionNo,PaymentAmount,FORMAT(TransactionDate,'dd-MM-yyyy') as TransactionDate, ptr.Description,ContractID, PayeeName, kind.PaymentKind, TransactionTypeID, PaymentFor,ActionID, PrintStatus, pro.PropertyName, PropertyAddress  from Pms_Transactions ptr
                
                 left join Pms_PaymentKinds kind on ptr.PaymentKind=kind.PaymentKindID
            left join Pms_Property pro on pro.PropertyID=ptr.PropertyID
                 where ptr.Suspend=0 and TransactionID={transactionid}";
                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Charges = Conn.ExecuteDataTableSqlDA(conn, ReceiptsQuery);

                if (Charges.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<InvoiceTransactionVm>(Charges);
                    InvoiceData = ListToBeSend.FirstOrDefault();
                }

                var ContractQuery = $@"select pt.FirstName +' '+pt.MiddleName+' '+pt.FullName as Name, PropertyName, BuildingNumber, FORMAT(fromDate,'dd-MM-yyyy') as fromDate, FORMAT(ToDate,'dd-MM-yyyy') as ToDate, Contractno from Pms_Contract pc
                   left join Pms_Tenant pt on pc.TenantID= pt.TenantID
                   left join Pms_Property pro on pro.PropertyID= pc.PropertyID
                   where pt.Suspend=0 and pc.Suspend=0 and ContractID={InvoiceData.ContractID}";

                var Property = Conn.ExecuteDataTableSqlDA(conn, ContractQuery);

                if (Charges.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<InvoiceContractDate>(Property);
                    InvoiceContractData = ListToBeSend.FirstOrDefault();
                }


                var PaymentForQuery = $@"select PaymentFor from Pms_ReasonOfPayment where Suspend= 0 and ReasonofPaymentID={InvoiceData.PaymentFor}";

                var PaymentFor = Conn.ExecuteDataTableSqlDA(conn, PaymentForQuery);

                if (Charges.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<PaymentForVm>(PaymentFor);
                    PaymentVm = ListToBeSend.FirstOrDefault();
                }

                var ChequeQuery = $@"select pc.ChequeNo as ChequeNo, FORMAT(ChequeDate,'dd-MM-yyyy') as ChequeDate, pb.BankName as BankName, pc.ChequeAmount as ChequeAmount  from Pms_Cheques pc
              left join Pms_BankNames pb on pc.BankNameID=pb.BankNameID
              where pc.Suspend=0 and pb.Suspend=0 and pc.TransactionID={transactionid}";

                var Cheque = Conn.ExecuteDataTableSqlDA(conn, ChequeQuery);

                if (Charges.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<ChequesVM>(Cheque);
                    Cheques = ListToBeSend.ToList();
                }
                invoice.Transaction = InvoiceData;
                invoice.ContractVM = InvoiceContractData;
                invoice.PaymentFor = PaymentVm;
                invoice.Cheques = Cheques;

                try
                {
                    invoice.logo = ConvertImageToByteArray("Logo.jpg");
                }
                catch (Exception)
                {
                   
                }


                if (invoice.Transaction.TransactionNo != null)
                {
                    var pdf = new ViewAsPdf("~/Views/Payments/DownloadInvoice.cshtml", invoice)
                    {
                        FileName = invoice.Transaction.TransactionNo + ".pdf",
                        CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 8"
                    };

                    try
                    {
                        byte[] pdfData = await pdf.BuildFile(ControllerContext);
                        //var path = _hostEnv.WebRootPath + "\\Downloads\\Receipts\\" + pdf.FileName;
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

        private bool TransactionInUserId(int transactionid, int userId)
        {
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var Contracts = Conn.ExecuteDataTableSqlDA(conn, $@"SELECT   TOP (1)     1 FROM Pms_Transactions INNER JOIN
                         Pms_Contract ON Pms_Transactions.ContractID = Pms_Contract.ContractID INNER JOIN
                         Pms_Tenant ON Pms_Contract.TenantID = Pms_Tenant.TenantID INNER JOIN
                         App.UserRoles ON Pms_Tenant.TenantID = App.UserRoles.ProfileId
WHERE        (App.UserRoles.UserId = {userId}) AND (App.UserRoles.RoleId = 1) AND TransactionID = {transactionid}");

            return Contracts.Rows.Count > 0;
        }


    }

    public class DownloadReceipt
    {
        public string transactionid { get; set; }
    }
}
