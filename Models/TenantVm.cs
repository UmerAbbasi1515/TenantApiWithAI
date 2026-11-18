using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantApi.Models
{
    public class TenantVm
    {
        public int TenantId { get; set; }
        public string Name { get; set; }
        public string NameAR { get; set; }
        public string AddressAR { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Nationality { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string PhotoUrl { get; set; }
        public string Fax { get; set; }
        public string TermsAndConditions { get; set; }
    }
    public class CaseServiceVm
    {
        public int CaseOwnerId { get; set; }
        public string CaseNo { get; set; }
        public string Email { get; set; }
        public string WorkNo { get; set; }
        public string ResidenceTel { get; set; }
        public string MobileTel { get; set; }
        public string Dated { get; set; }
        public string EmirateId { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
        public string BuildingNumber { get; set; }
        public string BuildingRefNo { get; set; }
        public string Description { get; set; }
        public string ProposedRemedy { get; set; }
        public string ReasonForVocation { get; set; }
    }

    public class TenantContractUnitVm
    {
       
        public string PropertyImage { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
        public decimal Amount { get; set; }
        public string UnitRefNo { get; set; }

        public int UnitID { get; set; }
        public string PropertyType { get; set; }
        public string PropertyTypeAR { get; set; }
        public string UnitType { get; set; }
        public string UnitTypeAR { get; set; }
    }

    public class UnitVm
    {
        public string ReferenceNo { get; set; }
        public string UnitNo { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string PropertyImage { get; set; }
        public string PropertyName { get; set; }
        public string PropertyNameAR { get; set; }
        public decimal Amount { get; set; }
        public string unitRefnumber { get; set; }
        public string UnitRefNo { get; set; }
        
        public int UnitID { get; set; }
        public string PropertyType { get; set; }
        public string UnitType { get; set; }
    }
    public class NotificationVm
    {
        public int NotificationId { get; set; }
        public string CreatedOn { get; set; }
        public string CurrentStatus { get; set; }
        public int NotificationTypeId { get; set; }
        public bool IsRead { get; set; }
        public string ReadOn { get; set; }
        public bool Sent { get; set; }
        public string SentOn { get; set; }
        public string Batch { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Mobile { get; set; }
        public string Title { get; set; }
        public string TitleAR { get; set; }
        public string Description { get; set; }
        public string DescriptionAR { get; set; }
        public string NotificationType { get; set; }
        public int TotalRecords { get; set; }
    }
    public class NotificationModel
    {
        public int NotificationId { get; set; }

    }

    public class NotificationFilesVm
    {
        public int fileid { get; set; }
        public string FileSource { get; set; }
        public string UploadOn { get; set; }
    }
    public class NotificationFileResponse
    {
        public NotificationFileResponse()
        {
            imageByteArray = new List<byte[]>();
        }
        public List<byte[]> imageByteArray { get; set; }
    }

}
