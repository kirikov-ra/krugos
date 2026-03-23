import * as Phaser from 'phaser';
import { getSudoku } from 'sudoku-gen';

const KRUGOS_COLOR_MAP: Record<number, number> = {
  1: 0x4dd0e1,
  2: 0xba68c8,
  3: 0xf48fb1,
  4: 0xce93d8,
  5: 0x64b5f6,
  6: 0x9575cd,
  7: 0xfff176,
  8: 0xffb74d,
  9: 0x9e9e9e 
};

export class GameScene extends Phaser.Scene {
  private board: string[][] = [];
  private solution: string[][] = [];
  
  private readonly gridSize = 9;
  private cellSize = 50;
  private gridOffset = 100;

  constructor() {
    super('GameScene');
  }

  create() {
    const { width, height } = this.scale;

    this.cellSize = Math.floor((width * 0.9) / this.gridSize);
    this.gridOffset = height / 2 - (this.gridSize * this.cellSize) / 2 + 30;

    this.generateBallTextures();

    this.add.text(width / 2, 50, 'Krugos', {
      fontSize: '32px',
      color: '#000000',
      fontStyle: 'bold'
    }).setOrigin(0.5);

    this.initSudoku('easy');

    this.drawKrugosBoard(width / 2);
  }

  private generateBallTextures() {
    for (let id = 1; id <= 9; id++) {
      const color = KRUGOS_COLOR_MAP[id];
      
      const graphics = this.make.graphics({ x: 0, y: 0 });
      
      graphics.fillStyle(color, 1);
      graphics.fillCircle(this.cellSize / 2, this.cellSize / 2, this.cellSize * 0.4);
      
      graphics.lineStyle(2, 0x000000, 0.3);
      graphics.strokeCircle(this.cellSize / 2, this.cellSize / 2, this.cellSize * 0.4);

      graphics.generateTexture(`krug_${id}`, this.cellSize, this.cellSize);
      
      graphics.destroy();
    }
  }

  // --- Логика Sudoku ---
  private initSudoku(difficulty: 'easy' | 'medium' | 'hard' | 'expert') {
    const sudoku = getSudoku(difficulty);
    this.board = this.parseStringToArray(sudoku.puzzle);
    this.solution = this.parseStringToArray(sudoku.solution);
  }

  private parseStringToArray(str: string): string[][] {
    const result: string[][] = [];
    for (let i = 0; i < this.gridSize; i++) {
      result.push(str.substring(i * 9, i * 9 + 9).split(''));
    }
    return result;
  }

  // --- Логика Отрисовки ---
  private drawKrugosBoard(centerX: number) {
    const gridTotalSize = this.gridSize * this.cellSize;
    const startX = centerX - gridTotalSize / 2;

    for (let row = 0; row < this.gridSize; row++) {
      for (let col = 0; col < this.gridSize; col++) {
        const val = this.board[row][col];
        const x = startX + col * this.cellSize;
        const y = this.gridOffset + row * this.cellSize;

        this.add.rectangle(x, y, this.cellSize, this.cellSize, 0xffffff)
          .setOrigin(0)
          .setStrokeStyle(1, 0x000000, 0.2);

        if (val !== '-') {
          const ballId = parseInt(val, 10);
          this.add.image(x + this.cellSize / 2, y + this.cellSize / 2, `krug_${ballId}`);
        }
      }
    }
  }
}