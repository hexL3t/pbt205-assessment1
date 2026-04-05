using MassTransit;
using MassTransit.RabbitMqTransport;
using RabbitMQ.Client;
using ChatApp.Consumers;
using ChatApp.Messages;

namespace ChatApp.Middleware
{
    public static class ChatMiddleware
    {
        // Required convention: room.<roomname>
        public const string ExchangeName = "room"; // topic exchange name

        public static string NormalizeRoom(string room)
            => (room ?? "general").Trim().ToLowerInvariant();

        public static string RoomKey(string room)
            => $"room.{NormalizeRoom(room)}"; // routing key AND queue name format

        /// <summary>
        /// Build the chat bus:
        /// - RabbitMQ host configured once
        /// - Publish ChatMessage to exchange "room" (TOPIC)
        /// - Receive endpoint queue = room.<roomname>
        /// - Bind queue to exchange "room" with routing key room.<roomname>
        /// </summary>
        public static IBusControl CreateChatBus(string endpointHost, string localUsername, string room)
        {
            var roomKey = RoomKey(room); // e.g. room.general

            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(endpointHost, "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                // Force ChatMessage to use the exchange named "room"
                cfg.Message<ChatMessage>(x => x.SetEntityName(ExchangeName));

                // Ensure that exchange is TOPIC
                cfg.Publish<ChatMessage>(x =>
                {
                    x.ExchangeType = ExchangeType.Topic;
                });

                // Receive endpoint (queue) for this room: room.<roomname>
                cfg.ReceiveEndpoint(roomKey, e =>
                {
                    // We want to control bindings ourselves:
                    // don't let MassTransit auto-bind to message-type exchanges.
                    e.ConfigureConsumeTopology = false;

                    // Bind this ROOM QUEUE to the TOPIC exchange "room" using routing key room.<roomname>
                    e.Bind(ExchangeName, bind =>
                    {
                        bind.ExchangeType = ExchangeType.Topic;
                        bind.RoutingKey = roomKey; // room.general
                    });

                    // Consumer prints incoming messages; it can filter local user's own messages
                    e.Consumer(() => new ChatMessageConsumer(localUsername));
                });
            });
        }

        /// <summary>
        /// Publish a message into a room by setting routing key room.<roomname>.
        /// This ensures only that room's queue(s) receive it via the topic exchange.
        /// </summary>
        public static Task PublishToRoom(IBus bus, string room, ChatMessage message)
        {
            var normalizedRoom = NormalizeRoom(room);
            var roomKey = RoomKey(normalizedRoom); // room.general

            // Keep payload consistent
            message.Room = normalizedRoom;
            message.SentAt = message.SentAt == default ? DateTime.UtcNow : message.SentAt;

            // Publish with routing key so topic routing works
            return bus.Publish(message, ctx =>
            {
                if (ctx is RabbitMqSendContext<ChatMessage> rmq)
                    rmq.SetRoutingKey(roomKey);
            });
        }
    }
}