using Microsoft.AspNetCore.Mvc;

namespace CsvChartServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly string _folder;

    public FilesController(IConfiguration config)
    {
        _folder = config["CsvFolder"]
                  ?? Path.Combine(Directory.GetCurrentDirectory(), "CsvFiles");
    }

    [HttpGet]
    public IActionResult GetFiles()
    {
        var files = Directory.GetFiles(_folder, "*.csv")
            .Select(f =>
            {
                var fi = new FileInfo(f);
                return new
                {
                    Name = fi.Name,
                    Size = fi.Length,
                    LastModified = fi.LastWriteTime
                };
            })
            .OrderBy(f => f.Name)
            .ToList();

        return Ok(files);
    }

    [HttpGet("{fileName}")]
    public async Task<IActionResult> GetFile(string fileName)
    {
        var path = Path.Combine(_folder, fileName);

        Console.WriteLine($"[SERVER] Trazim: {path}");
        Console.WriteLine($"[SERVER] Postoji? {System.IO.File.Exists(path)}");

        if (!System.IO.File.Exists(path))
            return NotFound();

        for (int i = 0; i < 10; i++)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open,
                                              FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var content = await sr.ReadToEndAsync();

                Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

                return Ok(new { FileName = fileName, Content = content });
            }
            catch (IOException) when (i < 9)
            {
                await Task.Delay(200);
            }
        }
        return StatusCode(500, "Fajl je zakljucan.");
    }
}