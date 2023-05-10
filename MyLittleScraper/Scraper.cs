using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace MyLittleScraper;

public class Scraper
{
    private ConcurrentDictionary<string, bool> _scrapedPaths = new ConcurrentDictionary<string, bool>();
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
        _saver.DeleteAll();
        await ScrapeUrl(_baseUrl);

        Console.WriteLine($"{_scrapeCount} unique paths successfully scraped!");
    }


    public async Task ScrapeUrl(string url)
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
                var tasks = links.Select(ScrapeUrl);
                await Task.WhenAll(tasks);
                await _saver.Save(result, uri);
            }

            Interlocked.Increment(ref _scrapeCount);
            Console.WriteLine($"Finished scraping {_scrapeCount} of {_scrapedPaths.Count()} found paths");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Occurred When Scraping: {url}");
        }
    }
}
