
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FABTenantApi.Controllers
{
    public class ImagesController : Controller
    {
        private readonly Classes.Settings _appSettings;
        private readonly Classes.Settings _smsRepository;
        public ImagesController()
        {
            _smsRepository = new Classes.Settings();
            _appSettings = new Classes.Settings();

        }

        public class CheckImage
        {
            public byte[] ImageByte { get; set; }
            public string Status { get; set; }
            public string Path { get; set; }
            public string NewPath { get; set; }
        }

        public class ImageInfo
        {
            public byte[] imageByteArray{ get; set; }
            public string Path{ get; set; }
        }
        public ImageInfo ConvertImageToByteArray(string imageName)
        {
            if (imageName != null)
            {
                string MainDiectory = @"/mnt/azure/Propertyimages/";
                //string MainDiectory = @"C:\workspace\UBS\FAB_App_Develop\FABApiD2\Tenant\wwwroot\assets\images\";
                imageName = MainDiectory + imageName;
                ImageInfo model=new ImageInfo();
                model.imageByteArray = null;
                model.Path = imageName;
                try
                {
                    var path = System.IO.Path.Combine(MainDiectory, imageName);
                    if (System.IO.File.Exists(path))
                    {
                        {
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
                        return model;
                    }
                }
                catch (Exception ex)
                {
                    return model;
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
            var image = ConvertImageToByteArray("1.jpg");
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

            return View(model);
        }

       
    }
}
