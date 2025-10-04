using Dapper;
using System.Data;
using DataToolkit.Library;

namespace {{RepositoryNamespace}}
{
    public class {{Domain}}SpRepository
    {
        private readonly SqlExecutor _executor;

        public {{Domain}}SpRepository(SqlExecutor executor)
        {
            _executor = executor;
        }

        public async Task<int> {{SPName}}Async({{Parameters}})
        {
            var parameters = new DynamicParameters();
{{ParameterMapping}}

            return await _executor.ExecuteAsync(
                "{{Schema}}.{{SPName}}",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
