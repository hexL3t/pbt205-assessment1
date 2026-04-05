using ChatApp.Messages;
using ChatApp.Middleware;

// ─────────────────────────────────────────────
// STEP 1: READ COMMAND LINE ARGUMENTS
// Usage: dotnet run --project ChatApp -- <username> <endpoint> <room>
// Example: dotnet run --project ChatApp -- Tia localhost general
// ─────────────────────────────────────────────

if (args.Length < 3)
{
    Console.WriteLine("Usage: ChatApp <username> <rabbitmq-endpoint> <room>");
    Console.WriteLine("Example: ChatApp Tia localhost general");
    return;
}

string username = args[0]; // e.g. "Tia"
string endpoint = args[1]; // e.g. "localhost"
string room     = args[2]; // e.g. "general"

Console.WriteLine($"Connecting as '{username}' to '{endpoint}', joining room '{room}'...");
Console.WriteLine("Type a message and press Enter to send. Press Ctrl+C to exit.");
Console.WriteLine(new string('-', 50));

// ─────────────────────────────────────────────
// STEP 2: CREATE AND CONFIGURE THE BUS (MIDDLEWARE/TOPICS OWNED HERE)
// This is now handled by ChatMiddleware so it's reusable and consistent.
// ─────────────────────────────────────────────

var busControl = ChatMiddleware.CreateChatBus(endpoint, username, room);

// ─────────────────────────────────────────────
// STEP 3: START THE BUS
// ─────────────────────────────────────────────

await busControl.StartAsync();

try
{
    // ─────────────────────────────────────────────
    // STEP 4: MAIN CHAT LOOP
    // ─────────────────────────────────────────────

    while (true)
    {
        var input = Console.ReadLine();

        // null means Ctrl+C or EOF — exit cleanly
        if (input == null) break;

        // Don't send blank messages
        if (string.IsNullOrWhiteSpace(input)) continue;

        var message = new ChatMessage
        {
            Username = username,
            Room     = room,
            Content  = input,
            SentAt   = DateTime.UtcNow
        };

        // IMPORTANT:
        // Publish using the room-specific routing key (room.<roomname>)
        await ChatMiddleware.PublishToRoom(busControl, room, message);
    }
}
finally
{
    // ─────────────────────────────────────────────
    // STEP 5: GRACEFUL SHUTDOWN
    // ─────────────────────────────────────────────
    await busControl.StopAsync();
}