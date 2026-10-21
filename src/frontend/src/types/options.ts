export interface OptionsConfig {
  enabled: boolean;
  lotSize: number;
  strikeStep: number;
  wingOffset: number;
  capital: number;
  daysToExpiry: number;
  speeds: number[];
}

export interface Leg {
  id: string;
  type: 'CE' | 'PE';
  strike: number;
  side: 'SHORT' | 'LONG';
  lots: number;
  entryPrice: number;
  lastPrice: number;
  entryIv: number;
  iv: number;
  delta: number;
  gamma: number;
  thetaPerDay: number;
  vegaPerPoint: number;
  unrealized: number;
  realized: number;
  status: 'Open' | 'Exited';
}

export interface FuturesLeg {
  id: string;
  side: 'LONG' | 'SHORT';
  lots: number;
  entryPrice: number;
  lastPrice: number;
  unrealized: number;
  realized: number;
  status: 'Open' | 'Exited';
}

export interface Totals {
  realized: number;
  unrealized: number;
  net: number;
  premiumCollected: number;
  maxLossSoFar: number;
  drawdownFraction: number;
  netDelta: number;
  netGamma: number;
  netThetaPerDay: number;
  netVegaPerPoint: number;
}

export interface PayoffPoint {
  spot: number;
  pnl: number;
}

export interface SpotPoint {
  tick: number;
  spot: number;
}

export interface SpikeEvent {
  tickIndex: number;
  simMinute: number;
  fromSpot: number;
  toSpot: number;
  ivJump: number;
}

export type CoachActionKind = 'BuyFutures' | 'SellFutures' | 'ExitLeg';

export interface CoachAction {
  kind: CoachActionKind;
  lots: number;
  legId: string | null;
}

export interface Projection {
  holdPnl: number;
  actPnl: number;
  shockMove: number;
  ivChangePoints: number;
}

export interface CoachSuggestion {
  id: string;
  rule: string;
  title: string;
  explanation: string;
  action: CoachAction;
  projection: Projection;
  firstRaisedTick: number;
}

export interface Decision {
  tickIndex: number;
  simMinute: number;
  rule: string;
  description: string;
  outcome: string;
  holdPnl: number | null;
  actPnl: number | null;
  netPnlAtDecision: number;
  outcomePnl: number | null;
}

export interface OptionsSnapshot {
  status: 'NotStarted' | 'Running' | 'Paused' | 'Finished';
  tickIndex: number;
  simMinute: number;
  minutesToExpiry: number;
  spot: number;
  ivMultiplier: number;
  futuresPrice: number;
  atmStrike: number;
  speed: number;
  lotSize: number;
  legs: Leg[];
  futures: FuturesLeg[];
  totals: Totals;
  breakevens: number[];
  payoffExpiry: PayoffPoint[];
  payoffToday: PayoffPoint[];
  spotHistory: SpotPoint[];
  spikes: SpikeEvent[];
  suggestions: CoachSuggestion[];
  decisions: Decision[];
}
