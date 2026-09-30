using CsvChartClient.Models;
using CsvChartClient.Services;
using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace CsvChartClient.ViewModels;

public class MainViewModel : BindableObject
{
    private const string BaseUrl = "http://localhost:5000";

    private readonly ApiService _api;
    private readonly SignalRService _signalR;

    public ObservableCollection<FileInfoModel> Files { get; } = new();
    public ObservableCollection<DataPoint> ChartData { get; } = new();
    public ObservableCollection<string> SeriesNames { get; } = new();
    public ObservableCollection<string> AllColumns { get; } = new();
    public ObservableCollection<SeriesToggle> SeriesToggles { get; } = new();
    public ObservableCollection<int> VisibleSeriesIndices { get; } = new();

    public event Action? OnChartChanged;
    public event Action<string>? OnNotification;
    public event Action<string>? OnFileRemoved;

    private FileInfoModel? _selectedFile;
    public FileInfoModel? SelectedFile
    {
        get => _selectedFile;
        set
        {
            Debug.WriteLine($" SelectedFile setter: {value?.Name}");
            if (_selectedFile == value) return;
            _selectedFile = value;
            OnPropertyChanged();
            if (value != null) _ = LoadFileAsync(value.Name);
        }
    }

    private string _status = "Spremno";
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    private int _chartType = 0;
    public int ChartType
    {
        get => _chartType;
        set
        {
            _chartType = value;
            OnPropertyChanged();
            OnChartChanged?.Invoke();
        }
    }

    private int _displayCount = 100;
    public int DisplayCount
    {
        get => _displayCount;
        set
        {
            if (value < 10) value = 10;
            if (value > 5000) value = 5000;
            if (_displayCount == value) return;
            _displayCount = value;
            OnPropertyChanged();
            Debug.WriteLine($" DisplayCount = {value}");
            if (_selectedFile != null)
                _ = LoadFileAsync(_selectedFile.Name);
        }
    }

    private int _totalRows = 0;
    public int TotalRows
    {
        get => _totalRows;
        set { _totalRows = value; OnPropertyChanged(); }
    }

    private string _xAxisColumn = "";
    public string XAxisColumn
    {
        get => _xAxisColumn;
        set
        {
            if (_xAxisColumn == value) return;
            _xAxisColumn = value;
            OnPropertyChanged();
            Debug.WriteLine($"[VM] XAxisColumn = {value}");
            if (_selectedFile != null)
                _ = LoadFileAsync(_selectedFile.Name);
        }
    }

    private bool _sortByXAxis = false;
    public bool SortByXAxis
    {
        get => _sortByXAxis;
        set
        {
            _sortByXAxis = value;
            OnPropertyChanged();
            OnChartChanged?.Invoke();
        }
    }

    public MainViewModel()
    {
        _api = new ApiService(BaseUrl);
        _signalR = new SignalRService();
    }

    public async Task InitializeAsync()
    {
        _signalR.OnFileAdded += info => HandleFileAdded(info);
        _signalR.OnFileUpdated += info => HandleFileUpdated(info);
        _signalR.OnFileDeleted += name => HandleFileDeleted(name);

        try
        {
            await _signalR.ConnectAsync(BaseUrl);
            Status = "Povezan sa serverom.";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{ex}");
            Status = "SignalR nije dostupan: " + ex.Message;
        }

        try
        {
            await RefreshFilesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API] {ex}");
            Status = "Server nije dostupan: " + ex.Message;
        }
    }

    public async Task RefreshFilesAsync()
    {
        var files = await _api.GetFilesAsync();
        Files.Clear();
        foreach (var f in files) Files.Add(f);
    }

    private void HandleFileAdded(FileInfoModel info)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                Debug.WriteLine($" FileAdded: {info.Name}");

                var existing = Files.FirstOrDefault(f => f.Name == info.Name);
                if (existing != null) Files.Remove(existing);

                Files.Add(info);

                var sorted = Files.OrderBy(f => f.Name).ToList();
                Files.Clear();
                foreach (var f in sorted) Files.Add(f);

                OnNotification?.Invoke($"Novi fajl: {info.Name} ({info.Size} B)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HANDLE ADDED] {ex}");
            }
        });
    }

    private void HandleFileUpdated(FileInfoModel info)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                Debug.WriteLine($" FileUpdated: {info.Name} ({info.Size} B)");

                var existing = Files.FirstOrDefault(f => f.Name == info.Name);
                if (existing != null)
                {
                    existing.Size = info.Size;
                    existing.LastModified = info.LastModified;

                    int idx = Files.IndexOf(existing);
                    Files.RemoveAt(idx);
                    Files.Insert(idx, existing);
                }
                else
                {
                    Files.Add(info);
                }

                if (_selectedFile?.Name == info.Name)
                {
                    Debug.WriteLine($" Auto-reload grafikona za {info.Name}");
                    await LoadFileAsync(info.Name);
                    OnNotification?.Invoke($"Fajl '{info.Name}' je osvezen.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HANDLE UPDATED] {ex}");
            }
        });
    }

    private void HandleFileDeleted(string fileName)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                Debug.WriteLine($"[SIGNALR] FileDeleted: {fileName}");

                var existing = Files.FirstOrDefault(f => f.Name == fileName);
                if (existing != null) Files.Remove(existing);

                if (_selectedFile?.Name == fileName)
                {
                    _selectedFile = null;
                    ChartData.Clear();
                    SeriesNames.Clear();
                    AllColumns.Clear();
                    SeriesToggles.Clear();
                    VisibleSeriesIndices.Clear();
                    OnChartChanged?.Invoke();
                    Status = "Izabrani fajl je obrisan.";
                }

                OnNotification?.Invoke($"Fajl '{fileName}' je obrisan sa servera.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HANDLE DELETED] {ex}");
            }
        });
    }

    public async Task ReloadSelectedAsync()
    {
        if (_selectedFile != null)
            await LoadFileAsync(_selectedFile.Name);
    }

    private async Task LoadFileAsync(string fileName)
    {
        Debug.WriteLine($"=== LOADFILE: {fileName} ===");
        Status = $"Ucitavam {fileName}...";

        string? content = null;
        try
        {
            content = await _api.GetFileContentAsync(fileName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API greska: {ex}");
            Status = "Greska pri komunikaciji sa serverom.";
            return;
        }

        if (string.IsNullOrEmpty(content))
        {
            Status = "Fajl je prazan ili nije moguce ucitati.";
            return;
        }

        try
        {
            ParseCsv(content);
            Debug.WriteLine($"PARSIRANO REDOVA: {ChartData.Count}, SERIJA: {SeriesNames.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PARSE] {ex}");
            Status = "Greska pri parsiranju: " + ex.Message;
            return;
        }

        Status = $"Ucitano: {fileName} ({ChartData.Count} od {TotalRows} redova, {SeriesNames.Count} serija)";
        OnChartChanged?.Invoke();
    }

    private void ParseCsv(string content)
    {
        ChartData.Clear();
        SeriesNames.Clear();
        AllColumns.Clear();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            DetectDelimiter = true
        };

        using var reader = new StringReader(content);
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord;
        if (headers == null || headers.Length < 2)
        {
            Debug.WriteLine("Manje od 2 kolone.");
            return;
        }

        foreach (var h in headers)
            AllColumns.Add(h);

        if (string.IsNullOrEmpty(XAxisColumn))
            XAxisColumn = headers[0];

        int xIndex = Array.IndexOf(headers, XAxisColumn);
        if (xIndex < 0) xIndex = 0;

        var tempRows = new List<(string label, List<double?> values)>();
        int rowCounter = 0;

        while (csv.Read() && rowCounter < DisplayCount)
        {
            rowCounter++;
            var label = csv.GetField(xIndex) ?? "";

            var vals = new List<double?>();
            for (int c = 0; c < headers.Length; c++)
            {
                if (double.TryParse(csv.GetField(c), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out var v))
                    vals.Add(v);
                else
                    vals.Add(null);
            }
            tempRows.Add((label, vals));
        }

        TotalRows = 0;
        using (var counterReader = new StringReader(content))
        {
            string? line;
            bool first = true;
            while ((line = counterReader.ReadLine()) != null)
            {
                if (first) { first = false; continue; }
                if (!string.IsNullOrWhiteSpace(line)) TotalRows++;
            }
        }

        var allSeries = new List<string>();
        for (int c = 0; c < headers.Length; c++)
        {
            if (c == xIndex) continue;
            allSeries.Add(headers[c]);
        }

        var numericIndices = new List<int>();
        var numericSeriesNames = new List<string>();

        for (int i = 0; i < allSeries.Count; i++)
        {
            if (tempRows.Any(r => r.values[i].HasValue))
            {
                numericIndices.Add(i);
                numericSeriesNames.Add(allSeries[i]);
            }
        }

        foreach (var (label, vals) in tempRows)
        {
            var dp = new DataPoint { Label = label };
            foreach (var idx in numericIndices)
                dp.Values.Add(vals[idx] ?? 0);
            ChartData.Add(dp);
        }

        foreach (var n in numericSeriesNames)
            SeriesNames.Add(n);

        RebuildSeriesToggles();

        Debug.WriteLine($"Ucitano {ChartData.Count} od {TotalRows} redova, {SeriesNames.Count} serija");
    }

    public void RebuildSeriesToggles()
    {
        SeriesToggles.Clear();
        for (int i = 0; i < SeriesNames.Count; i++)
        {
            var toggle = new SeriesToggle
            {
                Name = SeriesNames[i],
                Index = i,
                IsVisible = true
            };
            toggle.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SeriesToggle.IsVisible))
                    RebuildVisibleSeries();
            };
            SeriesToggles.Add(toggle);
        }
        RebuildVisibleSeries();
    }

    private void RebuildVisibleSeries()
    {
        VisibleSeriesIndices.Clear();
        for (int i = 0; i < SeriesToggles.Count; i++)
        {
            if (SeriesToggles[i].IsVisible)
                VisibleSeriesIndices.Add(SeriesToggles[i].Index);
        }
        OnChartChanged?.Invoke();
    }

    public IList<DataPoint> GetDisplayData()
    {
        if (!SortByXAxis)
            return ChartData;

        bool numericLabels = ChartData.All(d =>
            double.TryParse(d.Label, NumberStyles.Any,
                CultureInfo.InvariantCulture, out _));

        if (numericLabels)
        {
            return ChartData.OrderBy(d =>
                double.Parse(d.Label, CultureInfo.InvariantCulture)).ToList();
        }
        else
        {
            return ChartData.OrderBy(d => d.Label).ToList();
        }
    }
}

public class SeriesToggle : BindableObject
{
    public string Name { get; set; } = "";
    public int Index { get; set; }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            OnPropertyChanged();
        }
    }
}