import * as Phaser from 'phaser';
import { GameScene } from './scenes/GameScene';
import './style.css';
import { StartScene } from './scenes/StartScene';

const config: Phaser.Types.Core.GameConfig = {
  type: Phaser.AUTO,
  width: window.innerWidth,
  height: window.innerHeight,
  parent: 'app',
  backgroundColor: '#ffffff',
  scale: {
    mode: Phaser.Scale.FIT, 
    autoCenter: Phaser.Scale.CENTER_BOTH, 
    
    width: window.innerWidth,
    height: window.innerHeight,
  },
  scene: [StartScene, GameScene]
};

new Phaser.Game(config);