using Azure.Storage.Blobs;
using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Glasswall.IcapServer.CloudProxyApp.Setup;
using Glasswall.IcapServer.CloudProxyApp.StorageAccess;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Glasswall.IcapServer.CloudProxyApp.Tests.Setup
{
    class ServiceCollectionExtentionMethodsTests
    {
        private ServiceCollection _serviceCollection;
        private ConfigurationBuilder _configurationBuilder;
        private readonly Func<string, BlobServiceClient> _stubBlobFactory = StubBlobFactory;
        private readonly Func<string, string, IQueueClient> _stubQueueFactory = StubQueueFactory;

        private static BlobServiceClient StubBlobFactory(string name) { return new BlobServiceClient("test"); }
        private static IQueueClient StubQueueFactory(string connectionString, string name) { return Mock.Of<IQueueClient>(); }

        [SetUp]
        public void ServiceCollectionExtentionMethodsTestsSetup()
        {
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddSingleton(_stubBlobFactory);
            _serviceCollection.AddSingleton(_stubQueueFactory);
            _configurationBuilder = new ConfigurationBuilder();
        }

        [Test]
        public void CloudProxyApplication_is_added_as_singleton()
        {
            // Arrange
            IConfiguration configuration = _configurationBuilder.Build();

            // Act
            var serviceProvider = _serviceCollection.ConfigureServices(configuration).BuildServiceProvider(true);
            var cloudProxyApplication = serviceProvider.GetService<CloudProxyApplication>();
            var secondCloudProxyApplication = serviceProvider.GetService<CloudProxyApplication>();

            // Assert
            Assert.That(cloudProxyApplication, Is.Not.Null, "expected the object to be available");
            Assert.AreSame(cloudProxyApplication, secondCloudProxyApplication, "expected the same object to be provided");
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

        [Test]
        public void StorageUploaded_is_added_as_transient()
        {
            // Arrange
            IConfiguration configuration = _configurationBuilder.Build();

            // Act
            var serviceProvider = _serviceCollection.ConfigureServices(configuration).BuildServiceProvider(true);
            var uploader = serviceProvider.GetService<IUploader>();
            var secondUploader = serviceProvider.GetService<IUploader>();

            // Assert
            Assert.That(uploader, Is.Not.Null, "expected the object to be available");
            Assert.AreNotSame(uploader, secondUploader, "don't expect the same object to be provided");
        }
    }
}
