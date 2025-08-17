using DynamicConfiguration.Core.Models;

namespace DynamicConfiguration.Core.Interfaces
{
    public interface IConfigurationRepository
    {
        Task<IEnumerable<ConfigurationItem>> GetAllByApplicationAsync(string applicationName);
        Task<ConfigurationItem?> GetByNameAndApplicationAsync(string name, string applicationName);
        Task<bool> InsertAsync(ConfigurationItem item);
        Task<bool> UpdateAsync(ConfigurationItem item);
        Task<bool> DeleteAsync(string id);
        Task<bool> TestConnectionAsync();
        Task<IEnumerable<ConfigurationItem>> GetAllActiveConfigurationsAsync();
        Task<ConfigurationItem?> GetByIdAsync(string id);
    }
}