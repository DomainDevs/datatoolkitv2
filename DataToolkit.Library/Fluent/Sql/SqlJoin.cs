namespace DataToolkit.Library.Fluent.Sql;

public sealed record SqlJoin(
    string Type,
    string Table,
    string On
) : SqlNode;