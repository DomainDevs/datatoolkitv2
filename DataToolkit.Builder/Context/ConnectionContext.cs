using Microsoft.Data.SqlClient;
using System.Data;

namespace DataToolkit.Builder.Context;

public class ConnectionContext
{
    public string? ConnectionString { get; private set; }
    public SqlConnection? SqlConnection { get; private set; }

    public bool IsConnected => SqlConnection?.State == ConnectionState.Open;

    public void Connect(string server, string database, string user, string password)
    {
        ConnectionString = $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate=True;MultipleActiveResultSets=true;";
        SqlConnection = new SqlConnection(ConnectionString);
        SqlConnection.Open();
    }

    public void Close()
    {
        if (SqlConnection?.State == ConnectionState.Open)
            SqlConnection.Close();
    }
}