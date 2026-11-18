using FABTenantApi.Classes;
using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Data;
using System.Threading.Tasks;

namespace FABTenantD2Api.DAL
{
    public class DBConnectionLogic : IDBConnectionLogic
    {
        private readonly string conString;
        private readonly Settings _settings;

        public DBConnectionLogic()
        {
            _settings = new Settings();
            conString = _settings.GetConnectionString();
        }
        public async Task<DataSet> ExecuteSpDataSet(string SpName, Hashtable Param, string DataSetName)
        {
            DataSet ds = new DataSet(DataSetName);
            try
            {
                using (SqlConnection SqlConnection = new SqlConnection(conString))
                {
                    using (SqlCommand SqlCommand = SqlConnection.CreateCommand())
                    {
                        using (SqlDataAdapter DbAdapter = new SqlDataAdapter())
                        {
                            if (SqlConnection != null && SqlConnection.State == 0)
                            {
                                SqlConnection.Open();
                            }
                            else if (SqlConnection != null && (SqlConnection.State.Equals(ConnectionState.Connecting)))
                            {
                                SqlConnection.Dispose();
                                SqlConnection.Open();
                            }
                            SqlCommand.Connection = SqlConnection;
                            SqlCommand.CommandText = SpName;
                            SqlCommand.CommandType = CommandType.StoredProcedure;//CommandType.StoredProcedure;
                            SqlCommand.Parameters.Clear();
                            SqlCommand.CommandTimeout = 60;
                            foreach (string para in Param.Keys)
                            {
                                SqlCommand.Parameters.AddWithValue(para, Param[para]);
                            }
                            DbAdapter.SelectCommand = (SqlCommand);
                            DbAdapter.Fill(ds);
                            DbAdapter.Dispose();
                            SqlCommand.Dispose();
                            return await Task.FromResult(ds);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
