using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glasswall.IcapServer.CloudProxyApp
{
    public class NativeProxyApplication : IDisposable
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger<NativeProxyApplication> _logger;
        private readonly CancellationTokenSource _processingCancellationTokenSource;
        private readonly TimeSpan _processingTimeoutDuration = TimeSpan.FromSeconds(60);

        private readonly IConnection _outcomeQueueConnection;
        private readonly IModel _outcomeQueueModel;
        private readonly EventingBasicConsumer _outcomeQueueConsumer;

        private readonly string ExchangeName = "adaptation-exchange";
        private readonly string RequestQueueName = "adaptation-request-queue";
        private readonly string OutcomeQueueName = "adaptation-outcome-queue";

        private readonly string RequestMessageName = "adaptation-request";
        private readonly string ResponseMessageName = "adaptation-outcome";

        readonly string OriginalStorePath = "/var/source";
        readonly string RebuiltStorePath = "/var/target";
        private bool disposedValue;

        public NativeProxyApplication(IAppConfiguration appConfiguration, ILogger<NativeProxyApplication> logger)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processingCancellationTokenSource = new CancellationTokenSource(_processingTimeoutDuration);

            _outcomeQueueConnection = GetQueueConnection();
            _outcomeQueueModel = _outcomeQueueConnection.CreateModel();
            _outcomeQueueConsumer = new EventingBasicConsumer(_outcomeQueueModel);
        }

        public Task<int> RunAsync()
        {
            var returnOutcomeStatus = new BlockingCollection<int>();
            var fileId = Guid.NewGuid().ToString();
            try
            {
                var processingCancellationToken = _processingCancellationTokenSource.Token;

                var originalStoreFilePath = Path.Combine(OriginalStorePath, fileId);
                var rebuiltStoreFilePath = Path.Combine(RebuiltStorePath, fileId);

                _logger.LogInformation($"Updating 'Original' store for {fileId}");
                File.Copy(_appConfiguration.InputFilepath, originalStoreFilePath);

                ListenForOutcome(fileId, returnOutcomeStatus);

                SendRequest(fileId, originalStoreFilePath, rebuiltStoreFilePath);

                var result = returnOutcomeStatus.Take(processingCancellationToken);
                _logger.LogInformation($"Returning '{result}' Outcome for {fileId}");

                return Task.FromResult(result);
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogError(oce, $"Error Processing Timeout 'input' {fileId} exceeded {_processingTimeoutDuration.TotalSeconds}s");
                return Task.FromResult((int)ReturnOutcome.GW_ERROR);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error Processing 'input' {fileId}");
                return Task.FromResult((int)ReturnOutcome.GW_ERROR);
            }
        }

        private void ListenForOutcome(string fileId, BlockingCollection<int> returnOutcomeStatus)
        {
            var queueDeclare = _outcomeQueueModel.QueueDeclare(queue: OutcomeQueueName,
                          durable: false,
                          exclusive: false,
                          autoDelete: false,
                          arguments: null);
            _logger.LogInformation($"Receive Request Queue '{queueDeclare.QueueName}' Declared : MessageCount = {queueDeclare.MessageCount},  ConsumerCount = {queueDeclare.ConsumerCount}");

            _outcomeQueueModel.QueueBind(queue: OutcomeQueueName,
                              exchange: ExchangeName,
                              routingKey: ResponseMessageName);

            _outcomeQueueConsumer.Received += (model, ea) =>
            {
                _logger.LogInformation($"Received Outcome for {fileId}");
                var headers = ea.BasicProperties.Headers;

                returnOutcomeStatus.Add((int)ReturnOutcome.GW_UNPROCESSED);

                _outcomeQueueModel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                _logger.LogInformation($"Acked Outcome for {fileId}");
            };
            _outcomeQueueModel.BasicConsume(_outcomeQueueConsumer, OutcomeQueueName);
        }

        private void SendRequest(string fileId, string originalStoreFilePath, string rebuiltStoreFilePath)
        {
            using (var connection = GetQueueConnection())
            using (var channel = connection.CreateModel())
            {
                var queueDeclare = channel.QueueDeclare(queue: RequestQueueName,
                                                         durable: false,
                                                         exclusive: false,
                                                         autoDelete: false,
                                                         arguments: null);
                _logger.LogInformation($"Send Request Queue '{queueDeclare.QueueName}' Declared : MessageCount = {queueDeclare.MessageCount},  ConsumerCount = {queueDeclare.ConsumerCount}");


                IDictionary<string, object> headerMap = new Dictionary<string, object>
                    {
                        { "file-id", fileId },
                        { "request-mode", "respmod" },
                        { "source-file-location", originalStoreFilePath},
                        { "rebuilt-file-location", rebuiltStoreFilePath}
                    };

                string messageBody = JsonConvert.SerializeObject(headerMap, Formatting.None);
                var body = Encoding.UTF8.GetBytes(messageBody);

                var messageProperties = channel.CreateBasicProperties();
                messageProperties.Headers = headerMap;

                _logger.LogInformation($"Sending {RequestMessageName} for {fileId}");

                channel.BasicPublish(exchange: ExchangeName,
                                     routingKey: RequestMessageName,
                                     basicProperties: messageProperties,
                                     body: body);
                //channel.BasicPublish(exchange: "",
                //                     routingKey: RequestMessageName,
                //                     basicProperties: messageProperties,
                //                     body: body);
            }

        }

        private static IConnection GetQueueConnection()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "rabbitmq-service",
                Port = 5672,
                UserName = ConnectionFactory.DefaultUser,
                Password = ConnectionFactory.DefaultPass
            };
            return factory.CreateConnection();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _outcomeQueueModel.Dispose();
                    _outcomeQueueConnection.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
