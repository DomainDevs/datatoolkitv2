namespace DataToolkit.Builder.Models
{
    public class DbConnectionRequest
    {
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = "master"; // por defecto
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
