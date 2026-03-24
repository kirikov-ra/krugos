import React, { useEffect, useRef } from 'react';
import * as Phaser from 'phaser';
import { GameScene } from '../game/scenes/GameScene';

interface PhaserGameProps {
  difficulty: 'easy' | 'medium' | 'hard' | 'expert';
  loadFromStorage: boolean;
  onExitToMenu: () => void;
}

export const PhaserGame: React.FC<PhaserGameProps> = ({ difficulty, loadFromStorage, onExitToMenu }) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const gameRef = useRef<Phaser.Game | null>(null);

  useEffect(() => {
    if (!gameRef.current && containerRef.current) {
      const config: Phaser.Types.Core.GameConfig = {
        type: Phaser.AUTO,
        parent: containerRef.current,
        scale: {
          mode: Phaser.Scale.FIT,
          autoCenter: Phaser.Scale.CENTER_BOTH,
          width: 720,
          height: 1280
        },
        backgroundColor: '#f0f0f0'
      };

      const game = new Phaser.Game(config);
      gameRef.current = game;

      game.registry.set('onExitToMenu', onExitToMenu);

      game.scene.add('GameScene', GameScene, true, { difficulty, loadFromStorage });
    }

    return () => {
      if (gameRef.current) {
        gameRef.current.destroy(true);
        gameRef.current = null;
      }
    };
  }, [difficulty, loadFromStorage, onExitToMenu]);

  return <div ref={containerRef} className="w-full h-full" />;
};