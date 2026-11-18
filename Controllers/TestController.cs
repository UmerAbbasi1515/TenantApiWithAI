using FABTenantD2Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace FABTenantD2Api.Controllers
{
    [Route("api/Tenant/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {

        [HttpPost]
        [Route("UploadFile")]
        public async Task<IActionResult> UploadFiles([FromForm] TestImage model)
        {
            if (model.Image == null || model.Image.Length == 0)
                return Content("file not selected");

            //var path = Path.Combine( Directory.GetCurrentDirectory(), "wwwroot", model.Image.FileName);

            var path = Path.Combine("/mnt/azure/servicerequestfiles/", model.Image.FileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await model.Image.CopyToAsync(stream);
            }
            var data = new
            {
                Status = "file Uploaded",
            };
            return Ok(data);
        }

        [HttpGet]
        [Route("UploadTestFile")]
        public IActionResult UploadTestFile()
        {
            
                return Ok("Done");//
            
        }
    }
}
