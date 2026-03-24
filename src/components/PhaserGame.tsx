import React, { useEffect, useRef } from 'react';
import * as Phaser from 'phaser';
import { GameScene } from '../game/scenes/GameScene';
import type { Difficulty } from 'sudoku-gen/dist/types/difficulty.type';

interface PhaserGameProps {
  difficulty: Difficulty;
  loadFromStorage: boolean;
  onExitToMenu: () => void;
}

export const PhaserGame: React.FC<PhaserGameProps> = ({ difficulty, loadFromStorage, onExitToMenu }) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const gameRef = useRef<Phaser.Game | null>(null);

  useEffect(() => {
    if (!gameRef.current && containerRef.current) {

      const config: Phaser.Types.Core.GameConfig = {
        type: Phaser.WEBGL,
        parent: containerRef.current,
        transparent: true,
        scale: {
          mode: Phaser.Scale.FIT,
          width: 1080, 
          height: 1080
        },
        render: {
          antialias: true,
          roundPixels: true,
        }
      };

      Object.assign(config, { resolution: window.devicePixelRatio || 1 });

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