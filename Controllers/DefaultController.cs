using FABModels.Model;
using FABModels.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantApi.Controllers
{
    public class DefaultController : Controller
    {
        private readonly FABAppDbContext _dbContext;
        public DefaultController(FABAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        [Route("/error-development")]
        public IActionResult HandleErrorDevelopment(
    [FromServices] IHostEnvironment hostEnvironment)
        {
            if (!hostEnvironment.IsDevelopment())
            {
                return NotFound();
            }

            var exceptionHandlerFeature =
                HttpContext.Features.Get<IExceptionHandlerFeature>()!;

            return Problem(
                detail: exceptionHandlerFeature.Error.StackTrace,
                title: exceptionHandlerFeature.Error.Message);
        }

        [Route("/error")]
        public IActionResult HandleError() =>
            Problem();
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(string UserName, string Password)
        {
            var loginVm = new LoginVm();
            loginVm.Password = Password;
            loginVm.UserName = UserName;
            if (ModelState.IsValid)
            {
                List<LoginData> logins = new List<LoginData>();
                var newAdminLogin = new LoginData
                {
                    Email = "apiadmin@fabapp.live",
                    Password = "JRK<~5as6]tX3",
                    UserId = 1
                };
                logins.Add(newAdminLogin);
                var newUserLogin = new LoginData
                {
                    Email = "apiuser1@fabapp.live",
                    Password = "Fcf*69my5WQLT",
                    UserId = 2
                };
                logins.Add(newUserLogin);
                List<string> ipLists = _dbContext.LoginIpAddresses.Where(p => p.Active == true).Select(p => p.IpAddress).ToList();
                 var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;
                //if(!ipLists.Contains(remoteIpAddress.ToString()))
                //{
                //    return Json(new { response = "Denied", message = "Access denied please try again later." });
                //}
                var user = logins.Where(p => p.Email == loginVm.UserName && p.Password == loginVm.Password).FirstOrDefault();
                if (user == null)
                {
                    return Json(new { response = "Invalid", message = "Invalid user name or password." });
                }
                else
                {
                    HttpContext.Session.SetInt32("UserId", user.UserId);
                    HttpContext.Session.SetString("UserName", user.Email);
                    HttpContext.Session.SetString("Email", user.Email);
                    return Json(new { response = "Success", message = "Login success redirecting to dashboard." });
                }
            }
            else
            {
                return Json(new { response = "Failed", message = "Sorry something went wrong" });
            }

        }
        public class LoginData
        {
            public int UserId { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
        }
        public class IpList
        {
            public string IpAddress { get; set; }
        }
    }
}
