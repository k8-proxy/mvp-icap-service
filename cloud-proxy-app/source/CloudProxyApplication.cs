using Azure;
using Azure.Storage.Blobs;
using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Glasswall.IcapServer.CloudProxyApp.QueueAccess;
using Glasswall.IcapServer.CloudProxyApp.StorageAccess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Glasswall.IcapServer.CloudProxyApp
{
    public class CloudProxyApplication
    {
        private readonly IAppConfiguration _appConfiguration;

        private readonly IUploader _uploader;
        private readonly IServiceQueueClient _queueClient;

        private readonly CancellationTokenSource _processingCancellationTokenSource;

        private readonly TimeSpan _processingTimeoutDuration = TimeSpan.FromSeconds(60);

        public CloudProxyApplication(IAppConfiguration appConfiguration, IUploader uploader, IServiceQueueClient queueClient)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            _uploader = uploader ?? throw new ArgumentNullException(nameof(uploader));
            _queueClient = queueClient ?? throw new ArgumentNullException(nameof(uploader));

            _processingCancellationTokenSource = new CancellationTokenSource(_processingTimeoutDuration);
        }

        internal async Task<int> RunAsync()
        {
            Guid inputFileId = Guid.NewGuid();

            if (!CheckConfigurationIsValid(_appConfiguration))
            {
                return (int)ReturnOutcome.GW_ERROR;
            }

            try
            {
                var processingCancellationToken = _processingCancellationTokenSource.Token;
                var queueListener = _queueClient.Register(TransactionOutcomeMessage.Label, inputFileId.ToString());

                Console.WriteLine($"Uploading  file '{Path.GetFileName(_appConfiguration.InputFilepath)}' with FileId {inputFileId}");
                await UploadInputFile(inputFileId, _appConfiguration.InputFilepath);

                Console.WriteLine($"Waiting on outcome for file '{Path.GetFileName(_appConfiguration.InputFilepath)}' with FileId {inputFileId}");
                var receivedMessage = queueListener.Take(processingCancellationToken);

                Console.WriteLine($"Received message for file '{Path.GetFileName(_appConfiguration.InputFilepath)}' with FileId {inputFileId}");
                var transactionOutcome = TransactionOutcomeBuilder.Build(receivedMessage);

                Console.WriteLine($"Received outcome for file '{Path.GetFileName(_appConfiguration.InputFilepath)}' with FileId {inputFileId}, Outcome = {Enum.GetName(typeof(ReturnOutcome), transactionOutcome.FileOutcome)}");

                await WriteRebuiltFile(transactionOutcome.FileRebuildSas);

                return (int)transactionOutcome.FileOutcome;
            }
            catch (RequestFailedException rfe)
            {
                Console.WriteLine($"Error Uploading 'input' {inputFileId}, {rfe.Message}");
                return (int)ReturnOutcome.GW_ERROR;
            }
            catch (OperationCanceledException oce)
            {
                Console.WriteLine($"Error Processing Timeout 'input' {inputFileId} exceeded {_processingTimeoutDuration.TotalSeconds}s, {oce.Message}");
                return (int)ReturnOutcome.GW_ERROR;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Processing 'input' {inputFileId}, {ex.Message}");
                return (int)ReturnOutcome.GW_ERROR;
            }
        }

        private async Task WriteRebuiltFile(string fileRebuildSas)
        {
            BlobClient rebuiltFileBlobClient = new BlobClient(new Uri(fileRebuildSas));
            using FileStream downloadFileStream = File.OpenWrite(_appConfiguration.OutputFilepath);
            await rebuiltFileBlobClient.DownloadToAsync(downloadFileStream);
        }

        private async Task UploadInputFile(Guid inputFileId, string inputFilepath)
        {
            Console.WriteLine($"Uploading file '{Path.GetFileName(inputFilepath)}' with FileId {inputFileId}");
            await _uploader.UploadInputFile(inputFileId,  inputFilepath);
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