using ChatGUI.Hubs;
using ChatGUI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHostedService<ChatListenerService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapHub<ChatHub>("/chathub");

app.Run();
