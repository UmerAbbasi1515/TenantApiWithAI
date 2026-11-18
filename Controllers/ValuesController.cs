using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FABTenantD2Api.Controllers
{
    [Route("api/Tenant/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpGet]
        [Route("UploadServiceRequestFiles3")]
        public async Task<IActionResult> UploadServiceRequestFiles2()
        {
            return Content("1");
        }
           
    }
}
