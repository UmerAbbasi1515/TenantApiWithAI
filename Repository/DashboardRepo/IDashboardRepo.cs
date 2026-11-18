using FABTenantApi.Models;
using System.Threading.Tasks;

namespace FABTenantD2Api.Repository.DashboardRepo
{
    public interface IDashboardRepo
    {
        Task<TenantDashboardVmV1> GetDataV1(string TanentId, string UserId);
       Task<GetOccupantOutstandingVM> GetOccupantOutstanding(int TenantId);
    }
}
