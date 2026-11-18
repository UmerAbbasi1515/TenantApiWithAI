using System.Collections;
using System.Data;
using System.Threading.Tasks;

namespace FABTenantD2Api.DAL
{
    public interface IDBConnectionLogic
    {
        Task<DataSet> ExecuteSpDataSet(string SpName, Hashtable Param, string DataSetName);
    }
}
