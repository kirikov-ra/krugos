import * as Phaser from 'phaser';

export class GameScene extends Phaser.Scene {
  constructor() {
    super('GameScene');
  }

  preload() {
    // Здесь позже будем грузить "Круглозверей"
    // this.load.image('krosh_placeholder', 'assets/roundies/1.png');
  }

  create() {
    const { width, height } = this.scale;

    // Центрируем заголовок
    this.add.text(width / 2, 50, 'Roundoku', {
      fontSize: '32px',
      color: '#000000',
      fontStyle: 'bold'
    }).setOrigin(0.5);

    // Временная заглушка для сетки
    const gridSize = 450;
    this.add.rectangle(width / 2, height / 2 + 30, gridSize, gridSize, 0xf0f0f0)
      .setStrokeStyle(4, 0x000000);
      
    this.add.text(width / 2, height / 2 + 30, 'Здесь будет сетка 9x9', {
      color: '#555555'
    }).setOrigin(0.5);
  }
}