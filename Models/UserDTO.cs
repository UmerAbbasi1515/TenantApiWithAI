using System.Collections.Generic;

namespace FABTenantApi.Models
{
    public class UserDTO
    {
        public string Mobile { get; set; }
        public string Password { get; set; }
        public List<UserRoleVm> Roles { get; set; }
        public string Role { get; set; }

    }
    public class UserVm
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string Password { get; set; }
        public List<UserRoleVm> Roles { get; set; }
        public string Role { get; set; }

    }
    public class UserRoleVm
    {
        public int RoleId { get; set; }
        public string Role { get; set; }
    }
}
