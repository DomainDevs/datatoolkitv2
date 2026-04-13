namespace DataToolkit.Builder.Models;

public class ColumnComparisonResult
{
    public string ColumnName { get; set; } = "";
    public TableMetadata? SourceColumn { get; set; }
    public TableMetadata? TargetColumn { get; set; }
    public bool ExistsInSource { get; set; }
    public bool ExistsInTarget { get; set; }
    public bool IsDifferent { get; set; }
}
