using Microsoft.AspNetCore.Http;

namespace FABTenantD2Api.Models
{
    public class TestImage
    {
        public IFormFile Image { get; set; }
    }
    public class ImageVM
    {
        public string Name { get; set; }
        public string ImageName { get; set; }
    }
    public class ImageViewModel
    {
        public string Name { get; set; }
        public IFormFile File { get; set; }
    }
}
