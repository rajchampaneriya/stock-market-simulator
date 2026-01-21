# Technical Prototype: Stock Market Data Streaming System


## Implementation Details

### 1. Stock Exchange Simulator

```csharp
// ExchangeSimulator.cs
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MarketDataSimulator.Exchange
{
    public class ExchangeSimulator : BackgroundService
    {
        private readonly ILogger<ExchangeSimulator> _logger;
        private readonly IMarketDataPublisher _publisher;
        private readonly ConcurrentDictionary<string, Security> _securities;
        private readonly Random _random;
        private readonly Timer _timer;

        public ExchangeSimulator(
            ILogger<ExchangeSimulator> logger,
            IMarketDataPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
            _securities = new ConcurrentDictionary<string, Security>();
            _random = new Random();
            
            // Initialize securities
            InitializeSecurities();
            
            // Set up timer to generate market data
            _timer = new Timer(GenerateMarketData, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        }

        private void InitializeSecurities()
        {
            var symbols = new[] { "AAPL", "MSFT", "GOOGL", "AMZN", "TSLA", "META", "NVDA", "JPM" };
            
            foreach (var symbol in symbols)
            {
                var basePrice = 100 + _random.NextDouble() * 900;
                _securities.TryAdd(symbol, new Security
                {
                    Symbol = symbol,
                    LastPrice = Math.Round(basePrice, 2),
                    BidPrice = Math.Round(basePrice - 0.01, 2),
                    AskPrice = Math.Round(basePrice + 0.01, 2),
                    BidSize = _random.Next(100, 10000),
                    AskSize = _random.Next(100, 10000),
                    Volume = _random.Next(100000, 10000000),
                    LastUpdate = DateTime.UtcNow
                });
            }
        }

        private void GenerateMarketData(object state)
        {
            foreach (var security in _securities.Values)
            {
                // Simulate price movement
                var priceChange = (_random.NextDouble() - 0.5) * 2; // -1 to 1
                var newPrice = Math.Max(0.01, security.LastPrice + priceChange);
                
                // Simulate bid/ask spread
                var spread = 0.01 + _random.NextDouble() * 0.1;
                var newBid = Math.Round(newPrice - spread / 2, 2);
                var newAsk = Math.Round(newPrice + spread / 2, 2);
                
                // Simulate volume changes
                var volumeChange = _random.Next(100, 1000);
                
                // Update security
                security.LastPrice = Math.Round(newPrice, 2);
                security.BidPrice = newBid;
                security.AskPrice = newAsk;
                security.BidSize = _random.Next(100, 10000);
                security.AskSize = _random.Next(100, 10000);
                security.Volume += volumeChange;
                security.LastUpdate = DateTime.UtcNow;
                
                // Create market data event
                var marketDataEvent = new MarketDataEvent
                {
                    Symbol = security.Symbol,
                    EventType = MarketDataEventType.Quote,
                    Timestamp = DateTime.UtcNow,
                    Data = new Dictionary<string, object>
                    {
                        ["LastPrice"] = security.LastPrice,
                        ["BidPrice"] = security.BidPrice,
                        ["AskPrice"] = security.AskPrice,
                        ["BidSize"] = security.BidSize,
                        ["AskSize"] = security.AskSize,
                        ["Volume"] = security.Volume
                    }
                };
                
                // Publish market data
                _publisher.PublishAsync(marketDataEvent);
                
                // Occasionally generate a trade event
                if (_random.NextDouble() < 0.1) // 10% chance
                {
                    var tradeEvent = new MarketDataEvent
                    {
                        Symbol = security.Symbol,
                        EventType = MarketDataEventType.Trade,
                        Timestamp = DateTime.UtcNow,
                        Data = new Dictionary<string, object>
                        {
                            ["Price"] = security.LastPrice,
                            ["Size"] = _random.Next(100, 5000),
                            ["Volume"] = security.Volume
                        }
                    };
                    
                    _publisher.PublishAsync(tradeEvent);
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Exchange Simulator is running.");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }

    public class Security
    {
        public string Symbol { get; set; }
        public decimal LastPrice { get; set; }
        public decimal BidPrice { get; set; }
        public decimal AskPrice { get; set; }
        public int BidSize { get; set; }
        public int AskSize { get; set; }
        public long Volume { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class MarketDataEvent
    {
        public string Symbol { get; set; }
        public MarketDataEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    public enum MarketDataEventType
    {
        Quote,
        Trade,
        OrderBookUpdate
    }

    public interface IMarketDataPublisher
    {
        Task PublishAsync(MarketDataEvent marketDataEvent);
    }
}
```

### 2. Market Data Gateway

```csharp
// MarketDataGateway.cs
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MarketDataSimulator.Gateway
{
    public class MarketDataGateway : BackgroundService, IMarketDataPublisher
    {
        private readonly ILogger<MarketDataGateway> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, List<IMarketDataSubscriber>> _subscribers;
        private readonly Channel<MarketDataEvent> _marketDataChannel;

        public MarketDataGateway(
            ILogger<MarketDataGateway> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _subscribers = new ConcurrentDictionary<string, List<IMarketDataSubscriber>>();
            
            // Create a channel for market data events
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };
            _marketDataChannel = Channel.CreateBounded<MarketDataEvent>(options);
        }

        public async Task PublishAsync(MarketDataEvent marketDataEvent)
        {
            await _marketDataChannel.Writer.WriteAsync(marketDataEvent);
        }

        public void Subscribe(string symbol, IMarketDataSubscriber subscriber)
        {
            var subscribers = _subscribers.GetOrAdd(symbol, _ => new List<IMarketDataSubscriber>());
            lock (subscribers)
            {
                if (!subscribers.Contains(subscriber))
                {
                    subscribers.Add(subscriber);
                }
            }
        }

        public void Unsubscribe(string symbol, IMarketDataSubscriber subscriber)
        {
            if (_subscribers.TryGetValue(symbol, out var subscribers))
            {
                lock (subscribers)
                {
                    subscribers.Remove(subscriber);
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Market Data Gateway is running.");
            
            // Start a task to process market data events
            var processingTask = ProcessMarketDataEventsAsync(stoppingToken);
            
            // Wait for cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
            
            // Wait for processing to complete
            await processingTask;
        }

        private async Task ProcessMarketDataEventsAsync(CancellationToken stoppingToken)
        {
            await foreach (var marketDataEvent in _marketDataChannel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    // Get subscribers for this symbol
                    if (_subscribers.TryGetValue(marketDataEvent.Symbol, out var subscribers))
                    {
                        List<IMarketDataSubscriber> subscribersCopy;
                        lock (subscribers)
                        {
                            subscribersCopy = new List<IMarketDataSubscriber>(subscribers);
                        }
                        
                        // Send data to all subscribers
                        var tasks = subscribersCopy.Select(subscriber => 
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await subscriber.OnMarketDataAsync(marketDataEvent);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error sending market data to subscriber");
                                }
                            }, stoppingToken));
                        
                        await Task.WhenAll(tasks);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing market data event");
                }
            }
        }
    }

    public interface IMarketDataSubscriber
    {
        Task OnMarketDataAsync(MarketDataEvent marketDataEvent);
    }
}
```

### 3. Retail Broker Implementation

```csharp
// RetailBroker.cs
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MarketDataSimulator.Brokers
{
    public class RetailBroker : BackgroundService, IMarketDataSubscriber
    {
        private readonly ILogger<RetailBroker> _logger;
        private readonly MarketDataGateway _gateway;
        private readonly ConcurrentDictionary<string, List<IRetailClient>> _clients;
        private readonly ConcurrentDictionary<string, MarketDataEvent> _latestData;
        private readonly Timer _heartbeatTimer;

        public RetailBroker(
            ILogger<RetailBroker> logger,
            MarketDataGateway gateway)
        {
            _logger = logger;
            _gateway = gateway;
            _clients = new ConcurrentDictionary<string, List<IRetailClient>>();
            _latestData = new ConcurrentDictionary<string, MarketDataEvent>();
            
            // Set up heartbeat timer
            _heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public void RegisterClient(string symbol, IRetailClient client)
        {
            var clients = _clients.GetOrAdd(symbol, _ => new List<IRetailClient>());
            lock (clients)
            {
                if (!clients.Contains(client))
                {
                    clients.Add(client);
                    
                    // Subscribe to market data if this is the first client for this symbol
                    if (clients.Count == 1)
                    {
                        _gateway.Subscribe(symbol, this);
                    }
                    
                    // Send latest data if available
                    if (_latestData.TryGetValue(symbol, out var latestData))
                    {
                        _ = Task.Run(() => client.OnMarketDataAsync(latestData));
                    }
                }
            }
        }

        public void UnregisterClient(string symbol, IRetailClient client)
        {
            if (_clients.TryGetValue(symbol, out var clients))
            {
                lock (clients)
                {
                    clients.Remove(client);
                    
                    // Unsubscribe from market data if there are no more clients for this symbol
                    if (clients.Count == 0)
                    {
                        _gateway.Unsubscribe(symbol, this);
                    }
                }
            }
        }

        public async Task OnMarketDataAsync(MarketDataEvent marketDataEvent)
        {
            // Store the latest data
            _latestData.AddOrUpdate(marketDataEvent.Symbol, marketDataEvent, (_, _) => marketDataEvent);
            
            // Get clients for this symbol
            if (_clients.TryGetValue(marketDataEvent.Symbol, out var clients))
            {
                List<IRetailClient> clientsCopy;
                lock (clients)
                {
                    clientsCopy = new List<IRetailClient>(clients);
                }
                
                // Send data to all clients
                var tasks = clientsCopy.Select(client => 
                    Task.Run(async () =>
                    {
                        try
                        {
                            await client.OnMarketDataAsync(marketDataEvent);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending market data to retail client");
                        }
                    }));
                
                await Task.WhenAll(tasks);
            }
        }

        private void SendHeartbeat(object state)
        {
            foreach (var symbolClients in _clients.Values)
            {
                List<IRetailClient> clientsCopy;
                lock (symbolClients)
                {
                    clientsCopy = new List<IRetailClient>(symbolClients);
                }
                
                var heartbeatEvent = new MarketDataEvent
                {
                    EventType = MarketDataEventType.Quote,
                    Timestamp = DateTime.UtcNow,
                    Data = new Dictionary<string, object>
                    {
                        ["Heartbeat"] = true
                    }
                };
                
                foreach (var client in clientsCopy)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await client.OnMarketDataAsync(heartbeatEvent);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending heartbeat to retail client");
                        }
                    });
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Retail Broker is running.");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            _heartbeatTimer?.Dispose();
            base.Dispose();
        }
    }

    public interface IRetailClient
    {
        Task OnMarketDataAsync(MarketDataEvent marketDataEvent);
    }
}
```

### 4. Institutional Broker Implementation

```csharp
// InstitutionalBroker.cs
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MarketDataSimulator.Brokers
{
    public class InstitutionalBroker : BackgroundService, IMarketDataSubscriber
    {
        private readonly ILogger<InstitutionalBroker> _logger;
        private readonly MarketDataGateway _gateway;
        private readonly ConcurrentDictionary<string, List<IInstitutionalClient>> _clients;
        private readonly ConcurrentDictionary<string, MarketDataEvent> _latestData;
        private readonly Timer _statisticsTimer;

        public InstitutionalBroker(
            ILogger<InstitutionalBroker> logger,
            MarketDataGateway gateway)
        {
            _logger = logger;
            _gateway = gateway;
            _clients = new ConcurrentDictionary<string, List<IInstitutionalClient>>();
            _latestData = new ConcurrentDictionary<string, MarketDataEvent>();
            
            // Set up statistics timer
            _statisticsTimer = newTimer(SendStatistics, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public void RegisterClient(string symbol, IInstitutionalClient client)
        {
            var clients = _clients.GetOrAdd(symbol, _ => new List<IInstitutionalClient>());
            lock (clients)
            {
                if (!clients.Contains(client))
                {
                    clients.Add(client);
                    
                    // Subscribe to market data if this is the first client for this symbol
                    if (clients.Count == 1)
                    {
                        _gateway.Subscribe(symbol, this);
                    }
                    
                    // Send latest data if available
                    if (_latestData.TryGetValue(symbol, out var latestData))
                    {
                        _ = Task.Run(() => client.OnMarketDataAsync(latestData));
                    }
                }
            }
        }

        public void UnregisterClient(string symbol, IInstitutionalClient client)
        {
            if (_clients.TryGetValue(symbol, out var clients))
            {
                lock (clients)
                {
                    clients.Remove(client);
                    
                    // Unsubscribe from market data if there are no more clients for this symbol
                    if (clients.Count == 0)
                    {
                        _gateway.Unsubscribe(symbol, this);
                    }
                }
            }
        }

        public async Task OnMarketDataAsync(MarketDataEvent marketDataEvent)
        {
            // Store the latest data
            _latestData.AddOrUpdate(marketDataEvent.Symbol, marketDataEvent, (_, _) => marketDataEvent);
            
            // Get clients for this symbol
            if (_clients.TryGetValue(marketDataEvent.Symbol, out var clients))
            {
                List<IInstitutionalClient> clientsCopy;
                lock (clients)
                {
                    clientsCopy = new List<IInstitutionalClient>(clients);
                }
                
                // Send data to all clients
                var tasks = clientsCopy.Select(client => 
                    Task.Run(async () =>
                    {
                        try
                        {
                            await client.OnMarketDataAsync(marketDataEvent);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending market data to institutional client");
                        }
                    }));
                
                await Task.WhenAll(tasks);
            }
        }

        private void SendStatistics(object state)
        {
            foreach (var symbol in _latestData.Keys)
            {
                if (_clients.TryGetValue(symbol, out var clients))
                {
                    List<IInstitutionalClient> clientsCopy;
                    lock (clients)
                    {
                        clientsCopy = new List<IInstitutionalClient>(clients);
                    }
                    
                    if (_latestData.TryGetValue(symbol, out var latestData))
                    {
                        var statisticsEvent = new MarketDataEvent
                        {
                            Symbol = symbol,
                            EventType = MarketDataEventType.Quote,
                            Timestamp = DateTime.UtcNow,
                            Data = new Dictionary<string, object>
                            {
                                ["Statistics"] = true,
                                ["LastPrice"] = latestData.Data["LastPrice"],
                                ["Volume"] = latestData.Data["Volume"]
                            }
                        };
                        
                        foreach (var client in clientsCopy)
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await client.OnMarketDataAsync(statisticsEvent);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error sending statistics to institutional client");
                                }
                            });
                        }
                    }
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Institutional Broker is running.");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            _statisticsTimer?.Dispose();
            base.Dispose();
        }
    }

    public interface IInstitutionalClient
    {
        Task OnMarketDataAsync(MarketDataEvent marketDataEvent);
    }
}
```

### 5. Proprietary Trading Desk Implementation

```csharp
// ProprietaryTradingDesk.cs
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarketDataSimulator.Brokers
{
    public class ProprietaryTradingDesk : BackgroundService, IMarketDataSubscriber
    {
        private readonly ILogger<ProprietaryTradingDesk> _logger;
        private readonly MarketDataGateway _gateway;
        private readonly ConcurrentDictionary<string, List<ITradingAlgorithm>> _algorithms;
        private readonly ConcurrentDictionary<string, MarketDataEvent> _latestData;
        private readonly ConcurrentDictionary<string, List<decimal>> _priceHistory;
        private readonly Timer _analysisTimer;

        public ProprietaryTradingDesk(
            ILogger<ProprietaryTradingDesk> logger,
            MarketDataGateway gateway)
        {
            _logger = logger;
            _gateway = gateway;
            _algorithms = new ConcurrentDictionary<string, List<ITradingAlgorithm>>();
            _latestData = new ConcurrentDictionary<string, MarketDataEvent>();
            _priceHistory = new ConcurrentDictionary<string, List<decimal>>();
            
            // Set up analysis timer
            _analysisTimer = new Timer(AnalyzeMarketData, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public void RegisterAlgorithm(string symbol, ITradingAlgorithm algorithm)
        {
            var algorithms = _algorithms.GetOrAdd(symbol, _ => new List<ITradingAlgorithm>());
            lock (algorithms)
            {
                if (!algorithms.Contains(algorithm))
                {
                    algorithms.Add(algorithm);
                    
                    // Subscribe to market data if this is the first algorithm for this symbol
                    if (algorithms.Count == 1)
                    {
                        _gateway.Subscribe(symbol, this);
                    }
                    
                    // Initialize price history if needed
                    _priceHistory.TryAdd(symbol, new List<decimal>());
                }
            }
        }

        public void UnregisterAlgorithm(string symbol, ITradingAlgorithm algorithm)
        {
            if (_algorithms.TryGetValue(symbol, out var algorithms))
            {
                lock (algorithms)
                {
                    algorithms.Remove(algorithm);
                    
                    // Unsubscribe from market data if there are no more algorithms for this symbol
                    if (algorithms.Count == 0)
                    {
                        _gateway.Unsubscribe(symbol, this);
                    }
                }
            }
        }

        public async Task OnMarketDataAsync(MarketDataEvent marketDataEvent)
        {
            // Store the latest data
            _latestData.AddOrUpdate(marketDataEvent.Symbol, marketDataEvent, (_, _) => marketDataEvent);
            
            // Update price history
            if (marketDataEvent.Data.TryGetValue("LastPrice", out var priceObj) && priceObj is decimal price)
            {
                var priceHistory = _priceHistory.GetOrAdd(marketDataEvent.Symbol, _ => new List<decimal>());
                lock (priceHistory)
                {
                    priceHistory.Add(price);
                    
                    // Keep only the last 100 prices
                    if (priceHistory.Count > 100)
                    {
                        priceHistory.RemoveAt(0);
                    }
                }
            }
            
            // Get algorithms for this symbol
            if (_algorithms.TryGetValue(marketDataEvent.Symbol, out var algorithms))
            {
                List<ITradingAlgorithm> algorithmsCopy;
                lock (algorithms)
                {
                    algorithmsCopy = new List<ITradingAlgorithm>(algorithms);
                }
                
                // Send data to all algorithms
                var tasks = algorithmsCopy.Select(algorithm => 
                    Task.Run(async () =>
                    {
                        try
                        {
                            await algorithm.OnMarketDataAsync(marketDataEvent);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending market data to trading algorithm");
                        }
                    }));
                
                await Task.WhenAll(tasks);
            }
        }

        private void AnalyzeMarketData(object state)
        {
            foreach (var symbol in _priceHistory.Keys)
            {
                if (_algorithms.TryGetValue(symbol, out var algorithms))
                {
                    List<ITradingAlgorithm> algorithmsCopy;
                    lock (algorithms)
                    {
                        algorithmsCopy = new List<ITradingAlgorithm>(algorithms);
                    }
                    
                    if (_priceHistory.TryGetValue(symbol, out var priceHistory))
                    {
                        List<decimal> priceHistoryCopy;
                        lock (priceHistory)
                        {
                            priceHistoryCopy = new List<decimal>(priceHistory);
                        }
                        
                        if (priceHistoryCopy.Count > 10) // Need enough data for analysis
                        {
                            // Calculate simple moving average
                            var sma = priceHistoryCopy.TakeLast(10).Average();
                            
                            // Calculate volatility (standard deviation)
                            var variance = priceHistoryCopy.TakeLast(10).Select(p => Math.Pow((double)(p - sma), 2)).Average();
                            var volatility = Math.Sqrt(variance);
                            
                            var analysisEvent = new MarketDataEvent
                            {
                                Symbol = symbol,
                                EventType = MarketDataEventType.Quote,
                                Timestamp = DateTime.UtcNow,
                                Data = new Dictionary<string, object>
                                {
                                    ["Analysis"] = true,
                                    ["SMA"] = Math.Round(sma, 2),
                                    ["Volatility"] = Math.Round((decimal)volatility, 4)
                                }
                            };
                            
                            foreach (var algorithm in algorithmsCopy)
                            {
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await algorithm.OnMarketDataAsync(analysisEvent);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error sending analysis to trading algorithm");
                                    }
                                });
                            }
                        }
                    }
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Proprietary Trading Desk is running.");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            _analysisTimer?.Dispose();
            base.Dispose();
        }
    }

    public interface ITradingAlgorithm
    {
        Task OnMarketDataAsync(MarketDataEvent marketDataEvent);
    }
}
```

### 6. Web Client Implementation

```csharp
// WebClientController.cs
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MarketDataSimulator.Clients
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebClientController : ControllerBase, IRetailClient
    {
        private readonly ILogger<WebClientController> _logger;
        private readonly RetailBroker _retailBroker;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<MarketDataEvent>> _dataQueues;

        public WebClientController(
            ILogger<WebClientController> logger,
            RetailBroker retailBroker)
        {
            _logger = logger;
            _retailBroker = retailBroker;
            _dataQueues = new ConcurrentDictionary<string, ConcurrentQueue<MarketDataEvent>>();
        }

        [HttpGet("stream/{symbol}")]
        public async Task StreamMarketData(string symbol)
        {
            // Set up Server-Sent Events
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            // Create a queue for this connection
            var queue = new ConcurrentQueue<MarketDataEvent>();
            _dataQueues.TryAdd(symbol, queue);

            // Register with the broker
            _retailBroker.RegisterClient(symbol, this);

            try
            {
                // Send data as it becomes available
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    if (queue.TryDequeue(out var marketDataEvent))
                    {
                        var jsonData = System.Text.Json.JsonSerializer.Serialize(marketDataEvent);
                        await Response.WriteAsync($"data: {jsonData}\n\n");
                        await Response.Body.FlushAsync();
                    }
                    else
                    {
                        await Task.Delay(100, HttpContext.RequestAborted);
                    }
                }
            }
            finally
            {
                // Clean up
                _dataQueues.TryRemove(symbol, out _);
                _retailBroker.UnregisterClient(symbol, this);
            }
        }

        public async Task OnMarketDataAsync(MarketDataEvent marketDataEvent)
        {
            if (_dataQueues.TryGetValue(marketDataEvent.Symbol, out var queue))
            {
                queue.Enqueue(marketDataEvent);
                
                // Limit queue size to prevent memory issues
                while (queue.Count > 100)
                {
                    queue.TryDequeue(out _);
                }
            }

            await Task.CompletedTask;
        }
    }
}
```

### 7. Program.cs (Main Application Entry Point)

```csharp
// Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MarketDataSimulator.Exchange;
using MarketDataSimulator.Gateway;
using MarketDataSimulator.Brokers;
using MarketDataSimulator.Clients;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register our services
builder.Services.AddSingleton<MarketDataGateway>();
builder.Services.AddSingleton<RetailBroker>();
builder.Services.AddSingleton<InstitutionalBroker>();
builder.Services.AddSingleton<ProprietaryTradingDesk>();
builder.Services.AddHostedService<ExchangeSimulator>();
builder.Services.AddHostedService<MarketDataGateway>(provider => provider.GetRequiredService<MarketDataGateway>());
builder.Services.AddHostedService<RetailBroker>(provider => provider.GetRequiredService<RetailBroker>());
builder.Services.AddHostedService<InstitutionalBroker>(provider => provider.GetRequiredService<InstitutionalBroker>());
builder.Services.AddHostedService<ProprietaryTradingDesk>(provider => provider.GetRequiredService<ProprietaryTradingDesk>());

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 8. Frontend HTML/JavaScript for Web Client

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Stock Market Data Streaming Demo</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background-color: white;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
            padding: 20px;
        }
        h1 {
            color: #333;
            margin-bottom: 20px;
        }
        .controls {
            display: flex;
            margin-bottom: 20px;
            gap: 10px;
        }
        .controls select, .controls button {
            padding: 8px 12px;
            border-radius: 4px;
            border: 1px solid #ddd;
        }
        .controls button {
            background-color: #4CAF50;
            color: white;
            border: none;
            cursor: pointer;
        }
        .controls button:hover {
            background-color: #45a049;
        }
        .controls button.disconnect {
            background-color: #f44336;
        }
        .controls button.disconnect:hover {
            background-color: #d32f2f;
        }
        .status {
            margin-bottom: 20px;
            padding: 10px;
            border-radius: 4px;
        }
        .status.connected {
            background-color: #dff0d8;
            color: #3c763d;
        }
        .status.disconnected {
            background-color: #f2dede;
            color: #a94442;
        }
        .market-data {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
            gap: 20px;
        }
        .stock-card {
            border: 1px solid #ddd;
            border-radius: 8px;
            padding: 15px;
            background-color: #fff;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
        }
        .stock-header {
            display: flex;
            justify-content: space-between;
            margin-bottom: 10px;
        }
        .stock-symbol {
            font-weight: bold;
            font-size: 1.2em;
        }
        .stock-time {
            color: #777;
            font-size: 0.8em;
        }
        .stock-price {
            font-size: 1.5em;
            font-weight: bold;
            margin-bottom: 10px;
        }
        .stock-details {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 10px;
        }
        .stock-detail {
            display: flex;
            justify-content: space-between;
        }
        .detail-label {
            color: #777;
        }
        .detail-value {
            font-weight: bold;
        }
        .price-up {
            color: #4CAF50;
        }
        .price-down {
            color: #f44336;
        }
        .log {
            margin-top: 20px;
            border: 1px solid #ddd;
            border-radius: 4px;
            padding: 10px;
            height: 200px;
            overflow-y: auto;
            font-family: monospace;
            font-size: 0.9em;
            background-color: #f9f9f9;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>Stock Market Data Streaming Demo</h1>
        
        <div class="controls">
            <select id="symbolSelect">
                <option value="AAPL">AAPL</option>
                <option value="MSFT">MSFT</option>
                <option value="GOOGL">GOOGL</option>
                <option value="AMZN">AMZN</option>
                <option value="TSLA">TSLA</option>
                <option value="META">META</option>
                <option value="NVDA">NVDA</option>
                <option value="JPM">JPM</option>
            </select>
            <button id="connectBtn">Connect</button>
            <button id="disconnectBtn" class="disconnect" disabled>Disconnect</button>
        </div>
        
        <div id="status" class="status disconnected">Status: Disconnected</div>
        
        <div id="marketData" class="market-data"></div>
        
        <div id="log" class="log"></div>
    </div>

    <script>
        document.addEventListener('DOMContentLoaded', function() {
            const symbolSelect = document.getElementById('symbolSelect');
            const connectBtn = document.getElementById('connectBtn');
            const disconnectBtn = document.getElementById('disconnectBtn');
            const statusDiv = document.getElementById('status');
            const marketDataDiv = document.getElementById('marketData');
            const logDiv = document.getElementById('log');
            
            let eventSource = null;
            let stockData = {};
            let previousPrices = {};
            
            connectBtn.addEventListener('click', connect);
            disconnectBtn.addEventListener('click', disconnect);
            
            function connect() {
                const symbol = symbolSelect.value;
                
                if (eventSource) {
                    eventSource.close();
                }
                
                eventSource = new EventSource(`/api/WebClient/stream/${symbol}`);
                
                eventSource.onopen = function() {
                    statusDiv.textContent = 'Status: Connected';
                    statusDiv.className = 'status connected';
                    connectBtn.disabled = true;
                    disconnectBtn.disabled = false;
                    symbolSelect.disabled = true;
                    
                    addLog(`Connected to stream for ${symbol}`);
                };
                
                eventSource.onmessage = function(event) {
                    try {
                        const data = JSON.parse(event.data);
                        updateMarketData(data);
                    } catch (e) {
                        addLog(`Error parsing data: ${e.message}`);
                    }
                };
                
                eventSource.onerror = function() {
                    statusDiv.textContent = 'Status: Error';
                    statusDiv.className = 'status disconnected';
                    addLog('Connection error');
                    
                    // Try to reconnect after a delay
                    setTimeout(() => {
                        if (eventSource) {
                            connect();
                        }
                    }, 5000);
                };
            }
            
            function disconnect() {
                if (eventSource) {
                    eventSource.close();
                    eventSource = null;
                }
                
                statusDiv.textContent = 'Status: Disconnected';
                statusDiv.className = 'status disconnected';
                connectBtn.disabled = false;
                disconnectBtn.disabled = true;
                symbolSelect.disabled = false;
                
                addLog('Disconnected');
            }
            
            function updateMarketData(data) {
                // Handle heartbeat
                if (data.Data && data.Data.Heartbeat) {
                    addLog('Heartbeat received');
                    return;
                }
                
                const symbol = data.Symbol;
                
                // Initialize stock data if needed
                if (!stockData[symbol]) {
                    stockData[symbol] = {};
                    previousPrices[symbol] = null;
                }
                
                // Update stock data
                Object.assign(stockData[symbol], data.Data);
                
                // Check if price changed
                let priceChanged = false;
                if (data.Data.LastPrice !== undefined && previousPrices[symbol] !== null) {
                    priceChanged = data.Data.LastPrice !== previousPrices[symbol];
                }
                previousPrices[symbol] = data.Data.LastPrice;
                
                // Update UI
                renderStockCard(symbol, priceChanged);
                
                // Log trade events
                if (data.EventType === 'Trade') {
                    addLog(`Trade: ${symbol} @ ${data.Data.Price} x ${data.Data.Size}`);
                }
            }
            
            function renderStockCard(symbol, priceChanged) {
                let card = document.getElementById(`card-${symbol}`);
                
                if (!card) {
                    card = document.createElement('div');
                    card.id = `card-${symbol}`;
                    card.className = 'stock-card';
                    marketDataDiv.appendChild(card);
                }
                
                const data = stockData[symbol];
                const lastPrice = data.LastPrice || 0;
                const bidPrice = data.BidPrice || 0;
                const askPrice = data.AskPrice || 0;
                const bidSize = data.BidSize || 0;
                const askSize = data.AskSize || 0;
                const volume = data.Volume || 0;
                const timestamp = new Date().toLocaleTimeString();
                
                let priceClass = '';
                if (priceChanged) {
                    priceClass = lastPrice > previousPrices[symbol] ? 'price-up' : 'price-down';
                }
                
                card.innerHTML = `
                    <div class="stock-header">
                        <div class="stock-symbol">${symbol}</div>
                        <div class="stock-time">${timestamp}</div>
                    </div>
                    <div class="stock-price ${priceClass}">$${lastPrice.toFixed(2)}</div>
                    <div class="stock-details">
                        <div class="stock-detail">
                            <span class="detail-label">Bid:</span>
                            <span class="detail-value">$${bidPrice.toFixed(2)}</span>
                        </div>
                        <div class="stock-detail">
                            <span class="detail-label">Ask:</span>
                            <span class="detail-value">$${askPrice.toFixed(2)}</span>
                        </div>
                        <div class="stock-detail">
                            <span class="detail-label">Bid Size:</span>
                            <span class="detail-value">${bidSize}</span>
                        </div>
                        <div class="stock-detail">
                            <span class="detail-label">Ask Size:</span>
                            <span class="detail-value">${askSize}</span>
                        </div>
                        <div class="stock-detail">
                            <span class="detail-label">Volume:</span>
                            <span class="detail-value">${volume.toLocaleString()}</span>
                        </div>
                    </div>
                `;
            }
            
            function addLog(message) {
                const timestamp = new Date().toLocaleTimeString();
                logDiv.innerHTML += `[${timestamp}] ${message}\n`;
                logDiv.scrollTop = logDiv.scrollHeight;
                
                // Limit log size
                const lines = logDiv.innerHTML.split('\n');
                if (lines.length > 100) {
                    logDiv.innerHTML = lines.slice(-100).join('\n');
                }
            }
        });
    </script>
</body>
</html>
```
