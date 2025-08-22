using Microsoft.Data.SqlClient;

namespace DataToolkit.Builder.Services
{
    public interface ISqlConnectionManager
    {
        void Close();
        void Connect(string server, string database, string user, string password);
        SqlConnection GetConnection();
        bool IsConnected();
    }
}