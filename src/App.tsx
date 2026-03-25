import { useCallback, useEffect, useState } from 'react';
import { MainMenu } from './components/MainMenu';
import { PhaserGame } from './components/PhaserGame';
import type { IHudData, ISelectionCounters } from './game/types';
import type { Difficulty } from 'sudoku-gen/dist/types/difficulty.type';
import AnimalsPanel from './components/AnimalsPanel';

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

  const [hud, setHud] = useState<IHudData>({ time: 1200, lives: 3, hints: 3 });

  const formatTime = (seconds: number) => {
    const m = Math.floor(Math.max(0, seconds) / 60);
    const s = Math.max(0, seconds) % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  const [hintError, setHintError] = useState<string | null>(null);

  useEffect(() => {
    let timeoutId: ReturnType<typeof setTimeout>;

    const handleHintError = (e: Event) => {
      const customEvent = e as CustomEvent<string>;
      setHintError(customEvent.detail);
      
      if (timeoutId) clearTimeout(timeoutId);
      
      timeoutId = setTimeout(() => {
        setHintError(null);
      }, 3000);
    };

    window.addEventListener('krugos-hint-error', handleHintError);
    
    return () => {
      window.removeEventListener('krugos-hint-error', handleHintError);
      if (timeoutId) clearTimeout(timeoutId);
    };
  }, []);

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

            
              

            <div className='flex flex-nowrap gap-x-1'>
              {['K','R','U','G','O','S'].map((i, index) => {
                return (
                  <div 
                    key={index} 
                  >
                    <img 
                      src={`/assets/ui/logo/${i}.png`}
                      alt={i}
                      className="h-6 object-contain drop-shadow-[2px_3px_2px_rgba(0,0,0,0.25)]" 
                    />
                  </div>
                );
              })}
            </div>

            <div className="flex flex-col items-end">
              <span className="text-red-600 font-extrabold text-2xl tracking-wide">
                {formatTime(hud.time)}
              </span>
              <div className="flex gap-1 mt-1">
                {[1, 2, 3].map((i) => {
                  const isLost = i > hud.lives;
                  
                  return (
                    <div 
                      key={i} 
                      className={`w-5 h-5 transition-all duration-300 ${isLost ? 'opacity-30 grayscale scale-90' : 'opacity-100 scale-100'}`}
                    >
                      <img 
                        src="/assets/ui/heart.png" 
                        alt="heart" 
                        className="w-full h-full object-contain drop-shadow-sm" 
                      />
                    </div>
                  );
                })}
              </div>
            </div>
          </div>

          <div className="my-4 w-full max-w-sm aspect-square rounded-xl border-[3px] border-gray-300 bg-white/20 
                        relative overflow-hidden shadow-lg">
            <PhaserGame 
              difficulty={gameConfig.difficulty} 
              loadFromStorage={gameConfig.loadFromStorage} 
              onExitToMenu={handleExitToMenu} 
            />

            {hintError && (
              <div className="absolute top-[30%] left-1/2 -translate-x-1/2 z-50 px-6 py-4 bg-[#4781ff]/95 backdrop-blur-md text-white font-extrabold text-lg rounded-2xl shadow-[0_10px_25px_rgba(71, 83, 255, 0.4)] border-2 border-white/20 pointer-events-none text-center w-[85%] max-w-[320px]">
                {hintError}
              </div>
            )}
          </div>

          <AnimalsPanel 
            counters={counters}
            hints={hud.hints}
          />

        </div>
      )}
    </div>
  );
}