using System.Linq.Expressions;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Interfaces;

/// <summary>
/// Generic repository interface for asynchronous data access operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IAsyncRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
}
