import type { RedisStreamInfo } from '../types';

/**
 * Read-only Redis monitoring API. Distinguishes an empty, healthy stream from
 * a temporary Broker/Redis outage. Aspire startup can briefly return 503, so
 * retry twice with a small bounded delay before surfacing the real error.
 */
export class RedisMonitoringService {
  private readonly brokerUrl: string;

  constructor(brokerUrl: string) {
    this.brokerUrl = brokerUrl;
  }

  async fetchStreamsInfo(): Promise<RedisStreamInfo[]> {
    const url = `${this.brokerUrl}/api/redis/streams/info`;
    let failure: Error = new Error('Redis monitoring request failed');

    for (let attempt = 0; attempt < 3; attempt++) {
      try {
        const response = await fetch(url, {
          method: 'GET',
          headers: { Accept: 'application/json' },
        });

        if (response.ok) {
          return await response.json() as RedisStreamInfo[];
        }

        const body: { error?: string; code?: string } | null =
          await response.json().catch(() => null);
        if (response.status === 503 && body?.code === 'redis_unavailable') {
          failure = new Error(
            'Broker Service cannot connect to Redis. Check that the Aspire cache resource is running and healthy.',
          );
        } else if (response.status === 503 && body?.code === 'redis_timeout') {
          failure = new Error(
            'Redis request timed out. Check the cache resource and Broker Service logs in Aspire.',
          );
        } else if (response.status === 503) {
          failure = new Error(
            'The Broker API is temporarily unavailable (HTTP 503). Check the Aspire broker-service resource and the Vite API proxy target.',
          );
        } else {
          throw new Error(
            `Redis monitoring request failed (HTTP ${response.status}): ${body?.error || response.statusText}`,
          );
        }
        if (response.status !== 503 || attempt === 2) break;
      } catch (error) {
        failure = error instanceof Error ? error : new Error('Network request failed');
        // A fetch/network failure may also be a transient Aspire startup event.
        if (attempt === 2) break;
      }

      await new Promise<void>(resolve => setTimeout(resolve, 600 * (attempt + 1)));
    }

    throw failure;
  }
}
