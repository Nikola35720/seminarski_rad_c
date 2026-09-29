using CsvChartServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using static System.Net.WebRequestMethods;

namespace CsvChartServer.Service
{

    public class FileWatcherService : BackgroundService
    {

        private readonly IHubContext<FileWatcherHub> _hub;
        private readonly ILogger<FileWatcherService> _logger;
        private readonly string _folder;

        public FileWatcherService(IHubContext<FileWatcherHub> hub,ILogger<FileWatcherService> logger,IConfiguration config)
        {
            _hub = hub;
            _logger = logger;
            _folder = config["CsvFolder"] ?? Path.Combine(Directory.GetCurrentDirectory(), "CsvFiles");
            if (!Directory.Exists(_folder))
                Directory.CreateDirectory(_folder);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var watcher = new FileSystemWatcher(_folder)
            {
                Filter = "*.csv",
                NotifyFilter = NotifyFilters.LastWrite
                         | NotifyFilters.FileName
                         | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            watcher.Created += async (s, e) =>
            {
                await Task.Delay(500); 
                _logger.LogInformation("Dodat: {File}", e.Name);
                await _hub.Clients.All.SendAsync("FileAdded", e.Name);
            };

            watcher.Changed += async (s, e) =>
            {
                await Task.Delay(500);
                _logger.LogInformation("Izmenjen: {File}", e.Name);
                await _hub.Clients.All.SendAsync("FileUpdated", e.Name);
            };

            watcher.Deleted += async (s, e) =>
            {
                _logger.LogInformation("Obrisan: {File}", e.Name);
                await _hub.Clients.All.SendAsync("FileDeleted", e.Name);
            };

            watcher.Renamed += async (s, e) =>
            {
                _logger.LogInformation("Preimenovan: {Old} → {New}", e.OldName, e.Name);
                await _hub.Clients.All.SendAsync("FileDeleted", e.OldName);
                await _hub.Clients.All.SendAsync("FileAdded", e.Name);
            };

            _logger.LogInformation("FileWatcher pokrenut na: {Folder}", _folder);

            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000, stoppingToken);

            watcher.Dispose();
        }
    }
}
