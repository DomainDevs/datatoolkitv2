namespace DataToolkit.Library.Fluent.Sql;

public sealed record SqlFrom(List<string> Tables) : SqlNode;