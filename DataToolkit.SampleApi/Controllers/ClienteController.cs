using Azure.Core;
using DataToolkit.Library.UnitOfWorkLayer;
using DataToolkit.SampleApi.Models;
using Microsoft.AspNetCore.Mvc;
using static Dapper.SqlMapper;

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
    public async Task<ActionResult> Put(int id, [FromBody] Cliente request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.Id != id)
            return BadRequest();

        var cliente = new Cliente
        {
            Id = request.Id,
            Nombre = request.Nombre,
            Apellido = request.Apellido
        };

        var repo = _unitOfWork.GetRepository<Cliente>();
        await repo.UpdateAsync(cliente, c => c.Nombre, c => c.Apellido);
        _unitOfWork.Commit();
        return NoContent();

        /*
        var repo = _unitOfWork.GetRepository<Cliente>();
        await repo.UpdateAsync(cliente);
        _unitOfWork.Commit();

        return NoContent();

        var allProps = _meta.Properties.Select(p => p.Name).ToArray();
        return UpdateAsync(entity, allProps);
        */


        // Actualizar solo el Nombre y apellido
        //return await _clienteRepository.UpdateAsync(cliente, c => c.Nombre, c => c.Apellido);
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
