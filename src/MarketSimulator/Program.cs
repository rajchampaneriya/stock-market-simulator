using MarketSimulator;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

var ingestionUrl = builder.Configuration["IngestionApiUrl"] 
    ?? "http://localhost:5001";

builder.Services.AddHttpClient("IngestionApi", client =>
{
    client.BaseAddress = new Uri(ingestionUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHostedService<ExchangeSimulator>();

// Configure OpenTelemetry tracing
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("MarketSimulator",
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddConsoleExporter()
        .AddOtlpExporter());

var host = builder.Build();
await host.RunAsync();
