import { useState, useEffect, useCallback, useRef } from 'react';
import type { OptionsConfig, OptionsSnapshot } from '../types/options';

interface UseOptionsRiskResult {
  config: OptionsConfig | null;
  configError: boolean;
  snapshot: OptionsSnapshot | null;
  connected: boolean;
  sessionId: string | null;
  actionError: string | null;
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
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const broker = brokerUrl || '';
  const esRef = useRef<EventSource | null>(null);
  const sessionRef = useRef<string | null>(null);

  const post = useCallback(async (path: string, body?: unknown) => {
    if (!sessionRef.current) return;
    try {
      setActionError(null);
      const response = await fetch(`${broker}/api/options/sessions/${sessionRef.current}/${path}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: body === undefined ? undefined : JSON.stringify(body),
      });
      if (!response.ok) throw new Error(`Action rejected (HTTP ${response.status})`);
      const next = await response.json() as OptionsSnapshot;
      setSnapshot(next);
    } catch (cause) {
      setActionError(cause instanceof Error ? cause.message : 'Action failed');
    }
  }, [broker]);

  const start = useCallback(async (seed?: number) => {
    try {
      setActionError(null);
      const response = await fetch(`${broker}/api/options/sessions`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ seed }),
      });
      if (!response.ok) throw new Error(`Unable to start session (HTTP ${response.status})`);
      const data = await response.json();
      esRef.current?.close();
      if (sessionRef.current) {
        void fetch(`${broker}/api/options/sessions/${sessionRef.current}`, { method: 'DELETE' });
      }
      sessionRef.current = data.sessionId;
      setSessionId(data.sessionId);
      setSnapshot(data.snapshot);
      const es = new EventSource(`${broker}/api/options/sessions/${data.sessionId}/stream`);
      esRef.current = es;
      es.addEventListener('snapshot', (event) => {
        try { setSnapshot(JSON.parse((event as MessageEvent).data)); setConnected(true); } catch { /* malformed frame */ }
      });
      es.onerror = () => setConnected(false);
    } catch (e) { setConnected(false); setActionError(e instanceof Error ? e.message : 'Start failed'); }
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
    sessionId,
    actionError,
    start,
    pause,
    resume,
    setSpeed,
    applySuggestion,
    ignoreSuggestion,
  };
}
