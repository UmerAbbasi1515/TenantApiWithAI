using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using EitsalatRegTry.DTO.Registration;
using Microsoft.Data.SqlClient;

namespace FABTenantD2Api.Classes
{
    public class PaymentApi
    {
        public PaymentGatway _gatewayLog= new PaymentGatway();
        public ConfigValues configValues= new ConfigValues();
        public Transaction Registration(decimal? Amount, decimal OrderId, int UserId, string OrderInfo)
        {
            var addLogRegistration = _gatewayLog.AddCmsLog(1, "EndPointCalled", "Registration");

            string URL = configValues.GetPaymentURL();
            string Customer = configValues.GetPaymentCustomer();
            string UserName = configValues.GetPaymentUserName();
            string Password = configValues.GetPaymentUserPassword();
            string ReturnUrl = configValues.GetReturnUrl();
            
            string InputJSON = "{\"Registration\":"
                + "{\"Customer\":\"{Customer}\"," +
                "\"Channel\":\"Web\"," +
                "\"Amount\":\"{Amount}\"," +
                "\"Currency\":\"AED\"," +
                "\"OrderID\":{OrderId}," +
                "\"OrderName\":\"PayTicket\"," +
                "\"OrderInfo\":{OrderInfo}," +
                "\"TransactionHint\":\"CPT:Y;VCC:Y;\"," +
                "\"UserName\":\"{UserName}\"," +
                "\"Password\":\"{Password}\"," +
                "\"ReturnPath\":\"{ReturnUrl}\"}}";
                
              InputJSON= InputJSON.Replace("{Amount}", Amount.ToString()).Replace("{OrderId}", OrderId.ToString()).Replace("{Customer}", Customer).Replace("{UserName}", UserName).Replace("{Password}", Password).Replace("{ReturnUrl}", ReturnUrl).Replace("{OrderInfo}", OrderInfo);

             _gatewayLog.AddCmsLog(UserId, "Input json", InputJSON);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(URL));
                byte[] lbPostBuffer = ASCIIEncoding.ASCII.GetBytes(InputJSON);
                //request.ClientCertificates.Add(certificate);
                request.UserAgent = ".NET Framework Test Client";
                request.Accept = "application/json";
                request.Method = "POST";
                request.ContentLength = lbPostBuffer.Length;
                request.ContentType = "application/json";
                request.Timeout = 600000;
             //   request.Proxy = new WebProxy("10.163.0.84", 8080);

                HttpWebResponse response;
                Stream loPostData = request.GetRequestStream();
                loPostData.Write(lbPostBuffer, 0, lbPostBuffer.Length);
                loPostData.Close();

                response = (HttpWebResponse)request.GetResponse();

                Encoding enc = Encoding.GetEncoding(1252);
                StreamReader loResponseStream = new StreamReader(response.GetResponseStream(), enc);
                string result = loResponseStream.ReadToEnd();
                response.Close();
                loResponseStream.Close();
                _gatewayLog.AddCmsLog(UserId, "response", result);
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<EitsalatRegTry.DTO.Registration.Response>(result);
                //var f = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);

                return obj.Transaction;

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception :" + e.ToString());
                Console.ReadLine();
                _gatewayLog.AddCmsLog(UserId, "Exception", e.ToString());
                throw;
            }
        }

        public Transaction RegistrationSSLTransaction(SqlTransaction tran,SqlConnection conn,decimal? Amount, decimal OrderId, int UserId, string OrderInfo)
        {
            var addLogRegistration = _gatewayLog.AddCmsLogTransaction(tran,conn,UserId, "EndPointCalledwithSSL", "RegistrationSSL");

            string URL = configValues.GetPaymentURL();
            string Customer = configValues.GetPaymentCustomer();
            string UserName = configValues.GetPaymentUserName();
            string Password = configValues.GetPaymentUserPassword();
            string ReturnUrl = configValues.GetReturnUrl();
            string TransactionHint = configValues.GetTransactionHint();
            //string Certificate = configValues.GetCertificate();
            //string CertPassword = configValues.GetCertPassword();

            try
            {
                string InputJSON = "{\"Registration\":"
                + "{\"Customer\":\"{Customer}\"," +
                "\"Channel\":\"Web\"," +
                "\"Amount\":\"{Amount}\"," +
                "\"Currency\":\"AED\"," +
                "\"OrderID\":{OrderId}," +
                "\"OrderName\":\"PayTicket\"," +
                "\"OrderInfo\":\"{OrderInfo}\"," +
                "\"TransactionHint\":\"{TransactionHint}\"," +
                "\"UserName\":\"{UserName}\"," +
                "\"Password\":\"{Password}\"," +
                "\"ReturnPath\":\"{ReturnUrl}\"}}";

                InputJSON = InputJSON.Replace("{Amount}", Amount.ToString()).Replace("{OrderId}", OrderId.ToString()).Replace("{Customer}", Customer).Replace("{UserName}", UserName).Replace("{Password}", Password).Replace("{ReturnUrl}", ReturnUrl).Replace("{OrderInfo}", OrderInfo).Replace("{TransactionHint}", TransactionHint);

            var addLog = _gatewayLog.AddCmsLogTransaction(tran,conn,UserId, "Input json", InputJSON);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
           
                //  //X509Certificate2 certificate = new X509Certificate2(Certificate, CertPassword);
                //  HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(URL));
                //  request.ServerCertificateValidationCallback = (message, cert, chain, error) => true;
                //  byte[] lbPostBuffer = ASCIIEncoding.ASCII.GetBytes(InputJSON);
                ////  request.ClientCertificates.Add(certificate);
                //  request.UserAgent = ".NET Framework Test Client";

                //  //changed on 30112022 as per etidalat recommendation
                //  // request.Accept = "text/xml-standard-api";
                //  request.Accept = "application/json";
                //  request.Method = "POST";
                //  request.ContentLength = lbPostBuffer.Length;
                //  request.ContentType = "application/json";
                //  request.Timeout = 600000;
                //  request.Proxy = new WebProxy("10.163.0.84", 8080);
                //  HttpWebResponse response;
                //  Stream loPostData = request.GetRequestStream();
                //  loPostData.Write(lbPostBuffer, 0, lbPostBuffer.Length);
                //  loPostData.Close();
                //  response = (HttpWebResponse)request.GetResponse();
                //  Encoding enc = Encoding.GetEncoding(1252);
                //  StreamReader loResponseStream = new StreamReader(response.GetResponseStream(), enc);
                //  string result = loResponseStream.ReadToEnd();
                //  response.Close();
                //  loResponseStream.Close();

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(URL));
                _gatewayLog.AddCmsLogTransaction(tran,conn,UserId , "Exception1", "1");
                byte[] lbPostBuffer = ASCIIEncoding.ASCII.GetBytes(InputJSON);
                _gatewayLog.AddCmsLogTransaction(tran,conn,UserId , "Exception2", "2");
                //request.ClientCertificates.Add(certificate);
                request.UserAgent = ".NET Framework Test Client";
                request.Accept = "application/json";
                request.Method = "POST";
                request.ContentLength = lbPostBuffer.Length;
                request.ContentType = "application/json";
                request.Timeout = 600000;
              //  request.Proxy = new WebProxy("10.163.0.84", 8080);
                _gatewayLog.AddCmsLog(UserId, "Exception3", "3");

                HttpWebResponse response;
                Stream loPostData = request.GetRequestStream();
                _gatewayLog.AddCmsLogTransaction(tran,conn,UserId , "Exception4", "4");
                loPostData.Write(lbPostBuffer, 0, lbPostBuffer.Length);
                loPostData.Close();
                _gatewayLog.AddCmsLogTransaction(tran,conn,UserId , "Exception5", "5");

                response = (HttpWebResponse)request.GetResponse();
                _gatewayLog.AddCmsLogTransaction(tran,conn,UserId , "Response", response.ToString());

                Encoding enc = Encoding.GetEncoding(1252);
                _gatewayLog.AddCmsLogTransaction(tran,conn,UserId , "Exception7", "7");
                StreamReader loResponseStream = new StreamReader(response.GetResponseStream(), enc);
                string result = loResponseStream.ReadToEnd();
                response.Close();
                loResponseStream.Close();
                _gatewayLog.AddCmsLogTransaction(tran,conn,UserId, "Result", result);

                //dynamic obj = JsonConvert.DeserializeObject(result);
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<EitsalatRegTry.DTO.Registration.Response>(result);
                _gatewayLog.AddCmsLogTransaction(tran,conn,UserId , "obj.Transaction", obj.Transaction.TransactionID.ToString());
                return obj.Transaction;

            }
            catch (Exception ex)
            {
                var addExceptionLog = _gatewayLog.AddCmsLog(UserId, "Exception", "Inner Exception: "+ ex.InnerException +"Message: "+ex.Message);
                Console.WriteLine("Exception :" + ex.ToString());
                _gatewayLog.AddCmsLog(1, "Exception", ex.ToString());
                Console.ReadLine();
                throw ex;
            }
        }


        public Transaction RegistrationSSL(decimal? Amount, decimal OrderId, int UserId, string OrderInfo)
        {
            var addLogRegistration = _gatewayLog.AddCmsLog(UserId, "EndPointCalledwithSSL", "RegistrationSSL");

            string URL = configValues.GetPaymentURL();
            string Customer = configValues.GetPaymentCustomer();
            string UserName = configValues.GetPaymentUserName();
            string Password = configValues.GetPaymentUserPassword();
            string ReturnUrl = configValues.GetReturnUrl();
            string TransactionHint = configValues.GetTransactionHint();
            //string Certificate = configValues.GetCertificate();
            //string CertPassword = configValues.GetCertPassword();

            try
            {
                string InputJSON = "{\"Registration\":"
                + "{\"Customer\":\"{Customer}\"," +
                "\"Channel\":\"Web\"," +
                "\"Amount\":\"{Amount}\"," +
                "\"Currency\":\"AED\"," +
                "\"OrderID\":{OrderId}," +
                "\"OrderName\":\"PayTicket\"," +
                "\"OrderInfo\":\"{OrderInfo}\"," +
                "\"TransactionHint\":\"{TransactionHint}\"," +
                "\"UserName\":\"{UserName}\"," +
                "\"Password\":\"{Password}\"," +
                "\"ReturnPath\":\"{ReturnUrl}\"}}";

                InputJSON = InputJSON.Replace("{Amount}", Amount.ToString()).Replace("{OrderId}", OrderId.ToString()).Replace("{Customer}", Customer).Replace("{UserName}", UserName).Replace("{Password}", Password).Replace("{ReturnUrl}", ReturnUrl).Replace("{OrderInfo}", OrderInfo).Replace("{TransactionHint}", TransactionHint);

                var addLog = _gatewayLog.AddCmsLog(UserId, "Input json", InputJSON);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                //  //X509Certificate2 certificate = new X509Certificate2(Certificate, CertPassword);
                //  HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(URL));
                //  request.ServerCertificateValidationCallback = (message, cert, chain, error) => true;
                //  byte[] lbPostBuffer = ASCIIEncoding.ASCII.GetBytes(InputJSON);
                ////  request.ClientCertificates.Add(certificate);
                //  request.UserAgent = ".NET Framework Test Client";

                //  //changed on 30112022 as per etidalat recommendation
                //  // request.Accept = "text/xml-standard-api";
                //  request.Accept = "application/json";
                //  request.Method = "POST";
                //  request.ContentLength = lbPostBuffer.Length;
                //  request.ContentType = "application/json";
                //  request.Timeout = 600000;
                //  request.Proxy = new WebProxy("10.163.0.84", 8080);
                //  HttpWebResponse response;
                //  Stream loPostData = request.GetRequestStream();
                //  loPostData.Write(lbPostBuffer, 0, lbPostBuffer.Length);
                //  loPostData.Close();
                //  response = (HttpWebResponse)request.GetResponse();
                //  Encoding enc = Encoding.GetEncoding(1252);
                //  StreamReader loResponseStream = new StreamReader(response.GetResponseStream(), enc);
                //  string result = loResponseStream.ReadToEnd();
                //  response.Close();
                //  loResponseStream.Close();

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(URL));
                _gatewayLog.AddCmsLog(UserId, "Exception1", "1");
                byte[] lbPostBuffer = ASCIIEncoding.ASCII.GetBytes(InputJSON);
                _gatewayLog.AddCmsLog(UserId, "Exception2", "2");
                //request.ClientCertificates.Add(certificate);
                request.UserAgent = ".NET Framework Test Client";
                request.Accept = "application/json";
                request.Method = "POST";
                request.ContentLength = lbPostBuffer.Length;
                request.ContentType = "application/json";
                request.Timeout = 600000;
              //  request.Proxy = new WebProxy("10.163.0.84", 8080);
                _gatewayLog.AddCmsLog(UserId, "Exception3", "3");

                HttpWebResponse response;
                Stream loPostData = request.GetRequestStream();
                _gatewayLog.AddCmsLog(UserId, "Exception4", "4");
                loPostData.Write(lbPostBuffer, 0, lbPostBuffer.Length);
                loPostData.Close();
                _gatewayLog.AddCmsLog(UserId, "Exception5", "5");

                response = (HttpWebResponse)request.GetResponse();
                _gatewayLog.AddCmsLog(UserId, "Response", response.ToString());

                Encoding enc = Encoding.GetEncoding(1252);
                _gatewayLog.AddCmsLog(UserId, "Exception7", "7");
                StreamReader loResponseStream = new StreamReader(response.GetResponseStream(), enc);
                string result = loResponseStream.ReadToEnd();
                response.Close();
                loResponseStream.Close();
                _gatewayLog.AddCmsLog(UserId, "Result", result);

                //dynamic obj = JsonConvert.DeserializeObject(result);
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<EitsalatRegTry.DTO.Registration.Response>(result);
                _gatewayLog.AddCmsLog(UserId, "obj.Transaction", obj.Transaction.TransactionID.ToString());
                return obj.Transaction;

            }
            catch (Exception e)
            {
                var addExceptionLog = _gatewayLog.AddCmsLog(UserId, "Exception", "Inner Exception: " + e.InnerException + "Message: " + e.Message);
                Console.WriteLine("Exception :" + e.ToString());
                _gatewayLog.AddCmsLog(1, "Exception", e.ToString());
                Console.ReadLine();
                throw;
            }
        }



    }
}
