using Microsoft.AspNetCore.Mvc;

namespace FABTenantD2Api.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult OfferLetter()
        {

            return View();
        }
        public IActionResult OfferLetterAdPolice()
        {
            return View();
        }
    }
}
