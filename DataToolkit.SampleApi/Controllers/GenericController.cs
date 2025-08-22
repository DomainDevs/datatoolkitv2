using DataToolkit.Library.Sql;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace DataToolkit.SampleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenericController : ControllerBase
{
    private readonly SqlExecutor _sqlExecutor;

    public GenericController(SqlExecutor sqlExecutor)
    {
        _sqlExecutor = sqlExecutor;
    }

    // GET: api/generic/query-multiple
    [HttpGet("query-multiple")]
    public async Task<IActionResult> GetMultiResult()
    {
        // Nombre del procedimiento almacenado o SQL con múltiples SELECT
        var storedProcedure = "sp_get_empleados_y_departamentos";

        // Parámetros opcionales (si el SP los requiere)
        var parameters = new { };

        // Ejecutar el SP y obtener múltiples resultados
        var resultSets = await _sqlExecutor.QueryMultipleAsync(storedProcedure, parameters);

        if (resultSets.Count < 2)
            return BadRequest("No se devolvieron los dos conjuntos esperados");

        var empleados = resultSets[0];
        var departamentos = resultSets[1];

        return Ok(new
        {
            Empleados = empleados,
            Departamentos = departamentos
        });
    }

    [HttpPost("obtener-info-seguro")]
    public async Task<IActionResult> ObtenerInfoSeguro([FromBody] InfoSeguroRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Clave))
            return BadRequest("La clave es requerida.");

        var (rows, output) = await _sqlExecutor.ExecuteWithOutputAsync(
            "ObtenerConsecutivoSeguro",
            parameters =>
            {
                parameters.Add("@Clave", request.Clave);
                parameters.Add("@Consecutivo", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 12, scale: 0);
                //parameters.Add("@Prefijo", dbType: DbType.String, size: 10, direction: ParameterDirection.Output);
            });

        //var consecutivo = output.Get<decimal>("@Consecutivo");
        //var prefijo = output.Get<string>("@Prefijo");

        return Ok(new
        {
            Clave = request.Clave,
            Consecutivo = output.Get<decimal>("@Consecutivo") //consecutivo
            //,Prefijo = prefijo
        });
    }

    public class ConsecutivoRequest
    {
        public string Clave { get; set; }
    }
    public class InfoSeguroRequest
    {
        public string Clave { get; set; }
    }
}
