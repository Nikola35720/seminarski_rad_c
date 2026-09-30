using Microsoft.AspNetCore.SignalR;

namespace CsvChartServer.Hubs;

public class FileWatcherHub : Hub
{
    public async Task JoinFileGroup(string fileName)
        => await Groups.AddToGroupAsync(Context.ConnectionId, fileName);

    public async Task LeaveFileGroup(string fileName)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, fileName);
}