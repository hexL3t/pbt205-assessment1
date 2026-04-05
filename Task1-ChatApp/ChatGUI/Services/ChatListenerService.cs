using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;
using ChatGUI.Hubs;
 
namespace ChatGUI.Services
{
    public class ChatMessageDto
    {
        public string Username { get; set; } = string.Empty;
        public string Room     { get; set; } = string.Empty;
        public string Content  { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
 
    public class ChatListenerService : BackgroundService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<ChatListenerService> _logger;
        private readonly IConfiguration _configuration;
 
        public ChatListenerService(
            IHubContext<ChatHub> hubContext,
            ILogger<ChatListenerService> logger,
            IConfiguration configuration)
        {
            _hubContext = hubContext;
            _logger = logger;
            _configuration = configuration;
        }
 
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string host = _configuration["RabbitMQ:Host"] ?? "localhost";
            string username = _configuration["RabbitMQ:Username"] ?? "guest";
            string password = _configuration["RabbitMQ:Password"] ?? "guest";
 
            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = username,
                Password = password
            };
 
            using var connection = await factory.CreateConnectionAsync(stoppingToken);
            using var channel    = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
 
            // Declare the same topic exchange ChatApp uses
            await channel.ExchangeDeclareAsync(
                exchange:    "room",
                type:        ExchangeType.Topic,
                durable:     true,
                autoDelete:  false,
                cancellationToken: stoppingToken);
 
            // Temporary exclusive queue bound to room.# — catches all rooms
            var queue = await channel.QueueDeclareAsync(
                queue:      string.Empty,
                durable:    false,
                exclusive:  true,
                autoDelete: true,
                cancellationToken: stoppingToken);
 
            await channel.QueueBindAsync(
                queue:       queue.QueueName,
                exchange:    "room",
                routingKey:  "room.#",
                cancellationToken: stoppingToken);
 
            _logger.LogInformation("ChatListenerService subscribed to room.# on exchange 'room'");
 
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    _logger.LogInformation("Raw message received: {Json}", json);
 
                    // MassTransit wraps messages in an envelope — try unwrapping first
                    ChatMessageDto? msg = null;
                    try
                    {
                        // Try direct deserialisation first
                        msg = JsonConvert.DeserializeObject<ChatMessageDto>(json);
 
                        // If Username is empty, it's probably a MassTransit envelope
                        if (msg == null || string.IsNullOrEmpty(msg.Username))
                        {
                            dynamic? envelope = JsonConvert.DeserializeObject<dynamic>(json);
                            if (envelope?.message != null)
                            {
                                msg = JsonConvert.DeserializeObject<ChatMessageDto>(
                                    JsonConvert.SerializeObject(envelope.message));
                            }
                        }
                    }
                    catch
                    {
                        msg = null;
                    }
 
                    if (msg == null || string.IsNullOrEmpty(msg.Username))
                    {
                        _logger.LogWarning("Could not deserialise message: {Json}", json);
                        return;
                    }
 
                    _logger.LogInformation("[{Room}] {Username}: {Content}", msg.Room, msg.Username, msg.Content);
 
                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", msg, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error processing message: {Message}", ex.Message);
                }
            };
 
            await channel.BasicConsumeAsync(
                queue:   queue.QueueName,
                autoAck: true,
                consumer: consumer,
                cancellationToken: stoppingToken);
 
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}