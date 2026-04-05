using MassTransit;
using ChatApp.Messages;

namespace ChatApp.Consumers
{
    /// <summary>
    /// Listens for incoming chat messages from RabbitMQ and displays them
    /// in the terminal. This runs in the background while the user can
    /// still type and send messages at the same time.
    /// </summary>
    public class ChatMessageConsumer : IConsumer<ChatMessage>
    {
        // The username of the LOCAL user running this instance of the app.
        // We use this to avoid printing our own messages back to ourselves.
        private readonly string _localUsername;

        /// <summary>
        /// Constructor receives the local username so we can filter
        /// out messages sent by this user (they already see what they typed).
        /// </summary>
        /// <param name="localUsername">The username passed in at startup</param>
        public ChatMessageConsumer(string localUsername)
        {
            _localUsername = localUsername;
        }

        /// <summary>
        /// Called automatically by MassTransit whenever a new ChatMessage
        /// arrives on the RabbitMQ topic this consumer is subscribed to.
        /// </summary>
        /// <param name="context">Contains the received message and metadata</param>
        public async Task Consume(ConsumeContext<ChatMessage> context)
        {
            var message = context.Message;

            // Don't print the message if WE sent it —
            // the user already saw it when they typed it
            if (message.Username == _localUsername)
                return;

            // Print the message in the format: [HH:mm] Username: Content
            // e.g. [14:32] David: Hey everyone!
            Console.WriteLine(
                $"[{message.SentAt.ToLocalTime():HH:mm}] {message.Username}: {message.Content}"
            );

            // Consume is async by interface contract — we don't need
            // to do anything async here but must return a completed task
            await Task.CompletedTask;
        }
    }
}