import { useCallback, useEffect, useState } from 'react';
import { MainMenu } from './components/MainMenu';
import { PhaserGame } from './components/PhaserGame';
import type { IHudData, ISelectionCounters } from './game/types';
import type { Difficulty } from 'sudoku-gen/dist/types/difficulty.type';

type GameState = 'menu' | 'playing';

export default function App() {
  const [gameState, setGameState] = useState<GameState>('menu');
  
  const [gameConfig, setGameConfig] = useState<{
    difficulty: Difficulty;
    loadFromStorage: boolean;
  }>({ difficulty: 'easy', loadFromStorage: false });

  const [counters, setCounters] = useState<Record<number, number>>({
    1:9, 2:9, 3:9, 4:9, 5:9, 6:9, 7:9, 8:9, 9:9
  });

  const [hud, setHud] = useState<IHudData>({ time: 1200, lives: 3 });

  const formatTime = (seconds: number) => {
    const m = Math.floor(Math.max(0, seconds) / 60);
    const s = Math.max(0, seconds) % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  useEffect(() => {
  const handleUpdateCounters = (e: Event) => {
    const customEvent = e as CustomEvent<ISelectionCounters>;
    setCounters(customEvent.detail);
  };

  window.addEventListener('update-krugos-counters', handleUpdateCounters);
  
  return () => {
    window.removeEventListener('update-krugos-counters', handleUpdateCounters);
  };
}, []);

  useEffect(() => {
    const handleUpdateHud = (e: Event) => {
      const customEvent = e as CustomEvent<IHudData>;
      setHud(customEvent.detail);
    };
    window.addEventListener('update-krugos-hud', handleUpdateHud);
    return () => window.removeEventListener('update-krugos-hud', handleUpdateHud);
  }, []);

  const handleStartGame = (difficulty: Difficulty, loadFromSave: boolean) => {
    setGameConfig({ difficulty, loadFromStorage: loadFromSave });
    setGameState('playing');
  };

  const handleExitToMenu = useCallback(() => {
    setGameState('menu');
  }, []);

  return (
    <div className="w-screen h-screen relative overflow-hidden bg-gradient-to-b from-[#ffffff] to-[#c8cfd7]">
      {gameState === 'menu' && (
        <MainMenu onStartGame={handleStartGame} />
      )}

      {gameState === 'playing' && (
        <div className="relative grow flex flex-col items-center justify-center p-4">

          <div className="w-full max-w-sm flex justify-between items-center">
            
            <button
              onClick={() => window.dispatchEvent(new CustomEvent('pause-krugos-game'))}
              className="w-12 h-12 bg-white/40 border-2 border-white/60 rounded-xl font-black text-gray-700 shadow-md active:scale-95 transition-transform flex items-center justify-center text-xl"
            >
              II
            </button>

            <h1 className="text-4xl font-black text-gray-700 drop-shadow-sm tracking-tighter">
              KRUGOS
            </h1>

            <div className="flex flex-col items-end">
              <span className="text-red-600 font-extrabold text-2xl tracking-wide">
                {formatTime(hud.time)}
              </span>
              <div className="flex gap-1 mt-1">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="w-5 h-5">
                    <svg viewBox="0 0 24 24" className={i <= hud.lives ? "fill-red-500 drop-shadow-sm" : "fill-transparent stroke-gray-400 stroke-2"}>
                      <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
                    </svg>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <div className="w-full max-w-sm aspect-square rounded-lg border-[3px] border-white/50 bg-white/20 
                          shadow-[20px_20px_40px_rgba(160,170,180,0.5),-20px_-20px_40px_rgba(255,255,255,0.8)]
                          p-1 relative overflow-hidden">
            <PhaserGame 
              difficulty={gameConfig.difficulty} 
              loadFromStorage={gameConfig.loadFromStorage} 
              onExitToMenu={handleExitToMenu} 
            />
          </div>

          <div className="flex flex-wrap justify-center gap-5 mt-6 p-4 bg-white/30 rounded-lg border border-white/50 shadow-lg backdrop-blur-md max-w-sm">
            {[1, 2, 3, 4, 5, 6, 7, 8, 9].map((id) => {
              const remaining = counters[id] || 0;
              const isDisabled = remaining <= 0;

              return (
                <div key={id} className="flex flex-col items-center gap-1">
                  <button
                    disabled={isDisabled}
                    onClick={() => window.dispatchEvent(new CustomEvent('place-krugos-ball', { detail: id }))}
                    className={`w-12 h-12 transition-transform active:scale-90 hover:scale-110 ${isDisabled ? 'opacity-30' : 'opacity-100'}`}
                  >
                    <img src={`/assets/balls/ball_${id}.png`} alt={`ball_${id}`} className="w-full h-full object-contain drop-shadow-md" />
                  </button>
                  <span className={`font-bold text-[16px] ${isDisabled ? 'text-gray-400' : 'text-gray-700'}`}>
                    {remaining}
                  </span>
                </div>
              );
            })}
          </div>

        </div>
      )}
    </div>
  );
}