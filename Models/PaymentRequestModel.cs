namespace FABTenantD2Api.Models
{
    public class PaymentRequestModel
    {
        public string Page { get; set; } = "1";
        public string PageSize { get; set; } = "20";
        public string SearchParam { get; set; }
    }
}
