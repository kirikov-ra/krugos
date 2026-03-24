import { useState } from 'react';
import { MainMenu } from './components/MainMenu';
import { PhaserGame } from './components/PhaserGame';

type GameState = 'menu' | 'playing';

export default function App() {
  const [gameState, setGameState] = useState<GameState>('menu');
  
  const [gameConfig, setGameConfig] = useState<{
    difficulty: 'easy' | 'medium' | 'hard' | 'expert';
    loadFromStorage: boolean;
  }>({ difficulty: 'easy', loadFromStorage: false });

  const handleStartGame = (difficulty: 'easy' | 'medium' | 'hard' | 'expert', loadFromSave: boolean) => {
    setGameConfig({ difficulty, loadFromStorage: loadFromSave });
    setGameState('playing');
  };

  const handleExitToMenu = () => {
    setGameState('menu');
  };

  return (
    <div className="w-screen h-screen relative overflow-hidden bg-gradient-to-b from-[#ffffff] to-[#c8cfd7]">
      {gameState === 'menu' && (
        <MainMenu onStartGame={handleStartGame} />
      )}

      {gameState === 'playing' && (
        <div className="relative flex-grow flex items-center justify-center p-4">
          <div className="rounded-3xl border-[3px] border-white/50 bg-white/20 
                          shadow-[20px_20px_40px_rgba(160,170,180,0.5),-20px_-20px_40px_rgba(255,255,255,0.8)]
                          p-1 relative overflow-hidden">
            <PhaserGame 
              difficulty={gameConfig.difficulty} 
              loadFromStorage={gameConfig.loadFromStorage} 
              onExitToMenu={handleExitToMenu} 
            />
          </div>
        </div>
      )}
    </div>
  );
}

