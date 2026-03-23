import * as Phaser from 'phaser';
import { getSudoku } from 'sudoku-gen';

export class GameScene extends Phaser.Scene {
  private board: string[][] = [];
  private solution: string[][] = [];

  constructor() {
    super('GameScene');
  }

  create() {
    const { width, height } = this.scale;

    this.add.text(width / 2, 50, 'Krugos', {
      fontSize: '32px',
      color: '#000000',
      fontStyle: 'bold'
    }).setOrigin(0.5);

    this.initSudoku('easy');
    this.drawDebugBoard(width / 2, height / 2 + 30);
  }

  private initSudoku(difficulty: 'easy' | 'medium' | 'hard' | 'expert') {
    const sudoku = getSudoku(difficulty);
    
    this.board = this.parseStringToArray(sudoku.puzzle);
    this.solution = this.parseStringToArray(sudoku.solution);
    
    console.log('Текущая доска:', this.board);
  }

  private parseStringToArray(str: string): string[][] {
    const result: string[][] = [];
    for (let i = 0; i < 9; i++) {
      result.push(str.substring(i * 9, i * 9 + 9).split(''));
    }
    return result;
  }

  private drawDebugBoard(centerX: number, centerY: number) {
    const cellSize = 40;
    const offset = 4 * cellSize;

    for (let row = 0; row < 9; row++) {
      for (let col = 0; col < 9; col++) {
        const val = this.board[row][col];
        const x = centerX - offset + col * cellSize;
        const y = centerY - offset + row * cellSize;

        this.add.rectangle(x, y, cellSize, cellSize, 0xffffff)
          .setStrokeStyle(1, 0x000000);

        if (val !== '-') {
          this.add.text(x, y, val, { color: '#000' }).setOrigin(0.5);
        }
      }
    }
  }
}