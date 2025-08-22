using DataToolkit.Builder.Models;
using DataToolkit.Library.Sql;
using Microsoft.Data.SqlClient;
using DataToolkit.Builder.Context;
using System.Data;
using DataToolkit.Builder.Connections;

namespace DataToolkit.Builder.Services;


/*
🧠 ¿Cómo funciona?
Cada solicitud HTTP (API REST) crea un nuevo scope.
Dentro de ese scope, puedes resolver los servicios con AddScoped (por ejemplo, SqlConnectionManager).
Tu servicio SqlConnectionManager usa IHttpContextAccessor para identificar al usuario (por HttpContext.User.Identity.Name o por encabezados).
Según el usuario, recuperas su conexión desde un almacén en memoria (IUserConnectionStore), y la usas.
Si el usuario no tiene una conexión activa, se le puede devolver un error o permitir que la cree.

 * 🧱 Arquitectura (simplificada):
 HTTP Request (Usuario A)
 └─ Scoped: SqlConnectionManager
       └─ IHttpContextAccessor.HttpContext.User → "usuarioA"
       └─ InMemoryUserConnectionStore["usuarioA"] → SqlConnection abierta

HTTP Request (Usuario B)
 └─ Scoped: SqlConnectionManager
       └─ IHttpContextAccessor.HttpContext.User → "usuarioB"
       └─ InMemoryUserConnectionStore["usuarioB"] → otra SqlConnection abierta

 */
public class SqlConnectionManager : ISqlConnectionManager
{
    private readonly IUserConnectionStore _store;
    private SqlConnection? _connection;
    private readonly IHttpContextAccessor _httpContext;

    public SqlConnectionManager(IUserConnectionStore store, IHttpContextAccessor httpContext)
    {
        _store = store;
        _httpContext = httpContext;
    }

    public void Connect(string server, string database, string user, string password)
    {
        var connStr = $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate=True;MultipleActiveResultSets=true;";
        var userId = GetUserId();
        _store.Set(userId, connStr);
    }

    public bool IsConnected()
    {
        var connStr = _store.Get(GetUserId());
        if (connStr == null) return false;

        _connection = new SqlConnection(connStr);
        _connection.Open();

        return _connection.State == ConnectionState.Open;
    }

    public SqlConnection GetConnection()
    {
        if (_connection?.State == ConnectionState.Open) return _connection;

        var connStr = _store.Get(GetUserId());
        if (connStr == null)
            throw new InvalidOperationException("No hay conexión para este usuario.");

        _connection = new SqlConnection(connStr);
        _connection.Open();
        return _connection;
    }

    public void Close()
    {
        _connection?.Close();
        _store.Remove(GetUserId());
    }

    private string GetUserId()
    {
        // Aquí puedes leer desde JWT o header
        return _httpContext.HttpContext?.User?.Identity?.Name ?? "anonymous";
    }
}