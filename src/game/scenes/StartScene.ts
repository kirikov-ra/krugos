import * as Phaser from 'phaser';

export class StartScene extends Phaser.Scene {
  constructor() {
    super('StartScene');
  }

  create() {
    const { width, height } = this.scale;
    const centerX = width / 2;
    const centerY = height / 2;

    this.add.text(centerX, height * 0.3, 'KRUGOS', {
      fontSize: '48px',
      color: '#333',
      fontStyle: 'bold'
    }).setOrigin(0.5);

    this.add.text(centerX, height * 0.3 + 40, 'Animal Sudoku', {
      fontSize: '18px',
      color: '#666'
    }).setOrigin(0.5);

    const btnW = width * 0.6;
    const btnH = 60;
    
    const btnBg = this.add.rectangle(centerX, centerY, btnW, btnH, 0x4dd0e1)
      .setStrokeStyle(2, 0x000000)
      .setInteractive();

    const btnText = this.add.text(centerX, centerY, 'НОВАЯ ИГРА', {
      fontSize: '20px',
      color: '#000',
      fontStyle: 'bold'
    }).setOrigin(0.5);

    btnBg.on('pointerover', () => btnBg.setFillStyle(0x26c6da));
    btnBg.on('pointerout', () => btnBg.setFillStyle(0x4dd0e1));
    
    btnBg.on('pointerdown', () => {
      this.tweens.add({
        targets: [btnBg, btnText],
        scale: 0.95,
        duration: 100,
        yoyo: true,
        onComplete: () => {
          this.scene.start('GameScene');
        }
      });
    });
  }
}