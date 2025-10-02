using DataToolkit.Library.Repositories;
using {{EntityNamespace}};
using Domain.Interfaces;
using System.Data;
using System.Linq.Expressions;

namespace {{RepositoryNamespace}}
{
    public interface {{InterfaceName}}
    {
        Task<int> InsertAsync({{EntityName}} entity);
        Task<int> UpdateAsync({{EntityName}} entity, params Expression<Func<{{EntityName}}, object>>[] includeProperties);
        Task<IEnumerable<{{EntityName}}>> GetAllAsync();
{{InterfaceMethods}}
    }

    public class {{RepositoryName}} : {{InterfaceName}}
    {
        private readonly GenericRepository<{{EntityName}}> _repo;

        public {{RepositoryName}}(IDbConnection connection)
        {
            _repo = new GenericRepository<{{EntityName}}>(connection);
        }

        public Task<int> InsertAsync({{EntityName}} entity)
        {
            return _repo.InsertAsync(entity);
        }

        public Task<int> UpdateAsync({{EntityName}} entity, params Expression<Func<{{EntityName}}, object>>[] includeProperties)
        {
            return _repo.UpdateAsync(entity, includeProperties);
        }

        public async Task<IEnumerable<{{EntityName}}>> GetAllAsync()
        {
            var entities = await _repo.GetAllAsync();
            return entities;
        }

{{ClassMethods}}
    }
}
