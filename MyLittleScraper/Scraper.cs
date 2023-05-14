using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;

namespace MyLittleScraper;

public class Scraper
{
    private ConcurrentDictionary<string, bool> _scrapedPaths = new ConcurrentDictionary<string, bool>();
    private ConcurrentDictionary<string, Parser.FileType> _foundResources = new ConcurrentDictionary<string, Parser.FileType>();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Saver _saver;
    private readonly Parser _parser;
    private int _scrapeCount = 0;
    private readonly string _baseUrl;


    public Scraper(IHttpClientFactory httpClientFactory, Saver saver, Parser parser, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _saver = saver;
        _parser = parser;
        _baseUrl = config["ScrapeUrl"]!;
    }


    public async Task Scrape()
    {
        Console.WriteLine("Deleting any previous scrapes");

        _saver.DeleteAll();

        Console.WriteLine($"Beginning scaping {_baseUrl}");

        await ScrapeUrl(_baseUrl);

        Console.WriteLine($"{_scrapeCount} unique paths successfully scraped!");

        Console.WriteLine($"Downloading {_foundResources.Count()} resources...");

        await DownloadResources();

        Console.WriteLine("Resources downloaded");

        Console.WriteLine("Mission Complete!");
    }


    private async Task ScrapeUrl(string url)
    {
        try
        {
            var uri = new Uri(url);

            if (!_scrapedPaths.TryAdd(uri.LocalPath, true))
                return; //path already handled

            var client = _httpClientFactory.CreateClient("scrapeClient");
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                var links = _parser.FindLinks(result, uri);
                var images = _parser.FindImages(result, uri);
                var js = _parser.FindFiles(result, uri, Parser.FileType.JavaScript);
                var css = _parser.FindFiles(result, uri, Parser.FileType.CSS);


                // Add all found resources, skip if already added
                foreach (var img in images)
                    if (!_foundResources.TryAdd(img, Parser.FileType.Image)) continue;

                foreach (var j in js)
                    if (!_foundResources.TryAdd(j, Parser.FileType.JavaScript)) continue;

                foreach (var c in css)
                    if (!_foundResources.TryAdd(c, Parser.FileType.CSS)) continue;


                var tasks = links.Select(ScrapeUrl);
                await Task.WhenAll(tasks);
                await _saver.Save(result, uri);
            }

            Interlocked.Increment(ref _scrapeCount);
            
            
            //Console.WriteLine($"Finished scraping {_scrapeCount} of {_scrapedPaths.Count()} found paths");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Occurred When Scraping: {url}");
        }
    }


    private async Task DownloadResources()
    {
        try
        {
            var saveTasks = _foundResources.Select(kvp =>
            {
                if (kvp.Value == Parser.FileType.Image)
                    return _saver.SaveImage(kvp.Key);
                else
                    return _saver.DownloadAndSaveFile(kvp.Key);
            });
            await Task.WhenAll(saveTasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Occurred while Downloading Resources");
        }
    }
}
