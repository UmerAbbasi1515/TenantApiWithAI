using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using FABTenantD2Api.Classes;
using System.Net.Http;

namespace FABTenantD2Api.Classes
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class EncryptionDecryptionMiddleware
    {
        private readonly RequestDelegate _next;

        public EncryptionDecryptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {

            try
            {

                if (httpContext.Request.Headers["raw"].ToString().Equals("datawithoutfile"))
                {
                    var stream = httpContext.Request.Body;// currently holds the original stream

                    using (var sr = new StreamReader(stream))
                    {

                        var originalContent = await sr.ReadToEndAsync();

                        //var modifiedContent = string.Join("", originalContent.Reverse());
                        var ds = JsonConvert.DeserializeObject<RB>(originalContent);
                        
                        if (ds != null && ds.RequestBody != null)
                        {

                            var d = new Decryption();

                            var modifiedContent = d.Decrypt(ds.RequestBody);

                            var requestContent = new StringContent(modifiedContent, Encoding.UTF8, "application/json");
                            stream = await requestContent.ReadAsStreamAsync();//modified stream

                            httpContext.Request.Body = stream;
                        }
                    }


                }


                //var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
                //stream = await requestContent.ReadAsStreamAsync();//modified stream
                //var s= await ReadRequestBody(httpContext);
                //string modified = "\r\n{\r\n    \"AreaType\":\"SQF\",\r\n    \"MinArea\":0,\r\n    \"UnitType\":\"Apartment/Flat\"\r\n  }\r\n";
                // httpContext.Request.EnableBuffering();
                // string body;
                //using (var streamReader = new System.IO.StreamReader(
                //    httpContext.Request.Body, System.Text.Encoding.UTF8, leaveOpen: true))
                //    body = await streamReader.ReadToEndAsync();

                //httpContext.Request.Body.Position = 0;

                // httpContext.Items["body"] = modified;

            }
            catch (Exception ex)
            {
                // throw ex;
            }

            await _next(httpContext);


        }
    }
    internal class RB
    {
        public string RequestBody { get; set; }
    }
    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class EncryptionDecryptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseEncryptionDecryptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EncryptionDecryptionMiddleware>();
        }
    }
}

