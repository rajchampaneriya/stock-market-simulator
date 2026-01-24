import type { RedisStreamInfo } from '../types';

/**
 * Service for fetching Redis monitoring data
 */
export class RedisMonitoringService {
  private brokerUrl: string;

  constructor(brokerUrl: string) {
    this.brokerUrl = brokerUrl;
  }

  /**
   * Fetch information about all Redis streams
   * @returns Promise with list of stream information
   */
  async fetchStreamsInfo(): Promise<RedisStreamInfo[]> {
    const url = `${this.brokerUrl}/api/redis/streams/info`;
    
    try {
      const response = await fetch(url, {
        method: 'GET',
        headers: {
          'Accept': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch stream info: ${response.status} ${response.statusText}`);
      }

      const data: RedisStreamInfo[] = await response.json();
      return data;
    } catch (error) {
      console.error('Error fetching Redis stream info:', error);
      throw error;
    }
  }
}
