using DataToolkit.Builder.Models;

namespace DataToolkit.Builder.Helpers;

public static class SchemaComparer
{
    public static IEnumerable<ColumnComparisonResult> CompareTableColumns(
        IEnumerable<TableMetadata> source,
        IEnumerable<TableMetadata> target)
    {
        var results = new List<ColumnComparisonResult>();

        var allColumns = source.Select(c => c.ColumnName)
                               .Union(target.Select(c => c.ColumnName))
                               .Distinct();

        foreach (var col in allColumns)
        {
            var srcCol = source.FirstOrDefault(c => c.ColumnName == col);
            var tgtCol = target.FirstOrDefault(c => c.ColumnName == col);

            results.Add(new ColumnComparisonResult
            {
                ColumnName = col,
                SourceColumn = srcCol,
                TargetColumn = tgtCol,
                ExistsInSource = srcCol != null,
                ExistsInTarget = tgtCol != null,
                IsDifferent = srcCol != null && tgtCol != null && !ColumnsAreEqual(srcCol, tgtCol)
            });
        }

        return results;
    }

    private static bool ColumnsAreEqual(TableMetadata a, TableMetadata b)
    {
        return a.DataType == b.DataType
            && a.MaxLength == b.MaxLength
            && a.Precision == b.Precision
            && a.Scale == b.Scale
            && a.IsNullable == b.IsNullable
            && a.IsIdentity == b.IsIdentity
            && a.IsComputed == b.IsComputed;
    }
}
