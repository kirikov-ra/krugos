import * as Phaser from 'phaser';
import { getSudoku } from 'sudoku-gen';

const KRUGOS_COLOR_MAP: Record<number, number> = {
  1: 0x4dd0e1, 2: 0xba68c8, 3: 0xf48fb1, 4: 0xce93d8, 
  5: 0x64b5f6, 6: 0x9575cd, 7: 0xfff176, 8: 0xffb74d, 9: 0x9e9e9e 
};

export class GameScene extends Phaser.Scene {
  private board: string[][] = [];
  private initialBoard: string[][] = [];
  private solution: string[][] = [];
  private currentBalls: (Phaser.GameObjects.Image | null)[][] = [];
  private selectedCell: { row: number, col: number, rect: Phaser.GameObjects.Rectangle } | null = null;

  private readonly gridSize = 9;
  private cellSize = 0;
  private startX = 0;
  private startY = 0;

  constructor() { super('GameScene'); }

  create() {
    const { width, height } = this.scale;

    const maxGridWidth = width * 0.85;
    const maxGridHeight = height * 0.6;
    this.cellSize = Math.min(maxGridWidth / 9, maxGridHeight / 9);

    this.startX = (width - this.cellSize * 9) / 2;
    this.startY = height * 0.15;

    this.generateBallTextures();
    this.initSudoku('easy');

    this.add.text(width / 2, height * 0.05, 'KRUGOS', {
      fontSize: `${Math.floor(this.cellSize * 0.8)}px`,
      color: '#333',
      fontStyle: 'bold'
    }).setOrigin(0.5);

    this.drawKrugosBoard();
    this.drawSelectionPanel();
  }

  private generateBallTextures() {
    for (let id = 1; id <= 9; id++) {
      const graphics = this.make.graphics({ x: 0, y: 0 });
      graphics.fillStyle(KRUGOS_COLOR_MAP[id], 1);
      graphics.fillCircle(this.cellSize / 2, this.cellSize / 2, this.cellSize * 0.4);
      graphics.lineStyle(2, 0x000000, 0.2);
      graphics.strokeCircle(this.cellSize / 2, this.cellSize / 2, this.cellSize * 0.4);
      graphics.generateTexture(`krug_${id}`, this.cellSize, this.cellSize);
      graphics.destroy();
    }
  }

  private initSudoku(difficulty: 'easy' | 'medium' | 'hard' | 'expert') {
    const sudoku = getSudoku(difficulty);
    const parse = (str: string) => {
      const res: string[][] = [];
      for (let i = 0; i < 9; i++) res.push(str.substring(i * 9, i * 9 + 9).split(''));
      return res;
    };
    this.board = parse(sudoku.puzzle);
    this.initialBoard = JSON.parse(JSON.stringify(this.board));
    this.solution = parse(sudoku.solution);
    this.currentBalls = Array.from({ length: 9 }, () => new Array(9).fill(null));
  }

  private drawKrugosBoard() {
    for (let row = 0; row < 9; row++) {
      for (let col = 0; col < 9; col++) {
        const x = this.startX + col * this.cellSize;
        const y = this.startY + row * this.cellSize;
        const isDark = (Math.floor(row / 3) + Math.floor(col / 3)) % 2 === 1;

        const rect = this.add.rectangle(x, y, this.cellSize, this.cellSize, isDark ? 0xf9f9f9 : 0xffffff)
          .setOrigin(0).setStrokeStyle(1, 0x000000, 0.1).setInteractive();

        rect.on('pointerdown', () => this.handleCellClick(row, col, rect));

        const val = this.board[row][col];
        if (val !== '-') {
          this.renderBall(row, col, parseInt(val, 10), false);
        }
      }
    }
  }

  private drawSelectionPanel() {
    const { width, height } = this.scale;
    const panelY = height * 0.85;
    const spacing = width / 10;

    for (let id = 1; id <= 9; id++) {
      const x = spacing * id;
      const btn = this.add.image(x, panelY, `krug_${id}`)
        .setInteractive().setScale(0.9);
      
      btn.on('pointerover', () => btn.setScale(1.1));
      btn.on('pointerout', () => btn.setScale(0.9));
      btn.on('pointerdown', () => this.placeKrugos(id));
    }
  }

  private handleCellClick(row: number, col: number, rect: Phaser.GameObjects.Rectangle) {
    if (this.initialBoard[row][col] !== '-') return;
    if (this.selectedCell) this.selectedCell.rect.setFillStyle(this.selectedCell.rect.fillColor === 0xf9f9f9 ? 0xf9f9f9 : 0xffffff);
    
    rect.setFillStyle(0xd1e9ff);
    this.selectedCell = { row, col, rect };
  }

  private renderBall(row: number, col: number, id: number, isUserAction: boolean) {
    const x = this.startX + col * this.cellSize + this.cellSize / 2;
    const y = this.startY + row * this.cellSize + this.cellSize / 2;
    
    if (this.currentBalls[row][col]) this.currentBalls[row][col]?.destroy();
    
    const ball = this.add.image(x, y, `krug_${id}`);
    if (isUserAction) ball.setAlpha(0.9).setScale(0.8).setData('tween', this.tweens.add({
      targets: ball, scale: 1, duration: 100
    }));
    
    this.currentBalls[row][col] = ball;
    this.board[row][col] = id.toString();
  }

  private placeKrugos(id: number) {
    if (!this.selectedCell) return;
    this.renderBall(this.selectedCell.row, this.selectedCell.col, id, true);
    this.checkWinCondition();
  }

  private checkWinCondition() {
    const isComplete = this.board.every(row => row.every(cell => cell !== '-'));
    if (!isComplete) return;

    const isCorrect = JSON.stringify(this.board) === JSON.stringify(this.solution);
    if (isCorrect) alert('Вы победили в Krugos!');
  }
}