export interface IHudData {
  time: number;
  lives: number;
  hints: number;
}

export interface ISelectionCounters {
  [key: number]: number;
}

declare global {
  interface WindowEventMap {
    'update-krugos-counters': CustomEvent<ISelectionCounters>;
    'update-krugos-hud': CustomEvent<IHudData>;
    'place-krugos-ball': CustomEvent<number>;
    'pause-krugos-game': CustomEvent<void>;
  }
}

declare global {
  interface WindowEventMap {
    'update-krugos-counters': CustomEvent<ISelectionCounters>;
    'update-krugos-hud': CustomEvent<IHudData>;
    'place-krugos-ball': CustomEvent<number>;
    'pause-krugos-game': CustomEvent<void>;
    
    'use-krugos-hint': CustomEvent<void>;
    'krugos-hint-error': CustomEvent<string>;
  }
}