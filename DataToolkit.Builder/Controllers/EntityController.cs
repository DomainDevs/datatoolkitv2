using DataToolkit.Builder.Models;
using DataToolkit.Builder.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Threading.Tasks;

namespace DataToolkit.Builder.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EntityController : ControllerBase
    {
        private readonly ScriptExtractionService _scriptService;
        private readonly EntityGenerator _entityGenerator;

        public EntityController(ScriptExtractionService scriptService, EntityGenerator entityGenerator)
        {
            _scriptService = scriptService;
            _entityGenerator = entityGenerator;
        }

        [HttpGet("table_metadata")]
        public async Task<IActionResult> GetTable([FromQuery] string schema = "dbo", [FromQuery] string tableName = "")
        {
            if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(tableName))
                return BadRequest("Schema y TableName son requeridos.");

            var table = await _scriptService.ExtractTableMetadataAsync(schema, tableName);

            if (table == null)
                return NotFound($"No se encontró metadata para {schema}.{tableName}");

            // Devuelve la metadata en JSON
            return Ok(table);
        }

        [HttpGet("generate")]
        public async Task<IActionResult> GenerateEntity([FromQuery] string schema = "dbo", [FromQuery] string tableName = "")
        {
            if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(tableName))
                return BadRequest("Schema y TableName son requeridos.");

            var table = await _scriptService.ExtractTableMetadataAsync(schema, tableName);

            if (table == null)
                return NotFound($"No se encontró metadata para {schema}.{tableName}");

            var entityCode = _entityGenerator.GenerateEntity(table);

            // Devuelve el código C# como texto
            return Ok(entityCode);
        }

        [HttpPost("generate/multiple")]
        public async Task<IActionResult> GenerateEntities([FromBody] EntityGenerationRequest request)
        {
            if (request == null || request.Tables == null || request.Tables.Count == 0)
                return BadRequest("Debe proporcionar al menos una tabla.");

            // 1. Extraer metadata individual
            var metadata = new Dictionary<string, DbTable>();

            foreach (var table in request.Tables)
            {
                var meta = await _scriptService.ExtractTableMetadataAsync(request.Schema, table);
                if (meta != null)
                    metadata[table] = meta;
            }

            if (metadata.Count == 0)
                return NotFound("No se encontró metadata para ninguna tabla.");

            // 2. Generación de entidades finales
            List<string> allCode = new();

            if (!request.GenerateNavigation)
            {
                // Sin navegación
                foreach (var item in metadata)
                {
                    string code = _entityGenerator.GenerateEntity(item.Value);
                    allCode.Add($"// ----- {item.Key} -----\n{code}");
                }
            }
            else
            {
                // Con navegación
                var enhancedTables = _entityGenerator.AddNavigationProperties(
                    metadata.Values.ToList(),
                    request.NavigationMode,
                    request.MaxDepth
                );

                foreach (var table in enhancedTables)
                {
                    string code = _entityGenerator.GenerateEntity(table);
                    allCode.Add($"// ----- {table.Name} -----\n{code}");
                }
            }

            // 3. Combinar todo en un solo string limpio
            var combinedCode = string.Join("\n\n", allCode);

            // 4. Devolver como texto plano
            return Content(combinedCode, "text/plain");
        }



    }

    // -------------------------
    // Request DTOs / Enums
    // -------------------------
    public enum NavigationMode
    {
        /// <summary>
        /// A -> Añade colecciones (ICollection&lt;T&gt;) en la tabla principal (la referenciada),
        /// p. ej. Departamento tiene ICollection&lt;Empleado&gt; Empleados.
        /// </summary>
        PrincipalCollections = 0,

        /// <summary>
        /// B -> Añade referencia (propiedad de navegación) en la tabla dependiente,
        /// p. ej. Empleado tiene Departamento Departamento (y [ForeignKey("DepartamentoId")] opcional).
        /// </summary>
        ReferenceOnDependent = 1
    }

    public class EntityGenerationRequest
    {
        /// <summary>
        /// Schema por defecto "dbo"
        /// </summary>
        [DefaultValue("dbo")]
        public string Schema { get; set; } = "dbo";

        /// <summary>
        /// Lista de tablas a generar (solo entre estas se crearán las relaciones/navegaciones)
        /// </summary>
        public List<string> Tables { get; set; } = new();

        /// <summary>
        /// Indica si se deben generar propiedades de navegación (solo entre tablas solicitadas)
        /// </summary>
        public bool GenerateNavigation { get; set; } = false;

        /// <summary>
        /// Modo de navegación: PrincipalCollections (A) o ReferenceOnDependent (B)
        /// </summary>
        public NavigationMode NavigationMode { get; set; } = NavigationMode.PrincipalCollections;

        /// <summary>
        /// Profundidad de búsqueda de relaciones (0 = solo tablas solicitadas, 1 = incluye tablas directamente relacionadas, etc.)
        /// </summary>
        public int MaxDepth { get; set; } = 0;
    }

}
