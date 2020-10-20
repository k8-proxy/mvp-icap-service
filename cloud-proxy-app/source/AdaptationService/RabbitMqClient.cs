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
        private readonly IConnectionFactory connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        private EventingBasicConsumer _consumer;

        private readonly BlockingCollection<ReturnOutcome> _respQueue = new BlockingCollection<ReturnOutcome>();
        private readonly IResponseProcessor _responseProcessor;
        private readonly ILogger<RabbitMqClient<TResponseProcessor>> _logger;

        private readonly string ExchangeName = "adaptation-exchange";
        private readonly string RequestQueueName = "adaptation-request-queue";
        private readonly string OutcomeQueueName = "amq.rabbitmq.reply-to";

        private readonly string RequestMessageName = "adaptation-request";

        public RabbitMqClient(IResponseProcessor responseProcessor, ILogger<RabbitMqClient<TResponseProcessor>> logger)
        {
            _responseProcessor = responseProcessor ?? throw new ArgumentNullException(nameof(responseProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            connectionFactory = new ConnectionFactory()
            {
                HostName = "rabbitmq-service",
                Port = 5672,
                UserName = ConnectionFactory.DefaultUser,
                Password = ConnectionFactory.DefaultPass
            };
        }

        public void Connect()
        {
            if (_connection != null || _channel != null || _consumer != null)
                throw new AdaptationServiceClientException("'Connect' should only be called once.");

            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _consumer = new EventingBasicConsumer(_channel);

            _consumer.Received += (model, ea) =>
            {
                try
                {
                    _logger.LogInformation($"Received message: Exchange Name: '{ea.Exchange}', Routing Key: '{ea.RoutingKey}', CorrelationId: '{ea.BasicProperties.CorrelationId}'");
                    var headers = ea.BasicProperties.Headers;
                    var body = ea.Body.ToArray();

                    var response = _responseProcessor.Process(headers, body);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                    _respQueue.Add(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error Processing 'input'");
                    _respQueue.Add(ReturnOutcome.GW_ERROR);
                }
            };
        }

        public ReturnOutcome AdaptationRequest(Guid fileId, string originalStoreFilePath, string rebuiltStoreFilePath, CancellationToken processingCancellationToken)
        {
            if (_connection == null || _channel == null || _consumer == null)
                throw new AdaptationServiceClientException("'Connect' should be called before 'AdaptationRequest'.");

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
            messageProperties.ReplyTo = OutcomeQueueName;
            messageProperties.CorrelationId = fileId.ToString();

            _logger.LogInformation($"Sending {RequestMessageName} for {fileId}");

            _channel.BasicConsume(_consumer, OutcomeQueueName, autoAck: true);

            _channel.BasicPublish(exchange: ExchangeName,
                                 routingKey: RequestMessageName,
                                 basicProperties: messageProperties,
                                 body: body);

            return _respQueue.Take(processingCancellationToken);
        }
    }
}
