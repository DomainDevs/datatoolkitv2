namespace DataToolkit.Library.Fluent.Sql;

public sealed record SqlOrderBy(List<string> Columns) : SqlNode;