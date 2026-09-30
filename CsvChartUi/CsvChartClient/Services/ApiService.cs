using System.Diagnostics;
using CsvChartClient.Models;

namespace CsvChartClient.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService(string baseUrl)
    {
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.CacheControl =
            new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
    }

    public async Task<List<FileInfoModel>> GetFilesAsync()
    {
        try
        {
            var url = "api/files";
            Debug.WriteLine($" GET {_http.BaseAddress}{url}");
            var response = await _http.GetAsync(url);
            Debug.WriteLine($" STATUS: {(int)response.StatusCode}");

            if (!response.IsSuccessStatusCode) return new List<FileInfoModel>();

            var json = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<List<FileInfoModel>>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new List<FileInfoModel>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($" GetFiles GRESKA: {ex}");
            return new List<FileInfoModel>();
        }
    }

    public async Task<string?> GetFileContentAsync(string fileName)
    {
        try
        {
            var url = $"api/files/{Uri.EscapeDataString(fileName)}";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var obj = System.Text.Json.JsonSerializer.Deserialize<FileResponse>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return obj?.Content;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetFileContent GRESKA: {ex}");
            return null;
        }
    }

    private class FileResponse
    {
        public string FileName { get; set; } = "";
        public string Content { get; set; } = "";
    }
}