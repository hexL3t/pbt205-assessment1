using ContactTracerGui.Hubs;
using ContactTracerGui.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddSingleton<PositionListenerService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<PositionListenerService>());

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<TrackerHub>("/trackerhub");

app.Run();
