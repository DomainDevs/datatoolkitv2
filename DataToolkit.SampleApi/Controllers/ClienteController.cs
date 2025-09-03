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
        var clientes = await repo.GetAllAsync();
        return Ok(clientes);
    }

    // GET: /Cliente/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var repo = _unitOfWork.GetRepository<Cliente>();

        // Llenar solo la llave primaria
        var cliente = await repo.GetByIdAsync(new Cliente { Id = id });

        return cliente == null ? NotFound() : Ok(cliente);
    }


    // POST: /Cliente
    [HttpPost]
    public async Task<ActionResult> Post([FromBody] Cliente cliente)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var repo = _unitOfWork.GetRepository<Cliente>();
        await repo.InsertAsync(cliente);
        _unitOfWork.Commit();

        return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, cliente);
    }

    // PUT: /Cliente/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> Put(int id, [FromBody] Cliente cliente)
    {
        if (cliente.Id != id)
            return BadRequest();

        var repo = _unitOfWork.GetRepository<Cliente>();
        await repo.UpdateAsync(cliente);
        _unitOfWork.Commit();

        return NoContent();
    }

    // DELETE: /Cliente/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var repo = _unitOfWork.GetRepository<Cliente>();

        // Crear una instancia de Cliente solo con la llave primaria
        var clienteKey = new Cliente { Id = id };
        await repo.DeleteAsync(clienteKey);
        _unitOfWork.Commit();

        return NoContent();
    }
}
