using DataToolkit.Builder.Connections;
using DataToolkit.Builder.Context;
using DataToolkit.Builder.Services;
using DataToolkit.Library.Connections;
using DataToolkit.Library.Extensions;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var conStringSQl = builder.Configuration.GetConnectionString("SqlServer") ??
     throw new InvalidOperationException("Connection string 'ConnectionStrings'" +
    " not found.");
var conSybase = builder.Configuration.GetConnectionString("SqlServer") ??
     throw new InvalidOperationException("Connection string 'ConnectionStrings'" +
    " not found.");

//Modo debug, resolver dependencias en el arrancque
#if DEBUG
builder.Services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = true,
    ValidateOnBuild = true
});
#endif

// --- Agregar controladores y Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Registrar DataToolkit con una �nica l�nea limpia ---
//O, para m�ltiples conexiones:
builder.Services.AddDataToolkitWith(options =>
{
    options.AddConnection("SqlServer", conStringSQl, DatabaseProvider.SqlServer);
    options.AddConnection("Sybase", conSybase, DatabaseProvider.Sybase);
    options.DefaultAlias = "SqlServer";
});

// Servicios espec�ficos del builder
builder.Services.AddScoped<MetadataService>();
builder.Services.AddScoped<ScriptExtractionService>();
builder.Services.AddScoped<EntityGenerator>(); // Registrar EntityGenerator

//Servicio conexi�n
builder.Services.AddHttpContextAccessor(); //Para poder manejar cache, SqlConnectionManager depende de IHttpContextAccessor
builder.Services.AddScoped<ConnectionContext>();
builder.Services.AddScoped<ISqlConnectionManager, SqlConnectionManager>();
builder.Services.AddSingleton<IUserConnectionStore, InMemoryUserConnectionStore>(); //Todas las instancias comparten el mismo almacenamiento


var app = builder.Build();

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();