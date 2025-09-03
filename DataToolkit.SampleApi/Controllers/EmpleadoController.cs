using DataToolkit.Library.Common;
using DataToolkit.Library.Sql;
using DataToolkit.Library.UnitOfWorkLayer;
using DataToolkit.SampleApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.SampleApi.Controllers;

[ApiController]
[Route("[controller]")]
public class EmpleadoController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SqlExecutor _sql;

    public EmpleadoController(IUnitOfWork unitOfWork, SqlExecutor sql)
    {
        _unitOfWork = unitOfWork;
        _sql = sql;
    }

    // GET: /Empleado
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sqlv = @"
        SELECT 
        e.Id AS Id,
        e.Nombre AS Nombre,
        e.DepartamentoId AS DeptId,
        d.Id,
        d.Nombre
        FROM Empleado e
        JOIN Departamento d ON e.DepartamentoId = d.Id";

        var request = new MultiMapRequest
        {
            Sql = sqlv,
            Types = new[] { typeof(Empleado), typeof(Departamento) },
            SplitOn = "DeptId",
            MapFunction = objects =>
            {
                var empleado = (Empleado)objects[0];
                var depto = (Departamento)objects[1];
                empleado.Departamento = depto;
                return empleado;
            }
        };

        var result = await _sql.FromSqlMultiMapAsync<Empleado>(request);
        return Ok(result);
    }

    // GET: /Empleado/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var repo = _unitOfWork.GetRepository<Empleado>();

        // Crear instancia con la llave primaria
        var empleadoKey = new Empleado { Id = id };
        var empleado = await repo.GetByIdAsync(empleadoKey);

        return empleado == null ? NotFound() : Ok(empleado);
    }

    // POST: /Empleado
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Empleado empleado)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _unitOfWork.GetRepository<Empleado>().InsertAsync(empleado);
        _unitOfWork.Commit();

        return CreatedAtAction(nameof(GetById), new { id = empleado.Id }, empleado);
    }

    // PUT: /Empleado/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] Empleado empleado)
    {
        if (empleado.Id != id)
            return BadRequest();

        await _unitOfWork.GetRepository<Empleado>().UpdateAsync(empleado);
        _unitOfWork.Commit();

        return NoContent();
    }

    // DELETE: /Empleado/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var repo = _unitOfWork.GetRepository<Empleado>();

        // Crear instancia con la llave primaria
        var empleadoKey = new Empleado { Id = id };
        await repo.DeleteAsync(empleadoKey);

        _unitOfWork.Commit();
        return NoContent();
    }

}
