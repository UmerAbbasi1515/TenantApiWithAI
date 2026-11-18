using System;
using System.ComponentModel.DataAnnotations;

namespace FABTenantApi.Models
{
    public class UserSessionToken
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }
    public class UserModel
    {
        [Required]
        public string Mobile { get; set; }
        public string DeviceToken { get; set; }
        public string SecretKey { get; set; }
        public string DeviceName { get; set; }
        public string Type { get; set; }
    }
    public class UserLoginModel
    {
        [Required]
        public string Mobile { get; set; }
        [Required]
        public string MPIN { get; set; }
        public string DeviceName { get; set; }
        public string DeviceToken { get; set; }
        public string Type { get; set; }
    }
    public class NewUserModel
    {
        [Required]
        public string Mobile { get; set; }
        public string Name { get; set; }
        public string DeviceToken { get; set; }
        public string SecretKey { get; set; }
        public string DeviceName { get; set; }
        public string Type { get; set; }
    }
    public class NewUserDevice
    {
        public string DeviceName { get; set; }
        public string DeviceToken { get; set; }
        public string Type { get; set; }
        public int UserId { get; set; }

    }
    public class UserAuthToken 
    {
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime LastAccessDate { get; set; }

    }

    public class UserDevice
    {
        public string DeviceName { get; set; }
        public string DeviceToken { get; set; }
        public string Type { get; set; }
        public string CreatedOn { get; set; }
        public int UserId { get; set; }

    }
    public class UserPinModel
    {
        public int UserId { get; set; }
        public string Mobile { get; set; }
        public string Role { get; set; }
        public string MPIN { get; set; }
    }
   
}
