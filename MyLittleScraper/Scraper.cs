using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MyLittleScraper;

public class Scraper
{
    private ConcurrentDictionary<string, bool> _scrapedPaths = new ConcurrentDictionary<string, bool>();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Saver _saver;


    private Regex linkRegex = new Regex("<a\\s+(?:[^>]*?\\s+)?href=([\"'])(.*?)\\1");


    public Scraper(IHttpClientFactory httpClientFactory, Saver saver)
    {
        _httpClientFactory = httpClientFactory;
        _saver = saver;
    }


    public async Task Scrape()
    {
        var url = "https://books.toscrape.com";

        await ScrapeUrl(url);
    }


    public async Task ScrapeUrl(string url)
    {
        try
        {
            var uri = new Uri(url);

            if (!_scrapedPaths.TryAdd(uri.LocalPath, true))
                return; //path already handled

            Console.WriteLine($"Starting {url}, Thread: {Thread.CurrentThread.ManagedThreadId}");


            var client = _httpClientFactory.CreateClient("scrapeClient");
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var matches = linkRegex.Matches(result);

                var paths = matches.Select(m => m.Groups[2].Value);
                var tasks = paths.Select(p => ScrapeUrl(ParseLink(p, uri))).ToList();
                await Task.WhenAll(tasks);

                await _saver.Save(result, uri.LocalPath);
            }


            Console.WriteLine($"Done! {url}, Thread: {Thread.CurrentThread.ManagedThreadId}");
        }
        catch (Exception ex)
        {
            // TODO: Handle it
        }
    }


    // TODO: This is awful, clean this mess up
    private string ParseLink(string link, Uri currentUri)
    {
        var origLinkSegments = link.Split('/');
        var segCount = link.Split('/').Count(s => s == "..");
        var linkSegments = origLinkSegments.Where(s => s != ".." && !string.IsNullOrWhiteSpace(s)).ToArray();

        var docSeg = currentUri.LocalPath.EndsWith(".html");
        var currSegCount = docSeg ? currentUri.Segments.Length - 1 : currentUri.Segments.Length;
        var steps = currSegCount - segCount;

        string newPath;

        if (segCount > 0)
            newPath = $"{currentUri.Scheme}://{currentUri.Host}" + string.Concat(currentUri.Segments[0..steps]) + string.Join('/', linkSegments);
        else
            newPath = $"{currentUri.Scheme}://{currentUri.Host}/" + string.Join('/', linkSegments);

        return newPath;
    }


    /*
     * Each task should:
     * Check if this path has been processed, if it has then exit, if not then register this path
     * Get and parse the page
     * Save everything
     * Create new tasks from all <a> tags
     */
}
