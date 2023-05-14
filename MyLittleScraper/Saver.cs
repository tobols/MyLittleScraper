
using Microsoft.Extensions.Configuration;

namespace MyLittleScraper;

public class Saver
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseFolder;


    public Saver(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _baseFolder = config["BaseSaveDir"]!;
    }


    public async Task Save(string htmlString, Uri uri)
    {
        if (uri.LocalPath.EndsWith("/"))
            await SaveFile(htmlString, uri, "index.html");
        else
            await SaveFile(htmlString, uri);
    }


    public async Task SaveImage(string url)
    {
        var uri = new Uri(url);
        var filePath = _baseFolder + uri.LocalPath;
        
        CreateDir(uri.LocalPath);

        var client = _httpClientFactory.CreateClient("scrapeClient");
        var imageBytes = await client.GetByteArrayAsync(url);

        try
        {
            await File.WriteAllBytesAsync(filePath, imageBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error Writing Image: {ex.Message}");
            Console.WriteLine($"Exception Type: {ex.GetType().Name}");
            Console.WriteLine($"Occurred When Saving Image: {url}");
        }
    }


    public async Task DownloadAndSaveFile(string url)
    {
        var client = _httpClientFactory.CreateClient("scrapeClient");
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            await SaveFile(text, new Uri(url));
        }
    }


    private async Task SaveFile(string content, Uri uri, string fileName = "")
    {
        var filePath = _baseFolder + uri.LocalPath + fileName;

        CreateDir(uri.LocalPath);
        try
        {
            await File.WriteAllTextAsync(filePath, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error Writing File: {ex.Message}");
            Console.WriteLine($"Exception Type: {ex.GetType().Name}");
            Console.WriteLine($"Occurred When Saving File: {filePath}");
        }
    }


    private void CreateDir(string urlPath)
    {
        var segments = urlPath.Split('/');
        var dir = _baseFolder + string.Join('\\', segments[..(segments.Length - 1)]);
        Directory.CreateDirectory(dir);
    }


    public void DeleteAll()
    {
        if (!Directory.Exists(_baseFolder))
            return;

        DirectoryInfo dir = new DirectoryInfo(_baseFolder);

        foreach (FileInfo file in dir.EnumerateFiles())
            file.Delete();
        foreach (DirectoryInfo d in dir.EnumerateDirectories())
            d.Delete(true);
    }
}
