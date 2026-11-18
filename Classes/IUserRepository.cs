using FABTenantApi.Models;
using System;

namespace FABTenantApi.Classes
{
    public interface IUserRepository
    {
        UserVm GetUser(UserModel userModel);
        UserVm AddUser(UserModel userModel);
        PmsUserVm GetVendor(UserModel userModel);
        PmsUserVm GetLandlord(UserModel userModel);
        PmsUserVm GetTenant(UserModel userModel);
        UserRoleVm GetRole(string role);
        bool GenteratePin(string MPIN, int userId);
        bool ValidatePin(string MPIN, int userId);
        UserVm Login(UserLoginModel userLogin);
        UserDevice NewUserDevice(NewUserDevice newUserDevice);
       
    }
}
