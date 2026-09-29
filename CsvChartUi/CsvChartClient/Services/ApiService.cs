using System.Diagnostics;
using System.Net.Http.Json;
using CsvChartClient.Models;

namespace CsvChartClient.Services;

public class ApiService
{
    private readonly HttpClient _http;
    public ApiService(string baseUrl)
    {
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<List<FileInfoModel>> GetFilesAsync()
    {
        try
        {
            var url = "api/files";
            Debug.WriteLine($"[API] GET {_http.BaseAddress}{url}");
            var response = await _http.GetAsync(url);
            Debug.WriteLine($"[API] STATUS: {(int)response.StatusCode}");

            if (!response.IsSuccessStatusCode) return new List<FileInfoModel>();

            var json = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[API] JSON: {json.Substring(0, Math.Min(200, json.Length))}");

            return System.Text.Json.JsonSerializer.Deserialize<List<FileInfoModel>>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new List<FileInfoModel>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API] GetFiles GREsKA: {ex}");
            return new List<FileInfoModel>();
        }
    }

    public async Task<string?> GetFileContentAsync(string fileName)
    {
        try
        {
            var url = $"api/files/{Uri.EscapeDataString(fileName)}";
            Debug.WriteLine($"[API] GET {_http.BaseAddress}{url}");

            var response = await _http.GetAsync(url);
            Debug.WriteLine($"[API] STATUS: {(int)response.StatusCode} {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[API] Neuspesan status za {fileName}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[API] JSON (prvih 300): {json.Substring(0, Math.Min(300, json.Length))}");

            var obj = System.Text.Json.JsonSerializer.Deserialize<FileResponse>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return obj?.Content;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API] GetFileContent GREsKA: {ex}");
            return null;
        }
    }

    private class FileResponse
    {
        public string FileName { get; set; } = "";
        public string Content { get; set; } = "";
    }
}