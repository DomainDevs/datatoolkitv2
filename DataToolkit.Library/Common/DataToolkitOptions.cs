namespace DataToolkit.Library.Common;

public sealed class DataToolkitOptions
{
    public const string SectionName = "DataToolkit";

    // "UseLogs" o "Logging" es suficiente y mucho más corto
    public bool Logging { get; set; } = true;

    // "Metrics" o "Trace" resume perfectamente el rastreo de rendimiento
    public bool Metrics { get; set; } = true;

    // "SlowMs" es directo: el límite de milisegundos para lo lento
    public int SlowMs { get; set; } = 500;

    // "ShowParams" para la seguridad de los parámetros SQL
    public bool ShowParams { get; set; } = false;

    // "Prefix" ya se entiende que es para el log por el contexto
    public string Prefix { get; set; } = "DataToolkit";
}