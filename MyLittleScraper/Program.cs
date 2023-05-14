using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyLittleScraper;
using System.Net;


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
