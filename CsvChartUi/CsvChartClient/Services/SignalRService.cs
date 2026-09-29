using Microsoft.AspNetCore.SignalR.Client;

namespace CsvChartClient.Services;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _connection;
    public event Action<string>? OnFileAdded;
    public event Action<string>? OnFileUpdated;
    public event Action<string>? OnFileDeleted;
    public async Task ConnectAsync(string baseUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/filehub")
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
            })
            .Build();

        _connection.On<string>("FileAdded", f => OnFileAdded?.Invoke(f));
        _connection.On<string>("FileUpdated", f => OnFileUpdated?.Invoke(f));
        _connection.On<string>("FileDeleted", f => OnFileDeleted?.Invoke(f));

        await _connection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null) await _connection.DisposeAsync();
    }
}