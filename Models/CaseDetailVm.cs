using System;
using System.Collections.Generic;

namespace FABTenantD2Api.Models
{
    public class CaseDetailVm
    {
    }
    public class AddCaseRequest
    {
        public int? VacatingId { get; set; }
        public int ContractId { get; set; }
        public int? DueActionId { get; set; }
        public string Duration { get; set; }
        public string Description { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string VacatingDate { get; set; }
    }
    public class CaseTypeCodeVm
    {
        public int CaseTypeId { get; set; }
    }
    public class AddRequestDetailVm
    {
        public int CaseNo { get; set; }
    }
    public class ExtensionPeriodVm
    {
        public string Duration { get; set; }
        public decimal? Amount { get; set; }
        public OtherRenewalContractInfoVm ExtensionDetail { get; set; }
    }
    public class AddContractCaseRequest
    {
        public int ContractId { get; set; }
    }
    public class CheckInCaseRequest
    {
        public int ContractId { get; set; }
        public int? DueActionId { get; set; }
        public string Description { get; set; }
    }

    public class SaveSurveyVm
    {
       
       
        public int CustomerChoice { get; set; }
        public string Description { get; set; }
        public int IsCompleted { get; set; }
        public int AnswerId { get; set; }
    }
}
