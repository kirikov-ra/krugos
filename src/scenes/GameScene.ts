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

      const graphicsError = this.make.graphics({ x: 0, y: 0 });
      graphicsError.fillStyle(0xff1744, 0.9); 
      graphicsError.fillCircle(this.cellSize / 2, this.cellSize / 2, this.cellSize * 0.4);
      graphicsError.lineStyle(3, 0x000000, 0.4); 
      graphicsError.strokeCircle(this.cellSize / 2, this.cellSize / 2, this.cellSize * 0.4);
      graphicsError.generateTexture(`krug_error_${id}`, this.cellSize, this.cellSize);
      graphicsError.destroy();
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
          .setOrigin(0)
          .setStrokeStyle(1, 0x000000, 0.1)
          .setInteractive();

        rect.on('pointerdown', () => this.handleCellClick(row, col, rect));

        const val = this.board[row][col];
        if (val !== '-') {
          this.renderBall(row, col, parseInt(val, 10), true, false);
        }
      }
    }

    const gridGraphics = this.add.graphics();
    gridGraphics.lineStyle(3, 0x000000, 0.6);

    const totalSize = 9 * this.cellSize;

    for (let i = 0; i <= 9; i += 3) {
      const offset = i * this.cellSize;

      gridGraphics.moveTo(this.startX + offset, this.startY);
      gridGraphics.lineTo(this.startX + offset, this.startY + totalSize);

      gridGraphics.moveTo(this.startX, this.startY + offset);
      gridGraphics.lineTo(this.startX + totalSize, this.startY + offset);
    }
    
    gridGraphics.strokePath();
    gridGraphics.setDepth(10); 
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

  private renderBall(row: number, col: number, id: number, isValid: boolean, animate: boolean) {
    const x = this.startX + col * this.cellSize + this.cellSize / 2;
    const y = this.startY + row * this.cellSize + this.cellSize / 2;
    
    if (this.currentBalls[row][col]) this.currentBalls[row][col]?.destroy();
    
    const textureName = isValid ? `krug_${id}` : `krug_error_${id}`;
    const ball = this.add.image(x, y, textureName);

    if (animate) {
      if (!isValid) {
        this.tweens.add({ targets: ball, x: x + 5, duration: 50, yoyo: true, repeat: 3 });
      } else {
        ball.setAlpha(0.9).setScale(0.8);
        this.tweens.add({ targets: ball, scale: 1, duration: 100 });
      }
    }
    
    this.currentBalls[row][col] = ball;
  }

  private placeKrugos(id: number) {
    if (!this.selectedCell) return;
    const { row, col } = this.selectedCell;

    this.board[row][col] = id.toString();
    
    this.refreshBoardVisuals();
    
    this.checkWinCondition();
  }

  private refreshBoardVisuals() {
    for (let row = 0; row < 9; row++) {
      for (let col = 0; col < 9; col++) {
        const val = this.board[row][col];
        
        if (this.initialBoard[row][col] !== '-') continue;

        if (val !== '-') {
          const id = parseInt(val, 10);
          const isValid = this.isMoveValid(row, col, id);
          
          const animate = this.selectedCell !== null && this.selectedCell.row === row && this.selectedCell.col === col;
          
          this.renderBall(row, col, id, isValid, animate);
        }
      }
    }
  }

  private checkWinCondition() {
    const isComplete = this.board.every(row => row.every(cell => cell !== '-'));
    if (!isComplete) return;

    
    const hasErrors = this.board.some((row, r) => 
      row.some((cell, c) => cell !== '-' && cell !== this.solution[r][c])
    );

    if (!hasErrors) {
      alert('Вы победили в Krugos!');
    }
  }

  private isMoveValid(row: number, col: number, id: number): boolean {
    const valueStr = id.toString();
    
    return this.solution[row][col] === valueStr;
  }
}