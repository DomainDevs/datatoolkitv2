using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "MyApp");

        // URL del repo (normal o API recursiva)
        //string inputUrl = "https://github.com/DomainDevs/Hogar350";
        string inputUrl = "https://github.com/DomainDevs/MigrationEngineRP";
        //string inputUrl = "https://github.com/DomainDevs/Isoat/tree/main/04_Construction/Source/WebService/Web/UI/WebService";
        //string inputUrl = "https://github.com/DomainDevs/Prototype";
    

        // Tipos de archivo que quieres listar (pueden ser múltiples)
        //string[] allowedExtensions = new[] { ".json", ".css", ".js", ".htm", ".html", ".vue",  };
        string[] allowedExtensions = new[] { ".json", ".cs", ".html", ".conmgr", ".dtsx", ".sln", ".csproj" };


        string apiUrl = TransformToRecursiveApi(inputUrl);

        var response = await client.GetStringAsync(apiUrl);

        using var doc = JsonDocument.Parse(response);

        foreach (var item in doc.RootElement.GetProperty("tree").EnumerateArray())
        {
            if (item.GetProperty("type").GetString() != "blob")
                continue;

            string path = item.GetProperty("path").GetString();

            // Verifica si el path termina con alguna de las extensiones permitidas
            bool match = false;
            foreach (var ext in allowedExtensions)
            {
                if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    match = true;
                    break;
                }
            }

            if (!match)
                continue;

            string rawUrl = GetRawUrl(inputUrl, path);
            Console.WriteLine(rawUrl);
        }
    }

    static string TransformToRecursiveApi(string url)
    {
        if (url.Contains("api.github.com"))
            return url;

        var match = Regex.Match(url, @"github\.com/([^/]+)/([^/]+)");
        if (!match.Success)
            throw new ArgumentException("URL no válida de GitHub.");

        string user = match.Groups[1].Value;
        string repo = match.Groups[2].Value;

        return $"https://api.github.com/repos/{user}/{repo}/git/trees/main?recursive=1";
    }

    static string GetRawUrl(string inputUrl, string path)
    {
        var match = Regex.Match(inputUrl, @"github\.com/([^/]+)/([^/]+)");
        string user = match.Groups[1].Value;
        string repo = match.Groups[2].Value;

        return $"https://raw.githubusercontent.com/{user}/{repo}/main/{path}";
    }
}