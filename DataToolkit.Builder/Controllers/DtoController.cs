using DataToolkit.Builder.Models;
using DataToolkit.Builder.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Globalization;
using System.IO.Compression;

namespace DataToolkit.Builder.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DtoController : ControllerBase
    {
        private readonly ScriptExtractionService _scriptService;
        private readonly DtoGeneratorService _dtoGenerator;

        public DtoController(
            ScriptExtractionService scriptService,
            DtoGeneratorService dtoGenerator)
        {
            _scriptService = scriptService;
            _dtoGenerator = dtoGenerator;
        }

        // ---------------------------------------------------------
        // Obtener metadata de una tabla (igual al de Entity)
        // ---------------------------------------------------------
        [HttpGet("table_metadata")]
        public async Task<IActionResult> GetMetadata(
            [FromQuery] string schema = "dbo",
            [FromQuery] string tableName = "")
        {
            if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(tableName))
                return BadRequest("Schema y TableName son requeridos.");

            var table = await _scriptService.ExtractTableMetadataAsync(schema, tableName);

            if (table == null)
                return NotFound($"No se encontró metadata para {schema}.{tableName}");

            return Ok(table);
        }

        // ---------------------------------------------------------
        // Generar un DTO individual (sin cascada)
        // ---------------------------------------------------------
        [HttpGet("generate")]
        public async Task<IActionResult> GenerateSingle(
            [FromQuery] string schema = "dbo",
            [FromQuery] string tableName = "",
            [FromQuery] string domainName = "",
            [FromQuery] string mode = "response",
            [FromQuery] string operation = "Create")
        {
            if (string.IsNullOrWhiteSpace(schema) ||
                string.IsNullOrWhiteSpace(tableName) ||
                string.IsNullOrWhiteSpace(domainName))
                return BadRequest("Schema, TableName y DomainName son requeridos.");

            var meta = await _scriptService.ExtractTableMetadataAsync(schema, tableName);
            if (meta == null)
                return NotFound($"No metadata encontrada para {schema}.{tableName}");

            var code = _dtoGenerator.GenerateDto(meta, domainName, mode, operation);
            return Content(code, "text/plain");
        }

        // ---------------------------------------------------------
        // ❗GENERAR MÚLTIPLES DTOs → con CASCADA opcional
        // ---------------------------------------------------------
        [HttpPost("generate/multiple")]
        public async Task<IActionResult> GenerateMultiple([FromBody] DtoGenerationRequest request)
        {
            if (request == null || request.Tables == null || request.Tables.Count == 0)
                return BadRequest("Debe proporcionar al menos una tabla.");

            // 1. Extraer metadata
            var metadata = new Dictionary<string, DbTable>();

            foreach (var tableName in request.Tables)
            {
                var meta = await _scriptService.ExtractTableMetadataAsync(request.Schema, tableName);
                if (meta != null)
                    metadata[tableName] = meta;
            }

            if (metadata.Count == 0)
                return NotFound("No se encontró metadata para ninguna tabla.");

            // 2. Generar código para cada archivo
            var files = new Dictionary<string, string>();

            var tablesToProcess = request.GenerateNavigation
                ? _dtoGenerator.AddNavigationPropertiesForDtos(
                    metadata.Values.ToList(),
                    request.NavigationMode,
                    request.MaxDepth
                )
                : metadata.Values;

            DtoGeneratorService DtoGeneratorService = new DtoGeneratorService();
            foreach (var table in tablesToProcess)
            {
                string fileName;

                if (request.Operation.Equals("Create", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = $"{ToPascalCaseLocal(table.Name)}Create{request.Mode}Dto.cs";
                }
                else if (request.Operation.Equals("Update", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = $"{ToPascalCaseLocal(table.Name)}Update{request.Mode}Dto.cs";
                }
                else
                {
                    fileName = $"{ToPascalCaseLocal(table.Name)}{request.Operation}Dto.cs";
                }

                string code = _dtoGenerator.GenerateDto(
                    table,
                    request.DomainName,
                    request.Mode,
                    request.Operation
                );

                files[fileName] = code;
            }

            // 3. Crear ZIP en memoria
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    var zipEntry = archive.CreateEntry(file.Key, CompressionLevel.Fastest);
                    using var entryStream = zipEntry.Open();
                    using var streamWriter = new StreamWriter(entryStream);
                    streamWriter.Write(file.Value);
                }
            }

            ms.Position = 0;

            // 4. Devolver ZIP
            return File(
                ms.ToArray(),
                "application/zip",
                "Dtos.zip"
            );
        }

        private string ToPascalCaseLocal(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;

            var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[i].ToLower());
            }
            return string.Join("", parts);
        }

    }

    // -----------------------------------------------------
    // REQUEST usado para generación múltiple
    // -----------------------------------------------------
    public class DtoGenerationRequest
    {
        [DefaultValue("dbo")]
        public string Schema { get; set; } = "dbo";

        public List<string> Tables { get; set; } = new();

        public string DomainName { get; set; } = "";

        [DefaultValue("request")]
        public string Mode { get; set; } = "request";

        [DefaultValue("Create")]
        public string Operation { get; set; } = "Create";

        public bool GenerateNavigation { get; set; } = false;

        public NavigationMode NavigationMode { get; set; } = NavigationMode.PrincipalCollections;

        public int MaxDepth { get; set; } = 0;
    }
}
