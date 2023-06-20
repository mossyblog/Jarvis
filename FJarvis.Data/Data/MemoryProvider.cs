using System.Collections.ObjectModel;
using FJarvis.Data.Data;
using FJarvis.Data.Traits;

namespace FJarvis.Data
{
/// <summary>
    /// Memory broker.
    /// Default Data Broker used for maintaining state management prior to requesting
    /// I/O bound persistence providers. Caching is important to reduce traffic between various
    /// endpoints within the coalition services. This broker acts as a mid point between nominated
    /// DataProvider(s) and server-side memory.
    /// </summary>
    public class MemoryProvider<T> : IPersistenceProvider<T> where T : IEntity
    {
        private readonly HashSet<T> _entities = new HashSet<T>();

        public MemoryProvider()
        {
            MaxLimit = 10;
        }

        /// <summary>
        /// Memory Provider.
        /// </summary>
        /// <param name="maxLimit"></param>
        public MemoryProvider(int maxLimit)
        {
            MaxLimit = maxLimit;
        }

        public int MaxLimit { get;  }
        private int _total { get; set; }

        /// <summary>
        /// Save the specified entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public async Task Save(T entity)
        {
            // No Entity, fuck it, abort.
            if (entity == null)
                return;
            
            if(entity.UpdatedAt.Equals(DateTime.MinValue))
                entity.UpdatedAt = DateTime.UtcNow;
            
            if(_total >= MaxLimit) 
                RemoveFirst();

            _entities.RemoveWhere(e => e.Id == entity.Id);
            _entities.Add(entity);
            
            _total = _entities.Count;
        }

        /// <summary>
        /// Save the specified entity.
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public async Task Save(IReadOnlyList<T> entities)
        {
            // TODO : MemoryProvider : Move this into a Bulk Save/Insert.
            foreach (var entity in entities) await Save(entity);
        }

        /// <summary>
        /// Remove the specified entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public async Task Remove(T entity)
        {
            // No Entity, fuck it, abort.
            if (entity == null)
                return;
            _entities.RemoveWhere(e => e.Id == entity.Id);
            _total -= 1;
        }

        /// <summary>
        /// Removes all Entities from Memory.
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public async Task Remove(IReadOnlyList<T> entities)
        {
            foreach (var entity in entities) await Remove(entity);
        }

        /// <summary>
        /// Fetch the specified entity. If no entity found, returns null.
        /// </summary>
        /// <returns>The fetch.</returns>
        /// <param name="entity">Entity.</param>
        public async Task<T> Fetch(T entity)
        {
            // No Entity, fuck it, abort.
            return entity == null ? default(T) : _entities.FirstOrDefault(e=>e.Id == entity.Id);
        }

        /// <summary>
        /// Fetch the specified id. If no entity found, returns null.
        /// </summary>
        /// <returns>The fetch.</returns>
        /// <param name="id">Identifier.</param>
        public async Task<T> Fetch(Guid id)
        {
            var result =  _entities.FirstOrDefault(e=>e.Id == id);
            return result == null ? default(T) : result;
        }

        /// <summary>
        /// Total amount of entities of type T found within the DAL.
        /// </summary>
        /// <returns>The total.</returns>
        public async Task<int> Total()
        {
            return _entities.Count;
        }

        /// <summary>
        /// Releases all resource used by the <see cref="T:Jarvis.CommonData.MemoryProvider`1"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the
        /// <see cref="T:Jarvis.CommonData.MemoryProvider`1"/>. The <see cref="Dispose"/> method leaves the
        /// <see cref="T:Jarvis.CommonData.MemoryProvider`1"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the
        /// <see cref="T:Jarvis.CommonData.MemoryProvider`1"/> so the garbage collector can reclaim the memory
        /// that the <see cref="T:Jarvis.CommonData.MemoryProvider`1"/> was occupying.</remarks>
        public void Dispose()
        {
            
        }

        /// <summary>
        /// Returns all Records in MemoryProvider
        /// </summary>
        /// <returns>The records.</returns>
        public async Task<IReadOnlyList<T>> All(int pageSize = 100)
        {
            var result = _entities.ToList();
            return new ReadOnlyCollection<T>(result);
        }

        /// <summary>
        /// Returns whether the Entity Id exists.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> Exists(Guid id)
        {
            return _entities.Any(e => e.Id == id);
        }

        /// <summary>
        /// Returns the entity based on the Filter(s)
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public async Task<IReadOnlyList<T>> Filter(Func<T, bool> filter)
        {
            var result = _entities.Where(filter).ToList();
            return new ReadOnlyCollection<T>(result);
        }

        /// <summary>
        /// Any Records Exist with this Filter.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public async Task<bool> Any(Func<T, bool> filter)
        {
            var result = _entities.Any(filter);
            return result;
        }

        /// <summary>
        /// Clears the Entities.
        /// </summary>
        /// <returns></returns>
        public async Task Clear()
        {
            _total = 0;
            _entities.Clear();
        }

        /// <summary>
        /// Removes the first Entity.
        /// </summary>
        public void RemoveFirst()
        {
            if (_entities.Count > 0)
                _entities.Remove(_entities.ElementAtOrDefault(0));
            _total = _entities.Count;
        }
        
       
    }
}