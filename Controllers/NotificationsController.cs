using FABTenantApi.Classes;
using FABTenantApi.Models;
using FABTenantD2Api.Classes;
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
    public class NotificationsController : ControllerBase
    {
        private IConfiguration _config;
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;
        private readonly Settings _settings;
        public NotificationsController(IConfiguration config, ITokenService tokenService, ITenantRepository tenantRepository)
        {
            _config = config;
            _tenantRepository = tenantRepository;
            _tokenService = tokenService;
            _settings = new Settings();
        }
        [HttpPost]
        [Route("Get")]
        public async Task<ActionResult<string>> Get([FromBody] getNotificaion requestModel)
        {
            try
            {
                int pageNo = int.Parse(requestModel.pageNo);
                int pageSize = int.Parse(requestModel.pageSize);
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
                    if (!string.IsNullOrWhiteSpace(requestModel.status) && CrossSiteScriptingValidation.IsDangerousParameter(requestModel.status, out _))
                    {
                        var dataBadReq = new
                        {
                            Status = "BadRequest",
                            Message = "Invalid Request.",
                            StatusCode = "500"
                        };
                        return BadRequest(dataBadReq);
                    }
                    string search = string.Empty;
                    var notifications = _tenantRepository.GetNotifications(search, requestModel.status, UserId.Value, pageNo, pageSize);
                    if (notifications != null && notifications.Count() > 0)
                    {
                        var data = new
                        {
                            StatusCode = "200",
                            Status = "Ok",
                            notifications = notifications,
                            Message = notifications.Count() + " Record Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var nodata = new
                        {
                            StatusCode = "404",
                            Status = "NoData",
                            notifications = notifications,
                            Message = "No Record Found."
                        };
                        return NotFound(nodata);
                    }

                }
                else
                {
                    var data = new
                    {
                        StatusCode = "401",
                        Status = "Unauthorized",
                        Message = "Invalid Session.",
                        notifications = new List<NotificationVm>()
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
        [Route("GetDetails")]
        public ActionResult<string> GetDetails([FromBody] GetNotificationFiles requestModel)
        {
            try
            {
                int NotificationId = int.Parse(requestModel.NotificationId);
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
                    var notification = _tenantRepository.GeNotificationDetails(NotificationId, UserId.Value);
                    if (notification != null)
                    {
                        var data = new
                        {
                            StatusCode = "200",
                            Status = "Ok",
                            notification = notification,
                            Message = "Record Found."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var nodata = new
                        {
                            StatusCode = "404",
                            Status = "NoData",
                            notifications = notification,
                            Message = "No Record Found."
                        };
                        return NotFound(nodata);
                    }

                }
                else
                {
                    var data = new
                    {
                        StatusCode = "401",
                        Status = "Unauthorized",
                        Message = "Invalid Session.",
                        notification = new NotificationVm()
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
        [Route("Read")]
        public IActionResult Read([FromBody] NotificationModel notification)
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
                var model = ReaadNotication(notification.NotificationId, UserId);
                if (model == true)
                {
                    var data = new
                    {
                        status = "Ok",
                        statusCode = "200",
                        message = "Notification marked as read."
                    };
                    return Ok(data);
                }
                else
                {
                    var data = new
                    {
                        Status = "Error",
                        statusCode = "500",
                        Message = "Error while reading record please try again later."
                    };
                    return NotFound(data);
                }
            }
            else
            {
                var data = new
                {
                    Status = "Unauthorized",
                    statusCode = "401",
                    Message = "Invalid Token."
                };
                return Unauthorized(data);
            }
        }
        private bool ReaadNotication(int NotificationId, int? UserId)
        {
            bool read = false;
            read = _tenantRepository.ReadNotification(NotificationId, UserId);
            return read;
        }
        [HttpPost]
        [Route("Archived")]
        public IActionResult Archived([FromBody] NotificationModel notification)
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
                    var model = ReaadNotication(notification.NotificationId, UserId);
                    if (model == true)
                    {
                        var data = new
                        {
                            status = "Ok",
                            statusCode = "200",
                            message = "Notification archived successfully."
                        };
                        return Ok(data);
                    }
                    else
                    {
                        var data = new
                        {
                            Status = "Error",
                            statusCode = "500",
                            Message = "Error while reading record please try again later."
                        };
                        return NotFound(data);
                    }
                }
                else
                {
                    var data = new
                    {
                        Status = "Unauthorized",
                        statusCode = "401",
                        Message = "Invalid Token."
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
        private bool ArchivedNotification(int NotificationId, int? UserId)
        {
            bool read = false;
            read = _tenantRepository.ReadNotification(NotificationId, UserId);
            return read;
        }

        [Route("GetNotificationFiles")]
        [HttpPost]
        public ActionResult<string> GetNotificationFiles([FromBody] GetNotificationFiles requestModel)
        {
            int NotificationId = int.Parse(requestModel.NotificationId);
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

                List<NotificationFilesVm> model = new List<NotificationFilesVm>();
                var ImageQuery = $@"select fileid,FileSource, UploadOn from app.NotificationFiles files
                                left join app.Notifications noti on files.NotificationId=noti.NotificationId
                                where files.NotificationId={NotificationId} and UserId={UserId}";

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<NotificationFilesVm>(Image);
                    model = ListToBeSend.ToList();
                }


                if (model != null)
                {
                    var data = new
                    {
                        Status = "200",
                        Record = model,
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

        [Route("DownloadNotificationFile")]
        [HttpPost]
        public ActionResult<string> DownloadNotificationFile([FromBody] DownloadNotificationFile requestModel)
        {
            int FileId = int.Parse(requestModel.FileId);
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

                List<NotificationFilesVm> model = new List<NotificationFilesVm>();
                var ImageQuery = $@"select fileid,FileSource, UploadOn from app.NotificationFiles files left join app.Notifications noti on  noti.NotificationId=files.NotificationId where files.FileId={FileId} and noti.UserId={UserId}";

                var constring = _settings.GetConnectionString();
                constring = constring.ToString();
                SqlConnection conn = new SqlConnection(constring);
                var Image = Conn.ExecuteDataTableSqlDA(conn, ImageQuery);

                if (Image.Rows.Count > 0)
                {
                    var ListToBeSend = Conn.ToListof<NotificationFilesVm>(Image);
                    model = ListToBeSend.ToList();
                }

                NotificationFileResponse Response = new NotificationFileResponse();
                if (model != null)
                {
                    string imageName = null;
                    var GetFilePath = _config["Path:ServiceRequestFiles"].ToString();
                    string MainDiectory = GetFilePath;
                    foreach (var item in model)
                    {

                        imageName = item.FileSource;
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
                return Ok(data);
            }
        }
    }

    public class getNotificaion
    {
        public string status { get; set; }
        public string pageNo { get; set; } = "1";
        public string pageSize { get; set; } = "20";
    }
   

    public class GetNotificationFiles
    {
        public string NotificationId { get; set; }
    }

    public class DownloadNotificationFile
    {
        public string FileId { get; set; }
    }
}
