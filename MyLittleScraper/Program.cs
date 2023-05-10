using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyLittleScraper;
using System.Net;


// TODO: This doesn't work?
ServicePointManager.DefaultConnectionLimit = 30;


// DI to get some structure
var builder = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient("scrapeClient");
        services.AddSingleton<Scraper>();
        services.AddSingleton<Saver>();
        services.AddSingleton<Parser>();

        var config = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false)
                            .Build();
        services.AddSingleton<IConfiguration>(config);
    });

var host = builder.Build();

var scraper = host.Services.GetRequiredService<Scraper>();
await scraper.Scrape();


/*
 * Conclusions: Finding unique paths works fine with recursion, but it would probably be better
 * to first save unique urls to all resources, and then loop through and download them.
 * That way it'll be easier to utilize parallel GET requests, AND I won't have to worry about
 * IO conflicts because different paths reference the same images etc..
 * 
 * Next time I'll do something like this: https://stackoverflow.com/questions/22492383/throttling-asynchronous-tasks#answer-22493662
 */