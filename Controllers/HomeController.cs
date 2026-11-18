
using AzureFileStorageClient;
using FABTenantApi.Classes;
using FABTenantApi.Models;
using FABTenantD2Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Rotativa.AspNetCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FAB_SMS_App.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private IWebHostEnvironment _hostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Settings _settings;
        public HomeController(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _hostEnvironment = environment;
            _httpContextAccessor = httpContextAccessor;
            _settings = new Settings();
            _configuration = configuration;
            
        }

        public class CheckImage
        {
            public byte[] ImageByte { get; set; }
            public string Status { get; set; }
            public string Path { get; set; }
            public string Message { get; set; }

            public byte[] ImageByte2 { get; set; }
            public string Status2 { get; set; }
            public string Path2 { get; set; }
            public string Message2 { get; set; }

            public byte[] ImageByte3 { get; set; }
            public string Status3 { get; set; }
            public string Path3 { get; set; }
            public string Message3 { get; set; }
        }
        public class ImageInfo
        {
            public byte[] imageByteArray { get; set; }
            public string Path { get; set; }
            public string Message { get; set; }

        }

        public ImageInfo ConvertImageToByteArray(string imageName)
        {
            if (imageName != null)
            {
                string MainDiectory = @"/mnt/azure/Propertyimages/";
                //string MainDiectory = @"C:\workspace\UBS\FAB_App_Develop\FABApiD2\Tenant\wwwroot\assets\images\";
                imageName = MainDiectory + imageName;
                ImageInfo model = new ImageInfo();
                model.imageByteArray = null;
                model.Path = imageName;
                try
                {
                    var path = System.IO.Path.Combine(MainDiectory, imageName);
                    if (System.IO.File.Exists(path))
                    {
                        {
                            model.Message = "Image Found";
                            FileStream fileStream = new FileStream(imageName, FileMode.Open, FileAccess.Read);
                            using (BinaryReader reader = new BinaryReader(fileStream))
                            {
                                model.imageByteArray = new byte[reader.BaseStream.Length];
                                for (int i = 0; i < reader.BaseStream.Length; i++)
                                    model.imageByteArray[i] = reader.ReadByte();
                                return model;
                            }
                        }
                    }
                    else
                    {
                        model.Message = "Image Not Found in Path";
                        return model;
                    }
                }
                catch (Exception ex)
                {
                    model.Message = ex.Message;
                    return model;
                }
            }
            else
            {
                return null;
            }
        }

        public ImageInfo ConvertImageToByteArrayWithPath(string imageName)
        {
            if (imageName != null)
            {
                ImageInfo model = new ImageInfo();
                model.imageByteArray = null;
                model.Path = imageName;
                try
                {
                    var path = imageName;
                    if (System.IO.File.Exists(path))
                    {
                        {
                            model.Message = "Image Found";
                            FileStream fileStream = new FileStream(imageName, FileMode.Open, FileAccess.Read);
                            using (BinaryReader reader = new BinaryReader(fileStream))
                            {
                                model.imageByteArray = new byte[reader.BaseStream.Length];
                                for (int i = 0; i < reader.BaseStream.Length; i++)
                                    model.imageByteArray[i] = reader.ReadByte();
                                return model;
                            }
                        }
                    }
                    else
                    {
                        model.Message = "Image Not Found in Path";
                        return model;
                    }
                }
                catch (Exception ex)
                {
                    model.Message = ex.Message;
                    return model;
                }
            }
            else
            {
                return null;
            }
        }

        public static byte[] ConvertUnitImageToByteArray(string imageName)
        {
            if (imageName != null)
            {
                string MainDiectory = @"";
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

        public IActionResult PropertyImage()
        {
            CheckImage model = new CheckImage();
            model.Path = "";
            //var image = ConvertImageToByteArray("doc-thumb-1.jpg");
            var image = ConvertImageToByteArray("displayimage5.jpg");
            if (image != null)
            {
                model.ImageByte = image.imageByteArray;
                model.Status = "Ok";
                model.Path = image.Path;
            }
            else
            {
                model.Status = "Not OK";
                model.Path = image.Path;
            }
            model.Message = image.Message;



            string path = Path.Combine(_hostEnvironment.WebRootPath, "doc-thumb-1.jpg");
            var OurPath = ConvertImageToByteArrayWithPath(path);
            if (OurPath != null)
            {
                model.ImageByte2 = OurPath.imageByteArray;
                model.Status2 = "Ok";
                model.Path2 = OurPath.Path;
            }
            else
            {
                model.Status2 = "Not OK";
                model.Path2 = OurPath.Path;
            }
            model.Message2 = OurPath.Message;



            var ExternalPath = ConvertImageToByteArrayWithPath("");
            if (ExternalPath != null)
            {
                model.ImageByte3 = ExternalPath.imageByteArray;
                model.Status3 = "Ok";
                model.Path3 = ExternalPath.Path;
            }
            else
            {
                model.Status3 = "Not OK";
                model.Path3 = ExternalPath.Path;
            }
            model.Message3 = ExternalPath.Message;




            return View(model);
        }

        public IActionResult UnitImage()
        {
            CheckImage model = new CheckImage();

            var image = ConvertUnitImageToByteArray("Notimage.gif");
            if (image != null)
            {
                model.ImageByte = image;
                model.Status = "Ok";
            }
            else
            {
                model.Status = "Not OK";
            }

            return View(model);
        }


        public IActionResult Uploadfiletest()
        {
            return View();
        }

        //public async Task<IActionResult> UploadFile()
        //{
        //    //if (model.Image == null || model.Image.Length == 0)
        //    //    return Content("file not selected");

        //    var localFilePath = Path.Combine(_hostEnvironment.WebRootPath, "assets", "displayimage6.jpg");//local
        //    string dirName = "MainPhotos";//on azure
        //    AzureFileClient.UploadFile(dirName, localFilePath);

        //    return Content("file Uploaded");
        //}

        [HttpPost]
        public IActionResult UploadFile4()
        {
            return Content("Here not selected");
        }

        [HttpPost]
        public async Task<IActionResult> Post(IFormFile model)
        {
            if (model == null || model.Length == 0)
                return Content("file not selected");

            var uploadStream = model.OpenReadStream();
            using (var fileStream = System.IO.File.Create(Path.Join(_configuration.GetValue<string>("OutputDirectory"), Guid.NewGuid().ToString())))
            {
                await uploadStream.CopyToAsync(fileStream);
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFilePM(TestImage model)
        {
            if (model.Image == null || model.Image.Length == 0)
                return Content("file not selected");

            try
            {
                //var localFilePath = Path.Combine(_hostEnvironment.WebRootPath, "assets", "displayimage6.jpg");//local
                string dirName = "MainPhotos";//on azure
                AzureFileClient.UploadFile(dirName, model.Image.FileName, model.Image);

                return Content("file Uploaded" + model.Image.FileName);
            }
            catch (Exception ex)
            {
                return Content(ex.InnerException.Message);
            }
        }

        
        public async Task<IActionResult> UploadFile(TestImage model)
        {
            if (model.Image == null || model.Image.Length == 0)
                return Content("file not selected");

            //var path = Path.Combine( Directory.GetCurrentDirectory(), "wwwroot", model.Image.FileName);

            var GetFilePath = _configuration["Path:ServiceRequestFiles"].ToString();
            var path = GetFilePath+ model.Image.FileName;

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await model.Image.CopyToAsync(stream);
            }

            return Content("file Uploaded");
        }

        public IActionResult GetFile()
        {
            ImageVM image = new ImageVM { Name = "Test Image", ImageName = "displayimage7.jpg" };
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/assets", image.ImageName);

            ImageViewModel imageViewModel = new ImageViewModel();
            using (var stream = System.IO.File.OpenRead(path))
            {
                imageViewModel.Name = image.Name;
                imageViewModel.File = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name));
            }

            var path2 = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Uploads", imageViewModel.File.FileName);
            //var path2 = Path.Combine("/mnt/azure/servicerequestfiles/", imageViewModel.File.FileName);
            using (var stream = new FileStream(path2, FileMode.Create))
            {
                imageViewModel.File.CopyToAsync(stream);
            }

            return Content("file Uploaded");
        }



        public async Task<IActionResult> GetOtherFile()
        {
            ImageVM image = new ImageVM { Name = "Test Image", ImageName = "displayimage7.jpg" };
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/assets", image.ImageName);

            ImageViewModel imageViewModel = new ImageViewModel();

            var UploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Uploads", image.Name);

            long? UserAttachmentLength = 0;
            if (imageViewModel.File != null)
            {
                UserAttachmentLength = imageViewModel.File.Length;
            }
            string FileName = image.Name;
            if (UserAttachmentLength > 0)
            {
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await imageViewModel.File.CopyToAsync(stream);
                }
            }
            return Content("file Uploaded");
        }
        public async Task<ActionResult<string>> DownloadContract()
        {
            int ContractID = 0;
            DownloadContractVm invoice = new DownloadContractVm();
            ContactIdVm ContactIdVm = new ContactIdVm();
            string DateFormat = _settings.DateFormat();
            ContractsVM ContractData = new ContractsVM();
            List<UnitRefVm> UnitRefVm = new List<UnitRefVm>();
            List<UnitRefVm> UnitStoreVm = new List<UnitRefVm>();
            TanentVm tanentVm = new TanentVm();
            TransactionDateVm TransactionDate = new TransactionDateVm();
            PreparedByVm PreparedBy = new PreparedByVm();
            List<ParkingRefVm> parking = new List<ParkingRefVm>();


            var ContractIDQuery = $@"select top 1 ContractID from Pms_Contract";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);
            var ContractId = Conn.ExecuteDataTableSqlDA(conn, ContractIDQuery);

            if (ContractId.Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<ContactIdVm>(ContractId);
                ContactIdVm = ListToBeSend.FirstOrDefault();
                ContractID = ContactIdVm.ContractID;
            }


            var ContractQuery = $@"select Contractno, pc.TenantID, UserDefinedNo,ContractPosition, format(ContractDate,'{DateFormat}') as ContractDate, PropertyName +' /UBS#: '+BuildingNumber as Property, CityName as PropertyArea, Sector, PlotNumber as PlotNo, CONCAT(ISNULL(FORMAT(TenantShare,'#,###,##0'),0),'.00') as TenantShare, CONCAT(ISNULL(FORMAT(OccupantShare,'#,###,##0'),0),'.00') as OccupantShare, Subject, AdditionalTerms,Concat(tc.FirstName,' ', tc.MiddleName,' ', tc.FullName) as OccupiedBy ,FORMAT(fromDate,'dd-MM-yyy') as fromDate, FORMAT(ToDate,'{DateFormat}')as ToDate, format(GraceStartDate,'{DateFormat}') as GraceStartDate,CONCAT(ISNULL(FORMAT(AnnualMinRent,'#,###,##0'),0),'.00') as AnnualMinRent,  FORMAT(GraceEndDate, '{DateFormat}') as GraceEndDate , CONCAT(ISNULL(FORMAT(Rentforstay,'#,###,##0'),0),'.00') Rentforstay, CONCAT(ISNULL(FORMAT(SecurtiyDeposits,'#,###,##0'),0),'.00') SecurtiyDeposits,SDReceivedContractiD from Pms_Contract pc
                    left join Pms_Property pp on pc.PropertyID=pp.PropertyID
                    left join Pms_City pcity on pcity.CityID=pp.CityID
                    left join Pms_Tenant tc on pc.TenantID=Occupant
                    where pc.ContractID={ContractID} and pc.Suspend=0";

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


            if (invoice.ContractsVM.Contractno != null)
            {
                var pdf = new ViewAsPdf("~/Views/Payments/DownloadContract.cshtml", invoice)
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


    }
}
