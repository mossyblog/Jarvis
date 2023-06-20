namespace FJarvis.Data.Data
{
    /// <summary>
    /// Base level Data Broker Contract.
    /// </summary>
    public interface IPersistenceProvider<TEntity> : IDisposable 
    {
        Task Save(TEntity entity);
        Task Save(IReadOnlyList<TEntity> entities);

        Task Remove(TEntity entity);
        Task Remove(IReadOnlyList<TEntity> entities);
        Task<TEntity> Fetch(TEntity entity);
        Task<TEntity> Fetch(Guid id);
        Task<bool> Exists(Guid id);
        Task<int> Total();
        Task<IReadOnlyList<TEntity>> All(int pageSize);
        Task<IReadOnlyList<TEntity>> Filter(Func<TEntity, bool> filter);
        Task<bool> Any(Func<TEntity, bool> filter);
        
        Task Clear();
    }
}