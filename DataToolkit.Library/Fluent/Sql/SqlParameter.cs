namespace DataToolkit.Library.Fluent.Sql;

public sealed record SqlParameter(string Name, object? Value) : SqlNode;