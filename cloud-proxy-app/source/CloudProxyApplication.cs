using Azure;
using Azure.Storage.Blobs;
using Glasswall.IcapServer.CloudProxyApp.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Glasswall.IcapServer.CloudProxyApp
{
    public class CloudProxyApplication
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly ICloudConfiguration _cloudConfiguration;
        private readonly Func<string, BlobServiceClient> _blobServiceClientFactory;

        public CloudProxyApplication(IAppConfiguration appConfiguration, ICloudConfiguration cloudConfiguration, Func<string, BlobServiceClient> blobServiceClientFactory)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            _cloudConfiguration = cloudConfiguration ?? throw new ArgumentNullException(nameof(cloudConfiguration));
            _blobServiceClientFactory = blobServiceClientFactory ?? throw new ArgumentNullException(nameof(blobServiceClientFactory));
        }

        internal async Task<int> RunAsync()
        {
            Guid inputFileId = Guid.Empty;

            if (!CheckConfigurationIsValid(_cloudConfiguration) || !CheckConfigurationIsValid(_appConfiguration))
            {
                return (int)ReturnOutcome.GW_ERROR;
            }

            try
            {
                BlobServiceClient blobServiceClient = _blobServiceClientFactory(_cloudConfiguration.FileProcessingStorageConnectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_cloudConfiguration.FileProcessingStorageOriginalStoreName);

                var fileId = Guid.NewGuid();
                BlobClient blobClient = containerClient.GetBlobClient(fileId.ToString());

                Console.WriteLine($"Uploading file '{Path.GetFileName(_appConfiguration.InputFilepath)}' with FileId {fileId}");
                using (FileStream uploadFileStream = File.OpenRead(_appConfiguration.InputFilepath))
                {
                    var status = await blobClient.UploadAsync(uploadFileStream, true);
                }
                return (int)ReturnOutcome.GW_UNPROCESSED;

            }
            catch (RequestFailedException rfe)
            {
                Console.WriteLine($"Error Uploading 'input' {inputFileId}, {rfe.Message}");
                return (int)ReturnOutcome.GW_ERROR;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Processing 'input' {inputFileId}, {ex.Message}");
                return (int)ReturnOutcome.GW_ERROR;
            }
        }

        static bool CheckConfigurationIsValid(IAppConfiguration configuration)
        {
            var configurationErrors = new List<string>();

            if (string.IsNullOrEmpty(configuration.InputFilepath))
                configurationErrors.Add($"'InputFilepath' configuration is missing");

            if (string.IsNullOrEmpty(configuration.OutputFilepath))
                configurationErrors.Add($"'OutputFilepath' configuration is missing");

            if (configurationErrors.Any())
            {
                Console.WriteLine($"Error Processing Command {Environment.NewLine} \t{string.Join(',', configurationErrors)}");
            }

            return !configurationErrors.Any();
        }

        static bool CheckConfigurationIsValid(ICloudConfiguration configuration)
        {
            var configurationErrors = new List<string>();
            if (string.IsNullOrEmpty(configuration.FileProcessingStorageConnectionString))
                configurationErrors.Add($"'FileProcessingStorageConnectionString' configuration is missing");

            if (string.IsNullOrEmpty(configuration.FileProcessingStorageOriginalStoreName))
                configurationErrors.Add($"'FileProcessingStorageOriginalStoreName' configuration is missing");

            if (string.IsNullOrEmpty(configuration.TransactionOutcomeQueueConnectionString))
                configurationErrors.Add($"'TransactionOutcomeQueueConnectionString' configuration is missing");

            if (string.IsNullOrEmpty(configuration.TransactionOutcomeQueueName))
                configurationErrors.Add($"'TransactionOutcomeQueueName' configuration is missing");

            if (configurationErrors.Any())
            {
                Console.WriteLine($"Error Missing Configuration {Environment.NewLine} \t{string.Join(',', configurationErrors)}");
            }

            return !configurationErrors.Any();
        }
    }
}