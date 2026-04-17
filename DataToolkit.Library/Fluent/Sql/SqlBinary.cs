namespace DataToolkit.Library.Fluent.Sql;

public sealed record SqlBinary(SqlNode Left, string Op, SqlNode Right) : SqlNode;