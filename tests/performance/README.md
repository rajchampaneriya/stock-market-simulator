# Performance Testing Tools

This directory contains performance testing scripts for the Stock Market Simulator system.

## Prerequisites

- **k6**: Load testing tool
  ```bash
  # Install k6 (Linux)
  sudo gpg -k
  sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
  echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
  sudo apt-get update
  sudo apt-get install k6
  ```

- **redis-cli**: Redis command line interface
  ```bash
  sudo apt-get install redis-tools
  ```

- **bc**: Command line calculator
  ```bash
  sudo apt-get install bc
  ```

## Test Scripts

### 1. Load Testing with k6

#### Baseline Test
Tests with 1 virtual user simulating normal load:
```bash
k6 run load-test.js
```

#### High Load Test
Tests with 20 virtual users simulating high load:
```bash
k6 run high-load-test.js
```

### 2. Resource Monitoring

Monitor CPU, memory, and threads for all services:
```bash
# Monitor for 5 minutes (300 seconds), sampling every 5 seconds
chmod +x monitor-resources.sh
./monitor-resources.sh 300 5

# Monitor for 10 minutes
./monitor-resources.sh 600 5
```

Results are saved to `/tmp/perf-monitoring/resources.csv`

### 3. Redis Statistics

Check Redis stream health and statistics:
```bash
chmod +x redis-stats.sh
./redis-stats.sh
```

### 4. Latency Testing

Measure end-to-end latency with direct API calls:
```bash
# Run 100 iterations
chmod +x latency-test.sh
./latency-test.sh 100

# Run 500 iterations
./latency-test.sh 500
```

Results are saved to `/tmp/latency-results.txt`

## Running Complete Performance Suite

1. Start all services:
   ```bash
   cd /home/runner/work/stock-market-simulator/stock-market-simulator
   ./run-services.sh
   ```

2. In separate terminals, run:
   ```bash
   # Terminal 1: Resource monitoring
   cd tests/performance
   ./monitor-resources.sh 600 5
   
   # Terminal 2: Baseline load test
   k6 run load-test.js
   
   # Terminal 3: Check Redis stats periodically
   watch -n 30 ./redis-stats.sh
   ```

3. For high load testing:
   ```bash
   k6 run high-load-test.js
   ```

## Performance Targets

| Metric | Target | Acceptable | Poor |
|--------|--------|------------|------|
| Latency (p95) | < 300ms | < 500ms | > 500ms |
| Throughput | > 100 msg/s | > 50 msg/s | < 50 msg/s |
| CPU Usage | < 30% | < 50% | > 50% |
| Memory | Stable | < 500MB growth/hr | Constant growth |
| Success Rate | 100% | > 99% | < 99% |

## Output Files

All test outputs are saved to `/tmp/`:
- `/tmp/perf-results.json` - k6 baseline test results
- `/tmp/high-load-results.json` - k6 high load test results
- `/tmp/perf-monitoring/resources.csv` - Resource usage over time
- `/tmp/latency-results.txt` - Raw latency measurements
