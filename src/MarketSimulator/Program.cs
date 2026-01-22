using MarketSimulator;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient<ExchangeSimulator>(client =>
{
    var ingestionUrl = builder.Configuration["IngestionApiUrl"] 
        ?? "http://localhost:5001";
    client.BaseAddress = new Uri(ingestionUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHostedService<ExchangeSimulator>();

var host = builder.Build();
await host.RunAsync();
