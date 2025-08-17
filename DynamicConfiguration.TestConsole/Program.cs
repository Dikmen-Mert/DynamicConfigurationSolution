using DynamicConfiguration.Core;
using DynamicConfiguration.Core.Models;
using DynamicConfiguration.Core.Repositories;

namespace DynamicConfiguration.TestConsole
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("=== Dynamic Configuration Test ===\n");

            // MongoDB Connection String
            string connectionString = "mongodb://admin:password123@localhost:27017";
            string applicationName = "SERVICE-A";

            try
            {
                // Test 1: Repository ile test verileri ekleme
                await TestRepository(connectionString);

                // Test 2: ConfigurationReader test
                await TestConfigurationReader(connectionString, applicationName);

                Console.WriteLine("\n=== Test Tamamlandı ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }

            Console.WriteLine("\nÇıkmak için bir tuşa basın...");
            Console.ReadKey();
        }

        private static async Task TestRepository(string connectionString)
        {
            Console.WriteLine("1. Repository Test - Test verileri ekleniyor...");

            var repo = new MongoConfigurationRepository(connectionString);

            // Test verilerini ekle
            var testConfigs = new[]
            {
                new ConfigurationItem
                {
                    Name = "SiteName",
                    Type = "string",
                    Value = "soty.io",
                    IsActive = true,
                    ApplicationName = "SERVICE-A"
                },
                new ConfigurationItem
                {
                    Name = "IsBasketEnabled",
                    Type = "bool",
                    Value = "1",
                    IsActive = true,
                    ApplicationName = "SERVICE-B"
                },
                new ConfigurationItem
                {
                    Name = "MaxItemCount",
                    Type = "int",
                    Value = "50",
                    IsActive = false,
                    ApplicationName = "SERVICE-A"
                },
                new ConfigurationItem
                {
                    Name = "DatabaseTimeout",
                    Type = "int",
                    Value = "30",
                    IsActive = true,
                    ApplicationName = "SERVICE-A"
                },
                new ConfigurationItem
                {
                    Name = "ApiKey",
                    Type = "string",
                    Value = "abc123xyz",
                    IsActive = true,
                    ApplicationName = "SERVICE-A"
                }
            };

            foreach (var config in testConfigs)
            {
                bool result = await repo.InsertAsync(config);
                Console.WriteLine($"   {config.Name} -> {(result ? "✓ Eklendi" : "✗ Hata")}");
            }

            Console.WriteLine("   Repository test tamamlandı!\n");
        }

        private static async Task TestConfigurationReader(string connectionString, string applicationName)
        {
            Console.WriteLine("2. ConfigurationReader Test...");

            using var configReader = new ConfigurationReader(applicationName, connectionString, 5000);

            // Biraz bekle (ilk yükleme için)
            await Task.Delay(2000);

            Console.WriteLine($"   Uygulama: {applicationName}");

            // String test
            var siteName = configReader.GetValue<string>("SiteName");
            Console.WriteLine($"   SiteName: '{siteName}'");

            // Int test
            var timeout = configReader.GetValue<int>("DatabaseTimeout");
            Console.WriteLine($"   DatabaseTimeout: {timeout}");

            // Bool test (SERVICE-B'den gelmeyecek)
            var basketEnabled = configReader.GetValue<bool>("IsBasketEnabled");
            Console.WriteLine($"   IsBasketEnabled: {basketEnabled} (SERVICE-B'ye ait, görünmemeli)");

            // Olmayan key test
            var nonExisting = configReader.GetValue<string>("NonExistingKey");
            Console.WriteLine($"   NonExistingKey: '{nonExisting}' (boş olmalı)");

            // Async test
            var apiKey = await configReader.GetValueAsync<string>("ApiKey");
            Console.WriteLine($"   ApiKey (Async): '{apiKey}'");

            // Key exists test
            bool hasApiKey = configReader.ContainsKey("ApiKey");
            bool hasNonExisting = configReader.ContainsKey("NonExistingKey");
            Console.WriteLine($"   ApiKey exists: {hasApiKey}");
            Console.WriteLine($"   NonExistingKey exists: {hasNonExisting}");

            Console.WriteLine("   ConfigurationReader test tamamlandı!");
        }
    }
}