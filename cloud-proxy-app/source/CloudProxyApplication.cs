using Azure;
using Azure.Storage.Blobs;
using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Glasswall.IcapServer.CloudProxyApp.QueueAccess;
using Glasswall.IcapServer.CloudProxyApp.StorageAccess;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<CloudProxyApplication> _logger;
        private readonly CancellationTokenSource _processingCancellationTokenSource;

        private readonly TimeSpan _processingTimeoutDuration = TimeSpan.FromSeconds(60);

        public CloudProxyApplication(IAppConfiguration appConfiguration, IUploader uploader, IServiceQueueClient queueClient, ILogger<CloudProxyApplication> logger)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            _uploader = uploader ?? throw new ArgumentNullException(nameof(uploader));
            _queueClient = queueClient ?? throw new ArgumentNullException(nameof(uploader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                _logger.LogInformation($"Uploading  file '{Path.GetFileName(_appConfiguration.InputFilepath)}' with FileId {inputFileId}");
                await UploadInputFile(inputFileId, _appConfiguration.InputFilepath);

                _logger.LogInformation($"Waiting on outcome for file '{Path.GetFileName(_appConfiguration.InputFilepath)}' with FileId {inputFileId}");
                var receivedMessage = queueListener.Take(processingCancellationToken);

                _logger.LogInformation($"Received message for file '{Path.GetFileName(_appConfiguration.InputFilepath)}' with FileId {inputFileId}");
                var transactionOutcome = TransactionOutcomeBuilder.Build(receivedMessage);

                _logger.LogInformation($"Received outcome for file '{Path.GetFileName(_appConfiguration.InputFilepath)}' with FileId {inputFileId}, Outcome = {Enum.GetName(typeof(ReturnOutcome), transactionOutcome.FileOutcome)}");

                await WriteRebuiltFile(transactionOutcome.FileRebuildSas);

                return (int)transactionOutcome.FileOutcome;
            }
            catch (RequestFailedException rfe)
            {
                _logger.LogError(rfe, $"Error Uploading 'input' {inputFileId}");
                return (int)ReturnOutcome.GW_ERROR;
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogError(oce, $"Error Processing Timeout 'input' {inputFileId} exceeded {_processingTimeoutDuration.TotalSeconds}s");
                return (int)ReturnOutcome.GW_ERROR;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error Processing 'input' {inputFileId}");
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
            _logger.LogInformation($"Uploading file '{Path.GetFileName(inputFilepath)}' with FileId {inputFileId}");
            await _uploader.UploadInputFile(inputFileId,  inputFilepath);
        }

        bool CheckConfigurationIsValid(IAppConfiguration configuration)
        {
            var configurationErrors = new List<string>();

            if (string.IsNullOrEmpty(configuration.InputFilepath))
                configurationErrors.Add($"'InputFilepath' configuration is missing");

            if (string.IsNullOrEmpty(configuration.OutputFilepath))
                configurationErrors.Add($"'OutputFilepath' configuration is missing");

            if (configurationErrors.Any())
            {
                _logger.LogError($"Error Processing Command {Environment.NewLine} \t{string.Join(',', configurationErrors)}");
            }

            return !configurationErrors.Any();
        }

        bool CheckConfigurationIsValid(ICloudConfiguration configuration)
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
                _logger.LogError($"Error Missing Configuration {Environment.NewLine} \t{string.Join(',', configurationErrors)}");
            }

            return !configurationErrors.Any();
        }
    }
}