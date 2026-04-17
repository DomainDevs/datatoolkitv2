namespace DataToolkit.Library.Fluent.Sql;

public sealed record SqlBinary(string Op, SqlNode Left, SqlNode Right) : SqlNode;