using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataToolkit.Library.Metadata
{
    public static class BuildSql
    {
        //Función que toma el sql y un objeto param, para reemplazar los placeholders:
        public static string BuildSqlWithParams(string sql, object parameters)
        {
            var result = sql;
            var props = parameters.GetType().GetProperties();

            foreach (var prop in props)
            {
                var value = prop.GetValue(parameters);
                string formattedValue = value switch
                {
                    string s => $"'{s}'",
                    DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                    bool b => b ? "1" : "0",
                    null => "NULL",
                    _ => value?.ToString() ?? "NULL"
                };

                result = result.Replace("@" + prop.Name, formattedValue);
            }

            return result;
        }
    }
}
