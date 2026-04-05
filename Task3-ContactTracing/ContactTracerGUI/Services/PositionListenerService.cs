using Microsoft.AspNetCore.SignalR;
using ContactTracerGui.Hubs;
using ContactTracerGui.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;

namespace ContactTracerGui.Services
{
    /// <summary>
    /// Subscribes to the RabbitMQ 'position' and 'query-response' topics
    /// and forwards events to connected browser clients via SignalR.
    /// Replaces the mock data implementation. Mirrors TradeListenerService
    /// from TradingGuiApp — same pattern, same RabbitMQ.Client v7 API.
    /// </summary>
    public class PositionListenerService : BackgroundService
    {
        private readonly IHubContext<TrackerHub> _hubContext;
        private readonly ILogger<PositionListenerService> _logger;
        private readonly IConfiguration _configuration;

        private readonly int _boardSize;

        // Contact log kept in memory so QueryController can still serve
        // REST queries alongside the RabbitMQ query-response flow
        private readonly List<ContactEvent> _contactLog = new();
        private readonly Lock _lock = new();

        public PositionListenerService(
            IHubContext<TrackerHub> hubContext,
            ILogger<PositionListenerService> logger,
            IConfiguration configuration)
        {
            _hubContext = hubContext;
            _logger = logger;
            _configuration = configuration;
            _boardSize = configuration.GetValue<int>("Board:Size", 10);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string endpoint = _configuration["RabbitMQ:Endpoint"] ?? "localhost";
            var parts = endpoint.Split(':');
            string host = parts[0];
            int port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 5672;

            _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port}", host, port);

            // ── CONNECTION ────────────────────────────────────────────────
            var factory = new ConnectionFactory { HostName = host, Port = port };
            using var connection = await factory.CreateConnectionAsync(stoppingToken);
            using var channel    = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // Declare all three exchanges — matches TrackerApp and PersonApp declarations
            await channel.ExchangeDeclareAsync("position",       ExchangeType.Fanout, durable: true,  cancellationToken: stoppingToken);
            await channel.ExchangeDeclareAsync("query",          ExchangeType.Fanout, durable: true,  cancellationToken: stoppingToken);
            await channel.ExchangeDeclareAsync("query-response", ExchangeType.Fanout, durable: true,  cancellationToken: stoppingToken);

            // ── SUBSCRIBE: position ───────────────────────────────────────
            // Receives PersonPosition from every PersonApp instance.
            // Forwards to browser via SignalR ReceivePositions event.
            var posQueue = await channel.QueueDeclareAsync(
                queue: string.Empty, durable: false, exclusive: true, autoDelete: true,
                cancellationToken: stoppingToken);
            await channel.QueueBindAsync(posQueue.QueueName, "position", string.Empty, cancellationToken: stoppingToken);

            // Keep a local view of the board so we can broadcast full state
            var positions = new Dictionary<string, PersonPosition>();

            var posConsumer = new AsyncEventingBasicConsumer(channel);
            posConsumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var pos  = JsonConvert.DeserializeObject<PersonPosition>(json);
                    if (pos == null) return;

                    _logger.LogInformation("{Name} → ({X},{Y})", pos.Name, pos.X, pos.Y);

                    // Update board view
                    positions[pos.Name] = pos;

                    // Check for contacts against all other known positions
                    foreach (var (otherName, otherPos) in positions)
                    {
                        if (otherName == pos.Name) continue;
                        if (otherPos.X == pos.X && otherPos.Y == pos.Y)
                        {
                            var contact = new ContactEvent
                            {
                                Person1   = pos.Name,
                                Person2   = otherName,
                                X         = pos.X,
                                Y         = pos.Y,
                                Timestamp = pos.Timestamp
                            };

                            lock (_lock) { _contactLog.Insert(0, contact); }

                            _logger.LogInformation(
                                "Contact: {P1} & {P2} at ({X},{Y})",
                                pos.Name, otherName, pos.X, pos.Y);
                        }
                    }

                    // Broadcast full board state to all browser clients
                    await _hubContext.Clients.All.SendAsync(
                        "ReceivePositions",
                        positions.Values.ToList(),
                        _boardSize,
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Position deserialisation error: {Message}", ex.Message);
                }
            };

            await channel.BasicConsumeAsync(posQueue.QueueName, autoAck: true, consumer: posConsumer, cancellationToken: stoppingToken);

            // ── SUBSCRIBE: query-response ─────────────────────────────────
            // Receives QueryResponse from TrackerApp and forwards to browser
            // via SignalR ReceiveQueryResponse event so the GUI can display results.
            var qrQueue = await channel.QueueDeclareAsync(
                queue: string.Empty, durable: false, exclusive: true, autoDelete: true,
                cancellationToken: stoppingToken);
            await channel.QueueBindAsync(qrQueue.QueueName, "query-response", string.Empty, cancellationToken: stoppingToken);

            var qrConsumer = new AsyncEventingBasicConsumer(channel);
            qrConsumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var json     = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var response = JsonConvert.DeserializeObject<QueryResponse>(json);
                    if (response == null) return;

                    _logger.LogInformation(
                        "Query response for {Name}: {Count} contact(s)",
                        response.QueryName, response.Contacts.Count);

                    // Forward to browser — index.html listens for "ReceiveQueryResponse"
                    await _hubContext.Clients.All.SendAsync(
                        "ReceiveQueryResponse",
                        response,
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Query-response deserialisation error: {Message}", ex.Message);
                }
            };

            await channel.BasicConsumeAsync(qrQueue.QueueName, autoAck: true, consumer: qrConsumer, cancellationToken: stoppingToken);

            _logger.LogInformation("PositionListenerService running — subscribed to 'position' and 'query-response'.");

            // Keep alive until the app shuts down
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        // Used by QueryController for REST-based contact history queries
        public List<ContactEvent> GetContactsFor(string name)
        {
            lock (_lock)
            {
                return _contactLog
                    .Where(c => c.Person1 == name || c.Person2 == name)
                    .OrderByDescending(c => c.Timestamp)
                    .ToList();
            }
        }
    }
}