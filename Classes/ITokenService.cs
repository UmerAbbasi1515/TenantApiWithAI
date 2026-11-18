using FABTenantApi.Models;

namespace FABTenantApi.Classes
{
    public interface ITokenService
    {
        string BuildToken(string key, string issuer, UserVm user);
        //string GenerateJSONWebToken(string key, string issuer, UserDTO user);
        bool IsTokenValid(string key, string issuer, string token);
        string SessionToken(string key, string issuer, UserSessionToken user);
    }
}
