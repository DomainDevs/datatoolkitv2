using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataToolkit.Library.Common
{
    public class SqlExecutorException : Exception
    {
        public string? SqlQuery { get; }

        public SqlExecutorException(string message, Exception inner, string? sql = null)
            : base(message, inner)
        {
            SqlQuery = sql;
        }
    }
}
