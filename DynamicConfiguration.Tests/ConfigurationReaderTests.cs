using DynamicConfiguration.Core;
using DynamicConfiguration.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace DynamicConfiguration.Tests
{
    public class ConfigurationReaderTests : IDisposable
    {
        private readonly Mock<IMongoCollection<ConfigurationItem>> _mockCollection;
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoClient> _mockClient;
        private readonly Mock<ILogger<ConfigurationReader>> _mockLogger;
        private readonly ConfigurationReader _configurationReader;
        private readonly string _testApplicationName = "TEST-APP";
        private readonly string _testConnectionString = "mongodb://localhost:27017/testdb";

        public ConfigurationReaderTests()
        {
            _mockCollection = new Mock<IMongoCollection<ConfigurationItem>>();
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockClient = new Mock<IMongoClient>();
            _mockLogger = new Mock<ILogger<ConfigurationReader>>();

            _mockClient.Setup(x => x.GetDatabase(It.IsAny<string>(), null))
                      .Returns(_mockDatabase.Object);

            _mockDatabase.Setup(x => x.GetCollection<ConfigurationItem>(It.IsAny<string>(), null))
                        .Returns(_mockCollection.Object);

            // Test için ConfigurationReader'ı mock'larla oluştur
            _configurationReader = new ConfigurationReader(_testApplicationName, _testConnectionString, 5000);
        }

        [Fact]
        public void Constructor_ShouldInitialize_WithValidParameters()
        {
            // Arrange & Act
            var reader = new ConfigurationReader("TEST-APP", "mongodb://localhost:27017", 1000);

            // Assert
            Assert.NotNull(reader);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_ShouldThrowException_WhenApplicationNameIsInvalid(string applicationName)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new ConfigurationReader(applicationName, "mongodb://localhost:27017", 1000));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_ShouldThrowException_WhenConnectionStringIsInvalid(string connectionString)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new ConfigurationReader("TEST-APP", connectionString, 1000));
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenRefreshIntervalIsInvalid()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new ConfigurationReader("TEST-APP", "mongodb://localhost:27017", -1));
        }

        [Fact]
        public void GetValue_ShouldReturnStringValue_WhenKeyExists()
        {
            // Arrange
            var testItems = new List<ConfigurationItem>
            {
                new ConfigurationItem
                {
                    Id = "1",
                    Name = "SiteName",
                    Type = "string",
                    Value = "test.io",
                    IsActive = true,
                    ApplicationName = _testApplicationName
                }
            };

            SetupMockCollection(testItems);

            // Act
            var result = _configurationReader.GetValue<string>("SiteName");

            // Assert
            Assert.Equal("test.io", result);
        }

        [Fact]
        public void GetValue_ShouldReturnBoolValue_WhenKeyExists()
        {
            // Arrange
            var testItems = new List<ConfigurationItem>
            {
                new ConfigurationItem
                {
                    Id = "2",
                    Name = "IsBasketEnabled",
                    Type = "bool",
                    Value = "true",
                    IsActive = true,
                    ApplicationName = _testApplicationName
                }
            };

            SetupMockCollection(testItems);

            // Act
            var result = _configurationReader.GetValue<bool>("IsBasketEnabled");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetValue_ShouldReturnIntValue_WhenKeyExists()
        {
            // Arrange
            var testItems = new List<ConfigurationItem>
            {
                new ConfigurationItem
                {
                    Id = "3",
                    Name = "MaxItemCount",
                    Type = "int",
                    Value = "50",
                    IsActive = true,
                    ApplicationName = _testApplicationName
                }
            };

            SetupMockCollection(testItems);

            // Act
            var result = _configurationReader.GetValue<int>("MaxItemCount");

            // Assert
            Assert.Equal(50, result);
        }

        [Fact]
        public void GetValue_ShouldReturnDoubleValue_WhenKeyExists()
        {
            // Arrange
            var testItems = new List<ConfigurationItem>
            {
                new ConfigurationItem
                {
                    Id = "4",
                    Name = "Price",
                    Type = "double",
                    Value = "99.99",
                    IsActive = true,
                    ApplicationName = _testApplicationName
                }
            };

            SetupMockCollection(testItems);

            // Act
            var result = _configurationReader.GetValue<double>("Price");

            // Assert
            Assert.Equal(99.99, result);
        }

        [Fact]
        public void GetValue_ShouldThrowException_WhenKeyNotExists()
        {
            // Arrange
            SetupMockCollection(new List<ConfigurationItem>());

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() =>
                _configurationReader.GetValue<string>("NonExistentKey"));
        }

        [Fact]
        public void GetValue_ShouldReturnOnlyActiveItems()
        {
            // Arrange
            var testItems = new List<ConfigurationItem>
            {
                new ConfigurationItem
                {
                    Id = "1",
                    Name = "TestKey",
                    Type = "string",
                    Value = "ActiveValue",
                    IsActive = true,
                    ApplicationName = _testApplicationName
                },
                new ConfigurationItem
                {
                    Id = "2",
                    Name = "TestKey",
                    Type = "string",
                    Value = "InactiveValue",
                    IsActive = false,
                    ApplicationName = _testApplicationName
                }
            };

            SetupMockCollection(testItems);

            // Act
            var result = _configurationReader.GetValue<string>("TestKey");

            // Assert
            Assert.Equal("ActiveValue", result);
        }

        [Fact]
        public void GetValue_ShouldReturnOnlyApplicationSpecificItems()
        {
            // Arrange
            var testItems = new List<ConfigurationItem>
            {
                new ConfigurationItem
                {
                    Id = "1",
                    Name = "TestKey",
                    Type = "string",
                    Value = "MyAppValue",
                    IsActive = true,
                    ApplicationName = _testApplicationName
                },
                new ConfigurationItem
                {
                    Id = "2",
                    Name = "TestKey",
                    Type = "string",
                    Value = "OtherAppValue",
                    IsActive = true,
                    ApplicationName = "OTHER-APP"
                }
            };

            SetupMockCollection(testItems);

            // Act
            var result = _configurationReader.GetValue<string>("TestKey");

            // Assert
            Assert.Equal("MyAppValue", result);
        }

        [Fact]
        public void GetValue_ShouldThrowException_WhenTypeConversionFails()
        {
            // Arrange
            var testItems = new List<ConfigurationItem>
            {
                new ConfigurationItem
                {
                    Id = "1",
                    Name = "TestKey",
                    Type = "int",
                    Value = "invalid_number",
                    IsActive = true,
                    ApplicationName = _testApplicationName
                }
            };

            SetupMockCollection(testItems);

            // Act & Assert
            Assert.Throws<InvalidCastException>(() =>
                _configurationReader.GetValue<int>("TestKey"));
        }

        private void SetupMockCollection(List<ConfigurationItem> items)
        {
            var mockCursor = new Mock<IAsyncCursor<ConfigurationItem>>();
            mockCursor.Setup(x => x.Current).Returns(items);
            mockCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
                     .Returns(true)
                     .Returns(false);
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true)
                     .ReturnsAsync(false);

            _mockCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<ConfigurationItem>>(),
                It.IsAny<FindOptions<ConfigurationItem, ConfigurationItem>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
        }

        public void Dispose()
        {
            _configurationReader?.Dispose();
        }
    }
}