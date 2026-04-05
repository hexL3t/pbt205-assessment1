namespace ChatApp.Messages
{
    /// <summary>
    /// Represents a single chat message sent through RabbitMQ.
    /// This is the shared message schema everyone on the team must use.
    /// David's middleware and this CLI both rely on this structure.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// The username of the person who sent the message.
        /// Set from the command line argument when the app starts.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// The room the message was sent to, e.g. "general", "sports".
        /// Used by RabbitMQ to route messages to the correct topic.
        /// Topic format will be: room.<RoomName> e.g. room.general
        /// </summary>
        public string? Room { get; set; }

        /// <summary>
        /// The actual text content of the message typed by the user.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// The UTC timestamp of when the message was sent.
        /// Set automatically before publishing, not entered by the user.
        /// </summary>
        public DateTime SentAt { get; set; }
    }
}