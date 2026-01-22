using Broker.Service;
using StackExchange.Redis;
using System.Threading.Channels;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Redis
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));

// Channel for broadcasting
builder.Services.AddSingleton(Channel.CreateUnbounded<MarketDataEvent>(
    new UnboundedChannelOptions { SingleReader = false, SingleWriter = true }));

// Background consumer
builder.Services.AddHostedService<RedisStreamConsumer>();

// CORS for browser access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5000", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapControllers();

app.Run();
