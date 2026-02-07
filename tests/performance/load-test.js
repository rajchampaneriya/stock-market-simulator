import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Counter, Rate } from 'k6/metrics';

const latencyTrend = new Trend('ingestion_latency');
const successRate = new Rate('success_rate');
const requestCounter = new Counter('total_requests');

export const options = {
  scenarios: {
    baseline: {
      executor: 'constant-vus',
      vus: 1,
      duration: '2m',
      tags: { test_type: 'baseline' },
    },
  },
  thresholds: {
    'http_req_duration': ['p(95)<300'],
    'success_rate': ['rate>0.99'],
  },
};

const symbols = ['NIFTY50', 'BANKNIFTY', 'RELIANCE', 'TCS'];

export default function () {
  const symbol = symbols[Math.floor(Math.random() * symbols.length)];
  const payload = JSON.stringify({
    symbol: symbol,
    price: Math.random() * 1000 + 100,
    volume: Math.floor(Math.random() * 100000),
    timestamp: new Date().toISOString(),
  });

  const res = http.post('http://localhost:5001/api/marketdata', payload, {
    headers: { 'Content-Type': 'application/json' },
  });

  const success = check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 300ms': (r) => r.timings.duration < 300,
  });

  latencyTrend.add(res.timings.duration);
  successRate.add(success);
  requestCounter.add(1);

  sleep(0.3);
}

export function handleSummary(data) {
  return {
    'stdout': JSON.stringify(data, null, 2),
    '/tmp/perf-results.json': JSON.stringify(data),
  };
}
