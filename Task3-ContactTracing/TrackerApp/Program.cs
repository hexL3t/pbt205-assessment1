using ContactTracingCore.Models;
using ContactTracingCore.Services;

// ─────────────────────────────────────────────
// USAGE: dotnet run --project TrackerApp -- <endpoint>
// Example: dotnet run --project TrackerApp -- localhost
// ─────────────────────────────────────────────

if (args.Length < 1)
{
    Console.WriteLine("Usage: TrackerApp <rabbitmq-endpoint>");
    Console.WriteLine("Example: TrackerApp localhost");
    return;
}

string endpoint = args[0];
var parts = endpoint.Split(':');
string host = parts[0];
int port = parts.Length == 2 && int.TryParse(parts[1], out int p) ? p : 5672;

Console.WriteLine($"┌─ TRACKER ────────────────────────────────┐");
Console.WriteLine($"  Starting tracker on {host}:{port}");
Console.WriteLine($"└──────────────────────────────────────────┘");

var currentPositions = new Dictionary<string, (int X, int Y)>();
var contactLog = new List<ContactEvent>();

using var mq = new RabbitMQService(host, port);

mq.Subscribe<PersonPosition>(RabbitMQService.POSITION_TOPIC, pos =>
{
    currentPositions[pos.Name] = (pos.X, pos.Y);

    Console.WriteLine($"  [{pos.Timestamp:HH:mm:ss}] {pos.Name} → ({pos.X},{pos.Y})");

    foreach (var (otherName, otherPos) in currentPositions)
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

            contactLog.Insert(0, contact);

            Console.WriteLine($"┌─ CONTACT ────────────────────────────────┐");
            Console.WriteLine($"  {pos.Name} & {otherName} at ({pos.X},{pos.Y})");
            Console.WriteLine($"└──────────────────────────────────────────┘");
        }
    }
});

mq.Subscribe<QueryRequest>(RabbitMQService.QUERY_TOPIC, req =>
{
    Console.WriteLine($"┌─ QUERY ──────────────────────────────────┐");
    Console.WriteLine($"  Received query for: {req.Name}");

    var matches = contactLog
        .Where(c => c.Person1 == req.Name || c.Person2 == req.Name)
        .ToList();

    var response = new QueryResponse
    {
        QueryName = req.Name,
        Contacts  = matches
    };

    mq.Publish(RabbitMQService.QUERY_RESPONSE_TOPIC, response);

    Console.WriteLine($"  Found {matches.Count} contact(s). Response published.");
    Console.WriteLine($"└──────────────────────────────────────────┘");
});

Console.WriteLine("Tracker running. Press Ctrl+C to exit.");
Console.WriteLine(new string('-', 46));

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try { await Task.Delay(Timeout.Infinite, cts.Token); }
catch (TaskCanceledException) { }

Console.WriteLine("Tracker stopped.");