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
