using FABTenantApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FABTenantApi.Classes
{
    
    public class Settings
    {
        public string PaymentMessage = "Your documents have been approved";
        public string DateFormat()
        {
            string Format = "dd-MM-yyyy";
            return Format;
        }

        public string GetConnectionString()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var env = "production";
            if (environment != null)
            {
                env = environment;
            }
            if (env == "production")
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                  .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                  .AddJsonFile("appsettings.json", optional: false)
                  .AddJsonFile("appsettings.production.json", optional: false)
                  .Build();
                return configuration.GetConnectionString("FABModels");
            }
            else
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
               .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
               .AddJsonFile("appsettings.json", optional: false)
               .Build();
                return configuration.GetConnectionString("FABModels");
            }

        }
        public string GetPropTConnectionString()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var env = "production";
            if (environment != null)
            {
                env = environment;
            }
            if (env == "production")
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                  .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                  .AddJsonFile("appsettings.json", optional: false)
                  .AddJsonFile("appsettings.production.json", optional: false)
                  .Build();
                return configuration.GetConnectionString("FMConnection");
            }
            else
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
               .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
               .AddJsonFile("appsettings.json", optional: false)
               .Build();
                return configuration.GetConnectionString("FMConnection");
            }

        }
        public int? GetUserIdByToken(string _bearer_token)
        {
            var handler = new JwtSecurityTokenHandler();
            var tokenS = handler.ReadToken(_bearer_token) as JwtSecurityToken;
            var CheckToken = tokenS.Claims.Where(claim => claim.Type.Contains("userdata")).FirstOrDefault();
            if (CheckToken != null)
            {

                var checkRole = GetRoleIdByToken(_bearer_token);
                if (checkRole != null && checkRole == 1)
                {
                    var Value = CheckToken.Value;
                    return Convert.ToInt32(Value);
                }
                else
                {
                    return null;
                }

            }
            return null;
        }
        //need to relace for token idel time 10-02-2022
        //public int? GetUserIdByToken(string _bearer_token)
        //{
        //    var handler = new JwtSecurityTokenHandler();
        //    var jsonToken = handler.ReadToken(_bearer_token);
        //    var tokenS = handler.ReadToken(_bearer_token) as JwtSecurityToken;
        //    var todate = tokenS.ValidTo;
        //    var isvalid = ValidateDbToken(_bearer_token).Result;
        //    if (!isvalid)
        //    {
        //        return null;
        //    }
        //    //var currentdate = DateTime.UtcNow.AddHours(4).Minute;
        //    //var expires = currentdate - todate;
        //    var userid = Convert.ToInt32(tokenS.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value);
        //    var token = GetAuthToken(userid, todate).Result;
        //    if (tokenS.ValidTo >= DateTime.UtcNow.AddHours(4))
        //    {
        //        var CheckToken = tokenS.Claims.Where(claim => claim.Type.Contains("userdata")).FirstOrDefault();
        //        if (CheckToken != null)
        //        {

        //            var checkRole = GetRoleIdByToken(_bearer_token);
        //            if (checkRole != null && checkRole == 1)
        //            {
        //                var Value = CheckToken.Value;
        //                UpdateAuthToken(tokenS.EncodedPayload, tokenS.ValidTo, DateTime.UtcNow.AddHours(4), Convert.ToInt32(Value));
        //                return Convert.ToInt32(Value);
        //            }
        //            else
        //            {
        //                return null;
        //            }
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //    else if (token != null)
        //    {
        //        if (token.ExpiryDate.Date == tokenS.ValidTo.Date)
        //        {

        //            //if more than half of the timeout interval has elapsed.
        //            var lastAccessMin = token.LastAccessDate.AddMinutes(10);
        //            var currentDate = DateTime.UtcNow.AddHours(4);
        //            if (lastAccessMin >= currentDate)
        //            {
        //                var CheckToken = tokenS.Claims.Where(claim => claim.Type.Contains("userdata")).FirstOrDefault();
        //                if (CheckToken != null)
        //                {

        //                    var checkRole = GetRoleIdByToken(_bearer_token);
        //                    if (checkRole != null && checkRole == 1)
        //                    {
        //                        var Value = CheckToken.Value;
        //                        UpdateAuthToken(tokenS.EncodedPayload, tokenS.ValidTo, DateTime.UtcNow.AddHours(4), Convert.ToInt32(Value));
        //                        return Convert.ToInt32(Value);
        //                    }
        //                    else
        //                    {
        //                        return null;
        //                    }
        //                }
        //                else
        //                {
        //                    return null;
        //                }

        //            }
        //            else
        //            {
        //                return null;
        //            }

        //        }
        //    }
        //    return null;
        //}
       

        public int? GetRoleIdByToken(string _bearer_token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(_bearer_token);
            var tokenS = handler.ReadToken(_bearer_token) as JwtSecurityToken;
            var CheckToken = tokenS.Claims.Where(claim => claim.Type.Contains("role")).FirstOrDefault();
            if (CheckToken != null)
            {
                var Value = CheckToken.Value;
                return Convert.ToInt32(Value);
            }
            else
            {
                return null;
            }
        }
        public DateTime GetTokenExpiryDate(string _bearer_token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(_bearer_token);
            var tokenS = handler.ReadToken(_bearer_token) as JwtSecurityToken;
            var todate = tokenS.ValidTo;
            return todate;
        }
        public string RemoveSpecialChar(string value)
        {
            var result = "";
            Regex rRemScript = new Regex(@"<script[^>]*>[\s\S]*?</script>");
            Regex r = new Regex("(?:[^a-z0-9 ]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
            value = rRemScript.Replace(value, "");
            value = r.Replace(value, String.Empty);
            result = value;
            return result;
        }
        public async Task<UserAuthToken> GetAuthToken(int UserId, DateTime? ExpiryDate)
        {
            UserAuthToken authToken = new UserAuthToken();
            //var query = $@"Select UserId, Token, ExpiryDate, LastAccessDate from App.AuthorizationToken WITH (NOLOCK) Where ExpiryDate = @ExpiryDate and UserId = {UserId}";
            var constring = GetConnectionString();
            constring = constring.ToString();
            SqlConnection conn = new SqlConnection(constring);

            //SqlCommand cmd = new SqlCommand("GetAuthToken", conn);
            //cmd.CommandType = CommandType.StoredProcedure;
            //cmd.Parameters.AddWithValue("@ExpiryDate", ExpiryDate == null ? DateTime.UtcNow.AddHours(4) : ExpiryDate
            //);

            //cmd.Parameters.AddWithValue("@UserId", ExpiryDate
            // );

            //SqlDataAdapter adp = new SqlDataAdapter(cmd);
            //DataSet ds = new DataSet();

            //var expiryDateParam = new SqlParameter("@ExpiryDate", ExpiryDate);//add paramter
            //if (ExpiryDate == null)
            //{
            //    expiryDateParam = new SqlParameter("@ExpiryDate", DateTime.UtcNow.AddHours(4));
            //}
            //var temp = Conn.ExecuteDataTableSqlDA(conn, query, expiryDateParam);


            SqlCommand cmd = new SqlCommand("GetAuthToken", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ExpiryDate", ExpiryDate == null ? DateTime.UtcNow.AddHours(4) : ExpiryDate
            );

            cmd.Parameters.AddWithValue("@UserId", UserId);

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
            var constring = GetConnectionString();
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
            return false;
        }
        public async Task<bool> ValidateDbToken(string token)
        {
            bool isvalid = false;
            //var query = $@"Select 1 From [App].[AuthorizationToken] WITH (NOLOCK) Where Token = @pToken AND IsActive = 1 and FORMAT(CreatedOn, 'dd-MM-yyyy') = FORMAT(GetDate(), 'dd-MM-yyyy')";
            var constring = GetConnectionString();
            constring = constring.ToString();
            //SqlConnection conn = new SqlConnection(constring);
            //var tokenparam = new SqlParameter("@pToken", token);
            //if (string.IsNullOrWhiteSpace(token))
            //{
            //    tokenparam = new SqlParameter("@pToken", DBNull.Value);
            //}//add paramter
            //var temp = Conn.ExecuteDataTableSqlDA(conn, query, tokenparam);

            SqlConnection conn = new SqlConnection(constring);
            SqlCommand cmd = new SqlCommand("ValidateToken", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@token", token == null || token == "" ? DBNull.Value : token);
            SqlDataAdapter adp = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            adp.Fill(ds);
            if (ds.Tables[0].Rows.Count > 0)
            {
                isvalid = true;
            }
            return await Task.FromResult(isvalid);
        }
        public async Task<bool> UpdateAuthToken(string Token, DateTime expiryDate, DateTime? LastAccessDate, int UserId)
        {
            //var query = $@"Update [App].[AuthorizationToken] WITH (UPDLOCK) Set LastAccessDate = @LastAccessDate, LastUpdatedOn =@LastAccessDate   Where ExpiryDate= @ExpiryDate and UserId = {UserId}";
            var constring = GetConnectionString();
            constring = constring.ToString();

            SqlConnection conn = new SqlConnection(constring);
            SqlCommand cmd = new SqlCommand("UpdateAuthToken", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate == null ? DBNull.Value : expiryDate);
            cmd.Parameters.AddWithValue("@LastAccessDate", LastAccessDate == null ? DateTime.UtcNow.AddHours(4) : LastAccessDate);
            cmd.Parameters.AddWithValue("@UserId", UserId);
            conn.Open();
            var temp = cmd.ExecuteNonQuery();
            conn.Close();

            if (temp > 0)
            {
                return await Task.FromResult(true);
            }
            return await Task.FromResult(false);


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

            //var temp = Conn.ExecuteDataTableSqlDA(conn, query,  expiryDateParam, LastAccessDateParam);


        }
    }
    public static class Conn
    {

        public static List<T> ToListof<T>(this DataTable dt)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var columnNames = dt.Columns.Cast<DataColumn>()
                .Select(c => c.ColumnName)
                .ToList();
            var objectProperties = typeof(T).GetProperties(flags);
            var targetList = dt.AsEnumerable().Select(dataRow =>
            {
                var instanceOfT = Activator.CreateInstance<T>();

                foreach (var properties in objectProperties.Where(properties => columnNames.Contains(properties.Name) && dataRow[properties.Name] != DBNull.Value))
                {
                    properties.SetValue(instanceOfT, dataRow[properties.Name], null);
                }
                return instanceOfT;
            }).ToList();

            return targetList;
        }
        //public static DataTable ExecuteDataTableSqlDA(SqlConnection conn, string cmdText)
        //{
        //    DataTable dt = new DataTable();
        //    SqlDataAdapter da = new SqlDataAdapter(cmdText, conn);
        //    da.SelectCommand.CommandTimeout = 300;
        //    da.Fill(dt);
        //    return dt;
        //}

        public static DataTable ExecuteDataTableSqlDA(SqlConnection conn, string cmdText, params SqlParameter[] sqlParams)
        {
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmdText, conn);
            da.SelectCommand.CommandTimeout = 300;
            if (sqlParams.Length > 0)
            {
                da.SelectCommand.Parameters.AddRange(sqlParams);//update for sql injection
            }
            da.Fill(dt);
            return dt;
        }



        public static System.Data.DataTable ExecuteDataTableSqlDAWithTrnSaction(Microsoft.Data.SqlClient.SqlTransaction transaction = null, SqlConnection conn = null, string cmdText = null, SqlParameter[] sqlParams = null)
        {
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmdText, conn);
            da.SelectCommand.CommandTimeout = 300;
            if (sqlParams != null)
            {
                da.SelectCommand.Parameters.AddRange(sqlParams);//update for sql injection
            }
            if (transaction != null)
            {
                da.SelectCommand.Transaction = transaction;
            }
            da.Fill(dt);
            return dt;
        }
        
    }
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;
        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult IsValid(
        object value, ValidationContext validationContext)
        {
            var file = value as IFormFile;
            if (file != null)
            {
                var extension = Path.GetExtension(file.FileName);
                if (!_extensions.Contains(extension.ToLower()))
                {
                    return new ValidationResult(GetErrorMessage());
                }
            }

            return ValidationResult.Success;
        }

        public string GetErrorMessage()
        {
            return $"This file extension is not allowed!";
        }
    }
}
