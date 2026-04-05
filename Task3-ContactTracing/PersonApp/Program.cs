using ContactTracingCore.Models;
using ContactTracingCore.Services;

// ─────────────────────────────────────────────
// USAGE: dotnet run --project PersonApp -- <endpoint> <name> <speed>
// Example: dotnet run --project PersonApp -- localhost Alice 1
//
// speed = moves per second (e.g. 1 = one move/sec, 2 = two moves/sec)
// ─────────────────────────────────────────────

if (args.Length < 3)
{
    Console.WriteLine("Usage: PersonApp <rabbitmq-endpoint> <name> <moves-per-second>");
    Console.WriteLine("Example: PersonApp localhost Alice 1");
    return;
}

string endpoint = args[0];
string name     = args[1];

var parts = endpoint.Split(':');
string host = parts[0];
int port = parts.Length == 2 && int.TryParse(parts[1], out int p) ? p : 5672;

if (!double.TryParse(args[2], out double speed) || speed <= 0)
{
    Console.WriteLine("Invalid speed. Must be a positive number e.g. 1 or 0.5");
    return;
}

// Board size — default 10x10, matches GUI default
// Can be overridden via optional 4th argument: PersonApp localhost Alice 1 20
int boardSize = args.Length >= 4 && int.TryParse(args[3], out int bs) ? bs : 10;

int delayMs = (int)(1000.0 / speed);

Console.WriteLine($"┌─ PERSON ─────────────────────────────────┐");
Console.WriteLine($"  Name:      {name}");
Console.WriteLine($"  Endpoint:  {host}:{port}");
Console.WriteLine($"  Speed:     {speed} move(s)/sec");
Console.WriteLine($"  Board:     {boardSize}x{boardSize}");
Console.WriteLine($"└──────────────────────────────────────────┘");

using var mq = new RabbitMQService(host, port);

var rng = new Random();
int x = rng.Next(boardSize);
int y = rng.Next(boardSize);

Console.WriteLine($"  Starting at ({x},{y})");
Console.WriteLine("  Moving... Press Ctrl+C to exit.");
Console.WriteLine(new string('-', 46));

// Publish initial position
mq.Publish(RabbitMQService.POSITION_TOPIC, new PersonPosition
{
    Name      = name,
    X         = x,
    Y         = y,
    Timestamp = DateTime.UtcNow
});

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(delayMs, cts.Token);

        int dx = rng.Next(-1, 2);
        int dy = rng.Next(-1, 2);

        x = Math.Clamp(x + dx, 0, boardSize - 1);
        y = Math.Clamp(y + dy, 0, boardSize - 1);

        var position = new PersonPosition
        {
            Name      = name,
            X         = x,
            Y         = y,
            Timestamp = DateTime.UtcNow
        };

        mq.Publish(RabbitMQService.POSITION_TOPIC, position);

        Console.WriteLine($"  [{position.Timestamp:HH:mm:ss}] {name} → ({x},{y})");
    }
}
catch (TaskCanceledException) { }

Console.WriteLine($"{name} stopped.");