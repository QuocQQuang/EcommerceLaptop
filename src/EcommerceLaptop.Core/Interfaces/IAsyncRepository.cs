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
    Task<IReadOnlyList<T>> GetAsync(ISpecification<T> spec);
    Task<T?> GetEntityWithSpec(ISpecification<T> spec);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(ISpecification<T> spec);
    /// <summary>Persist all tracked changes in one round-trip (use after bulk in-memory updates)</summary>
    Task SaveChangesAsync();
}
