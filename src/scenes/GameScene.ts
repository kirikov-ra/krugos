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

  private currentPuzzleStr: string = '';
  private currentSolutionStr: string = '';

  private isGameOver: boolean = false;
  private isPaused: boolean = false; 
  
  private timeRemaining: number = 300;
  private timerText!: Phaser.GameObjects.Text;
  private timerEvent!: Phaser.Time.TimerEvent;

  private lives: number = 3;
  private heartImages: Phaser.GameObjects.Image[] = [];

  constructor() { super('GameScene'); }

  init(data: any) {
    if (data && data.puzzle && data.solution) {
      this.currentPuzzleStr = data.puzzle;
      this.currentSolutionStr = data.solution;
    } else {
      this.currentPuzzleStr = '';
      this.currentSolutionStr = '';
    }
  }

  create() {
    const { width, height } = this.scale;

    const maxGridWidth = width * 0.85;
    const maxGridHeight = height * 0.6;
    this.cellSize = Math.min(maxGridWidth / 9, maxGridHeight / 9);

    this.startX = (width - this.cellSize * 9) / 2;
    this.startY = height * 0.15;

    this.isGameOver = false;
    this.isPaused = false;
    this.timeRemaining = 1200;
    this.lives = 3;
    this.heartImages = [];

    this.generateBallTextures();
    this.initSudoku('easy');

    this.add.text(width / 2, height * 0.05, 'KRUGOS', {
      fontSize: `${Math.floor(this.cellSize * 0.8)}px`,
      color: '#333',
      fontStyle: 'bold'
    }).setOrigin(0.5);

    const pauseBtnSize = this.cellSize * 0.6;
    const uiY = height * 0.05;
    
    const pauseBtnBg = this.add.rectangle(this.startX, uiY, pauseBtnSize, pauseBtnSize, 0xffffff)
      .setOrigin(0, 0.5).setStrokeStyle(2, 0x000000).setInteractive();
    
    const pauseIcon = this.add.text(this.startX + pauseBtnSize / 2, uiY, 'II', {
      fontSize: '20px', color: '#000', fontStyle: 'bold'
    }).setOrigin(0.5);

    pauseBtnBg.on('pointerdown', () => this.pauseGame());

    const livesXStart = this.startX + pauseBtnSize + 15;
    const heartSpacing = this.cellSize * 0.8;
    for (let i = 0; i < 3; i++) {
      const x = livesXStart + i * heartSpacing;
      const heart = this.add.image(x, uiY, 'heart_filled').setOrigin(0, 0.5);
      this.heartImages.push(heart);
    }

    this.timerText = this.add.text(this.startX + this.cellSize * 9, uiY, this.formatTime(this.timeRemaining), {
      fontSize: `${Math.floor(this.cellSize * 0.5)}px`,
      color: '#d32f2f',
      fontStyle: 'bold'
    }).setOrigin(1, 0.5);

    this.timerEvent = this.time.addEvent({
      delay: 1000, callback: this.onTimerTick, callbackScope: this, loop: true
    });

    this.drawKrugosBoard();
    this.drawSelectionPanel();
  }

  private pauseGame() {
    if (this.isGameOver || this.isPaused) return;
    
    this.isPaused = true;
    this.timerEvent.paused = true;
    
    this.showPauseModal();
  }

  private showPauseModal() {
    const { width, height } = this.scale;
    const modalX = width / 2;
    const modalY = height / 2;
    const modalW = width * 0.8;
    const modalH = 260;

    const pauseContainer = this.add.container(0, 0).setDepth(200);

    const overlay = this.add.rectangle(0, 0, width, height, 0x000000, 0.7)
      .setOrigin(0).setInteractive();
    pauseContainer.add(overlay);

    const bg = this.add.graphics();
    bg.fillStyle(0xffffff, 1);
    bg.fillRoundedRect(modalX - modalW / 2, modalY - modalH / 2, modalW, modalH, 16);
    bg.lineStyle(4, 0x000000, 1);
    bg.strokeRoundedRect(modalX - modalW / 2, modalY - modalH / 2, modalW, modalH, 16);
    pauseContainer.add(bg);

    const title = this.add.text(modalX, modalY - modalH / 2 + 40, 'ПАУЗА', {
      fontSize: '26px', color: '#333', fontStyle: 'bold'
    }).setOrigin(0.5);
    pauseContainer.add(title);

    const btnW = modalW * 0.8;
    const btnH = 55;
    
    const btnContinueY = modalY + 10;
    const btnContinueBg = this.add.rectangle(modalX, btnContinueY, btnW, btnH, 0x4dd0e1)
      .setStrokeStyle(2, 0x000000).setInteractive();
    const btnContinueText = this.add.text(modalX, btnContinueY, 'ПРОДОЛЖИТЬ', {
      fontSize: '18px', color: '#000', fontStyle: 'bold'
    }).setOrigin(0.5);
    pauseContainer.add([btnContinueBg, btnContinueText]);

    btnContinueBg.on('pointerover', () => btnContinueBg.setFillStyle(0x26c6da));
    btnContinueBg.on('pointerout', () => btnContinueBg.setFillStyle(0x4dd0e1));
    btnContinueBg.on('pointerdown', () => {
      pauseContainer.destroy();
      this.isPaused = false;
      this.timerEvent.paused = false;
    });

    const btnNewY = modalY + 90;
    const btnNewBg = this.add.rectangle(modalX, btnNewY, btnW, btnH, 0xe0e0e0)
      .setStrokeStyle(2, 0x000000).setInteractive();
    const btnNewText = this.add.text(modalX, btnNewY, 'НОВАЯ ИГРА', {
      fontSize: '18px', color: '#000', fontStyle: 'bold'
    }).setOrigin(0.5);
    pauseContainer.add([btnNewBg, btnNewText]);

    btnNewBg.on('pointerover', () => btnNewBg.setFillStyle(0xd0d0d0));
    btnNewBg.on('pointerout', () => btnNewBg.setFillStyle(0xe0e0e0));
    btnNewBg.on('pointerdown', () => {
      this.currentPuzzleStr = '';
      this.currentSolutionStr = '';
      this.scene.restart({});
    });
  }

  private generateBallTextures() {
    if (this.textures.exists('krug_1')) return;

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

    const lifeSize = this.cellSize * 0.6;
    const lifeFilledG = this.make.graphics({ x: 0, y: 0 });
    lifeFilledG.fillStyle(0xff1744, 1); 
    lifeFilledG.fillRect(0, 0, lifeSize, lifeSize);
    lifeFilledG.generateTexture('heart_filled', lifeSize, lifeSize);
    lifeFilledG.destroy();

    const lifeOutlineG = this.make.graphics({ x: 0, y: 0 });
    lifeOutlineG.lineStyle(3, 0x9e9e9e, 1); 
    lifeOutlineG.strokeRect(1.5, 1.5, lifeSize - 3, lifeSize - 3);
    lifeOutlineG.generateTexture('heart_outline', lifeSize, lifeSize);
    lifeOutlineG.destroy();
  }

  private initSudoku(difficulty: 'easy' | 'medium' | 'hard' | 'expert') {
    if (!this.currentPuzzleStr || !this.currentSolutionStr) {
      const sudoku = getSudoku(difficulty);
      this.currentPuzzleStr = sudoku.puzzle;
      this.currentSolutionStr = sudoku.solution;
    }

    const parse = (str: string) => {
      const res: string[][] = [];
      for (let i = 0; i < 9; i++) res.push(str.substring(i * 9, i * 9 + 9).split(''));
      return res;
    };

    this.board = parse(this.currentPuzzleStr);
    this.initialBoard = JSON.parse(JSON.stringify(this.board));
    this.solution = parse(this.currentSolutionStr);
    this.currentBalls = Array.from({ length: 9 }, () => new Array(9).fill(null));
  }

  private formatTime(seconds: number): string {
    const m = Math.floor(Math.max(0, seconds) / 60);
    const s = Math.max(0, seconds) % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  }

  private onTimerTick() {
    if (this.isGameOver || this.isPaused) return;
    this.timeRemaining--;
    this.timerText.setText(this.formatTime(this.timeRemaining));
    if (this.timeRemaining <= 0) {
      this.isGameOver = true;
      this.timerEvent.remove();
      this.showGameOverModal('time');
    }
  }

  private subtractLife() {
    if (this.isGameOver || this.isPaused) return;
    
    this.lives--;
    
    const heartToUpdate = this.heartImages[this.lives];
    if (heartToUpdate) {
        this.tweens.add({
            targets: heartToUpdate,
            alpha: 0,
            duration: 100,
            yoyo: true,
            repeat: 2,
            onComplete: () => {
                heartToUpdate.setTexture('heart_outline');
                heartToUpdate.setAlpha(1);
                if (this.lives <= 0) {
                    this.showGameOverModal('no_lives');
                }
            }
        });
    }

    if (this.lives <= 0) {
      this.isGameOver = true;
      this.timerEvent.remove();
    }
  }

  private showGameOverModal(reason: 'time' | 'no_lives' | 'win') {
    const { width, height } = this.scale;

    let userFilledCount = 0;
    let emptyCount = 0;
    
    for (let r = 0; r < 9; r++) {
      for (let c = 0; c < 9; c++) {
        const isStartingCell = this.currentPuzzleStr[r * 9 + c] !== '-';
        if (this.board[r][c] === '-') {
          emptyCount++;
        } else if (!isStartingCell) {
          userFilledCount++;
        }
      }
    }

    this.add.rectangle(0, 0, width, height, 0x000000, 0.7)
      .setOrigin(0).setDepth(100).setInteractive();

    const modalW = width * 0.85;
    const modalH = 420;
    const modalX = width / 2;
    const modalY = height / 2;

    const bg = this.add.graphics().setDepth(101);
    bg.fillStyle(0xffffff, 1);
    bg.fillRoundedRect(modalX - modalW / 2, modalY - modalH / 2, modalW, modalH, 16);
    bg.lineStyle(4, 0x000000, 1);
    bg.strokeRoundedRect(modalX - modalW / 2, modalY - modalH / 2, modalW, modalH, 16);

    let titleText = '';
    let titleColor = '';
    if (reason === 'win') {
      titleText = 'ВЫ ПОБЕДИЛИ!';
      titleColor = '#388e3c';
    } else if (reason === 'time') {
      titleText = 'ВРЕМЯ ВЫШЛО!';
      titleColor = '#d32f2f';
    } else {
      titleText = 'ЖИЗНИ ЗАКОНЧИЛИСЬ!';
      titleColor = '#d32f2f';
    }

    this.add.text(modalX, modalY - modalH / 2 + 40, titleText, {
      fontSize: '26px', color: titleColor, fontStyle: 'bold'
    }).setOrigin(0.5).setDepth(102);

    let statsText = 
      `Осталось жизней: ${this.lives}\n` +
      `Время: ${this.formatTime(this.timeRemaining)}\n` +
      `Заполнено клеток: ${userFilledCount}`;

    if (reason !== 'win') {
      statsText += `\nОсталось пустых: ${emptyCount}`;
    }

    this.add.text(modalX, modalY - 40, statsText, {
      fontSize: '20px', color: '#333', align: 'center', lineSpacing: 10
    }).setOrigin(0.5).setDepth(102);

    const btnW = modalW * 0.8;
    const btnH = 55;
    const btnRestartY = modalY + 60;
    
    const btnRestartBg = this.add.rectangle(modalX, btnRestartY, btnW, btnH, 0xe0e0e0)
      .setDepth(102).setStrokeStyle(2, 0x000000).setInteractive();
    this.add.text(modalX, btnRestartY, 'ПОВТОРИТЬ ПАРТИЮ', {
      fontSize: '18px', color: '#000', fontStyle: 'bold'
    }).setOrigin(0.5).setDepth(103);

    btnRestartBg.on('pointerover', () => btnRestartBg.setFillStyle(0xd0d0d0));
    btnRestartBg.on('pointerout', () => btnRestartBg.setFillStyle(0xe0e0e0));
    btnRestartBg.on('pointerdown', () => {
      this.scene.restart({ puzzle: this.currentPuzzleStr, solution: this.currentSolutionStr });
    });

    const btnNewY = modalY + 140;
    const btnNewBg = this.add.rectangle(modalX, btnNewY, btnW, btnH, 0x4dd0e1)
      .setDepth(102).setStrokeStyle(2, 0x000000).setInteractive();
    this.add.text(modalX, btnNewY, 'НОВАЯ ИГРА', {
      fontSize: '18px', color: '#000', fontStyle: 'bold'
    }).setOrigin(0.5).setDepth(103);

    btnNewBg.on('pointerover', () => btnNewBg.setFillStyle(0x26c6da));
    btnNewBg.on('pointerout', () => btnNewBg.setFillStyle(0x4dd0e1));
    btnNewBg.on('pointerdown', () => {
      this.currentPuzzleStr = '';
      this.currentSolutionStr = '';
      this.scene.restart({}); 
    });
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
          this.renderBall(row, col, parseInt(val, 10), 'initial');
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
      const btn = this.add.image(x, panelY, `krug_${id}`).setInteractive().setScale(0.9);
      btn.on('pointerover', () => btn.setScale(1.1));
      btn.on('pointerout', () => btn.setScale(0.9));
      btn.on('pointerdown', () => this.placeKrugos(id));
    }
  }

  private handleCellClick(row: number, col: number, rect: Phaser.GameObjects.Rectangle) {
    if (this.isGameOver || this.isPaused) return;
    if (this.initialBoard[row][col] !== '-') return; 
    
    if (this.selectedCell) this.selectedCell.rect.setFillStyle(this.selectedCell.rect.fillColor === 0xf9f9f9 ? 0xf9f9f9 : 0xffffff);
    rect.setFillStyle(0xd1e9ff);
    this.selectedCell = { row, col, rect };
  }

  private renderBall(row: number, col: number, id: number, state: 'initial' | 'success' | 'error' | 'error-idle') {
    const x = this.startX + col * this.cellSize + this.cellSize / 2;
    const y = this.startY + row * this.cellSize + this.cellSize / 2;
    
    if (this.currentBalls[row][col]) this.currentBalls[row][col]?.destroy();
    
    const isValid = state === 'initial' || state === 'success';
    const textureName = isValid ? `krug_${id}` : `krug_error_${id}`;
    const ball = this.add.image(x, y, textureName);

    if (state === 'success') {
      ball.setScale(0);
      this.tweens.add({ targets: ball, scale: 1, duration: 500, ease: 'Back.out' });
    } else if (state === 'error') {
      this.tweens.add({ targets: ball, x: x + 5, duration: 50, yoyo: true, repeat: 3 });
    } else {
      ball.setScale(1);
    }
    
    this.currentBalls[row][col] = ball;
  }

  private placeKrugos(id: number) {
    if (this.isGameOver || this.isPaused || !this.selectedCell) return;
    const { row, col, rect } = this.selectedCell;

    this.board[row][col] = id.toString();
    const isValidMove = this.isMoveValid(row, col, id);

    if (isValidMove) {
      this.initialBoard[row][col] = id.toString(); 
      this.renderBall(row, col, id, 'success');
      
      const isDark = (Math.floor(row / 3) + Math.floor(col / 3)) % 2 === 1;
      rect.setFillStyle(isDark ? 0xf9f9f9 : 0xffffff);
      this.selectedCell = null;
    } else {
      this.subtractLife();
      this.renderBall(row, col, id, 'error');
    }

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
          if (this.selectedCell && this.selectedCell.row === row && this.selectedCell.col === col) continue;
          this.renderBall(row, col, id, 'error-idle');
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
      this.isGameOver = true;
      this.timerEvent.remove();
      this.showGameOverModal('win');
    }
  }

  private isMoveValid(row: number, col: number, id: number): boolean {
    const valueStr = id.toString();
    return this.solution[row][col] === valueStr;
  }
}