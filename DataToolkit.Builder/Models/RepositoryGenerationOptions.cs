namespace DataToolkit.Builder.Models
{
    public class RepositoryGenerationOptions
    {
        public string Schema { get; set; } = "dbo";
        public string TableName { get; set; } = string.Empty;

        public bool EnableInsert { get; set; } = true;
        public bool EnableUpdate { get; set; } = true;
        public bool EnableDelete { get; set; } = true;
        public bool EnableSelectByKey { get; set; } = true;
        public bool EnableSelectAll { get; set; } = true;
    }
}
