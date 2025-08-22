
using DataToolkit.Library.UnitOfWorkLayer;
using DataToolkit.SampleApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataToolkit.SampleApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ClienteController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ClienteController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Cliente
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var repo = _unitOfWork.GetRepository<Cliente>();
        var clientes = await repo.GetAllAsync(); // ✔️ aquí sí lo esperas
        return Ok(clientes);
    }

    // GET: /Cliente/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var key = new Dictionary<string, object>
    {
        { "Id", id }
    };

        var cliente = await _unitOfWork.GetRepository<Cliente>().GetByIdAsync(key);

        return cliente == null ? NotFound() : Ok(cliente);
    }

    // POST: /Cliente
    [HttpPost]
    public async Task<ActionResult> Post([FromBody] Cliente cliente)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _unitOfWork.GetRepository<Cliente>().InsertAsync(cliente);
        _unitOfWork.Commit();

        return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, cliente);
    }

    // PUT: /Cliente/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> Put(int id, [FromBody] Cliente cliente)
    {
        if (cliente.Id != id)
            return BadRequest();

        await _unitOfWork.GetRepository<Cliente>().UpdateAsync(cliente);
        _unitOfWork.Commit();

        return NoContent();
    }

    // DELETE: /Cliente/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var key = new Dictionary<string, object>
            {
                { "Id", id }
            };

        await _unitOfWork.GetRepository<Cliente>().DeleteAsync(key);
        _unitOfWork.Commit();

        return NoContent();
    }
}
