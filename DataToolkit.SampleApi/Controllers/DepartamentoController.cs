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
        var key = new Dictionary<string, object> { { "Id", id } };
        var depto = await _unitOfWork.GetRepository<Departamento>().GetByIdAsync(key);
        return depto == null ? NotFound() : Ok(depto);
    }

    // POST: /Departamento
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Departamento departamento)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _unitOfWork.GetRepository<Departamento>().InsertAsync(departamento);
        _unitOfWork.Commit();

        return CreatedAtAction(nameof(GetById), new { id = departamento.Id }, departamento);
    }

    // PUT: /Departamento/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] Departamento departamento)
    {
        if (departamento.Id != id)
            return BadRequest();

        await _unitOfWork.GetRepository<Departamento>().UpdateAsync(departamento);
        _unitOfWork.Commit();

        return NoContent();
    }

    // DELETE: /Departamento/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var key = new Dictionary<string, object> { { "Id", id } };
        await _unitOfWork.GetRepository<Departamento>().DeleteAsync(key);
        _unitOfWork.Commit();

        return NoContent();
    }
}
