using DataToolkit.Builder.Models;
using DataToolkit.Builder.Services;
using DataToolkit.Library.Sql;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.Builder.Controllers;

[ApiController]
[Route("api/connection")]
public class ConnectionController : ControllerBase
{
    private readonly ISqlConnectionManager _connectionManager;

    public ConnectionController(ISqlConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    [HttpPost("open")]
    public IActionResult Open([FromBody] ConnectionRequest request)
    {
        try
        {
            _connectionManager.Connect(request.Server, request.Database, request.User, request.Password);
            return Ok("Conexión establecida.");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error al conectar: {ex.Message}");
        }
    }

    [HttpPost("close")]
    public IActionResult Close()
    {
        _connectionManager.Close();
        return Ok("Conexión cerrada.");
    }

    [HttpGet("test-query")]
    public IActionResult TestQuery()
    {
        if (!_connectionManager.IsConnected())
            return BadRequest("Conexión no activa.");

        using var executor = new SqlExecutor(_connectionManager.GetConnection());
        var result = executor.FromSql<dynamic>("SELECT name FROM sys.databases");
        return Ok(result);
    }
}

public class ConnectionRequest
{
    public string Server { get; set; } = "";
    public string Database { get; set; } = "";
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
}