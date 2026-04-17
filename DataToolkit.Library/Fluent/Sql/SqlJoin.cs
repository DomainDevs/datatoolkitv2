namespace DataToolkit.Library.Fluent.Sql;

public enum JoinType
{
    Inner,
    Left,
    Right,
    Full
}

public sealed record SqlJoin(
    JoinType Type,
    string Table,
    SqlNode On
) : SqlNode;