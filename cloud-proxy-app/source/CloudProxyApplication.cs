using Azure;
using Azure.Storage.Blobs;
using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Concurrent;
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
        private readonly ICloudConfiguration _cloudConfiguration;
        private readonly Func<string, BlobServiceClient> _blobServiceClientFactory;

        private readonly BlockingCollection<Message> _messageQueue;
        private readonly CancellationTokenSource _processingCancellationTokenSource;

        private readonly TimeSpan _processingTimeoutDuration = TimeSpan.FromSeconds(60);

        private readonly Dictionary<string, ReturnOutcome> OutcomeMap = new Dictionary<string, ReturnOutcome>
        {
            ["Unknown"]=ReturnOutcome.GW_ERROR,
            ["Rebuilt"]=ReturnOutcome.GW_REBUILT,
            ["Unmanaged"]=ReturnOutcome.GW_UNPROCESSED,
            ["Failed"]=ReturnOutcome.GW_FAILED,
            ["Error"]=ReturnOutcome.GW_ERROR
        };

        public CloudProxyApplication(IAppConfiguration appConfiguration, ICloudConfiguration cloudConfiguration, Func<string, BlobServiceClient> blobServiceClientFactory)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            _cloudConfiguration = cloudConfiguration ?? throw new ArgumentNullException(nameof(cloudConfiguration));
            _blobServiceClientFactory = blobServiceClientFactory ?? throw new ArgumentNullException(nameof(blobServiceClientFactory));
            _messageQueue = new BlockingCollection<Message>();
            _processingCancellationTokenSource = new CancellationTokenSource(_processingTimeoutDuration);
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
                var processingCancellationToken = _processingCancellationTokenSource.Token;
                var queueClient = new QueueClient(_cloudConfiguration.TransactionOutcomeQueueConnectionString, _cloudConfiguration.TransactionOutcomeQueueName);
                var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
                {
                    MaxConcurrentCalls = 1,
                    AutoComplete = false
                };

                queueClient.RegisterMessageHandler((Message msg, CancellationToken ct) =>
                {
                    if (msg.UserProperties["file-id"] as string == inputFileId.ToString())
                    {
                        _messageQueue.Add(msg, ct);
                        _messageQueue.CompleteAdding();
                    }
                    return Task.CompletedTask;
                }, messageHandlerOptions);

                BlobServiceClient blobServiceClient = _blobServiceClientFactory(_cloudConfiguration.FileProcessingStorageConnectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_cloudConfiguration.FileProcessingStorageOriginalStoreName);

                inputFileId = Guid.NewGuid();
                BlobClient blobClient = containerClient.GetBlobClient(inputFileId.ToString());

                Console.WriteLine($"Uploading file '{Path.GetFileName(_appConfiguration.InputFilepath)}' with FileId {inputFileId}");
                using (FileStream uploadFileStream = File.OpenRead(_appConfiguration.InputFilepath))
                {
                    var status = await blobClient.UploadAsync(uploadFileStream, true);
                }

                var receivedMessage = _messageQueue.Take(processingCancellationToken);
                await queueClient.CompleteAsync(receivedMessage.SystemProperties.LockToken);
                var outcome = OutcomeMap[receivedMessage.UserProperties["file-outcome"] as string];
                Console.WriteLine($"Received outcome for file '{Path.GetFileName(_appConfiguration.InputFilepath)}' with FileId {inputFileId}, Outcome = {Enum.GetName(typeof(ReturnOutcome), outcome)}");
                var rebuiltFileToken = receivedMessage.UserProperties["file-rebuild-sas"] as string;
                BlobClient rebuiltFileBlobClient = new BlobClient(new Uri(rebuiltFileToken));
                using (FileStream downloadFileStream = File.OpenWrite(_appConfiguration.OutputFilepath))
                {
                    await rebuiltFileBlobClient.DownloadToAsync(downloadFileStream);
                }

                return (int)outcome;
            }
            catch (RequestFailedException rfe)
            {
                Console.WriteLine($"Error Uploading 'input' {inputFileId}, {rfe.Message}");
                return (int)ReturnOutcome.GW_ERROR;
            }
            catch (OperationCanceledException oce)
            {
                Console.WriteLine($"Error Processing Timeout pf 'input' {inputFileId} exceeded {_processingTimeoutDuration.TotalSeconds}s, {oce.Message}");
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

        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            return Task.CompletedTask;
        }
    }
}