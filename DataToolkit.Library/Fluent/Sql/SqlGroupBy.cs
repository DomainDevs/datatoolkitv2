namespace DataToolkit.Library.Fluent.Sql;

public sealed record SqlGroupBy(List<string> Columns) : SqlNode;