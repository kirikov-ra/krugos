import * as Phaser from 'phaser';
import { getSudoku } from 'sudoku-gen';

const STORAGE_KEY = 'krugos_save_data';

export class GameScene extends Phaser.Scene {
  private board: string[][] = [];
  private initialBoard: string[][] = []; 
  private solution: string[][] = [];
  private currentBalls: (Phaser.GameObjects.Image | null)[][] = [];
  private cellRects: Phaser.GameObjects.Rectangle[][] = [];
  private selectedCell: { row: number, col: number } | null = null;
  private highlightedId: number | null = null;

  private readonly gridSize = 9;
  private cellSize = 0;
  private startX = 0;
  private startY = 0;

  private currentPuzzleStr: string = '';
  private currentSolutionStr: string = '';

  private isGameOver: boolean = false;
  private isPaused: boolean = false; 
  
  private timeRemaining: number = 1200;
  private timerText!: Phaser.GameObjects.Text;
  private timerEvent!: Phaser.Time.TimerEvent;

  private lives: number = 3;
  private heartImages: Phaser.GameObjects.Image[] = [];

  private selectionButtons: Record<number, Phaser.GameObjects.Image> = {};
  private selectionCounters: Record<number, Phaser.GameObjects.Text> = {};
  
  private gameDifficulty: 'easy' | 'medium' | 'hard' | 'expert' = 'easy';

  constructor() { super('GameScene'); }

  init(data: { difficulty?: 'easy' | 'medium' | 'hard' | 'expert'; loadFromStorage?: boolean; puzzle?: string; solution?: string } = {}) {
    if (data.loadFromStorage) {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved) {
        try {
          const saveState = JSON.parse(saved);
          this.currentPuzzleStr = saveState.currentPuzzleStr;
          this.currentSolutionStr = saveState.currentSolutionStr;
          
          this.board = JSON.parse(JSON.stringify(saveState.board));
          this.initialBoard = JSON.parse(JSON.stringify(saveState.board)); 
          
          this.lives = saveState.lives;
          this.timeRemaining = saveState.timeRemaining;
          this.gameDifficulty = saveState.difficulty;
          return; 
        } catch (e) {
          console.error("Save load failed:", e);
        }
      }
    }

    if (data.puzzle && data.solution) {
      this.currentPuzzleStr = data.puzzle;
      this.currentSolutionStr = data.solution;
    } else {
      this.currentPuzzleStr = '';
      this.currentSolutionStr = '';
    }

    this.gameDifficulty = data.difficulty || 'easy';
    this.lives = 3;
    this.timeRemaining = 1200;
  }

  preload() {
    this.load.setPath('assets/balls'); 
    for (let id = 1; id <= 9; id++) {
      this.load.image(`ball_${id}`, `ball_${id}.png`);
    }
  }

  private generateHeartTextures() {
    if (this.textures.exists('heart_filled')) return;
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

  create() {
    const { width, height } = this.scale;
    const maxGridWidth = width * 0.85;
    const maxGridHeight = height * 0.6;
    this.cellSize = Math.min(maxGridWidth / 9, maxGridHeight / 9);
    this.startX = (width - this.cellSize * 9) / 2;
    this.startY = height * 0.15;

    this.isGameOver = false;
    this.isPaused = false;
    this.heartImages = [];
    this.selectionButtons = {};
    this.selectionCounters = {};
    this.selectedCell = null;
    this.highlightedId = null;
    this.cellRects = Array.from({ length: 9 }, () => new Array(9).fill(null));

    this.generateHeartTextures();
    this.initSudoku(this.gameDifficulty); 

    this.add.text(width / 2, height * 0.05, 'KRUGOS', {
      fontSize: `${Math.floor(this.cellSize * 0.8)}px`, color: '#333', fontStyle: 'bold'
    }).setOrigin(0.5);

    const pauseBtnSize = this.cellSize * 0.6;
    const uiY = height * 0.05;
    const pauseBtnBg = this.add.rectangle(this.startX, uiY, pauseBtnSize, pauseBtnSize, 0xffffff)
      .setOrigin(0, 0.5).setStrokeStyle(2, 0x000000).setInteractive();
    
    this.add.text(this.startX + pauseBtnSize / 2, uiY, 'II', {
      fontSize: '20px', color: '#000', fontStyle: 'bold'
    }).setOrigin(0.5);
    pauseBtnBg.on('pointerdown', () => this.pauseGame());

    const livesXStart = this.startX + pauseBtnSize + 15;
    const heartSpacing = this.cellSize * 0.8;
    for (let i = 0; i < 3; i++) {
      const x = livesXStart + i * heartSpacing;
      const texture = this.lives > i ? 'heart_filled' : 'heart_outline'; 
      const heart = this.add.image(x, uiY, texture).setOrigin(0, 0.5);
      this.heartImages.push(heart);
    }

    this.timerText = this.add.text(this.startX + this.cellSize * 9, uiY, this.formatTime(this.timeRemaining), {
      fontSize: `${Math.floor(this.cellSize * 0.5)}px`, color: '#d32f2f', fontStyle: 'bold'
    }).setOrigin(1, 0.5);

    this.timerEvent = this.time.addEvent({
      delay: 1000, callback: this.onTimerTick, callbackScope: this, loop: true
    });

    this.drawKrugosBoard();
    this.drawSelectionPanel();
    this.saveGameState();
  }

  private saveGameState() {
    if (this.isGameOver) return;
    const saveData = {
      currentPuzzleStr: this.currentPuzzleStr,
      currentSolutionStr: this.currentSolutionStr,
      board: this.board,
      lives: this.lives,
      timeRemaining: this.timeRemaining,
      difficulty: this.gameDifficulty
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(saveData));
  }

  private clearSave() {
    localStorage.removeItem(STORAGE_KEY);
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
    const overlay = this.add.rectangle(0, 0, width, height, 0x000000, 0.7).setOrigin(0).setInteractive();
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
    const btnContinueBg = this.add.rectangle(modalX, btnContinueY, btnW, btnH, 0x4dd0e1).setStrokeStyle(2, 0x000000).setInteractive();
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
    const btnNewBg = this.add.rectangle(modalX, btnNewY, btnW, btnH, 0xe0e0e0).setStrokeStyle(2, 0x000000).setInteractive();
    const btnNewText = this.add.text(modalX, btnNewY, 'В МЕНЮ', {
      fontSize: '18px', color: '#000', fontStyle: 'bold'
    }).setOrigin(0.5);
    pauseContainer.add([btnNewBg, btnNewText]);

    btnNewBg.on('pointerover', () => btnNewBg.setFillStyle(0xd0d0d0));
    btnNewBg.on('pointerout', () => btnNewBg.setFillStyle(0xe0e0e0));
    btnNewBg.on('pointerdown', () => {
      const exitCallback = this.registry.get('onExitToMenu');
      if (exitCallback) exitCallback();
    });
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
    if (this.board.length === 0) {
      this.board = parse(this.currentPuzzleStr);
      this.initialBoard = JSON.parse(JSON.stringify(this.board));
    }
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
    
    if (this.timeRemaining % 10 === 0) {
      this.saveGameState(); 
    }

    if (this.timeRemaining <= 0) {
      this.isGameOver = true;
      this.timerEvent.remove();
      this.clearSave();
      this.showGameOverModal('time');
    }
  }

  private subtractLife() {
    if (this.isGameOver || this.isPaused) return;
    this.lives--;
    const heartToUpdate = this.heartImages[this.lives];
    if (heartToUpdate) {
        this.tweens.add({
            targets: heartToUpdate, alpha: 0, duration: 100, yoyo: true, repeat: 2,
            onComplete: () => {
                heartToUpdate.setTexture('heart_outline');
                heartToUpdate.setAlpha(1);
                if (this.lives <= 0) {
                    this.clearSave();
                    this.showGameOverModal('no_lives');
                } else {
                    this.saveGameState();
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
        if (this.board[r][c] === '-') emptyCount++;
        else if (!isStartingCell) userFilledCount++;
      }
    }

    this.add.rectangle(0, 0, width, height, 0x000000, 0.7).setOrigin(0).setDepth(100).setInteractive();
    const modalW = width * 0.85;
    const modalH = 420;
    const modalX = width / 2;
    const modalY = height / 2;
    const bg = this.add.graphics().setDepth(101);
    bg.fillStyle(0xffffff, 1);
    bg.fillRoundedRect(modalX - modalW / 2, modalY - modalH / 2, modalW, modalH, 16);
    bg.lineStyle(4, 0x000000, 1);
    bg.strokeRoundedRect(modalX - modalW / 2, modalY - modalH / 2, modalW, modalH, 16);

    const titleText = reason === 'win' ? 'ВЫ ПОБЕДИЛИ!' : reason === 'time' ? 'ВРЕМЯ ВЫШЛО!' : 'ЖИЗНИ ЗАКОНЧИЛИСЬ!';
    const titleColor = reason === 'win' ? '#388e3c' : '#d32f2f';

    this.add.text(modalX, modalY - modalH / 2 + 40, titleText, {
      fontSize: '26px', color: titleColor, fontStyle: 'bold'
    }).setOrigin(0.5).setDepth(102);

    let statsText = `Осталось жизней: ${this.lives}\nВремя: ${this.formatTime(this.timeRemaining)}\nЗаполнено клеток: ${userFilledCount}`;
    if (reason !== 'win') statsText += `\nОсталось пустых: ${emptyCount}`;

    this.add.text(modalX, modalY - 40, statsText, {
      fontSize: '20px', color: '#333', align: 'center', lineSpacing: 10
    }).setOrigin(0.5).setDepth(102);

    const btnW = modalW * 0.8;
    const btnH = 55;
    const btnRestartY = modalY + 60;
    
    const btnRestartBg = this.add.rectangle(modalX, btnRestartY, btnW, btnH, 0xe0e0e0).setDepth(102).setStrokeStyle(2, 0x000000).setInteractive();
    this.add.text(modalX, btnRestartY, 'ПОВТОРИТЬ ПАРТИЮ', { fontSize: '18px', color: '#000', fontStyle: 'bold' }).setOrigin(0.5).setDepth(103);
    btnRestartBg.on('pointerdown', () => { this.scene.restart({ puzzle: this.currentPuzzleStr, solution: this.currentSolutionStr }); });

    const btnNewY = modalY + 140;
    const btnNewBg = this.add.rectangle(modalX, btnNewY, btnW, btnH, 0x4dd0e1).setDepth(102).setStrokeStyle(2, 0x000000).setInteractive();
    this.add.text(modalX, btnNewY, 'В МЕНЮ', { fontSize: '18px', color: '#000', fontStyle: 'bold' }).setOrigin(0.5).setDepth(103);
    btnNewBg.on('pointerdown', () => {
      const exitCallback = this.registry.get('onExitToMenu');
      if (exitCallback) exitCallback();
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

        this.cellRects[row][col] = rect;
        rect.on('pointerdown', () => this.handleCellClick(row, col));

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
      gridGraphics.moveTo(this.startX + offset, this.startY); gridGraphics.lineTo(this.startX + offset, this.startY + totalSize);
      gridGraphics.moveTo(this.startX, this.startY + offset); gridGraphics.lineTo(this.startX + totalSize, this.startY + offset);
    }
    gridGraphics.strokePath(); gridGraphics.setDepth(10); 
  }

  private updateCellBackgrounds() {
    for (let row = 0; row < 9; row++) {
      for (let col = 0; col < 9; col++) {
        const isDark = (Math.floor(row / 3) + Math.floor(col / 3)) % 2 === 1;
        let color = isDark ? 0xf9f9f9 : 0xffffff;

        if (this.selectedCell && this.selectedCell.row === row && this.selectedCell.col === col) {
          color = 0xd1e9ff;
        } 
        else if (this.highlightedId !== null && this.board[row][col] === this.highlightedId.toString()) {
          color = 0xfff59d;
        }

        this.cellRects[row][col].setFillStyle(color);
      }
    }
  }

  private drawSelectionPanel() {
    const { width, height } = this.scale;
    const panelY = height * 0.85;
    const spacing = width / 10;

    for (let id = 1; id <= 9; id++) {
      const x = spacing * id;
      const btn = this.add.image(x, panelY, `ball_${id}`).setInteractive();
      
      btn.setDisplaySize(this.cellSize * 0.9, this.cellSize * 0.9);
      
      const baseScale = btn.scale;

      this.selectionButtons[id] = btn;
      
      btn.on('pointerover', () => { 
        if (btn.input?.enabled) {
          this.tweens.add({ 
            targets: btn, 
            scale: baseScale * 1.1, 
            y: panelY - 5, 
            duration: 150, 
            ease: 'Sine.easeOut' 
          });
        } 
      });
      
      btn.on('pointerout', () => { 
        if (btn.input?.enabled) {
          this.tweens.add({ 
            targets: btn, 
            scale: baseScale, 
            y: panelY, 
            duration: 150, 
            ease: 'Sine.easeOut' 
          });
        } 
      });
      
      btn.on('pointerdown', () => {
        if (btn.input?.enabled) {
          this.tweens.add({ 
            targets: btn, 
            scale: baseScale * 0.95, 
            duration: 50, 
            yoyo: true 
          });
          this.placeKrugos(id);
        }
      });

      const counterText = this.add.text(x, panelY + this.cellSize * 0.55, '9', {
        fontSize: '16px', color: '#333', fontStyle: 'bold'
      }).setOrigin(0.5);
      
      this.selectionCounters[id] = counterText;
    }

    this.updateSelectionCounters();
  }

  private updateSelectionCounters() {
    const counts: Record<number, number> = { 1:0, 2:0, 3:0, 4:0, 5:0, 6:0, 7:0, 8:0, 9:0 };
    for (let r = 0; r < 9; r++) {
      for (let c = 0; c < 9; c++) {
        const val = this.initialBoard[r][c];
        if (val !== '-') counts[parseInt(val, 10)]++;
      }
    }
    for (let id = 1; id <= 9; id++) {
      const remaining = 9 - counts[id];
      const textObj = this.selectionCounters[id];
      const btnObj = this.selectionButtons[id];
      if (textObj && btnObj) {
        textObj.setText(remaining.toString());
        if (remaining <= 0) { textObj.setColor('#aaaaaa'); btnObj.setAlpha(0.3); btnObj.disableInteractive(); } 
        else { textObj.setColor('#333333'); btnObj.setAlpha(1); btnObj.setInteractive(); }
      }
    }
  }

  private handleCellClick(row: number, col: number) {
    if (this.isGameOver || this.isPaused) return; 
    
    if (this.board[row][col] !== '-') {
      const id = parseInt(this.board[row][col], 10);
      this.highlightedId = this.highlightedId === id ? null : id;
      this.selectedCell = null;
    } else {
      this.selectedCell = { row, col };
      this.highlightedId = null;
    }
    this.updateCellBackgrounds();
  }

  private renderBall(row: number, col: number, id: number, state: 'initial' | 'success' | 'error') {
    const x = this.startX + col * this.cellSize + this.cellSize / 2;
    const y = this.startY + row * this.cellSize + this.cellSize / 2;
    
    if (this.currentBalls[row][col]) this.currentBalls[row][col]?.destroy();
    
    const textureName = `ball_${id}`;
    const ball = this.add.image(x, y, textureName);

    ball.setDisplaySize(this.cellSize * 0.9, this.cellSize * 0.9);
    
    const targetScale = ball.scale; 

    if (state === 'success') {
      ball.setScale(0); 
      this.tweens.add({ targets: ball, scale: targetScale, duration: 500, ease: 'Back.out' });
      this.currentBalls[row][col] = ball;
    } 
    else if (state === 'error') {
      this.currentBalls[row][col] = ball; 
      ball.postFX.addGlow(0xff1744, 4); 

      this.tweens.add({ 
        targets: ball, x: x + 5, duration: 50, yoyo: true, repeat: 3,
        onComplete: () => {
          this.tweens.add({
            targets: ball, alpha: 0, delay: 500, duration: 300,
            onComplete: () => {
              ball.destroy();
              if (this.currentBalls[row][col] === ball) {
                this.currentBalls[row][col] = null; 
              }
            }
          });
        }
      });
    } 
    else {
      this.currentBalls[row][col] = ball;
    }
  }

  private placeKrugos(id: number) {
    if (this.isGameOver || this.isPaused) return;

    if (!this.selectedCell) {
      this.highlightedId = this.highlightedId === id ? null : id;
      this.updateCellBackgrounds();
      return;
    }

    const { row, col } = this.selectedCell;
    const isValidMove = this.isMoveValid(row, col, id);

    this.highlightedId = id;

    if (isValidMove) {
      this.board[row][col] = id.toString();
      this.initialBoard[row][col] = id.toString(); 
      this.renderBall(row, col, id, 'success');
      
      this.selectedCell = null;
      this.updateSelectionCounters();
      this.saveGameState(); 
    } else {
      this.subtractLife();
      this.renderBall(row, col, id, 'error');
    }

    this.updateCellBackgrounds();
    this.checkWinCondition();
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
      this.clearSave();
      this.showGameOverModal('win');
    }
  }

  private isMoveValid(row: number, col: number, id: number): boolean {
    return this.solution[row][col] === id.toString();
  }
}