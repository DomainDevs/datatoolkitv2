using System.Diagnostics;
using Serilog;

namespace DataToolkit.Library.Common;

internal static class ToolkitTelemetry
{
    public static Stopwatch? Iniciar(DataToolkitOptions opt)
        => opt.Metrics ? Stopwatch.StartNew() : null;

    public static void Finalizar(ILogger logger, DataToolkitOptions opt, string accion, Stopwatch? sw)
    {
        if (sw == null) return;
        sw.Stop();

        var ms = sw.ElapsedMilliseconds;

        if (ms >= opt.SlowMs)
        {
            logger.Warning("[{Prefix}] ⚠️ RENDIMIENTO: {Accion} tardó {ms}ms", opt.Prefix, accion, ms);
        }
        else if (opt.Logging)
        {
            logger.Information("[{Prefix}] {Accion} finalizada ({ms}ms)", opt.Prefix, accion, ms);
        }
    }

    public static void Error(ILogger logger, DataToolkitOptions opt, string accion, Exception ex, Stopwatch? sw)
    {
        sw?.Stop();
        var ms = sw?.ElapsedMilliseconds ?? 0;

        logger.Error(ex, "[{Prefix}] ❌ ERROR: {Accion} ({ms}ms). {Msg}", opt.Prefix, accion, ms, ex.Message);

        if (opt.Logging)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[{opt.Prefix}] CRITICAL: {accion} ({ms}ms) -> {ex.Message}");
            Console.ResetColor();
        }
    }
}