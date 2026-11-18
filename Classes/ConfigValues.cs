using Microsoft.Extensions.Configuration;
using System.IO;

namespace FABTenantD2Api.Classes
{
    public class ConfigValues
    {

        public int ServiceRequestCaseTypeId = 1;

        // cancel Service Request
        public int ServiceRequestCancellationCategory = 7;//Cancellation Id;
        public int ServiceRequestCancellationSubCategory = 78;//Cancellation Id;

        // Save Service Request
        public int SRCaseType = 4;// Maintenance Service Request 3
        public int DefaultCaseStatus = 7;//  Pending
        public int CancelStatus = 20;//  Cancelled
        public int ReOpenedStatus = 18;//  Re-Opened

        //Update Profile
        public int UPCaseType = 4;// General Service Request
        public int UPCaseCategory = 43;// Customer Info Management
        public int UPCaseSubCategory = 431;// Customer Data Change
        public int StatusNotIn = 19;


        // Renewal Service Request
        public int ServiceRequestRenewalCategory = 8;//REnewal of Lease Id;
        public int ServiceRequestRenewalSubCategory = 88;//REnewal of Lease Id;
        //below all are codes
        public int RSCaseType = 4;// General Service Request--casetype id --old 4
        public int RSCaseCategory =41 ;//  Renewal of Lease --	code
        // Committed due to not being used 05/09/2024
       // public int RSCaseCategoryId = 89;//  Renewal of Lease	--id --old 30
        public int RSCaseSubCategory = 411;//  Renewal of Lease - Residential/NHL --code

        // Extension Service Request
        public int ESCaseType = 4;// General Service Request
        public int ESCaseCategory = 41;//  Renewal of Lease	
        public int ESCaseSubCategory = 412;//  Short extension of contract


        // Termination Service Request
        public int TSCaseType = 4;// General Service Request
        public int TSCaseCategory = 42;//  Cancellation of Lease		
        public int TSCaseSubCategory = 421;//  Request Break Lease

        public int CSCaseType = 1;//  Enquiry
        public int CSCaseCategory = 45;//  Property
        public int CSCaseSubCategory = 451;//  Check In
        public int LSCaseSubCategory = 452;//  Legal Settlement  // account and finance Services


        // Contract Signature 
        public int SSCaseType = 1;// General Service Request
        public int SSCaseCategory = 41;//  Contract Documentation => Renewal of Lease
        public int SSCaseSubCategory = 461;//  Signature



        public string GetPaymentURL()
        {
            IConfiguration conf = (new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build());
            string Value = conf["Payment:URL"].ToString();

            return Value;
        }
        public string GetPaymentCustomer()
        {
            IConfiguration conf = (new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build());
            string Value = conf["Payment:Customer"].ToString();

            return Value;
        }
        public string GetPaymentUserName()
        {
            IConfiguration conf = (new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build());
            string Value = conf["Payment:UserName"].ToString();

            return Value;
        }
        public string GetPaymentUserPassword()
        {
            IConfiguration conf = (new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build());
            string Value = conf["Payment:Password"].ToString();

            return Value;
        }
        public string GetReturnUrl()
        {
            IConfiguration conf = (new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build());
            string Value = conf["Payment:ReturnUrl"].ToString();

            return Value;
        } 
        //added 09/01/2023
        public string GetTransactionHint()
        {
            IConfiguration conf = (new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build());
            string Value = conf["Payment:TransactionHint"].ToString();

            return Value;
        }
        public string GetCertificate()
        {
            IConfiguration conf = (new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build());
            string Value = conf["Payment:Certificate"].ToString();

            return Value;
        }
        public string GetCertPassword()
        {
            IConfiguration conf = (new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build());
            string Value = conf["Payment:CertPassword"].ToString();

            return Value;
        }

        //public string GetCMSBaseURL()
        //{
            //IConfiguration conf = (new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build());
            //string Value = conf["CMSURL:BaseURL"].ToString();

            //return Value;
       // }
    }


}
