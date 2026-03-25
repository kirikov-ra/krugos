export interface IHudData {
  time: number;
  lives: number;
  hints: number;
}

export interface ISelectionCounters {
  [key: number]: number;
}

export interface IGameOverData {
  reason: 'win' | 'time' | 'no_lives';
  stats: {
    lives: number;
    timeRemaining: number;
    userFilledCount: number;
    emptyCount: number;
  };
}

declare global {
  interface WindowEventMap {
    'update-krugos-counters': CustomEvent<ISelectionCounters>;
    'update-krugos-hud': CustomEvent<IHudData>;
    'place-krugos-ball': CustomEvent<number>;
    'pause-krugos-game': CustomEvent<void>;
    
    'use-krugos-hint': CustomEvent<void>;
    'krugos-hint-error': CustomEvent<string>;

    'krugos-pause-state': CustomEvent<boolean>;
    'krugos-resume-game': CustomEvent<void>;
    'krugos-exit-to-menu': CustomEvent<void>;

    'krugos-game-over': CustomEvent<IGameOverData>;
    'krugos-restart-game': CustomEvent<void>;
  }
}