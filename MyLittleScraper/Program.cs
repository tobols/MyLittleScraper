using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyLittleScraper;



// DI to get some structure
var builder = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient("scrapeClient");
        services.AddSingleton<Scraper>();
        services.AddSingleton<Saver>();
    });

var host = builder.Build();

var scraper = host.Services.GetRequiredService<Scraper>();
await scraper.Scrape();









/* TODO List:
 * 
 * Fix httpclient etc..
 * Recursive search through site
 * Only read pages once, save visited urls
 * Do it in parallell
 * Save everything
 * 
 */



