using FABModels.Model;
using FABTenantApi.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using BC = BCrypt.Net.BCrypt;
namespace FABTenantApi.Classes
{
    public class UserRepository : IUserRepository
    {

        private readonly FABAppDbContext _dbContext;
        public UserRepository(FABAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public UserVm GetUser(UserModel userModel)
        {
            var user = _dbContext.Users.Where(x => x.Mobile.ToLower() == userModel.Mobile.ToLower())
                 .Select(p => new UserVm
                 {
                     Password = p.Password,
                     Roles = p.UserRoles.Select(r => new UserRoleVm { Role = r.Role.Title, RoleId = r.RoleId }).ToList(),
                     Mobile = p.Mobile,
                     Role = p.UserRoles.Select(p => p.Role.Title).FirstOrDefault(),
                     UserId = p.UserId,
                     Name = p.FullName
                 }).FirstOrDefault();

            if (user == null)//new user
            {
                var newuser = new User();
                var userroles = new List<UserRoleVm>();
                var role = new UserRoleVm();
                var vendorRole = GetVendor(userModel);
                var tenantRole = GetTenant(userModel);
                var landlordRole = GetLandlord(userModel);

                if (landlordRole != null)//Exists in landlords table
                {
                    string roleName = "Landlord";
                    role = NewUser(userModel, user, newuser, userroles, landlordRole, roleName);
                }
                else if (vendorRole != null)//exists in vendor contact person
                {
                    string roleName = "Vendor";
                    role = NewUser(userModel, user, newuser, userroles, vendorRole, roleName);
                }
                else if (tenantRole != null) //exists in tenants
                {
                    string roleName = "Tenant";
                    role = NewUser(userModel, user, newuser, userroles, tenantRole, roleName);
                }
                else
                {
                    user = null;
                }

                if (newuser.UserId > 0)
                {
                    user.Mobile = newuser.Mobile;
                    user.Role = newuser.UserRole.Title;
                    user.Roles = userroles;
                    user.UserId = newuser.UserId;
                    user.Name = newuser.FullName;
                }
                else
                {
                    user = null;
                }

            }
            return user;
        }

        private UserRoleVm NewUser(UserModel userModel, UserVm user, User newuser, List<UserRoleVm> userroles, PmsUserVm vendorrole, string roleName)
        {
            UserRoleVm role = GetRole(roleName);
            userroles.Add(role);
            //userroles.Add(role);
            newuser.FullName = vendorrole.Name;
            newuser.Email = vendorrole.Email;

            newuser.CreatedOn = System.DateTime.Today;
            newuser.DeviceToken = userModel.DeviceToken;
            newuser.MetaUpdateRequired = false;
            newuser.Mobile = userModel.Mobile;
            _dbContext.Users.Add(newuser);
            _dbContext.SaveChanges();

            var userdevice = _dbContext.UserDeviceHistory.Where(p => p.DeviceToken == userModel.DeviceToken && p.UserId == newuser.UserId).FirstOrDefault();
            if (userdevice == null)
            {
                var device = new NewUserDevice();
                device.DeviceName = userModel.DeviceName;
                device.DeviceToken = userModel.DeviceToken;
                device.Type = userModel.Type;
                device.UserId = user.UserId;
                var newdevice = NewUserDevice(device);
            }

            return role;
        }

        public UserVm AddUser(UserModel userModel)
        {

            var newuser = new User();
            var userroles = new List<UserRoleVm>();
            newuser.CreatedOn = System.DateTime.Today;
            newuser.DeviceToken = userModel.DeviceToken;
            newuser.MetaUpdateRequired = false;
            newuser.Mobile = userModel.Mobile;
            newuser.FullName = userModel.Mobile;
            _dbContext.Users.Add(newuser);
            _dbContext.SaveChanges();
            var userrole = GetRole("Public");
            if (newuser.UserId > 0)
            {
                userroles.Add(userrole);
                var newUserRole = new UserRole();
                newUserRole.UserId = newuser.UserId;
                newUserRole.RoleId = userrole.RoleId;
                newUserRole.CreatedOn = System.DateTime.Today;
                newuser.UserRoleId = userrole.RoleId;
                _dbContext.UserRoles.Add(newUserRole);
                _dbContext.SaveChanges();


            }
            var user = new UserVm
            {
                Roles = userroles,
                Mobile = userModel.Mobile,
                Role = userrole.Role,
                UserId = newuser.UserId
            };
            return user;
        }
        public PmsUserVm GetVendor(UserModel userModel)
        {
            var user = _dbContext.ContactPerson.Where(x => x.MobileNo.ToLower() == userModel.Mobile.ToLower() && x.Suspend == 0)
                 .Select(p => new PmsUserVm
                 {
                     Email = p.Email,
                     Mobile = p.MobileNo,
                     Name = p.Name + "ABC",
                     Role = "Vendor",
                     UserId = p.ContactPersonId

                 }).FirstOrDefault();
            return user;
        }
        public PmsUserVm GetLandlord(UserModel userModel)
        {
            var user = _dbContext.Pms_LandLord.Where(x => x.Mobile.ToLower() == userModel.Mobile.ToLower() && x.Suspend == 0)
                 .Select(p => new PmsUserVm
                 {
                     Email = p.Email,
                     Mobile = p.Mobile,
                     Name = p.LandlordName,
                     Role = "Landlord",
                     UserId = p.LandlordId

                 }).FirstOrDefault();
            return user;
        }
        public PmsUserVm GetTenant(UserModel userModel)
        {
            
            var user = _dbContext.Pms_Tenant.Where(x => x.MobileCountryCode.Replace("00", "+") + x.MobileAreaCode + x.Mobile == userModel.Mobile && x.Suspend == 0)
                .Select(p => new PmsUserVm
                {
                    Email = p.Email,
                    Mobile = p.Mobile,
                    Name = p.FullName,
                    Role = "Tenant",
                    UserId = p.TenantId

                }).FirstOrDefault();
            return user;
        }
        public UserRoleVm GetRole(string role)
        {
            var userRole = _dbContext.Roles.Where(x => x.Title == role)
                .Select(p => new UserRoleVm
                {
                    Role = p.Title,
                    RoleId = p.RoleId
                }).FirstOrDefault();

            if (userRole == null)
            {
                var newRole = new Role
                {
                    Title = role,
                    Active = true
                };

                _dbContext.Roles.Add(newRole);
                _dbContext.SaveChanges();

                var newUserRole = new UserRoleVm
                {
                    Role = newRole.Title,
                    RoleId = newRole.RoleId
                };
                return newUserRole;
            }
            return userRole;
        }
        public bool GenteratePin(string MPIN, int userId)
        {
            var user = _dbContext.Users.Where(p => p.UserId == userId).FirstOrDefault();
            if (user != null)
            {
                user.Mpin = BC.HashPassword(MPIN);
                _dbContext.SaveChanges();
                return true;
            }
            return false;
        }
        public bool ValidatePin(string MPIN, int userId)
        {
            var user = _dbContext.Users.Where(p => p.UserId == userId).FirstOrDefault();
            if (user != null)
            {
                var verify = BC.Verify(MPIN, user.Mpin);
                if (verify == true)
                {
                    return true;
                }
            }
            return false;
        }
        public UserVm Login(UserLoginModel userLogin)
        {
            var usr = _dbContext.Users.Where(p => p.Mobile == userLogin.Mobile).FirstOrDefault();

            var user = _dbContext.Users.Where(p => p.Mobile == userLogin.Mobile)
                .Select(p => new UserVm
                {
                    Mobile = p.Mobile,
                    Name = p.FullName,
                    Role = p.UserRole.Title,
                    Roles = p.UserRoles
                    .Select(r => new UserRoleVm
                    {
                        Role = r.Role.Title,
                        RoleId = r.RoleId

                    }).ToList()
                }).FirstOrDefault();
            if (user != null)
            {
                var verify = BC.Verify(userLogin.Mobile, usr.Mpin);
                if (verify == true)
                {
                    var userdevice = _dbContext.UserDeviceHistory.Where(p => p.DeviceToken == userLogin.DeviceToken && p.UserId == user.UserId).FirstOrDefault();
                    if (userdevice == null)
                    {
                        var device = new NewUserDevice();
                        device.DeviceName = userLogin.DeviceName;
                        device.DeviceToken = userLogin.DeviceToken;
                        device.Type = userLogin.Type;
                        device.UserId = user.UserId;
                        var newdevice = NewUserDevice(device);
                    }
                    return user;
                }
                else
                {
                    user = null;
                }
            }
            return user;
        }
        public UserDevice NewUserDevice(NewUserDevice userDevice)
        {
            var newDevice = new UserDeviceHistory();
            if (userDevice != null)
            {
                newDevice.DateTime = System.DateTime.Today;
                newDevice.Device = userDevice.DeviceName;
                newDevice.UserId = userDevice.UserId;
                newDevice.DeviceToken = userDevice.DeviceToken;
                newDevice.Type = userDevice.Type;
                _dbContext.UserDeviceHistory.Add(newDevice);
                _dbContext.SaveChanges();
            }
            var userDeviceVm = new UserDevice
            {
                CreatedOn = newDevice.DateTime.ToString("MMM dd, yyy hh:mm tt"),
                DeviceName = newDevice.Device,
                DeviceToken = newDevice.DeviceToken,
                Type = userDevice.Type,
                UserId = newDevice.UserId
            };
            return userDeviceVm;
        }
        

    }
}
