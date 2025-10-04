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

        public async Task<{{SPName}}Result> {{SPName}}Async({{Parameters}})
        {
            var parameters = new DynamicParameters();
{{ParameterMapping}}

            var rows = await _executor.QueryAsync<{{Domain}}Response>(
                "{{Schema}}.{{SPName}}",
                parameters,
                commandType: CommandType.StoredProcedure);

            return new {{SPName}}Result
            {
                Items = rows.ToList(),
{{OutputAssignments}}
            };
        }
    }

    public class {{SPName}}Result
    {
        public List<{{Domain}}Response> Items { get; set; } = new();
{{OutputProperties}}
    }
}
