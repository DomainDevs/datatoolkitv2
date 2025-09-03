using System.Text;
using System.Text.RegularExpressions;

namespace DataToolkit.Builder.Helpers;

public static class ScriptUtils
{
    /// <summary>
    /// Normaliza saltos de línea y tabulaciones para que se vean bien en JSON o consola.
    /// </summary>
    public static string NormalizeSqlScript(string input) =>
        input
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\t", "    ")
            .Trim();
    
    /// <summary>
    /// Genera la cabecera DROP IF EXISTS para cualquier objeto SQL.
    /// </summary>
    public static string SetHeaderScript(string schema, string name, string objectType)
    {
        var typeKeyword = GetTypeKeyword(objectType);
        var fullName = $"[{schema}].[{name}]";

        return $"""
            IF OBJECT_ID('{fullName}') IS NOT NULL
            BEGIN
                DROP {typeKeyword} {fullName}
                IF OBJECT_ID('{fullName}') IS NOT NULL
                    PRINT '<<< FAILED DROPPING {typeKeyword} {fullName} >>>'
                ELSE
                    PRINT '<<< DROPPED {typeKeyword} {fullName} >>>'
            END
            GO

            """;}

    /// <summary>
    /// Genera el footer con validación de creación y, para SP, añade sp_procxmode.
    /// </summary>
    public static string SetFooterScript(string schema, string name, string objectType)
    {
        var fullName = $"{schema}.{name}";
        var typeKeyword = GetTypeKeyword(objectType);

        var sb = new StringBuilder($"""
            IF EXISTS (
                SELECT 1
                FROM sysobjects
                WHERE id = OBJECT_ID('{fullName}')
                AND type = '{objectType}'
            )
            BEGIN
                PRINT '<<CREATE {typeKeyword} {fullName} SUCCESSFUL!! >>'
            END
            GO

            """);

        return sb.ToString();
    }

    /// <summary>
    /// Reemplaza el encabezado CREATE para que incluya el esquema,
    /// soportando espacios/tabulaciones arbitrarios.
    /// </summary>
    public static string ReplaceCreateHeader(string script, string name, string schema, string objectType)
    {
        var typeKeyword = GetTypeKeyword(objectType);
        var pattern = $@"CREATE\s+{typeKeyword}\s+{Regex.Escape(name)}";
        var replacement = $"CREATE {typeKeyword} [{schema}].[{name}]";

        return Regex.Replace(script, pattern, replacement, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Mapea el código de objeto a la palabra clave SQL.
    /// </summary>
    private static string GetTypeKeyword(string objectType) =>
        objectType.ToUpper() switch
        {
            "P" => "PROCEDURE",
            "V" => "VIEW",
            "U" => "TABLE",
            "FN" or "TF" or "IF" => "FUNCTION",
            "TR" => "TRIGGER",
            _ => "OBJECT"
        };

}
