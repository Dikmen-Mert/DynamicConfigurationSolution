using System.Collections.Concurrent;
using System.ComponentModel;
using DynamicConfiguration.Core.Interfaces;
using DynamicConfiguration.Core.Models;
using DynamicConfiguration.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace DynamicConfiguration.Core
{
    public class ConfigurationReader : IConfigurationReader
    {
        private readonly string _applicationName;
        private readonly IConfigurationRepository _repository;
        private readonly Timer _refreshTimer;
        private readonly ILogger<ConfigurationReader>? _logger;

        // Thread-safe cache
        private readonly ConcurrentDictionary<string, ConfigurationItem> _cache;
        private readonly SemaphoreSlim _refreshSemaphore;
        private volatile bool _disposed;

        public ConfigurationReader(string applicationName, string connectionString, int refreshTimerIntervalInMs = 30000)
        {
            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentException("ApplicationName boş olamaz", nameof(applicationName));

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("ConnectionString boş olamaz", nameof(connectionString));

            _applicationName = applicationName;
            _cache = new ConcurrentDictionary<string, ConfigurationItem>();
            _refreshSemaphore = new SemaphoreSlim(1, 1);

            // Logger factory oluştur (opsiyonel)
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<ConfigurationReader>();

            // Repository oluştur
            _repository = new MongoConfigurationRepository(connectionString,
                loggerFactory.CreateLogger<MongoConfigurationRepository>());

            // İlk yüklemeyi yap
            _ = Task.Run(async () => await InitialLoadAsync());

            // Timer başlat
            _refreshTimer = new Timer(async _ => await RefreshConfigurationAsync(),
                null, TimeSpan.FromMilliseconds(refreshTimerIntervalInMs),
                TimeSpan.FromMilliseconds(refreshTimerIntervalInMs));
        }

        public T GetValue<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key boş olamaz", nameof(key));

            if (_cache.TryGetValue(key, out var item))
            {
                return ConvertValue<T>(item.Value, item.Type);
            }

            // Cache'de yoksa varsayılan değer dön
            return default(T)!;
        }

        public async Task<T> GetValueAsync<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key boş olamaz", nameof(key));

            // Cache'den kontrol et
            if (_cache.TryGetValue(key, out var cachedItem))
            {
                return ConvertValue<T>(cachedItem.Value, cachedItem.Type);
            }

            // Cache'de yoksa DB'den getir
            try
            {
                var item = await _repository.GetByNameAndApplicationAsync(key, _applicationName);
                if (item != null)
                {
                    _cache.TryAdd(key, item);
                    return ConvertValue<T>(item.Value, item.Type);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Async değer getirilemedi: {Key}", key);
            }

            return default(T)!;
        }

        public bool ContainsKey(string key)
        {
            return _cache.ContainsKey(key);
        }

        public void RefreshConfiguration()
        {
            _ = Task.Run(async () => await RefreshConfigurationAsync());
        }

        public async Task RefreshConfigurationAsync()
        {
            if (_disposed) return;

            await _refreshSemaphore.WaitAsync();
            try
            {
                _logger?.LogDebug("Konfigürasyon yenileniyor...");

                var items = await _repository.GetAllByApplicationAsync(_applicationName);

                // Eski cache'i temizle
                _cache.Clear();

                // Yeni değerleri ekle
                foreach (var item in items)
                {
                    _cache.TryAdd(item.Name, item);
                }

                _logger?.LogInformation("{Count} konfigürasyon yüklendi", items.Count());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Konfigürasyon yenilenemedi");
            }
            finally
            {
                _refreshSemaphore.Release();
            }
        }

        private async Task InitialLoadAsync()
        {
            try
            {
                // Bağlantıyı test et
                if (await _repository.TestConnectionAsync())
                {
                    await RefreshConfigurationAsync();
                }
                else
                {
                    _logger?.LogWarning("MongoDB bağlantısı kurulamadı, cached değerlerle çalışılacak");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "İlk yükleme yapılamadı");
            }
        }

        private T ConvertValue<T>(string value, string type)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                    return default(T)!;

                // Type converter kullanarak dönüştür
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter.CanConvertFrom(typeof(string)))
                {
                    return (T)converter.ConvertFromString(value)!;
                }

                // Manuel dönüşümler
                return typeof(T).Name.ToLower() switch
                {
                    "string" => (T)(object)value,
                    "int32" or "int" => (T)(object)int.Parse(value),
                    "boolean" or "bool" => (T)(object)(value == "1" || value.ToLower() == "true"),
                    "double" => (T)(object)double.Parse(value),
                    "decimal" => (T)(object)decimal.Parse(value),
                    "datetime" => (T)(object)DateTime.Parse(value),
                    _ => throw new NotSupportedException($"Tip desteklenmiyor: {typeof(T).Name}")
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Değer dönüştürülemedi: {Value} -> {Type}", value, typeof(T).Name);
                return default(T)!;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _refreshTimer?.Dispose();
            _refreshSemaphore?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}