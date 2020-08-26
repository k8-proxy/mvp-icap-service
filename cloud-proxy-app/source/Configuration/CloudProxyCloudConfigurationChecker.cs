using System.Collections.Generic;
using System.Linq;

namespace Glasswall.IcapServer.CloudProxyApp.Configuration
{
    public class CloudProxyCloudConfigurationChecker
    {
        public void CheckConfiguration(ICloudConfiguration configuration)
        {
            var configurationErrors = new List<string>();

            if (string.IsNullOrEmpty(configuration.FileProcessingStorageConnectionString))
                configurationErrors.Add($"'FileProcessingStorageConnectionString' configuration is missing");

            if (string.IsNullOrEmpty(configuration.FileProcessingStorageOriginalStoreName))
                configurationErrors.Add($"'FileProcessingStorageOriginalStoreName' configuration is missing");

            if (configurationErrors.Any())
            {
                throw new InvalidApplicationConfigurationException(string.Join(',', configurationErrors));
            }
        }
    }
}
