using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantApi.Models
{
    public class AccessLogVm
    {
        public int AccessLogId { get; set; }
        public int UserId { get; set; }
        public string CreatedOn { get; set; }
        public string Details { get; set; }
    }
    public class AddAccessLog
    {
        public int UserId { get; set; }
        public string Details { get; set; }
    }
}
