using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantApi.Models
{
    public class AppVersionVm
    {
        public string Version { get; set; }
        public string SecretKey { get; set; }
    }
    public class VersionVm
    {
        public string SecretKey { get; set; }
    }
    public class AppVersionDetailVm
    {
        public string Version { get; set; }
        public string Language { get; set; }
        public List<AppMetadataDetail> MetadataDetails { get; set; }

    }
    public class AppMetadataDetail
    {
        public string Code { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Group { get; set; }
        public string Date { get; set; }
        public string MetadataType { get; set; }
    }
}
