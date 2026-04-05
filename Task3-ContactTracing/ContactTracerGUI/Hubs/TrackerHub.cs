using Microsoft.AspNetCore.SignalR;

namespace ContactTracerGui.Hubs
{
    public class TrackerHub : Hub
    {
        // Clients call this to submit a query
        public async Task SendQuery(string personName)
        {
            await Clients.All.SendAsync("QueryRequested", personName);
        }
    }
}
