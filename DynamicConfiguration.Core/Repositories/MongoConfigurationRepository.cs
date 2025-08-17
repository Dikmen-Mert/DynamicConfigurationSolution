using MongoDB.Driver;
using DynamicConfiguration.Core.Interfaces;
using DynamicConfiguration.Core.Models;
using Microsoft.Extensions.Logging;

namespace DynamicConfiguration.Core.Repositories
{
    public class MongoConfigurationRepository : IConfigurationRepository
    {
        private readonly IMongoCollection<ConfigurationItem> _collection;
        private readonly ILogger<MongoConfigurationRepository>? _logger;

        public MongoConfigurationRepository(string connectionString, ILogger<MongoConfigurationRepository>? logger = null)
        {
            _logger = logger;

            try
            {
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase("DynamicConfiguration");
                _collection = database.GetCollection<ConfigurationItem>("Configurations");

                // Index oluştur
                CreateIndexes();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MongoDB bağlantısı kurulamadı");
                throw;
            }
        }

        private void CreateIndexes()
        {
            try
            {
                var indexKeys = Builders<ConfigurationItem>.IndexKeys
                    .Ascending(x => x.ApplicationName)
                    .Ascending(x => x.Name);

                var indexModel = new CreateIndexModel<ConfigurationItem>(indexKeys);
                _collection.Indexes.CreateOne(indexModel);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Index oluşturulamadı");
            }
        }

        public async Task<IEnumerable<ConfigurationItem>> GetAllByApplicationAsync(string applicationName)
        {
            try
            {
                var filter = Builders<ConfigurationItem>.Filter.And(
                    Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, applicationName),
                    Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, true)
                );

                return await _collection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Konfigürasyonlar getirilemedi: {ApplicationName}", applicationName);
                return new List<ConfigurationItem>();
            }
        }

        public async Task<ConfigurationItem?> GetByNameAndApplicationAsync(string name, string applicationName)
        {
            try
            {
                var filter = Builders<ConfigurationItem>.Filter.And(
                    Builders<ConfigurationItem>.Filter.Eq(x => x.Name, name),
                    Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, applicationName),
                    Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, true)
                );

                return await _collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Konfigürasyon getirilemedi: {Name} - {ApplicationName}", name, applicationName);
                return null;
            }
        }

        public async Task<bool> InsertAsync(ConfigurationItem item)
        {
            try
            {
                item.CreatedAt = DateTime.UtcNow;
                item.UpdatedAt = DateTime.UtcNow;

                await _collection.InsertOneAsync(item);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Konfigürasyon eklenemedi: {Name}", item.Name);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(ConfigurationItem item)
        {
            try
            {
                item.UpdatedAt = DateTime.UtcNow;

                var filter = Builders<ConfigurationItem>.Filter.Eq(x => x.Id, item.Id);
                var result = await _collection.ReplaceOneAsync(filter, item);

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Konfigürasyon güncellenemedi: {Id}", item.Id);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                var filter = Builders<ConfigurationItem>.Filter.Eq(x => x.Id, id);
                var result = await _collection.DeleteOneAsync(filter);

                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Konfigürasyon silinemedi: {Id}", id);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await _collection.CountDocumentsAsync(Builders<ConfigurationItem>.Filter.Empty);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<ConfigurationItem>> GetAllActiveConfigurationsAsync()
        {
            try
            {
                var filter = Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, true);
                return await _collection.Find(filter).SortBy(x => x.ApplicationName).ThenBy(x => x.Name).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Tüm aktif konfigürasyonlar getirilemedi");
                return new List<ConfigurationItem>();
            }
        }

        public async Task<ConfigurationItem?> GetByIdAsync(string id)
        {
            try
            {
                var filter = Builders<ConfigurationItem>.Filter.Eq(x => x.Id, id);
                return await _collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ID ile konfigürasyon getirilemedi: {Id}", id);
                return null;
            }
        }
    }
}