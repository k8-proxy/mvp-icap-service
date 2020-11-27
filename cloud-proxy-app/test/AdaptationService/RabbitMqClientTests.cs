using Glasswall.IcapServer.CloudProxyApp.AdaptationService;
using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;

namespace Glasswall.IcapServer.CloudProxyApp.Tests.AdaptationService
{
    class RabbitMqClientTests
    {
        const string AnyMBHostname = "test hostname";
        const int AnyMBPort = 1234;

        IResponseProcessor mockResponseProcessor;
        ILogger<RabbitMqClient<AdaptationOutcomeProcessor>> mockLogger;

        Mock<IQueueConfiguration> mockQueueConfiguration;

        [SetUp]
        public void RabbitMqClientTestsSetup()
        {
            mockResponseProcessor = Mock.Of<IResponseProcessor>();
            mockLogger = Mock.Of<ILogger<RabbitMqClient<AdaptationOutcomeProcessor>>>();
            mockQueueConfiguration = new Mock<IQueueConfiguration>();
            mockQueueConfiguration.SetupAllProperties();

            mockQueueConfiguration.Object.MBHostName = AnyMBHostname;
            mockQueueConfiguration.Object.MBPort = AnyMBPort;
        }

        [Test]
        public void Missing_RabbitMQ_Credentials_Set_To_Defaults()
        {
            // Arrange
            const string DefaultRabbitUser = ConnectionFactory.DefaultUser;
            const string DefaultRabbitPassword = ConnectionFactory.DefaultPass;

            // Act
            _ = new RabbitMqClient<AdaptationOutcomeProcessor>(mockResponseProcessor, mockQueueConfiguration.Object, mockLogger);

            // Assert
            Assert.That(mockQueueConfiguration.Object.MBUsername, Is.EqualTo(DefaultRabbitUser), "With no specified username the default should be set");
            Assert.That(mockQueueConfiguration.Object.MBPassword, Is.EqualTo(DefaultRabbitPassword), "With no specified password the default should be set");
        }

        [Test]
        public void Provided_RabbitMQ_Credentials_Should_Be_Used()
        {
            // Arrange
            const string ExpectedRabbitUser = "Rabbit User";
            const string ExpectedRabbitPassword = "Rabbit Password";
            mockQueueConfiguration.Object.MBUsername = ExpectedRabbitUser;
            mockQueueConfiguration.Object.MBPassword = ExpectedRabbitPassword;

            // Act
            _ = new RabbitMqClient<AdaptationOutcomeProcessor>(mockResponseProcessor, mockQueueConfiguration.Object, mockLogger);

            // Assert
            Assert.That(mockQueueConfiguration.Object.MBUsername, Is.EqualTo(ExpectedRabbitUser), "Specified username should be used");
            Assert.That(mockQueueConfiguration.Object.MBPassword, Is.EqualTo(ExpectedRabbitPassword), "Specified password should be used");
        }
    }
}
