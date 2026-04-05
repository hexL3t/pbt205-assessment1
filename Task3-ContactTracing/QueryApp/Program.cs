using ContactTracingCore.Models;
using ContactTracingCore.Services;

// ─────────────────────────────────────────────
// USAGE: dotnet run --project QueryApp -- <endpoint> <n>
// Example: dotnet run --project QueryApp -- localhost Alice
// ─────────────────────────────────────────────

if (args.Length < 2)
{
    Console.WriteLine("Usage: QueryApp <rabbitmq-endpoint> <person-name>");
    Console.WriteLine("Example: QueryApp localhost Alice");
    return;
}

string endpoint = args[0];
string name     = args[1];

var parts = endpoint.Split(':');
string host = parts[0];
int port = parts.Length == 2 && int.TryParse(parts[1], out int p) ? p : 5672;

Console.WriteLine($"┌─ QUERY ──────────────────────────────────┐");
Console.WriteLine($"  Querying contacts for: {name}");
Console.WriteLine($"  Endpoint: {host}:{port}");
Console.WriteLine($"└──────────────────────────────────────────┘");

using var mq = new RabbitMQService(host, port);

// Subscribe BEFORE publishing so we don't miss the response
var responseReceived = new TaskCompletionSource<QueryResponse>();

mq.Subscribe<QueryResponse>(RabbitMQService.QUERY_RESPONSE_TOPIC, response =>
{
    if (response.QueryName == name)
    {
        responseReceived.TrySetResult(response);
    }
});

mq.Publish(RabbitMQService.QUERY_TOPIC, new QueryRequest { Name = name });
Console.WriteLine("  Query sent. Waiting for response...");

// Timeout after 10 seconds in case TrackerApp is not running
var timeout = Task.Delay(TimeSpan.FromSeconds(10));
var completed = await Task.WhenAny(responseReceived.Task, timeout);

if (completed == timeout)
{
    Console.WriteLine("  No response received. Is TrackerApp running?");
    return;
}

var result = await responseReceived.Task;

Console.WriteLine();
Console.WriteLine($"┌─ CONTACTS FOR {name.ToUpper()} {new string('─', Math.Max(0, 27 - name.Length))}┐");

if (result.Contacts.Count == 0)
{
    Console.WriteLine($"  No contacts recorded.");
}
else
{
    foreach (var contact in result.Contacts)
    {
        string other = contact.Person1 == name ? contact.Person2 : contact.Person1;
        Console.WriteLine($"  [{contact.Timestamp:yyyy-MM-dd HH:mm:ss}] {other} at ({contact.X},{contact.Y})");
    }
}

Console.WriteLine($"└──────────────────────────────────────────┘");