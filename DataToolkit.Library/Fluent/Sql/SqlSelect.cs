namespace DataToolkit.Library.Fluent.Sql;

public sealed record SqlSelect(List<string> Columns) : SqlNode;