using DataToolkit.Library.Connections;
using DataToolkit.Library.Extensions;

var builder = WebApplication.CreateBuilder(args);

var conStringSQl = builder.Configuration.GetConnectionString("SqlServer") ??
     throw new InvalidOperationException("Connection string 'ConnectionStrings'" +
    " not found.");
var conSybase = builder.Configuration.GetConnectionString("SqlServer") ??
     throw new InvalidOperationException("Connection string 'ConnectionStrings'" +
    " not found.");

// --- Agregar controladores y Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// --- Registrar DataToolkit con una única línea limpia ---
//O, para múltiples conexiones:
builder.Services.AddDataToolkitWith(options =>
{
    options.AddConnection("SqlServer", conStringSQl, DatabaseProvider.SqlServer);
    options.AddConnection("Sybase", conSybase, DatabaseProvider.Sybase);
    options.DefaultAlias = "SqlServer";
});

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
