using FJarvis.Data.Traits;
using Microsoft.Azure.Cosmos;

namespace FJarvis.Data.Data
{
    public class CosmosDbProvider<TEntity> : IPersistenceProvider<TEntity> where TEntity : IEntity
    {
        private readonly CosmosClient _client;
        private readonly Container _container;

        public CosmosDbProvider(string connectionString, string databaseName, string containerName)
        {
            _client = new CosmosClient(connectionString);
            _container = _client.GetContainer(databaseName, containerName);
        }

        public async Task Save(TEntity entity)
        {
            await _container.UpsertItemAsync(entity);
        }

        public async Task Save(IReadOnlyList<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                await Save(entity);
            }
        }

        public async Task Remove(TEntity entity)
        {
            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty == null)
                throw new Exception("Entity must have an Id property");

            var id = idProperty.GetValue(entity).ToString();
            await _container.DeleteItemAsync<TEntity>(id, new PartitionKey(id));
        }

        public async Task Remove(IReadOnlyList<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                await Remove(entity);
            }
        }

        public async Task<TEntity> Fetch(TEntity entity) 
        {
            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty == null)
                throw new Exception("Entity must have an Id property");

            var id = idProperty.GetValue(entity).ToString();
            return await Fetch(Guid.Parse(id));
        }

        public async Task<TEntity> Fetch(Guid id)
        {
            ItemResponse<TEntity> response = await _container.ReadItemAsync<TEntity>(id.ToString(), new PartitionKey(id.ToString()));
            return response.Resource;
        }

        public async Task<bool> Exists(Guid id)
        {
            try
            {
                var entity = await Fetch(id);
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<int> Total()
        {
            var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
            var iterator = _container.GetItemQueryIterator<int>(query);
            var totals = await iterator.ReadNextAsync();
            return totals.FirstOrDefault();
        }

        public async Task<IReadOnlyList<TEntity>> All(int pageSize)
        {
            var query = new QueryDefinition($"SELECT * FROM c OFFSET 0 LIMIT {pageSize}");
            var iterator = _container.GetItemQueryIterator<TEntity>(query);
            var page = await iterator.ReadNextAsync();
            return page.Resource.ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<TEntity>> Filter(Func<TEntity, bool> filter)
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var iterator = _container.GetItemQueryIterator<TEntity>(query);
            var entities = new List<TEntity>();
            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                entities.AddRange(page.Resource.Where(filter));
            }

            return entities.AsReadOnly();
        }

        public async Task<bool> Any(Func<TEntity, bool> filter)
        {
            return (await Filter(filter)).Any();
        }

        public async Task Clear()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var iterator = _container.GetItemQueryIterator<TEntity>(query);
            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                foreach (var entity in page.Resource)
                {
                    await Remove(entity);
                }
            }
        }

        public void Dispose()

        {
            _client?.Dispose();
        }
    }
}