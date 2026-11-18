namespace FABTenantD2Api.Models
{
    public class GeneralDataVm
    {
    }
    public class ServicesCategoriesVm
    {
        public string Title { get; set; }
        public string TitleAR { get; set; }
        public int CategoryId { get; set; }
    }
    public class ServicesVm
    {
        public string Title { get; set; }
        public string TitleAR { get; set; }
        public string Description { get; set; }
        public string DescriptionAR { get; set; }
    }
    public class ConfigurationValueVm
    {
        public int Settingid { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string ValueAr { get; set; }
        public string Title { get; set; }
        public string TitleAr { get; set; }
        public string Type { get; set; }
    }
}
 