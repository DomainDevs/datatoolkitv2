using System.Data.Common;

namespace DataToolkit.Builder.Models
{
    /// <summary>
    /// Representa una tabla dentro de la base de datos.
    /// </summary>
    public class DbTable
    {
        /// <summary>
        /// Nombre de la tabla en la base de datos.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Esquema al que pertenece la tabla.
        /// </summary>
        public string Schema { get; set; } = "dbo";

        /// <summary>
        /// Columnas que pertenecen a esta tabla.
        /// </summary>
        public List<DbColumn> Columns { get; set; } = new();

        /// <summary>
        /// Indica si la tabla tiene clave primaria compuesta.
        /// </summary>
        public bool HasCompositeKey => Columns.Count(c => c.IsPrimaryKey) > 1;

        /// <summary>
        /// Retorna las columnas que son clave primaria.
        /// </summary>
        public IEnumerable<DbColumn> PrimaryKeys => Columns.Where(c => c.IsPrimaryKey);
    }
}
