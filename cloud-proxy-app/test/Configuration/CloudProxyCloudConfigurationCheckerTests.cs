using Glasswall.IcapServer.CloudProxyApp.Configuration;
using NUnit.Framework;
namespace Glasswall.IcapServer.CloudProxyApp.Tests.Configuration
{
    class CloudProxyCloudConfigurationCheckerTests
    {
        readonly string TestFileProcessingStorageConnectionString = "test connection string";
        readonly string TestFileProcessingStorageOriginalStoreName = "test container name";

        private CloudProxyCloudConfigurationChecker checkerUnderTest;

        [SetUp]
        public void Setup()
        {
            checkerUnderTest = new CloudProxyCloudConfigurationChecker();
        }

        [Test]
        public void CheckConfiguration_reports_missing_StorageConnectionString_configuration()
        {
            // Arrange
            var config = new CloudProxyCloudConfiguration
            {
                FileProcessingStorageOriginalStoreName = TestFileProcessingStorageOriginalStoreName
            };

            // Act
            var exception = Assert.Throws<InvalidApplicationConfigurationException>(delegate { checkerUnderTest.CheckConfiguration(config); }, "Expected an exception to be thrown due to missing configuration");
            // Assert
            Assert.That(exception.Message.Contains("FileProcessingStorageConnectionString"), "Expected missing StorageConnectionString to be reported");
        }

        [Test]
        public void CheckConfiguration_reports_missing_OriginalStoreName_configuration()
        {
            // Arrange
            var config = new CloudProxyCloudConfiguration
            {
                FileProcessingStorageConnectionString = TestFileProcessingStorageConnectionString
            };

            // Act
            var exception = Assert.Throws<InvalidApplicationConfigurationException>(delegate { checkerUnderTest.CheckConfiguration(config); }, "Expected an exception to be thrown due to missing configuration");
            // Assert
            Assert.That(exception.Message.Contains("FileProcessingStorageOriginalStoreName"), "Expected missing OriginalStoreName to be reported");
        }

        [Test]
        public void CheckConfiguration_reports_all_missing_configuration()
        {
            // Arrange
            var config = new CloudProxyCloudConfiguration();

            // Act
            var exception = Assert.Throws<InvalidApplicationConfigurationException>(delegate { checkerUnderTest.CheckConfiguration(config); }, "Expected an exception to be thrown due to missing configuration");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(exception.Message.Contains("FileProcessingStorageConnectionString"), "Expected missing StorageConnectionString to be reported");
                Assert.That(exception.Message.Contains("FileProcessingStorageOriginalStoreName"), "Expected missing OriginalStoreName to be reported");
            });
        }

        [Test]
        public void CheckConfiguration_valid_configuration()
        {
            // Arrange
            var config = new CloudProxyCloudConfiguration
            {
                FileProcessingStorageOriginalStoreName = TestFileProcessingStorageOriginalStoreName,
                FileProcessingStorageConnectionString = TestFileProcessingStorageConnectionString
            };

            // Assert
            Assert.DoesNotThrow(delegate { checkerUnderTest.CheckConfiguration(config); }, "No exception expected with correct configuration");
        }
    }
}
