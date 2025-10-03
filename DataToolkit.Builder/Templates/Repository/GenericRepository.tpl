using DataToolkit.Library.Repositories;
using {{EntityNamespace}};
using Domain.Interfaces;
using System.Data;
using System.Linq.Expressions;

namespace {{RepositoryNamespace}}
{
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

        public Task<IEnumerable<{{EntityName}}>> GetAllAsync(params Expression<Func<{{EntityName}}, object>>[]? selectProperties) {
            return _repo.GetAllAsync(selectProperties);
        }
            

        public Task<{{EntityName}}?> GetByIdAsync({{PKParameters}}{{EntityParameter}}{{SelectPropertiesParameter}}) 
        {
            {{PKInitializer}}{{EntityInitFallback}}
            return _repo.GetByIdAsync(entity, selectProperties);
        }

        public Task<int> Delete{{DeleteSuffix}}({{PKParameters}}{{EntityParameter}})
        {
            {{PKInitializer}}{{EntityInitFallback}}
            return _repo.DeleteAsync(entity);
        }
    }
}
