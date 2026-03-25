import { useCallback, useEffect, useState } from 'react';
import { MainMenu } from './components/MainMenu';
import { PhaserGame } from './components/PhaserGame';
import type { IHudData, ISelectionCounters } from './game/types';
import type { Difficulty } from 'sudoku-gen/dist/types/difficulty.type';
import AnimalsPanel from './components/AnimalsPanel';
import InfoPanel from './components/InfoPanel';
import PauseModal from './components/PauseModal';

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
  const [hintError, setHintError] = useState<string | null>(null);
  const [isPaused, setIsPaused] = useState(false);

  useEffect(() => {
    const handlePauseState = (e: Event) => {
      const customEvent = e as CustomEvent<boolean>;
      setIsPaused(customEvent.detail);
    };
    window.addEventListener('krugos-pause-state', handlePauseState);
    return () => window.removeEventListener('krugos-pause-state', handlePauseState);
  }, []);

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
    setIsPaused(false);
  }, []);

  return (
    <div className="w-screen h-screen relative overflow-hidden bg-gradient-to-b from-[#ffffff] to-[#c8cfd7]">
      {gameState === 'menu' && (
        <MainMenu onStartGame={handleStartGame} />
      )}

      {gameState === 'playing' && (
        <div className="relative grow flex flex-col items-center justify-center p-4">

          <InfoPanel hud={hud} />

          <div className="my-4 w-full max-w-sm aspect-square rounded-xl border-[3px] border-gray-300 bg-white/20 
                        relative overflow-hidden shadow-lg">
            <PhaserGame 
              difficulty={gameConfig.difficulty} 
              loadFromStorage={gameConfig.loadFromStorage} 
              onExitToMenu={handleExitToMenu} 
            />

            {isPaused && <PauseModal />}

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