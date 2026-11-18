using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class AutoLogMiddleWare
{
    private readonly RequestDelegate _next;

    public AutoLogMiddleWare(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            string route = context.Request.Path.Value;
            string httpStatus = "0";

            // Log Request
            var originalRequestBody = context.Request.Body;
            originalRequestBody.Seek(0, SeekOrigin.Begin);
            string requestBody =await new StreamReader(originalRequestBody).ReadToEndAsync();
            originalRequestBody.Seek(0, SeekOrigin.Begin);

            // Log Response
            string responseBody = string.Empty;
            using (var swapStream = new MemoryStream())
            {

                var originalResponseBody = context.Response.Body;
                context.Response.Body = swapStream;
                await _next(context);
                swapStream.Seek(0, SeekOrigin.Begin);
                responseBody = new StreamReader(swapStream).ReadToEnd();
                swapStream.Seek(0, SeekOrigin.Begin);
                await swapStream.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;
                httpStatus = context.Response.StatusCode.ToString();
            }

            // Clean route
            string cleanRoute = route;
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                cleanRoute = cleanRoute.Replace(c, '-');
            }

            StringBuilder sbRequestHeaders = new StringBuilder();
            foreach (var item in context.Request.Headers)
            {
                sbRequestHeaders.AppendLine(item.Key + ": " + item.Value.ToString());
            }

            StringBuilder sbResponseHeaders = new StringBuilder();
            foreach (var item in context.Response.Headers)
            {
                sbResponseHeaders.AppendLine(item.Key + ": " + item.Value.ToString());
            }


            string filename = DateTime.UtcNow.AddHours(4).ToString("yyyyMMdd.HHmmss.fff") + "_" + httpStatus + "_" + cleanRoute + ".log";
            StringBuilder sbLog = new StringBuilder();
            sbLog.AppendLine("Status: " + httpStatus + " - Route: " + route);
            sbLog.AppendLine("=============");
            sbLog.AppendLine("Request Headers:");
            sbLog.AppendLine(sbRequestHeaders.ToString());
            sbLog.AppendLine("=============");
            sbLog.AppendLine("Request Body:");
            sbLog.AppendLine(requestBody);
            sbLog.AppendLine("=============");
            sbLog.AppendLine("Response Headers:");
            sbLog.AppendLine(sbResponseHeaders.ToString());
            sbLog.AppendLine("=============");
            sbLog.AppendLine("Response Body:");
            sbLog.AppendLine(responseBody);
            sbLog.AppendLine("=============");

            var path = Directory.GetCurrentDirectory();
            string filepath = ($"{path}\\Logs\\{filename}");
            File.WriteAllText(filepath, sbLog.ToString());
        }
        catch (Exception ex)
        {
            // It cannot cause errors no matter what
        }

    }
}

public class EnableRequestRewindMiddleware
{
    private readonly RequestDelegate _next;

    public EnableRequestRewindMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Request.EnableBuffering();
        await _next(context);
    }
}

public static class EnableRequestRewindExtension
{
    public static IApplicationBuilder UseEnableRequestRewind(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EnableRequestRewindMiddleware>();
    }
}