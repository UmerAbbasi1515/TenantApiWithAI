using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ganss.XSS;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace FABTenantD2Api.Classes
{
    public class AntiXssMiddleware
    {
        private readonly RequestDelegate _next;
        private ErrorResponse _error;
        private readonly int _statusCode = (int)HttpStatusCode.BadRequest;

        public AntiXssMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            // Check XSS in URL
            if (!string.IsNullOrWhiteSpace(context.Request.Path.Value))
            {
                var url = context.Request.Path.Value;

                if (CrossSiteScriptingValidation.IsDangerousString(url, out _))
                {
                    await RespondWithAnError(context).ConfigureAwait(false);
                    return;
                }
            }

            // Check XSS in query string
            if (!string.IsNullOrWhiteSpace(context.Request.QueryString.Value))
            {
                var queryString = WebUtility.UrlDecode(context.Request.QueryString.Value);

                if (CrossSiteScriptingValidation.IsDangerousString(queryString, out _))
                {
                    await RespondWithAnError(context).ConfigureAwait(false);
                    return;
                }
            }

            // Check XSS in request content
            var originalBody = context.Request.Body;
            try
            {
                var content = await ReadRequestBody(context);

                if (CrossSiteScriptingValidation.IsDangerousString(content, out _))
                {
                    await RespondWithAnError(context).ConfigureAwait(false);
                    return;
                }
                await _next(context).ConfigureAwait(false);
            }
            finally
            {
                context.Request.Body = originalBody;
            }
        }

        private static async Task<string> ReadRequestBody(HttpContext context)
        {
            var buffer = new MemoryStream();
            await context.Request.Body.CopyToAsync(buffer);
            context.Request.Body = buffer;
            buffer.Position = 0;

            var encoding = Encoding.UTF8;

            var requestContent = await new StreamReader(buffer, encoding).ReadToEndAsync();
            context.Request.Body.Position = 0;

            return requestContent;
        }

        private async Task RespondWithAnError(HttpContext context)
        {
            context.Response.Clear();
            context.Response.Headers.AddHeaders();
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = _statusCode;

            if (_error == null)
            {
                _error = new ErrorResponse
                {
                    Description = "Error from AntiXssMiddleware",
                    ErrorCode = 500
                };
            }

            await context.Response.WriteAsync(_error.ToJSON());
        }
    }

    public static class AntiXssMiddlewareExtension
    {
        public static IApplicationBuilder UseAntiXssMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AntiXssMiddleware>();
        }
    }
    public static class SqlReservedKeywords
    {

        public static bool IsExist(string param)
        {
            var data = false;
            foreach (var item in SqlServerKeywords)
            {
                if(param.ToUpper().Contains(item))
                {
                    return true;
                }
            }
            return data;
        }

        private static HashSet<string> SqlServerKeywords = new HashSet<string>();

        static SqlReservedKeywords()
        {
            SqlServerKeywords.Add("ADD");
            SqlServerKeywords.Add("EXISTS");
            SqlServerKeywords.Add("PRECISION");
            SqlServerKeywords.Add("SELECT");
            SqlServerKeywords.Add("DELETE");
            SqlServerKeywords.Add("CREATE");
            SqlServerKeywords.Add("INSERT");
            SqlServerKeywords.Add("TRUNCATE");
            SqlServerKeywords.Add("UPDATE");
            SqlServerKeywords.Add("EXEC");
            SqlServerKeywords.Add("PIVOT");
            SqlServerKeywords.Add("WITH");
            SqlServerKeywords.Add("EXECUTE");
            SqlServerKeywords.Add("PLAN");
            SqlServerKeywords.Add("WRITETEXT");
            SqlServerKeywords.Add("CONCATE");
        }
    }
    
    /// <summary>
    /// Imported from System.Web.CrossSiteScriptingValidation Class
    /// </summary>
    public static class CrossSiteScriptingValidation
    {
        private static readonly char[] StartingChars = { '<', '&' };

        #region Public methods
        private static bool ContainsSpecialChars(string value)
        {
            var list = new[] { "~",  "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "=", "\"", "<", ">",  ";", ":" }; //"`",
            return list.Any(value.Contains);
        }
        public static bool RegexEmailCheck(string input)
        {
            // returns true if the input is a valid email
            return Regex.IsMatch(input, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }
        public static bool IsDangerousString(string s, out int matchIndex)
        {

            matchIndex = 0;
            if (string.IsNullOrWhiteSpace(s))
            {
                return false;
            }
            //bool inComment = false;
            var sanitiser = new HtmlSanitizer();
            var santized = sanitiser.Sanitize(s);
            if (s != santized)
            {
                return true;
            }
            bool stringIsValid = ContainsSpecialChars(s);
            if (stringIsValid)
            {
                return true;
            }

            for (var i = 0; ;)
            {

                // Look for the start of one of our patterns 
                var n = s.IndexOfAny(StartingChars, i);

                // If not found, the string is safe
                if (n < 0) return false;

                // If it's the last char, it's safe 
                if (n == s.Length - 1) return false;

                matchIndex = n;

                switch (s[n])
                {
                    case '<':
                        // If the < is followed by a letter or '!', it's unsafe (looks like a tag or HTML comment)
                        if (IsAtoZ(s[n + 1]) || s[n + 1] == '!' || s[n + 1] == '/' || s[n + 1] == '?')
                            return true;
                        break;
                    case '&':
                        // If the & is followed by a #, it's unsafe (e.g. S) 
                        if (s[n + 1] == '#')
                            return true;
                        break;
                }

                // Continue searching
                i = n + 1;
            }
        }
        public static bool IsDangerousParameter(string s, out int matchIndex)
        {

            matchIndex = 0;

            //commented temporary on 12/10/2022
            if (s == "null")
            {
                return true;
            }
            if (string.IsNullOrWhiteSpace(s))
            {
                return false;
            }

            /*Commneted all due to the assumsion add already in sql injection Handle*/
            /*var validParam = SqlReservedKeywords.IsExist(s);
            if (validParam)
            {
                return validParam;
            }*/

            //bool inComment = false;
            s = s.Replace("'", "''");
            s = s.Replace("[", "[[]");
            s = s.Replace("%", "[%]");
            s = s.Replace("_", "[_]");
            s = s.Replace("&", "and");
            var sanitiser = new HtmlSanitizer();
            var santized = sanitiser.Sanitize(s);
            if (s != santized)
            {
                return true;
            }
            bool stringIsValid = ContainsSpecialChars(s);
            if (stringIsValid)
            {
                return true;
            }

            for (var i = 0; ;)
            {

                // Look for the start of one of our patterns 
                var n = s.IndexOfAny(StartingChars, i);

                // If not found, the string is safe
                if (n < 0) return false;

                // If it's the last char, it's safe 
                if (n == s.Length - 1) return false;

                matchIndex = n;

                switch (s[n])
                {
                    case '<':
                        // If the < is followed by a letter or '!', it's unsafe (looks like a tag or HTML comment)
                        if (IsAtoZ(s[n + 1]) || s[n + 1] == '!' || s[n + 1] == '/' || s[n + 1] == '?')
                            return true;
                        break;
                    case '&':
                        // If the & is followed by a #, it's unsafe (e.g. S) 
                        if (s[n + 1] == '#')
                            return true;
                        break;

                }

                // Continue searching
                i = n + 1;
            }
        }

        #endregion

        #region Private methods

        private static bool IsAtoZ(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z' || (c >= '0' && c <= '9'));
        }

        #endregion

        public static void AddHeaders(this IHeaderDictionary headers)
        {
            if (headers["P3P"].IsNullOrEmpty())
            {
                headers.Add("P3P", "CP=\"IDC DSP COR ADM DEVi TAIi PSA PSD IVAi IVDi CONi HIS OUR IND CNT\"");
            }
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }
        public static string ToJSON(this object value)
        {
            return JsonConvert.SerializeObject(value);
        }
    }

    public class ErrorResponse
    {
        public int ErrorCode { get; set; }
        public string Description { get; set; }
    }

}