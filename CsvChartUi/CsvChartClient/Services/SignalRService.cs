using CsvChartClient.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;

namespace CsvChartClient.Services;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _connection;

    public event Action<FileInfoModel>? OnFileAdded;
    public event Action<FileInfoModel>? OnFileUpdated;
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
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
            })
            .Build();

        _connection.On<FileInfoModel>("FileAdded", info => OnFileAdded?.Invoke(info));
        _connection.On<FileInfoModel>("FileUpdated", info => OnFileUpdated?.Invoke(info));
        _connection.On<string>("FileDeleted", name => OnFileDeleted?.Invoke(name));

        _connection.Reconnecting += ex =>
        {
            Debug.WriteLine($" Reconnecting: {ex?.Message}");
            return Task.CompletedTask;
        };

        _connection.Reconnected += id =>
        {
            Debug.WriteLine($" Reconnected: {id}");
            return Task.CompletedTask;
        };

        _connection.Closed += ex =>
        {
            Debug.WriteLine($" Closed: {ex?.Message}");
            return Task.CompletedTask;
        };

        await _connection.StartAsync();
        Debug.WriteLine("Konekcija uspostavljena.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
            await _connection.DisposeAsync();
    }
}