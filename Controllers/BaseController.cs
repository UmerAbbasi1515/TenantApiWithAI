using Azure;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Pms.Controllers;
using System;
using System.Collections.Generic;
using System.IO;

namespace Pms.Controllers
{
    public class BaseController : Controller
    {
        public int UserID { get; set; }
        public int LoginHistoryID { get; set; }
        public int ReportPageSize { get; set; }
        public int DefaultPageSize { get; set; }
        public int UserGroupID { get; set; }
        public Dictionary<string, bool> Permissions { get; set; }

        public Dictionary<string, bool> ExtendedPermissions { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {

            LogToFile("public override void OnActionExecuting(ActionExecutingContext context)");

            var userId = HttpContext.Session.GetString("userId");
            var userLogin = userId;

            if (userLogin == null || userLogin == "")
            {
                LogToFile("LoginMemberID not found in session. Redirecting to /Account/Login");

                context.Result = new RedirectToRouteResult(new { controller = "Account", action = "Login", Area = "" });
            }
            else
            {
                if (int.TryParse(userLogin, out int userIdN))
                {
                    UserID = userIdN;
                }
                //LoginHistoryID = context.HttpContext.Session.GetInt32("LoginHistoryID").Value;
                //UserGroupID = context.HttpContext.Session.GetInt32("GroupID").Value;
                //ReportPageSize = context.HttpContext.Session.GetInt32("report_page_size").Value;
                //DefaultPageSize = context.HttpContext.Session.GetInt32("default_page_size").Value;

                LogToFile($"User session data loaded - UserID: {UserID}, LoginHistoryID: {LoginHistoryID}, GroupID: {UserGroupID}");
                ViewData["UserId"] = UserID;

                //Permissions = GetCurrentUrlPermisions(context);
                //ExtendedPermissions = GetExtendedCurrentUrlPermisions(context);
                base.OnActionExecuting(context);
            }
        }

        //private Dictionary<string, bool> GetCurrentUrlPermisions(ActionExecutingContext context)
        //{
        //    var area = context.RouteData.Values["area"]?.ToString();
        //    var controller = context.RouteData.Values["controller"]?.ToString();
        //    var action = context.RouteData.Values["action"]?.ToString();

        //    if (controller == "Contract" && action == "EditRenewal")
        //    {
        //        action = "Edit";
        //    }

        //    var CurrentUrl = (string.IsNullOrEmpty(area) ? "/" : area) + $"/{controller}/{action}";

        //    LogToFile($"Checking permissions for URL: '{CurrentUrl}'");

        //    var menuSimple = context.HttpContext.Session.Get<List<MenuPermissionViewModel>>("menuSimple");


        //    if (menuSimple == null)
        //    {
        //        LogToFile("Session 'menuSimple' is null during permission check.");
        //        return new Dictionary<string, bool>();
        //    }

        //    var currentUrlPermissions = menuSimple.Where(p => p.ActionUrl == CurrentUrl || p.ParentUrl == CurrentUrl).Select(s => s.Action).Distinct()?.ToDictionary(name => name, name => true);

        //    LogToFile($"Permissions found: {string.Join(", ", currentUrlPermissions.Keys)}");

        //    return currentUrlPermissions ?? new Dictionary<string, bool>();
        //}


        //private Dictionary<string, bool> GetExtendedCurrentUrlPermisions(ActionExecutingContext context)
        //{
        //    var area = context.RouteData.Values["area"]?.ToString();
        //    var controller = context.RouteData.Values["controller"]?.ToString();
        //    var action = context.RouteData.Values["action"]?.ToString();


        //    string CurrentUrl = string.Empty;

        //    CurrentUrl = (string.IsNullOrEmpty(area) ? "/" : area) + $"/{controller}/{action}";

        //    var menuSimple = context.HttpContext.Session.Get<List<MenuPermissionViewModel>>("menuSimple");

        //    var currentUrlPermissions = menuSimple.Where(p => p.ActionUrl == CurrentUrl || p.ParentUrl == CurrentUrl).Select(s => s.ActionUrl).Distinct()?.ToDictionary(name => name, name => true);

        //    LogToFile($"Permissions found: {string.Join(", ", currentUrlPermissions.Keys)}");

        //    return currentUrlPermissions ?? new Dictionary<string, bool>();
        //}

        private void LogToFile(string message)
        {
            try
            {
                string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "login_log.txt");

                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logging failed: {ex.Message}");
            }
        }
    }
}
