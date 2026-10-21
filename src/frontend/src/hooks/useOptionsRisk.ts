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
  const sessionRef = useRef<string | null>(null);

  const post = useCallback(
    (path: string, body?: unknown) =>
      fetch(`${broker}/api/options/sessions/${sessionRef.current}/${path}`, {
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

  const start = useCallback(async (seed?: number) => {
    try {
      const response = await fetch(`${broker}/api/options/sessions`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ seed }),
      });
      if (!response.ok) return;
      const data = await response.json();
      esRef.current?.close();
      sessionRef.current = data.sessionId;
      setSnapshot(data.snapshot);
      const es = new EventSource(`${broker}/api/options/sessions/${data.sessionId}/stream`);
      esRef.current = es;
      es.addEventListener('snapshot', (event) => {
        try { setSnapshot(JSON.parse((event as MessageEvent).data)); setConnected(true); } catch { /* malformed frame */ }
      });
      es.onerror = () => setConnected(false);
    } catch { setConnected(false); }
  }, [broker]);
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

  // Stream belongs to this browser's isolated session; close on unmount.
  useEffect(() => () => { esRef.current?.close(); }, []);

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
