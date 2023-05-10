
using Microsoft.Extensions.Configuration;

namespace MyLittleScraper;

public class Saver
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Parser _parser;
    private readonly string _baseFolder;


    public Saver(IHttpClientFactory httpClientFactory, Parser parser, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _parser = parser;
        _baseFolder = config["BaseSaveDir"]!;
    }


    public async Task Save(string htmlString, Uri uri)
    {
        var images = _parser.FindImages(htmlString, uri);
        var jsFiles = _parser.FindFiles(htmlString, uri, Parser.FileType.JavaScript);
        var cssFiles = _parser.FindFiles(htmlString, uri, Parser.FileType.CSS);

        var tasks = images.Select(SaveImage).ToList();
        tasks.AddRange(jsFiles.Select(DownloadAndSaveFile));
        tasks.AddRange(cssFiles.Select(DownloadAndSaveFile));

        await Task.WhenAll(tasks);

        if (uri.LocalPath.EndsWith("/"))
            await SaveFile(htmlString, uri, "index.html");
        else
            await SaveFile(htmlString, uri);
    }


    private async Task SaveImage(string url)
    {
        var uri = new Uri(url);
        var filePath = _baseFolder + uri.LocalPath;
        if (File.Exists(filePath))
            return;
        
        CreateDir(uri.LocalPath);

        var client = _httpClientFactory.CreateClient("scrapeClient");
        var imageBytes = await client.GetByteArrayAsync(url);

        if (File.Exists(filePath))
            return;

        try
        {
            // TODO: Have to do something about these IO conflicts
            File.WriteAllBytes(filePath, imageBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error Writing Image: {ex.Message}");
            Console.WriteLine($"Exception Type: {ex.GetType().Name}");
            Console.WriteLine($"Occurred When Saving Image: {url}");
        }
    }


    private async Task DownloadAndSaveFile(string url)
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
        if (File.Exists(filePath))
            return;

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
