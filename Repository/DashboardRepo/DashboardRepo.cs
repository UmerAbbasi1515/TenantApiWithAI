 using FABTenantApi.Classes;
using FABTenantApi.Models;
using FABTenantD2Api.DAL;
using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Data;
using System.Threading.Tasks;

namespace FABTenantD2Api.Repository.DashboardRepo
{
    public class DashboardRepo : IDashboardRepo
    {
        private readonly IDBConnectionLogic _dbConnectionLogic;
        private readonly Settings _settings;
        public DashboardRepo(IDBConnectionLogic dbConnectionLogic)
        {
            _dbConnectionLogic = dbConnectionLogic;
            _settings = new Settings();
        }

        public async Task<TenantDashboardVmV1> GetDataV1(string TenantId, string UserId)
        {
            TenantDashboardVmV1 tenantDashboardVm = new TenantDashboardVmV1();

            try
            {
                string spName = @"FAB_PROC_TENANT_D2_DASHBOARD_GET_DATA";

                Hashtable Param = new Hashtable
                {
                    { "@TenantId", TenantId },
                    { "@UserId", UserId }
                };

                DataSet ds = await _dbConnectionLogic.ExecuteSpDataSet(spName, Param, "GetData");
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        string FullName = ds.Tables[0].Rows[0]["FullName"].ToString();
                        string FullNameAR = ds.Tables[0].Rows[0]["FullNameAR"].ToString();

                        tenantDashboardVm.TenantName = string.IsNullOrEmpty(FullName.Trim()) ? "Name" : FullName;
                        tenantDashboardVm.TenantNameAR = string.IsNullOrEmpty(FullNameAR.Trim()) ? "Name" : FullNameAR;
                    }
                    if (ds.Tables[1].Rows.Count > 0)
                    {
                        tenantDashboardVm.ActiveContracts = Convert.ToInt32(ds.Tables[1].Rows[0]["ActiveContracts"].ToString());
                        //tenantDashboardVm.RentalValues = Convert.ToDecimal(ds.Tables[1].Rows[0]["RentalValues"].ToString());
                        tenantDashboardVm.ContractExpiringIn30Days = Convert.ToInt32(ds.Tables[1].Rows[0]["ContractExpiringIn30Days"].ToString());
                        tenantDashboardVm.CheckDueIn30Days = Convert.ToInt32(ds.Tables[1].Rows[0]["CheckDueIn30Days"].ToString());
                        tenantDashboardVm.UnreadNotificationCount = Convert.ToInt32(ds.Tables[1].Rows[0]["UnreadNotificationCount"].ToString());
                        tenantDashboardVm.ContractRenewalActionCount = Convert.ToInt32(ds.Tables[1].Rows[0]["ContractRenewalActionCount"].ToString());
                    }
                }
                return await Task.FromResult(tenantDashboardVm);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<GetOccupantOutstandingVM> GetOccupantOutstanding(int TenantId)
        {
            GetOccupantOutstandingVM paymendata = new GetOccupantOutstandingVM();

            try
            {
                string spName = @"pms_OccupantOutstanding";

                Hashtable Param = new Hashtable
                {
                    { "@OccupantId", TenantId },
                };

                DataSet ds = await _dbConnectionLogic.ExecuteSpDataSet(spName, Param, "GetData");
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {                       

                        paymendata.ActiveTcRent =Convert.ToDecimal(ds.Tables[0].Rows[0]["ActiveTcRent"]);
                        paymendata.ActiveTcRentBalance = Convert.ToDecimal(ds.Tables[0].Rows[0]["ActiveTcRentBalance"]);
                        paymendata.TotalLiability = Convert.ToDecimal(ds.Tables[0].Rows[0]["TotalLiability"]);
                    }
                   
                }
                return await Task.FromResult(paymendata);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
