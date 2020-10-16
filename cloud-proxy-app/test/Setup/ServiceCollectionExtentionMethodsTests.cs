using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Glasswall.IcapServer.CloudProxyApp.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Collections.Generic;

namespace Glasswall.IcapServer.CloudProxyApp.Tests.Setup
{
    class ServiceCollectionExtentionMethodsTests
    {
        private ServiceCollection _serviceCollection;
        private ConfigurationBuilder _configurationBuilder;

        [SetUp]
        public void ServiceCollectionExtentionMethodsTestsSetup()
        {
            _serviceCollection = new ServiceCollection();
            _configurationBuilder = new ConfigurationBuilder();
        }

        [Test]
        public void NativeProxyApplication_is_added_as_singleton()
        {
            // Arrange
            IConfiguration configuration = _configurationBuilder.Build();

            // Act
            var serviceProvider = _serviceCollection.ConfigureServices(configuration).BuildServiceProvider(true);
            var nativeProxyApplication = serviceProvider.GetService<NativeProxyApplication>();
            var secondNativeProxyApplication = serviceProvider.GetService<NativeProxyApplication>();

            // Assert
            Assert.That(nativeProxyApplication, Is.Not.Null, "expected the object to be available");
            Assert.AreSame(nativeProxyApplication, secondNativeProxyApplication, "expected the same object to be provided");
        }

        [Test]
        public void CloudConfiguration_is_added_as_singleton()
        {
            // Arrange
            IConfiguration configuration = _configurationBuilder.Build();

            // Act
            var serviceProvider = _serviceCollection.ConfigureServices(configuration).BuildServiceProvider(true);
            var cloudConfiguration = serviceProvider.GetService<ICloudConfiguration>();
            var secondCloudConfiguration = serviceProvider.GetService<ICloudConfiguration>();

            // Assert
            Assert.That(cloudConfiguration, Is.Not.Null, "expected the object to be available");
            Assert.AreSame(cloudConfiguration, secondCloudConfiguration, "expected the same object to be provided");
        }

        [Test]
        public void Supplied_CloudConfiguration_is_bound()
        {
            // Arrange
            const string TestFileProcessingStorageConnectionString = "test FileProcessingStorageConnectionString";
            const string TestFileProcessingStorageOriginalStoreName = "test FileProcessingStorageOriginalStoreName";
            var testConfiguration = new Dictionary<string, string>()
            {
                [nameof(ICloudConfiguration.FileProcessingStorageConnectionString)] = TestFileProcessingStorageConnectionString,
                [nameof(ICloudConfiguration.FileProcessingStorageOriginalStoreName)] = TestFileProcessingStorageOriginalStoreName
            };

            IConfiguration configuration = _configurationBuilder
                                                    .AddInMemoryCollection(testConfiguration)
                                                    .Build();

            // Act
            var serviceProvider = _serviceCollection.ConfigureServices(configuration).BuildServiceProvider(true);
            var cloudConfiguration = serviceProvider.GetService<ICloudConfiguration>();

            // Assert
            Assert.That(cloudConfiguration.FileProcessingStorageConnectionString, Is.EqualTo(TestFileProcessingStorageConnectionString), "expected the connection string configuration to be bound");
            Assert.That(cloudConfiguration.FileProcessingStorageOriginalStoreName, Is.EqualTo(TestFileProcessingStorageOriginalStoreName), "expected the store name configuration to be bound");
        }

        [Test]
        public void ApplicationConfiguration_is_added_as_singleton()
        {
            // Arrange
            IConfiguration configuration = _configurationBuilder.Build();

            // Act
            var serviceProvider = _serviceCollection.ConfigureServices(configuration).BuildServiceProvider(true);
            var appConfiguration = serviceProvider.GetService<IAppConfiguration>();
            var secondAppConfiguration = serviceProvider.GetService<IAppConfiguration>();

            // Assert
            Assert.That(appConfiguration, Is.Not.Null, "expected the object to be available");
            Assert.AreSame(appConfiguration, secondAppConfiguration, "expected the same object to be provided");
        }

        [Test]
        public void Supplied_ApplicationConfiguration_is_bound()
        {
            // Arrange
            const string TestInputFilepath = "c:\testinput\file.pdf";
            const string TestOutputFilepath = "c:\testoutput\file.pdf";
            var testConfiguration = new Dictionary<string, string>()
            {
                [nameof(IAppConfiguration.InputFilepath)] = TestInputFilepath,
                [nameof(IAppConfiguration.OutputFilepath)] = TestOutputFilepath
            };

            IConfiguration configuration = _configurationBuilder
                                                    .AddInMemoryCollection(testConfiguration)
                                                    .Build();

            // Act
            var serviceProvider = _serviceCollection.ConfigureServices(configuration).BuildServiceProvider(true);
            var appConfiguration = serviceProvider.GetService<IAppConfiguration>();

            // Assert
            Assert.That(appConfiguration.InputFilepath, Is.EqualTo(TestInputFilepath), "expected the input filepath to be bound");
            Assert.That(appConfiguration.OutputFilepath, Is.EqualTo(TestOutputFilepath), "expected the output filepath to be bound");
        }

     
    }
}
