using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Glasswall.IcapServer.CloudProxyApp.AdaptationService
{
    public class RabbitMqClient<TResponseProcessor> : IAdaptationServiceClient<TResponseProcessor> where TResponseProcessor : IResponseProcessor
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly EventingBasicConsumer _consumer;

        private readonly BlockingCollection<ReturnOutcome> _respQueue = new BlockingCollection<ReturnOutcome>();
        private readonly IResponseProcessor _responseProcessor;
        private readonly ILogger<RabbitMqClient<TResponseProcessor>> _logger;

        private readonly string ExchangeName = "adaptation-exchange";
        private readonly string RequestQueueName = "adaptation-request-queue";
        private readonly string OutcomeQueueName = "adaptation-outcome-queue";

        private readonly string RequestMessageName = "adaptation-request";
        private readonly string ResponseMessageName = "adaptation-outcome";

        public RabbitMqClient(IResponseProcessor responseProcessor, ILogger<RabbitMqClient<TResponseProcessor>> logger)
        {
            _responseProcessor = responseProcessor ?? throw new ArgumentNullException(nameof(responseProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var factory = new ConnectionFactory()
            {
                HostName = "rabbitmq-service",
                Port = 5672,
                UserName = ConnectionFactory.DefaultUser,
                Password = ConnectionFactory.DefaultPass
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _consumer = new EventingBasicConsumer(_channel);

            var queueDeclare = _channel.QueueDeclare(queue: OutcomeQueueName,
                      durable: false,
                      exclusive: false,
                      autoDelete: false,
                      arguments: null);
            _logger.LogInformation($"Receive Request Queue '{queueDeclare.QueueName}' Declared : MessageCount = {queueDeclare.MessageCount},  ConsumerCount = {queueDeclare.ConsumerCount}");

            _channel.QueueBind(queue: OutcomeQueueName,
                                exchange: ExchangeName,
                                routingKey: ResponseMessageName);

            _consumer.Received += (model, ea) =>
            {
                try
                {
                    var headers = ea.BasicProperties.Headers;
                    var body = ea.Body.ToArray();

                    var response = _responseProcessor.Process(headers, body);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                    _respQueue.Add(response);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, $"Error Processing 'input'");
                    _respQueue.Add(ReturnOutcome.GW_ERROR);
                }
            };
        }

        public ReturnOutcome AdaptationRequest(Guid fileId, string originalStoreFilePath, string rebuiltStoreFilePath, CancellationToken processingCancellationToken)
        {
            var queueDeclare = _channel.QueueDeclare(queue: RequestQueueName,
                                                          durable: false,
                                                          exclusive: false,
                                                          autoDelete: false,
                                                          arguments: null);
            _logger.LogInformation($"Send Request Queue '{queueDeclare.QueueName}' Declared : MessageCount = {queueDeclare.MessageCount},  ConsumerCount = {queueDeclare.ConsumerCount}");

            IDictionary<string, object> headerMap = new Dictionary<string, object>
                    {
                        { "file-id", fileId.ToString() },
                        { "request-mode", "respmod" },
                        { "source-file-location", originalStoreFilePath},
                        { "rebuilt-file-location", rebuiltStoreFilePath}
                    };

            string messageBody = JsonConvert.SerializeObject(headerMap, Formatting.None);
            var body = Encoding.UTF8.GetBytes(messageBody);

            var messageProperties = _channel.CreateBasicProperties();
            messageProperties.Headers = headerMap;

            _logger.LogInformation($"Sending {RequestMessageName} for {fileId}");

            _channel.BasicPublish(exchange: ExchangeName,
                                 routingKey: RequestMessageName,
                                 basicProperties: messageProperties,
                                 body: body);

            _channel.BasicConsume(_consumer, OutcomeQueueName);

            return _respQueue.Take(processingCancellationToken);
        }
    }
}
