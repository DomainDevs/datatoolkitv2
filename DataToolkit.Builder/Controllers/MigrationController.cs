using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using DataToolkit.Builder.Services;
using DataToolkit.Library.Connections;
using DataToolkit.Builder.Connections;

namespace DataToolkit.Builder.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MigrationController : ControllerBase
    {
        private readonly ISqlConnectionManager _activeConnectionManager;
        private readonly IUserConnectionStore _connectionStore;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MigrationMetadataService _metadataService;

        public MigrationController(
            ISqlConnectionManager activeConnectionManager,
            IUserConnectionStore connectionStore,
            IHttpContextAccessor httpContextAccessor)
        {
            _activeConnectionManager = activeConnectionManager;
            _connectionStore = connectionStore;
            _httpContextAccessor = httpContextAccessor;
            _metadataService = new MigrationMetadataService();
        }

        [HttpPost("compare-metadata")]
        public async Task<IActionResult> CompareMetadata([FromBody] TargetConnectionRequest request)
        {
            try
            {
                // Metadata de origen
                var sourceMetadata = await _metadataService.ExtractMetadataAsync(_activeConnectionManager);

                // Metadata de destino
                var targetConnectionManager = new SqlConnectionManager(_connectionStore, _httpContextAccessor);
                targetConnectionManager.Connect(request.Server, request.Database, request.User, request.Password);
                var targetMetadata = await _metadataService.ExtractMetadataAsync(targetConnectionManager);

                // Comparar
                var differences = _metadataService.CompareMetadata(sourceMetadata, targetMetadata);

                // 🔴 NUEVO: generar Work Files
                var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WF_OUTPUT");

                await _metadataService.GenerateWorkFilesAsync(
                    sourceMetadata,
                    targetMetadata,
                    outputPath
                );

                return Ok(new
                {
                    SourceCount = sourceMetadata.Count,
                    TargetCount = targetMetadata.Count,
                    Differences = differences,
                    WorkFilesPath = outputPath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al comparar metadata: {ex.Message}");
            }
        }


        public class TargetConnectionRequest
        {
            public string Server { get; set; } = "";
            public string Database { get; set; } = "";
            public string User { get; set; } = "";
            public string Password { get; set; } = "";
        }

    }
}
