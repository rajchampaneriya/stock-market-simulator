import { useState, useEffect, useCallback, useRef } from 'react';
import type { OptionsConfig, OptionsSnapshot } from '../types/options';

interface UseOptionsRiskResult {
  config: OptionsConfig | null;
  configError: boolean;
  snapshot: OptionsSnapshot | null;
  connected: boolean;
  start: (seed?: number) => void;
  pause: () => void;
  resume: () => void;
  setSpeed: (speed: number) => void;
  applySuggestion: (id: string) => void;
  ignoreSuggestion: (id: string) => void;
}

/**
 * Educational options risk simulator client: REST for control,
 * SSE (event: snapshot) for the tick-by-tick state stream.
 */
export function useOptionsRisk(brokerUrl: string): UseOptionsRiskResult {
  const [config, setConfig] = useState<OptionsConfig | null>(null);
  const [configError, setConfigError] = useState(false);
  const [snapshot, setSnapshot] = useState<OptionsSnapshot | null>(null);
  const [connected, setConnected] = useState(false);
  const broker = brokerUrl || '';
  const esRef = useRef<EventSource | null>(null);

  const post = useCallback(
    (path: string, body?: unknown) =>
      fetch(`${broker}/api/options/${path}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: body === undefined ? undefined : JSON.stringify(body),
      })
        .then((r) => (r.ok ? r.json() : null))
        .then((d) => {
          if (d) setSnapshot(d);
        })
        .catch(() => undefined),
    [broker],
  );

  const start = useCallback((seed?: number) => post('start', { seed }), [post]);
  const pause = useCallback(() => post('pause'), [post]);
  const resume = useCallback(() => post('resume'), [post]);
  const setSpeed = useCallback((speed: number) => post('speed', { speed }), [post]);
  const applySuggestion = useCallback(
    (id: string) => post(`suggestions/${id}/apply`),
    [post],
  );
  const ignoreSuggestion = useCallback(
    (id: string) => post(`suggestions/${id}/ignore`),
    [post],
  );

  // Fetch feature config once — page hides itself when the flag is off.
  useEffect(() => {
    fetch(`${broker}/api/options/config`)
      .then((r) => (r.ok ? r.json() : Promise.reject()))
      .then((c: OptionsConfig) => setConfig(c))
      .catch(() => setConfigError(true));
  }, [broker]);

  // SSE snapshot stream while the feature is enabled.
  useEffect(() => {
    if (!config?.enabled) return;

    const es = new EventSource(`${broker}/api/options/stream`);
    esRef.current = es;

    const apply = (e: MessageEvent) => {
      try {
        setSnapshot(JSON.parse(e.data));
        setConnected(true);
      } catch {
        /* malformed frame — next tick replaces it */
      }
    };

    es.addEventListener('connected', () => setConnected(true));
    es.addEventListener('snapshot', apply as EventListener);
    es.onerror = () => setConnected(false);

    return () => {
      es.close();
      esRef.current = null;
      setConnected(false);
    };
  }, [broker, config?.enabled]);

  return {
    config,
    configError,
    snapshot,
    connected,
    start,
    pause,
    resume,
    setSpeed,
    applySuggestion,
    ignoreSuggestion,
  };
}
