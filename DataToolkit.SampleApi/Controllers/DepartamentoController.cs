using DataToolkit.Library.UnitOfWorkLayer;
using DataToolkit.SampleApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.SampleApi.Controllers;

[ApiController]
[Route("[controller]")]
public class DepartamentoController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public DepartamentoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Departamento
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var repo = _unitOfWork.GetRepository<Departamento>();
        var departamentos = await repo.GetAllAsync();
        return Ok(departamentos);
    }

    // GET: /Departamento/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var repo = _unitOfWork.GetRepository<Departamento>();

        // Crear instancia con la llave primaria
        var deptoKey = new Departamento { Id = id };
        var depto = await repo.GetByIdAsync(deptoKey);

        return depto == null ? NotFound() : Ok(depto);
    }

    // POST: /Departamento
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Departamento departamento)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var repo = _unitOfWork.GetRepository<Departamento>();
        await repo.InsertAsync(departamento);
        _unitOfWork.Commit();

        return CreatedAtAction(nameof(GetById), new { id = departamento.Id }, departamento);
    }

    // PUT: /Departamento/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] Departamento departamento)
    {
        if (departamento.Id != id)
            return BadRequest();

        var repo = _unitOfWork.GetRepository<Departamento>();
        await repo.UpdateAsync(departamento);
        _unitOfWork.Commit();

        return NoContent();
    }

    // DELETE: /Departamento/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var repo = _unitOfWork.GetRepository<Departamento>();

        // Crear instancia con la llave primaria
        var deptoKey = new Departamento { Id = id };
        await repo.DeleteAsync(deptoKey);
        _unitOfWork.Commit();

        return NoContent();
    }
}
