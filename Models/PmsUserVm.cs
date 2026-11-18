using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FABTenantApi.Models
{
    public class PmsUserVm
    {
        public string Mobile { get; set; }
        public string Role { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int UserId { get; set; }
    }
}
