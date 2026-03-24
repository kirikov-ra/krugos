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
    <div className="w-screen h-screen relative overflow-hidden bg-black">
      {gameState === 'menu' && (
        <MainMenu onStartGame={handleStartGame} />
      )}

      {gameState === 'playing' && (
        <PhaserGame 
          difficulty={gameConfig.difficulty} 
          loadFromStorage={gameConfig.loadFromStorage} 
          onExitToMenu={handleExitToMenu} 
        />
      )}
    </div>
  );
}