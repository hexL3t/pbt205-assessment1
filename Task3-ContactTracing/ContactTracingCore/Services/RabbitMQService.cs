using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;

namespace ContactTracingCore.Services
{
    // Mirrors TradingCore.Services.RabbitMQService exactly.
    // Topic names match the assessment spec for Task 3.
    public class RabbitMQService : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel    _channel;

        public const string POSITION_TOPIC       = "position";
        public const string QUERY_TOPIC          = "query";
        public const string QUERY_RESPONSE_TOPIC = "query-response";

        public RabbitMQService(string host = "localhost", int port = 5672)
        {
            var factory = new ConnectionFactory { HostName = host, Port = port };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel    = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declare all three exchanges upfront — fanout, durable.
            // Safe to call on every startup even if exchanges already exist.
            _channel.ExchangeDeclareAsync(POSITION_TOPIC,       ExchangeType.Fanout, durable: true).GetAwaiter().GetResult();
            _channel.ExchangeDeclareAsync(QUERY_TOPIC,          ExchangeType.Fanout, durable: true).GetAwaiter().GetResult();
            _channel.ExchangeDeclareAsync(QUERY_RESPONSE_TOPIC, ExchangeType.Fanout, durable: true).GetAwaiter().GetResult();

            Console.WriteLine($"┌─ RABBITMQ ───────────────────────────────┐");
            Console.WriteLine($"  Connected to {host}:{port}");
            Console.WriteLine($"└──────────────────────────────────────────┘");
        }

        public void Publish<T>(string exchange, T message)
        {
            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                Persistent  = true,
                ContentType = "application/json"
            };

            _channel.BasicPublishAsync(
                exchange:         exchange,
                routingKey:       string.Empty,
                mandatory:        false,
                basicProperties:  props,
                body:             body
            ).GetAwaiter().GetResult();
        }

        public void Subscribe<T>(string exchange, Action<T> onMessage)
        {
            var queueName = _channel.QueueDeclareAsync(
                queue:      string.Empty,
                durable:    false,
                exclusive:  true,
                autoDelete: true
            ).GetAwaiter().GetResult().QueueName;

            _channel.QueueBindAsync(queueName, exchange, routingKey: string.Empty).GetAwaiter().GetResult();

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var obj  = JsonConvert.DeserializeObject<T>(json);
                    if (obj != null) onMessage(obj);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Deserialisation error: {ex.Message}");
                }
                await Task.CompletedTask;
            };

            _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer).GetAwaiter().GetResult();

            Console.WriteLine($"┌─ RABBITMQ ───────────────────────────────┐");
            Console.WriteLine($"  Subscribed to '{exchange}' topic.");
            Console.WriteLine($"└──────────────────────────────────────────┘");
        }

        public void Dispose()
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();
        }
    }
}
