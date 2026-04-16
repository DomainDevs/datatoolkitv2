namespace DataToolkit.Library.Common;

/// <summary>
/// Configuración global de comportamiento, diagnóstico y rendimiento para DataToolkit.
/// </summary>
public sealed class DataToolkitOptions
{
    /// <summary>
    /// Nombre de la sección en el archivo de configuración (appsettings.json).
    /// </summary>
    public const string SectionName = "DataToolkit";

    /// <summary>
    /// Habilita o deshabilita el logueo de eventos y flujo de la librería.
    /// </summary>
    public bool Logging { get; set; } = true;

    /// <summary>
    /// Habilita el rastreo de métricas de rendimiento (Stopwatch) para cada operación.
    /// </summary>
    public bool Metrics { get; set; } = true;

    /// <summary>
    /// Define el umbral en milisegundos para marcar una operación como "Lenta" en los logs.
    /// </summary>
    public int SlowMs { get; set; } = 500;

    /// <summary>
    /// Indica si se deben incluir los parámetros SQL en los logs de error (Cuidado con datos sensibles).
    /// </summary>
    public bool ShowParams { get; set; } = false;

    /// <summary>
    /// Prefijo que aparecerá en los logs para identificar los mensajes de la librería.
    /// </summary>
    public string Prefix { get; set; } = "DataToolkit";
}