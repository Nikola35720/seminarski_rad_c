using Microsoft.AspNetCore.SignalR;

namespace CsvChartServer.Hubs
{
    public class FileWatcherHub:Hub
    {
        public async Task JoinFileGroup(string filename)
            => await Groups.AddToGroupAsync(Context.ConnectionId, filename);

        public async Task LeaveFileGroup(string filename)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, filename);
    }
}
