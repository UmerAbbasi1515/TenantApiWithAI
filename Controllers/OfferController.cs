using FABTenantApi.Classes;
using FABTenantApi.Models;
using FABTenantD2Api.Classes;
using FABTenantD2Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Microsoft.Security.Application;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FABModels.Model;
using Microsoft.AspNetCore.Hosting;
using System.Globalization;
using System.Text.RegularExpressions;
using static FABTenantD2Api.Classes.ValidateQueryParam;

namespace FABTenantD2Api.Controllers
{
    [Route("api/Tenant/[controller]")]
    [ApiController]
    public class OfferController : ControllerBase
    {
        private IConfiguration _config;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        private readonly ConfigValues configValues;
        Decryption Decryption = new Decryption();
        private readonly IWebHostEnvironment _env;
        public OfferController(IWebHostEnvironment env, IConfiguration config,ITokenService tokenService, ITenantRepository tenantRepository)
        {
            _config = config;
            _tenantRepository = tenantRepository;
            _tokenService = tokenService;
            _settings = new Settings();
            configValues = new ConfigValues();
            _env = env;
        }

        private int GetTenantId(int UserId)
        {
            int TenantId = 0;
            TenantId = _tenantRepository.GetTenantId(UserId);
            return TenantId;
        }
        [Route("GetOffer")]
        [HttpPost]
        public ActionResult<string> GetOffer([FromBody]GetOfferVM requestModel)
        {

            if (User.Identity.IsAuthenticated)
            {
                int pageNo = int.Parse(requestModel.pageNo);
                int pageSize = int.Parse(requestModel.pageSize);

                var _bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                    int? UserId = _settings.GetUserIdByToken(_bearer_token);
                    if (UserId == null)
                    {
                        var datas = new
                        {
                            Status = "Invalid",
                            Message = "InvalidSession."
                        };
                        return Unauthorized(datas);
                    }
                    var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                    List<OfferVm> OfferList = new List<OfferVm>();

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                SqlCommand cmd = new SqlCommand("GetOffers", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@page", pageNo);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);
                cmd.Parameters.AddWithValue("@TenatId", tenantId);
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                adp.Fill(ds);
                //var GetRewalInfo = $@"select Offerid,Title,TitleAr, FORMAT(ValidateFrom,'dd-MM-yyyy') as ValidateFrom,FORMAT(ValidateTo,'dd-MM-yyyy') as ValidateTo from cms.offers";

                //var AddRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetRewalInfo);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<OfferVm>(ds.Tables[0]);
                    OfferList = ListToBeSend.ToList();
                }
                var data = new
                {
                    Statsus = "Ok",
                    record = OfferList
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

        [Route("GetOfferDetail")]
        [HttpPost]
        public ActionResult<string> GetOfferDetail([FromBody] GetOfferDetail requestModel )
        {
            int OfferId=int.Parse(requestModel.OfferId);
            if (User.Identity.IsAuthenticated)
            {
                OfferDetailVm offerDetail = new OfferDetailVm();
                List<OfferProperties> offerProperties = new List<OfferProperties>();

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);

                var GetRewalInfo = $@"select Offerid,Title,TitleAr, FORMAT(ValidateFrom,'dd-MM-yyyy') as ValidateFrom,FORMAT(ValidateTo,'dd-MM-yyyy') as ValidateTo, Description,DesriptionAR  as DescriptionAR from cms.offers where offerid={OfferId}";

                var AddRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetRewalInfo);
                if (AddRequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<OfferDetailVm>(AddRequestDetail);
                    offerDetail = ListToBeSend.FirstOrDefault();
                }

                var GetOfferPropertyInfo = $@"select pp.PropertyID, pp.PropertyName,pp.PropertyNameAR from cms.OfferProperties op
                   join Pms_Property pp on pp.PropertyID=op.propertyid
                   where op.ÓfferId={OfferId}";

                var GetOfferRequestDetail = Conn.ExecuteDataTableSqlDA(conn, GetOfferPropertyInfo);
                if (GetOfferRequestDetail.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<OfferProperties>(GetOfferRequestDetail);
                    offerProperties = ListToBeSend.ToList();
                }

                var data = new
                {
                    Statsus = "Ok",
                    record = offerDetail,
                    offerProperties = offerProperties
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
       
        #region Offers
        [HttpPost]
        [Route("GetOffersFromUbsPropT")]
        public async Task<ActionResult<string>> GetOffersFromUbsPropT()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    string DateFormat = _settings.DateFormat();
                    List<OfferModelNVM> offersData = new List<OfferModelNVM>();
                    int TenatId = 0;
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

                    TenatId = GetTenantId(UserId.Value);
                    var logged = _tenantRepository.InsertErrorLog("Tenant Id from JWT By getContracts in Tenant" + TenatId);

                    if (TenatId == 0)
                    {
                        var data = new
                        {
                            Status = "Invalid",
                            Offers = offersData,
                            Message = "Invalid User please try with valid user."
                        };
                        return Ok(data);
                    }
                    var constring = _settings.GetPropTConnectionString();

                    SqlConnection conn = new SqlConnection(constring);
                    TenatId = 413;
                    SqlCommand cmd = new SqlCommand("FAB_PROC_OFFERS_GET_OFFERSN", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TenatId", TenatId);
                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    adp.Fill(ds);
                       if (ds.Tables[0].Rows.Count > 0)
                    {
                        var ListToBeSend = Conn.ToListof<OfferModelNVM>(ds.Tables[0]);
                        offersData = ListToBeSend;
                        var data = new
                        {
                            Status = "Ok",
                            Offers = offersData,
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
                            Offers = offersData,
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

        [Route("AcceptOffersUbsPropT")]
        [HttpPost]
        public ActionResult<string> AcceptOffersUbsPropT(GetOfferIDNo requestModel)
        {
            if (!User.Identity.IsAuthenticated)
            {
                var data = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                };

                return Unauthorized(data);
            }

            int OfferId = int.Parse(requestModel.OfferId);

            var bearerToken = Request.Headers[HeaderNames.Authorization]
                .ToString()
                .Replace("Bearer ", "");

            int? UserId = _settings.GetUserIdByToken(bearerToken);

            if (UserId == null)
            {
                var data = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                };

                return Unauthorized(data);
            }

            var tenantid = GetTenantId(UserId.Value);
            var userid = UserId;

            var constring = _settings.GetPropTConnectionString();

            int affectedRows = 0;

            using (SqlConnection conn = new SqlConnection(constring))
            {
                conn.Open();

                string query = @"
            UPDATE Pms_Offer
            SET OfferStatusID = @OfferStatusID
            WHERE OfferID = @OfferId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@OfferStatusID", SqlDbType.Int).Value = 3;
                    cmd.Parameters.Add("@OfferId", SqlDbType.Int).Value = OfferId;

                    affectedRows = cmd.ExecuteNonQuery();
                }
            }

            if (affectedRows > 0)
            {
                var MessageAr = "تم قبول عرضك";
                var offerNo = requestModel.OfferNo;

                var AddNotification = _tenantRepository.SendPushNotification(
                    UserId.Value,
                    "Your offer has been accepted",
                    "Offer " + offerNo,
                    "Your Offer has been accepted",
                    MessageAr,
                    MessageAr,
                    "Tenant",
                    OfferId,
                    "Offer"
                );

                var data = new
                {
                    Status = "Ok",
                    Message = "offer accepted successfully"
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
        [Route("RejectOffersUbsPropT")]
        [HttpPost]
        public ActionResult<string> RejectOffersUbsPropT(GetOfferIDNo requestModel)
        {
            if (!User.Identity.IsAuthenticated)
            {
                var data = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                };

                return Unauthorized(data);
            }

            int OfferId = int.Parse(requestModel.OfferId);

            var bearerToken = Request.Headers[HeaderNames.Authorization]
                .ToString()
                .Replace("Bearer ", "");

            int? UserId = _settings.GetUserIdByToken(bearerToken);

            if (UserId == null)
            {
                var data = new
                {
                    Status = "Invalid",
                    Message = "InvalidSession."
                };

                return Unauthorized(data);
            }

            var tenantid = GetTenantId(UserId.Value);
            var userid = UserId;

            var constring = _settings.GetPropTConnectionString();

            int affectedRows = 0;

            using (SqlConnection conn = new SqlConnection(constring))
            {
                conn.Open();

                string query = @"
            UPDATE Pms_Offer
            SET OfferStatusID = @OfferStatusID
            WHERE OfferID = @OfferId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@OfferStatusID", SqlDbType.Int).Value = 4;
                    cmd.Parameters.Add("@OfferId", SqlDbType.Int).Value = OfferId;

                    affectedRows = cmd.ExecuteNonQuery();
                }
            }

            if (affectedRows > 0)
            {
                var MessageAr = "تم رفض عرضك.";
                var offerNo = requestModel.OfferNo;

                var AddNotification = _tenantRepository.SendPushNotification(
                    UserId.Value,
                    "Your offer has been rejected",
                    "Offer " + offerNo,
                    "Your Offer has been rejected",
                    MessageAr,
                    MessageAr,
                    "Tenant",
                    OfferId,
                    "Offer"
                );

                var data = new
                {
                    Status = "Ok",
                    Message = "Offer rejected successfully"
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
        #endregion

        #region Communication

        [Route("GetOfferTicketReplies")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetOfferTicketReplies([FromBody] GetOfferDetail requestModel)
        {
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
                var userid = UserId;
                
                //security api update end
                var TicketReplyQuery = $@"select  Id as TicketReplyId,Reply, Format(DateTime, 'dd-MM-yyyy hh:mm tt') as DateTime,TenantId as UserId, FileName from PMS.TicketReply where OfferId={int.Parse(requestModel.OfferId)} and ReplyTo=1";
                var constring = _settings.GetPropTConnectionString();

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
        }



        //i got top 1 user from Pms_LoginMembers instead of cms.user because offers and ticket reply is indexer pms soo
        [Route("AddOfferTicketReply")]
        [HttpPost]
        [RequestSizeLimit(15000000)]
        //[Consumes("multipart/form-data")]
        public async Task<IActionResult> AddOfferTicketReply([FromForm] AddOfferTicketVm model)
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

                    model.OfferId = Decryption.Decrypt(model.OfferId);
                    model.Reply = Decryption.Decrypt(model.Reply);
                    var userid = UserId;

                    _tenantRepository.InsertErrorLog("Tenant Ticket Reply Api Called 1");
                   
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
                    
                    var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                    var constring = _settings.GetPropTConnectionString();

                    SqlConnection conn = new SqlConnection(constring);
                    string Filname = null;
                    //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };//point no 10
                    _tenantRepository.InsertErrorLog("Ticket Reply Api Called 3");
                    if (model.File != null)
                    {
                        _tenantRepository.InsertErrorLog("Uploading File 1");
                        //var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();

                        var GetFilePath = Path.Combine(_env.WebRootPath, _config["Path:ServiceRequestFiles"].ToString());
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
                    var AddReplyQuery = $@"INSERT INTO PMS.TicketReply(OfferId,Reply, DateTime, UserId,TenantId,FileName,ReplyTo)
				                                VALUES ({model.OfferId},N'{model.Reply}','{DateTime.UtcNow.AddHours(4)}',(select top 1 LoginMemberID from Pms_LoginMembers),{tenantId},@FileName,1);";
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
                        _tenantRepository.InsertErrorLog("after offer reply insert");
                    }
                    catch (Exception EX)
                    {

                        _tenantRepository.InsertErrorLog(EX.Message);
                    }

                    string Subject = "Offer ID " + model.OfferId;
                    string Message = "Reply submitted against offer No " + model.OfferId;
                    //added 22-12-2022 ar
                    string SubjectAr = model.OfferId + "القضية رقم ";
                    string MessageAr = model.OfferId + "تم تقديم الرد ضد القضية رقم";
                    try
                    {
                        _tenantRepository.SendPushNotification(UserId.Value, Message, Message, Subject, SubjectAr, MessageAr, "Tenant", int.Parse(model.OfferId), "offer");
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


        [Route("DownloadOfferTicketFile")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> DownloadOfferTicketFile([FromBody] DownloadTicketFile requestModel)
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
                var ImageQuery = $@"select Id as photoId,FileName as PhotoSource from  PMS.TicketReply  
                                    where Id={ticketReplyId}";

                var constring = _settings.GetPropTConnectionString();

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
                    //var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    var GetFilePath = Path.Combine(_env.WebRootPath, _config["Path:ServiceRequestFiles"].ToString());

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

        #endregion

        #region Document
        [Route("GetOfferDocuments")]
        [HttpPost]
        [ValidateActionParameters]
        public ActionResult<string> GetOfferDocuments([FromBody] GetOfferID requestModel)
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
                var userid = UserId;

                //requestModel.Code = _tenantRepository.GetCategoryCode(int.Parse(requestModel.OfferId)).ToString();
                List<AttachmentOfferFileVm> model = new List<AttachmentOfferFileVm>();

                var ImageQuery = $@"SELECT 
                	  DocumentID,
                	  Title,
                	  OriginalFiles,
                	  PhysicalFolder,
                	  AddedDate,
                	  DocumentType
                
                FROM Pms_Document where DocumentType = 'offer' 
                 AND MasterID = @OfferId";

                var offerId = new SqlParameter("@OfferId", int.Parse(requestModel.OfferId));
                var constring = _settings.GetPropTConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery, offerId);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<AttachmentOfferFileVm>(Image);
                    model = ListToBeSend.ToList();
                }


                if (model != null)
                {
                    var data = new
                    {
                        Status = "200",
                        Record = model,
                        //ResponseMessage = _settings.PaymentMessage,
                        //CaseStageInfo = _tenantRepository.CaseStageInfo(int.Parse(requestModel.OfferId)),
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

        [HttpPost]
        [Route("UploadOfferFiles")]
        [RequestSizeLimit(15000000)]
        public async Task<IActionResult> UploadOfferFiles([FromForm] UploadOfferDocumentRequest model)

        {
            try
            {
                var logged = _tenantRepository.InsertErrorLog("UploadServiceRequestFiles start");
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
                    logged = _tenantRepository.InsertErrorLog("upload offer file after authentication");
                    var expirydate = "";
                    if (!model.ExpiryDate.IsNullOrEmpty())
                    {
                        model.ExpiryDate = Decryption.Decrypt(model.ExpiryDate);
                        var expdate = DateTime.ParseExact(model.ExpiryDate, "dd-MM-yyyy", null);
                        expirydate = expdate.ToString("dd-MM-yyyy");
                        _tenantRepository.InsertErrorLog(expirydate);
                    }
                    if (!model.DocumentType.IsNullOrEmpty())
                    {
                        model.DocumentType = Decryption.Decrypt(model.DocumentType);
                    }
                    if (!model.MasterID.IsNullOrEmpty())
                    {
                        model.MasterID = Decryption.Decrypt(model.MasterID);
                    }
                    if (!model.Title.IsNullOrEmpty())
                    {
                        model.Title = Decryption.Decrypt(model.Title);
                    }
                    if (!model.TitleAR.IsNullOrEmpty())
                    {
                        model.TitleAR = Decryption.Decrypt(model.TitleAR);
                    }
                    if (!model.Suspend.ToString().IsNullOrEmpty())
                    {
                        model.Suspend = Decryption.Decrypt(model.Suspend.ToString());
                    }
                    if (!model.AddedBy.IsNullOrEmpty())
                    {
                        model.AddedBy = Decryption.Decrypt(model.AddedBy);
                    }
                    if (!model.OriginalFiles.IsNullOrEmpty())
                    {
                        model.OriginalFiles = Decryption.Decrypt(model.OriginalFiles);
                    }
                    var tenantId = _tenantRepository.GetTenantId(UserId.Value);
                    var userid = UserId;

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
                        var constring = _settings.GetPropTConnectionString();
                       
                        SqlConnection conn = new SqlConnection(constring);

                        //var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                        var GetFilePath = Path.Combine(_env.WebRootPath, _config["Path:ServiceRequestFiles"].ToString());

                        if (!Directory.Exists(GetFilePath))
                        {
                            Directory.CreateDirectory(GetFilePath);
                        }
                        
                        //var allowedFiles = new string[] { ".jpg", ".png", ".pdf", ".doc", ".docx", ".HEIC", ".tiff", ".heic", ".jpeg", ".avif", ".bmp" };//point no 10
                        foreach (var item in model.File)
                        {

                            var ext = Path.GetExtension(item.FileName).Substring(1);
                            
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

                            logged = _tenantRepository.InsertErrorLog("afterFileStream");
                            var AddDocumentQuery = $@"
                                INSERT INTO Pms_Document
                                (
                                    DocumentType,
                                    MasterID,
                                    Title,
                                    TitleAR,
                                    AddedDate,
                                    AddedBy,
                                    Suspend,
                                    OriginalFiles
                                )
                                VALUES
                                (
                                    @DocumentType,
                                    @MasterID,
                                    @Title,
                                    @TitleAR,
                                    '{DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd HH:mm:ss.fff")}',
                                    @AddedBy,
                                    @Suspend,
                                    @OriginalFiles
                                );";
                            

                            var docTpye = new SqlParameter("@DocumentType", model.DocumentType);
                            var masterId = new SqlParameter("@MasterID", model.MasterID);
                            var suspended = new SqlParameter("@Suspend", model.Suspend);
                            var orignalFile = new SqlParameter("@OriginalFiles", Filename);
                            var filenameParam = new SqlParameter("@Title", model.Title);
                            var filenameARParam = new SqlParameter("@TitleAR", model.TitleAR);
                            if (string.IsNullOrWhiteSpace(Filename))
                            {
                                filenameParam = new SqlParameter("@Title", DBNull.Value);
                            }
                            if (string.IsNullOrWhiteSpace(Filename))
                            {
                                filenameARParam = new SqlParameter("@TitleAR", DBNull.Value);
                            }
                            
                            var addedByParam = new SqlParameter("@AddedBy", model.AddedBy);
                            if (string.IsNullOrWhiteSpace(model.AddedBy))
                            {
                                addedByParam = new SqlParameter("@AddedBy", DBNull.Value);
                            }
                            var expireDateParam = new SqlParameter("@ExpireDate", DBNull.Value);

                            var RequestDetail = Conn.ExecuteDataTableSqlDA(conn, AddDocumentQuery,
                                docTpye, masterId, suspended, orignalFile,filenameParam,filenameARParam,addedByParam);
                            if (RequestDetail.Rows.Count > 0)
                            {
                                logged = _tenantRepository.InsertErrorLog("RequestDetail.Rows.Count");

                                var ListToBeSend = Conn.ToListof<InsertedPhotoVM>(RequestDetail);
                                selected = ListToBeSend.FirstOrDefault();
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

        [Route("DownloadOfferDocuments")]
        [HttpPost]
        public ActionResult<string> DownloadOfferDocuments([FromBody] DownloadofferFilesRequestModel req)
        {
             int OfferId = int.Parse(req.OfferId);
             int DocumentId = int.Parse(req.DocumentId);

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
                var userid = UserId;
               
                List<DownloadofferFiles> model = new List<DownloadofferFiles>();
                var ImageQuery = $@"
                              select MasterID as OfferId, DocumentID as photoId,OriginalFiles as PhotoSource,DocumentType From Pms_Document
                                where  MasterID = @OfferId AND DocumentID = @DocumentId";
                var OfferIdParam = new SqlParameter("@OfferId", OfferId);
                var DocumentIdParam = new SqlParameter("@DocumentId", DocumentId);

                var constring = _settings.GetPropTConnectionString();

                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery, OfferIdParam, DocumentIdParam);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<DownloadofferFiles>(Image);
                    model = ListToBeSend.ToList();
                }

                ServiceRequestFileResponse Response = new ServiceRequestFileResponse();
                if (model != null)
                {
                    string imageName = null;
                    //var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    var GetFilePath = Path.Combine(_env.WebRootPath, _config["Path:ServiceRequestFiles"].ToString());

                   
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

        #endregion
    }

    public class GetOfferID
    {
        public string OfferId { get; set; }
    }

    public class  GetOfferIDNo
    {
        public string OfferId { get; set; }
        public string OfferNo { get; set; }
    }
    public class DownloadofferFilesRequestModel
    {
        public string OfferId { get; set; }
        public string DocumentId { get; set; }
    }
    public class DownloadofferFiles
    {
        public int photoId { get; set; }
        public string PhotoSource { get; set; }
        public string AttachmentType { get; set; }
        public int OfferId { get; set; }
        
    }
    
    public class    UploadOfferDocumentRequest
    {
        public string DocumentType { get; set; }
        public string MasterID { get; set; }

        public string Title { get; set; }

        public string TitleAR { get; set; }

        public string AddedBy { get; set; }

        public string Suspend { get; set; }

        public string OriginalFiles { get; set; }

        [AllowedExtensions(new string[] { ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".doc", ".gif", ".apng", ".bmp", ".pjpeg" })]
        public List<IFormFile> File { get; set; }

        public string ExpiryDate { get; set; }
    }

    public class GetOfferDetail
    {
        public string OfferId { get; set; }
    }


    public class AddOfferTicketVm
    {
        public string OfferId { get; set; }
        // [RegularExpression(@"^[a-zA-Z0-9 ,'-_.]*$", ErrorMessage = "Special characters are not allowed.")]
        public string Reply { get; set; }
        [AllowedExtensions(new string[] { ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".doc", ".gif", ".apng", ".bmp", ".pjpeg" })]
        public IFormFile File { get; set; }
    }
}
