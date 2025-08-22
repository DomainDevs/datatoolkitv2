﻿
namespace DataToolkit.Library.Common;
/// <summary>Permite encapsular consultas con mapeo múltiple (multi-mapping).
/// <param Sql="">Sentencias SQL</param>
/// <param Parameters="">Parámetros para la consulta</param>
/// <param Types="">Tipos involucrados (Ej: typeof(Empleado), typeof(Departamento))</param>
/// <param SplitOn="">Columna donde Dapper hace el corte entre entidades</param>
/// <param MapFunction="">Función que construye el objeto final</param>
/// <returns></returns>
/// </summary>
public class MultiMapRequest
{
    public string Sql { get; set; }
    public object Parameters { get; set; }
    public string SplitOn { get; set; } = "Id";
    public Type[] Types { get; set; }
    public Func<object[], object> MapFunction { get; set; }
}