using FABTenantApi.Classes;
using FABTenantD2Api.Classes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FABTenantD2Api.Controllers
{
    public class FPaymentController : Controller
    {
        private IConfiguration _config;
        private PaymentGatway _gatewayLog;
        private readonly Settings settings;

        public FPaymentController(IConfiguration config)
        {
            _config = config;
            _gatewayLog = new PaymentGatway();
            settings = new Settings();
        }

        public ActionResult FinalizeCopy(string TransactionID)
        {
            _gatewayLog.AddCmsLog(11, "Finalize", "Finalize Call");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var URL = _config["Payment:URL"].ToString();
            var Customer = _config["Payment:Customer"].ToString();
            var UserName = _config["Payment:UserName"].ToString();
            var Password = _config["Payment:Password"].ToString();
            //var Certificate = _config["Payment:Certificate"].ToString();
            //var CertPassword = _config["Payment:CertPassword"].ToString();

            _gatewayLog.AddCmsLog(11, "URL", URL.ToString());
            _gatewayLog.AddCmsLog(11, "Customer", Customer.ToString());
            _gatewayLog.AddCmsLog(11, "UserName", UserName.ToString());
            _gatewayLog.AddCmsLog(11, "Password", Password.ToString());

            try
            {

                string InputJSON = "{ \"Finalization\": { \"TransactionID\": \"{TxnId}\", \"Customer\": \"{Customer}\", \"UserName\":\"{UserName}\", \"Password\":\"{Password}\" } }".Replace("{TxnId}", TransactionID).Replace("{Customer}", Customer).Replace("{UserName}", UserName).Replace("{Password}", Password);
                _gatewayLog.AddCmsLog(11, "InputJSON", InputJSON);


                //X509Certificate2 certificate = new X509Certificate2(Certificate, CertPassword);
                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(URL));
                //request.ServerCertificateValidationCallback = (message, cert, chain, error) => true;
                //byte[] lbPostBuffer = ASCIIEncoding.ASCII.GetBytes(InputJSON);
                //request.ClientCertificates.Add(certificate);               
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(URL));
                byte[] lbPostBuffer = ASCIIEncoding.ASCII.GetBytes(InputJSON);
                //request.ClientCertificates.Add(certificate);
                request.UserAgent = ".NET Framework Test Client";
                request.Accept = "application/json";
                request.Method = "POST";
                request.ContentLength = lbPostBuffer.Length;
                request.ContentType = "application/json";
                request.Timeout = 600000;
            //    request.Proxy = new WebProxy("10.163.0.84", 8080);

                HttpWebResponse response;
                Stream loPostData = request.GetRequestStream();
                loPostData.Write(lbPostBuffer, 0, lbPostBuffer.Length);
                loPostData.Close();

                response = (HttpWebResponse)request.GetResponse();
                _gatewayLog.AddCmsLog(1, "Response", response.ToString());

                Encoding enc = Encoding.GetEncoding(1252);
                StreamReader loResponseStream = new StreamReader(response.GetResponseStream(), enc);
                string result = loResponseStream.ReadToEnd();
                response.Close();
                loResponseStream.Close();

                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<EitsalatRegTry.DTO.Finalize.Response>(result);
                var addLog = _gatewayLog.AddCmsLog(11, "Final Response", result);

                if (obj.Transaction.ResponseCode == "000" || obj.Transaction.ResponseCode == "200" || obj.Transaction.ResponseCode == "0")
                {
                    var Message = "response code: " + obj.Transaction.ResponseCode + " message: payment successfully with response " + result;
                    _gatewayLog.FinalizedPayment(obj.Transaction.UniqueID, TransactionID, "Success", obj.Transaction.ResponseCode, obj.Transaction.ResponseCode);
                    _gatewayLog.AddCmsLog(11, "full response", Message);
                    ViewBag.TransactionID = TransactionID;
                    ViewBag.otherResult = Message;
                }
                else
                {
                    var Message = "response code: " + obj.Transaction.ResponseCode + " message: " + obj.Transaction.ResponseDescription + "with response " + result;
                    ViewBag.otherResult = Message;
                    _gatewayLog.FinalizedPayment(obj.Transaction.UniqueID, TransactionID, obj.Transaction.ResponseDescription, obj.Transaction.ResponseCode, obj.Transaction.ResponseCode);
                    _gatewayLog.AddCmsLog(11, "full response", Message);
                }
                ViewBag.ResponseCode = obj.Transaction.ResponseCode;
                ViewBag.RawResult = result;
                var addLog2 = _gatewayLog.AddCmsLog(11, "response from api", result);
                return View(obj.Transaction);
            }
            catch (Exception e)
            {
                var addLog = _gatewayLog.AddCmsLog(11, "Exception Response", e.Message);
                Console.WriteLine("Exception :" + e.ToString());
                Console.ReadLine();
                return View();
            }
        }
        public ActionResult Finalize(string TransactionID)
        {
                SqlTransaction sqlTran = null;
            try
            {
                using (var conn = new SqlConnection(settings.GetConnectionString()))
                {
                    conn.Open();
                    sqlTran = conn.BeginTransaction();
                    _gatewayLog.AddCmsLogTransaction(sqlTran,conn,11, "Finalize", "Finalize Call");
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    var URL = _config["Payment:URL"].ToString();
                    var Customer = _config["Payment:Customer"].ToString();
                    var UserName = _config["Payment:UserName"].ToString();
                    var Password = _config["Payment:Password"].ToString();
                    //var Certificate = _config["Payment:Certificate"].ToString();
                    //var CertPassword = _config["Payment:CertPassword"].ToString();

                    _gatewayLog.AddCmsLogTransaction(sqlTran,conn,11, "URL", URL.ToString());
                    _gatewayLog.AddCmsLogTransaction(sqlTran, conn,11, "Customer", Customer.ToString());
                    _gatewayLog.AddCmsLogTransaction(sqlTran, conn, 11, "UserName", UserName.ToString());
                    _gatewayLog.AddCmsLogTransaction(sqlTran, conn, 11, "Password", Password.ToString());

                    try
                    {

                        string InputJSON = "{ \"Finalization\": { \"TransactionID\": \"{TxnId}\", \"Customer\": \"{Customer}\", \"UserName\":\"{UserName}\", \"Password\":\"{Password}\" } }".Replace("{TxnId}", TransactionID).Replace("{Customer}", Customer).Replace("{UserName}", UserName).Replace("{Password}", Password);
                        _gatewayLog.AddCmsLogTransaction(sqlTran, conn, 11, "InputJSON", InputJSON);


                        //X509Certificate2 certificate = new X509Certificate2(Certificate, CertPassword);
                        //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(URL));
                        //request.ServerCertificateValidationCallback = (message, cert, chain, error) => true;
                        //byte[] lbPostBuffer = ASCIIEncoding.ASCII.GetBytes(InputJSON);
                        //request.ClientCertificates.Add(certificate);               
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(URL));
                        byte[] lbPostBuffer = ASCIIEncoding.ASCII.GetBytes(InputJSON);
                        //request.ClientCertificates.Add(certificate);
                        request.UserAgent = ".NET Framework Test Client";
                        request.Accept = "application/json";
                        request.Method = "POST";
                        request.ContentLength = lbPostBuffer.Length;
                        request.ContentType = "application/json";
                        request.Timeout = 600000;

                        //Proxy commneted on 31/01/2024
                      //  request.Proxy = new WebProxy("10.163.0.84", 8080);

                        HttpWebResponse response;
                        Stream loPostData = request.GetRequestStream();
                        loPostData.Write(lbPostBuffer, 0, lbPostBuffer.Length);
                        loPostData.Close();

                        response = (HttpWebResponse)request.GetResponse();
                        _gatewayLog.AddCmsLogTransaction(sqlTran, conn, 1, "Response", response.ToString());

                        Encoding enc = Encoding.GetEncoding(1252);
                        StreamReader loResponseStream = new StreamReader(response.GetResponseStream(), enc);
                        string result = loResponseStream.ReadToEnd();
                        response.Close();
                        loResponseStream.Close();

                        var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<EitsalatRegTry.DTO.Finalize.Response>(result);
                        var addLog = _gatewayLog.AddCmsLogTransaction(sqlTran, conn, 11, "Final Response", result);

                        if (obj.Transaction.ResponseCode == "000" || obj.Transaction.ResponseCode == "200" || obj.Transaction.ResponseCode == "0")
                        {
                            var Message = "response code: " + obj.Transaction.ResponseCode + " message: payment successfully with response " + result;
                            _gatewayLog.FinalizedPaymentTransaction(sqlTran,conn,obj.Transaction.UniqueID, TransactionID, "Success", obj.Transaction.ResponseCode, obj.Transaction.ResponseCode);
                            _gatewayLog.AddCmsLogTransaction(sqlTran, conn, 11, "full response", Message);
                            ViewBag.TransactionID = TransactionID;
                            ViewBag.otherResult = Message;
                        }
                        else
                        {
                            var Message = "response code: " + obj.Transaction.ResponseCode + " message: " + obj.Transaction.ResponseDescription + "with response " + result;
                            ViewBag.otherResult = Message;
                            _gatewayLog.FinalizedPaymentTransaction(sqlTran,conn,obj.Transaction.UniqueID, TransactionID, obj.Transaction.ResponseDescription, obj.Transaction.ResponseCode, obj.Transaction.ResponseCode);
                            _gatewayLog.AddCmsLogTransaction(sqlTran, conn, 11, "full response", Message);
                        }
                        ViewBag.ResponseCode = obj.Transaction.ResponseCode;
                        ViewBag.RawResult = result;
                        var addLog2 = _gatewayLog.AddCmsLogTransaction(sqlTran, conn, 11, "response from api", result);
                        sqlTran.Commit();
                        return View(obj.Transaction);
                    }
                    catch (Exception ex)
                    {
                        var addLog = _gatewayLog.AddCmsLog(11, "Exception Response", ex.Message);
                        Console.WriteLine("Exception :" + ex.ToString());
                        Console.ReadLine();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                sqlTran.Rollback();
                return View();
            }
            }
    }
}
