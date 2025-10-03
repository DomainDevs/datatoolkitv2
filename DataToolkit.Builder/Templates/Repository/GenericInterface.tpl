using {{EntityNamespace}};
using System.Linq.Expressions;

namespace Domain.Interfaces;

// Interfaz {{InterfaceName}} (autogenerada)
public interface {{InterfaceName}}
{
    Task<int> InsertAsync({{EntityName}} entity);
    Task<int> UpdateAsync({{EntityName}} entity, params Expression<Func<{{EntityName}}, object>>[] includeProperties);
    Task<IEnumerable<{{EntityName}}>> GetAllAsync(params Expression<Func<{{EntityName}}, object>>[]? selectProperties);
    Task<{{EntityName}}?> GetByIdAsync({{PKParameters}}{{EntityParameter}}{{SelectPropertiesParameter}});
    Task<int> Delete{{DeleteSuffix}}({{PKParameters}}{{EntityParameter}});
}

