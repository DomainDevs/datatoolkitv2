namespace DataToolkit.Builder.Models
{
    public class TableMetadata
    {
        public string SchemaName { get; set; } = "";
        public string TableName { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public string DataType { get; set; } = "";
        public string MaxLength { get; set; } = "";
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public string IsNullable { get; set; } = "";
        public string IsIdentity { get; set; } = "";
        public string IsComputed { get; set; } = "";
        public string Collation { get; set; } = "";
        public string DefaultValue { get; set; } = "";
        public string IsPrimaryKey { get; set; } = "";
        public string PrimaryKeyName { get; set; } = "";
        public string ForeignTable { get; set; } = "";
        public string ForeignColumn { get; set; } = "";
        public string ForeignKeyName { get; set; } = "";
        public string FK_DeleteAction { get; set; } = "";
        public string FK_UpdateAction { get; set; } = "";
        public string FK_IsDisabled { get; set; } = "";
        public string FK_IsNotTrusted { get; set; } = "";

        public override bool Equals(object? obj)
        {
            if (obj is not TableMetadata other) return false;
            return ColumnName == other.ColumnName &&
                   DataType == other.DataType &&
                   MaxLength == other.MaxLength &&
                   Precision == other.Precision &&
                   Scale == other.Scale &&
                   IsNullable == other.IsNullable &&
                   IsIdentity == other.IsIdentity &&
                   IsComputed == other.IsComputed;
        }

        public override int GetHashCode() =>
            HashCode.Combine(ColumnName, DataType, MaxLength, Precision, Scale, IsNullable, IsIdentity, IsComputed);
    }

}
