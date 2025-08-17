namespace DynamicConfiguration.Core.Interfaces
{
    public interface IConfigurationReader : IDisposable
    {
        T GetValue<T>(string key);
        Task<T> GetValueAsync<T>(string key);
        bool ContainsKey(string key);
        void RefreshConfiguration();
        Task RefreshConfigurationAsync();
    }
}