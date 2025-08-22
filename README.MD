# DataToolkit.Library

Una librería liviana y extensible de acceso a datos en C# basada en **Dapper**, diseñada para trabajar con múltiples motores de 
base de datos como SQL Server y Sybase. Incluye soporte para Unit of Work, ejecución de SQL, procedimientos almacenados, 
multi-mapping, metadatos de entidades y más.

---

## ✨ Características

* ✅ Soporte multi-DB con `IDbConnectionFactory` y alias
* ✅ Ejecución de SQL interpolado (`FromSqlInterpolated`)
* ✅ Multi-mapping con Dapper (`FromSqlMultiMap`)
* ✅ Soporte para transacciones (`UnitOfWork`)
* ✅ Repositorio genérico (`GenericRepository<T>`)
* ✅ Lectura de metadatos vía atributos (\[Key], \[Column], \[Required], etc.)
* ✅ Extensible con SPs (`StoredProcedureExecutor`)
* ✅ Preparado para integración con `ModelBuilder` futuro

---

## 🧰 Instalación

Agrega el proyecto como referencia en tu solución o compílalo como paquete NuGet.

---


## 🔧 Configuración de servicios (Dependency Injection)

```csharp
//Simple conexión, a una base de datos
builder.Services.AddDataToolkitSqlServer(
    "Server=localhost;Database=pruebasdb;User Id=sqluser;Password=contrasena123;" +
    "Pooling=true;Min Pool Size=3;Max Pool Size=30;" +
    "Connection Timeout=15;APP=LIB;Language=us_english;" +
    "Encrypt=True;TrustServerCertificate=True;"
);

//Una conexión
builder.Services.AddDataToolkitSqlServer(
    "Server=localhost;Database=pruebasdb;User Id=sqluser;Password=contrasena123;" +
    "Pooling=true;Min Pool Size=3;Max Pool Size=30;" +
    "Connection Timeout=15;APP=LIB;Language=us_english;" +
    "Encrypt=True;TrustServerCertificate=True;",
    alias: "SqlServer");

//O, para múltiples conexiones:
builder.Services.AddDataToolkitWith(options =>
{
    options.AddConnection("SqlServer", "Server=localhost;Database=pruebasdb;User Id=sqluser;Password=contrasena123;" +
    "Pooling=true;Min Pool Size=3;Max Pool Size=30;" +
    "Connection Timeout=15;APP=LIB;Language=us_english;" +
    "Encrypt=True;TrustServerCertificate=True;", DatabaseProvider.SqlServer);
    options.AddConnection("Sybase", "Host=myhost;Port=5000;Database=DB;...", DatabaseProvider.Sybase);
    options.DefaultAlias = "SqlServer";
});
```

## 🚀 Ejemplos de uso
Ejecutar un SQL con parámetros interpolados

var result = sql.FromSqlInterpolated<Empleado>($"SELECT * FROM Empleado WHERE Estado = {1}");

## Lectura metadatos
* [Key]: Se utiliza para especificar la propiedad o propiedades que actuarán como clave principal de una entidad
* [DatabaseGenerated(DatabaseGeneratedOption.Identity)]: El valor de la propiedad de identidad depende del proveedor de la base de datos.
* [Column]: Permite especificar el nombre de la columna.
* [Required]: Se utiliza en la declaración de propiedades o campos para indicar que el campo es requerido
* [NotMapped]: Una propiedad de una entidad no debe ser mapeada a una columna en la base de datos
* [JsonIgnore]: no se incluirá en la salida JSON generada por el serializador, ni será considerado al deserializar desde JSON
* [ValidateNever]: Si tienes un modelo con varias propiedades y quieres asegurarte de que una de ellas no se valide. 


## Multi-mapping entre entidades

var empleados = sql.FromSqlMultiMap<EmpleadoDto>(new SqlMultiMapRequest
{
    Sql = "EXEC sp_empleados_departamentos_empresas",
    Types = new[] { typeof(Empleado), typeof(Departamento), typeof(Empresa) },
    SplitOn = "DepartamentoId,EmpresaId",
    MapFunction = objects =>
    {
        var e = (Empleado)objects[0];
        var d = (Departamento)objects[1];
        var emp = (Empresa)objects[2];
        return new EmpleadoDto { Nombre = e.Nombre, Departamento = d.Nombre, Empresa = emp.Nombre };
    }
});


## 🧩 Estructura del proyecto

DataToolkit.Library/ 
├── Common/ 
├── Connections/ 
├── Extensions/ 
├── Metadata/ 
├── Repositories/ 
├── Sql/ 
├── StoredProcedures/ 
└── UnitOfWork/ 

## 📦 Dependencias
Dapper
AdoNetCore.AseClient
Microsoft.Data.SqlClient

## 📌 Requisitos

.NET 6.0 o superior
SQL Server, Sybase u otros motores configurables

## 📣 Licencia

MIT

## ✍️ Autor

Fernando Poveda · @github.com/DomainDevs