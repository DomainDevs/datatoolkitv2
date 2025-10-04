using Dapper;
using System.Data;
using DataToolkit.Library;
using Domain.Entities;

namespace {{RepositoryNamespace}}
{
    public class {{Domain}}SpRepository
    {
        private readonly SqlExecutor _executor;

        public {{Domain}}SpRepository(SqlExecutor executor)
        {
            _executor = executor;
        }

        public async Task<IEnumerable<{{Domain}}Response>> {{SPName}}Async({{Parameters}})
        {
            var parameters = new DynamicParameters();
{{ParameterMapping}}

            return await _executor.QueryAsync<{{Domain}}Response>(
                "{{Schema}}.{{SPName}}",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
