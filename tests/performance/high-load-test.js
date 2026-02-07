import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Counter, Rate } from 'k6/metrics';

const latencyTrend = new Trend('ingestion_latency');
const successRate = new Rate('success_rate');
const requestCounter = new Counter('total_requests');

export const options = {
  scenarios: {
    highload: {
      executor: 'constant-vus',
      vus: 20,
      duration: '5m',
      tags: { test_type: 'highload' },
    },
  },
  thresholds: {
    'http_req_duration': ['p(95)<500'],
    'success_rate': ['rate>0.95'],
  },
};

const symbols = [
  'NIFTY50', 'BANKNIFTY', 'RELIANCE', 'TCS', 'INFY', 'HDFC', 'ICICI',
  'SBIN', 'BHARTIARTL', 'ITC', 'KOTAKBANK', 'LT', 'HCLTECH', 'AXISBANK',
  'WIPRO', 'MARUTI', 'SUNPHARMA', 'TATASTEEL', 'ULTRACEMCO', 'TITAN'
];

export default function () {
  const symbol = symbols[Math.floor(Math.random() * symbols.length)];
  const payload = JSON.stringify({
    symbol: symbol,
    price: Math.random() * 2000 + 50,
    volume: Math.floor(Math.random() * 200000),
    timestamp: new Date().toISOString(),
  });

  const res = http.post('http://localhost:5001/api/marketdata', payload, {
    headers: { 'Content-Type': 'application/json' },
  });

  const success = check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });

  latencyTrend.add(res.timings.duration);
  successRate.add(success);
  requestCounter.add(1);

  sleep(0.1);
}

export function handleSummary(data) {
  return {
    'stdout': JSON.stringify(data, null, 2),
    '/tmp/high-load-results.json': JSON.stringify(data),
  };
}
