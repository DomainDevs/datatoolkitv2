using System.Text.Json;

class Program
{
    static async Task Main()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "MyApp"); // obligatorio

        //https://api.github.com/repos/DomainDevs/migrationsps/git/trees/main?recursive=1
        //https://api.github.com/repos/DomainDevs/Prototype/git/trees/main?recursive=1

        //https://raw.githubusercontent.com/DomainDevs/migrationsps/refs/heads/main/{path}
        //https://raw.githubusercontent.com/DomainDevs/migrationsps/refs/heads/main/{path}


        string url = "https://api.github.com/repos/DomainDevs/migrationsps/git/trees/main?recursive=1";
        var response = await client.GetStringAsync(url);

        using JsonDocument json = JsonDocument.Parse(response);
        var tree = json.RootElement.GetProperty("tree");

        string outputFile = @"C:\Temp\listaMigra1.txt";
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

        using StreamWriter writer = new StreamWriter(outputFile, false);

        foreach (var item in tree.EnumerateArray())
        {
            string path = item.GetProperty("path").GetString();
            string type = item.GetProperty("type").GetString();

            if (type == "blob" && (path.EndsWith(".sql") || path.EndsWith(".bat")))
            {
                // URL directa tipo raw
                string rawUrl = $"https://raw.githubusercontent.com/DomainDevs/migrationsps/refs/heads/main/{path}";
                await writer.WriteLineAsync(rawUrl);
                Console.WriteLine(rawUrl); // opcional: ver en consola
            }
        }

        Console.WriteLine($"\nLista de URLs directas guardada en {outputFile}");
    }
}