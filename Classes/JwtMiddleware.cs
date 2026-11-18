using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Security.Claims;
using FABTenantApi.Classes;
using Microsoft.Extensions.Configuration;
using FABTenantApi.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Net.Http.Headers;
using System.Data;

namespace FABTenantD2Api.Classes
{
    public class SlidingExpirationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SlidingExpirationMiddleware> _logger;
        private readonly ITokenService _tokenService;
        private IConfiguration _config;
        private readonly Settings _settings;
        public SlidingExpirationMiddleware(RequestDelegate next,
            ILogger<SlidingExpirationMiddleware> logger, ITokenService tokenService, IConfiguration config)
        {
            _config = config;
            _next = next;
            _logger = logger;
            _tokenService = tokenService;
            _settings = new Settings();
        }

        // Here you can inject your services if needed
        // I've injected JwtTokenService to reissue jwt token if more than half of the timeout interval has elapsed

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                string authorization = context.Request.Headers["Authorization"];

                JwtSecurityToken token = null;
                if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer"))
                {
                    token = new JwtSecurityTokenHandler().ReadJwtToken(authorization[7..]);
                }
                // trim 'Bearer ' from the start
                var uservm = new UserSessionToken();
                if (token != null)
                {
                    var todate = token.ValidTo;
                    //var currentdate = DateTime.UtcNow.AddHours(4).Minute;
                    //var expires = currentdate - todate;
                    var userid = Convert.ToInt32(token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value);
                    var currentToken = token.EncodedPayload;
                    var usertoken = await GetAuthToken(userid, todate);
                    if (usertoken != null)
                    {
                        if (usertoken.ExpiryDate >= DateTime.UtcNow.AddHours(4))
                        {
                            context.Response.Headers.Add("Set-Authorization", usertoken.Token);
                        }
                        else if (usertoken.ExpiryDate.Date == token.ValidTo.Date)
                        {
                            int jwtIdleTime = Convert.ToInt32(_config["Jwt:IdleTime"]);
                            //if more than half of the timeout interval has elapsed.
                            var lastAccessMin = usertoken.LastAccessDate.AddMinutes(jwtIdleTime);
                            var currentDate = DateTime.UtcNow.AddHours(4);
                            if (lastAccessMin >= currentDate)
                            {
                                string jwtKey = _config["Jwt:Key"].ToString();
                                string jwtIssuer = _config["Jwt:Issuer"].ToString();

                                uservm.UserId = Convert.ToInt32(token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value);
                                uservm.RoleId = 1;
                                var newtoken = _tokenService.SessionToken(jwtKey, jwtIssuer, uservm);
                                if (!string.IsNullOrWhiteSpace(newtoken))
                                {
                                    var expirydate = token.ValidTo;
                                    if (expirydate != null)
                                    {
                                        var inserttoken =await InsertToken(newtoken, expirydate, DateTime.UtcNow.AddHours(4), uservm.UserId);
                                    }

                                }
                                context.Response.Headers.Add("Set-Authorization", newtoken);
                            }
 
                        }
                    }

                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            await _next(context);
        }
        public async Task<UserAuthToken> GetAuthToken(int UserId, DateTime? ExpiryDate)
        {
            UserAuthToken authToken = new UserAuthToken();
            //var query = $@"Select UserId, Token, ExpiryDate, LastAccessDate from App.AuthorizationToken WITH (NOLOCK)  Where ExpiryDate = @ExpiryDate and UserId = {UserId}";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            SqlCommand cmd = new SqlCommand("GetAuthToken", conn);
            cmd.CommandType = CommandType.StoredProcedure;
             cmd.Parameters.AddWithValue("@ExpiryDate", ExpiryDate == null ? DateTime.UtcNow.AddHours(4) : ExpiryDate  );

             cmd.Parameters.AddWithValue("@UserId",  UserId );

            SqlDataAdapter adp = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            adp.Fill(ds);

            if (ds.Tables[0].Rows.Count > 0)
            {
                var ListToBeSend = Conn.ToListof<UserAuthToken>(ds.Tables[0]);
                authToken = ListToBeSend.FirstOrDefault();
            }
            return await Task.FromResult(authToken);
        }
        public async Task<bool> InsertToken(string Token, DateTime expiryDate, DateTime? LastAccessDate, int UserId)
        {
            //var query = $@"INSERT INTO [App].[AuthorizationToken] ([Token] ,[ExpiryDate] ,[LastAccessDate] ,[CreatedOn], [UserId]) VALUES (@Token, @ExpiryDate, @LastAccessDate, @CreatedOn, {UserId})";
            var constring = _settings.GetConnectionString();
            constring = constring.ToString();
            //SqlConnection conn = new SqlConnection(constring);

            //var TokenParam = new SqlParameter("@Token", Token);//add paramter
            //if (string.IsNullOrWhiteSpace(Token))
            //{
            //    TokenParam = new SqlParameter("@Token", DBNull.Value);
            //}
            //var expiryDateParam = new SqlParameter("@ExpiryDate", expiryDate);
            //if (expiryDate == null)
            //{
            //    expiryDateParam = new SqlParameter("@ExpiryDate", DateTime.UtcNow.AddHours(4));
            //}//add paramter
            //var LastAccessDateParam = new SqlParameter("@LastAccessDate", LastAccessDate);
            //if (LastAccessDate == null)
            //{
            //    LastAccessDateParam = new SqlParameter("@LastAccessDate", DateTime.UtcNow.AddHours(4));
            //}
            //var CreateOnDateParam = new SqlParameter("@CreatedOn", DateTime.UtcNow.AddHours(4));
            //var temp = Conn.ExecuteDataTableSqlDA(conn, query, TokenParam, expiryDateParam, LastAccessDateParam, CreateOnDateParam);


            SqlConnection conn = new SqlConnection(constring);
            SqlCommand cmd = new SqlCommand("InsertToken", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Token", (Token == null || Token == "") ? DBNull.Value : Token);
            cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate == null ? DateTime.UtcNow.AddHours(4) : expiryDate
            );
            cmd.Parameters.AddWithValue("@LastAccessDate", LastAccessDate == null ? DateTime.UtcNow.AddHours(4) : LastAccessDate
            );
            cmd.Parameters.AddWithValue("@CreatedOn", DateTime.UtcNow.AddHours(4));
            cmd.Parameters.AddWithValue("@UserID", UserId);
            conn.Open();
            var temp = cmd.ExecuteNonQuery();
            conn.Close();
            if (temp > 0)
            {
                return await Task.FromResult(true);
            }
            return await Task.FromResult(false);
        }
    }
    public static class SlidingExpirationMiddlewareExtensions
    {
        public static IApplicationBuilder UseSlidingExpiration(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SlidingExpirationMiddleware>();
        }

    }

}
