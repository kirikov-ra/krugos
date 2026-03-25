import React from 'react';
import type { IGameOverData } from '../game/types';

interface GameOverModalProps {
  data: IGameOverData;
  onRestart: () => void;
}

const GameOverModal: React.FC<GameOverModalProps> = ({ data, onRestart }) => {
  const { reason, stats } = data;

  const formatTime = (seconds: number) => {
    const m = Math.floor(Math.max(0, seconds) / 60);
    const s = Math.max(0, seconds) % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  const isWin = reason === 'win';
  const titleText = isWin ? 'ВЫ ПОБЕДИЛИ!' : reason === 'time' ? 'ВРЕМЯ ВЫШЛО!' : 'ЖИЗНИ ЗАКОНЧИЛИСЬ!';
  const titleColor = isWin ? 'text-green-600' : 'text-red-500';

  const handleMenu = () => window.dispatchEvent(new CustomEvent('krugos-exit-to-menu'));

  return (
    <div className="absolute inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm rounded-xl">
      <div className="w-[85%] max-w-[320px] bg-[#e0e5ec] rounded-2xl p-6 flex flex-col items-center border border-white/50">
        
        <h2 className={`text-2xl font-black mb-4 text-center drop-shadow-sm ${titleColor}`}>
          {titleText}
        </h2>

        <div className="w-full bg-[#e0e5ec] rounded-xl p-4 mb-6 flex flex-col gap-2 text-gray-700 font-bold text-center text-sm">
          <p>Осталось жизней: <span className="text-red-500">{stats.lives}</span></p>
          <p>Время: <span>{formatTime(stats.timeRemaining)}</span></p>
          <p>Заполнено клеток: <span className="text-blue-600">{stats.userFilledCount}</span></p>
          {!isWin && <p>Осталось пустых: <span className="text-gray-500">{stats.emptyCount}</span></p>}
        </div>

        <button 
          onClick={onRestart}
          className="w-full py-3 mb-4 rounded-xl bg-[#4dd0e1] font-bold text-gray-800 shadow-neo-flat active:shadow-neo-inset transition-all"
        >
          ПОВТОРИТЬ ПАРТИЮ
        </button>

        <button 
          onClick={handleMenu}
          className="w-full py-3 rounded-xl bg-[#e0e0e0] font-bold text-gray-700 shadow-neo-flat active:shadow-neo-inset transition-all"
        >
          В МЕНЮ
        </button>
      </div>
    </div>
  );
};

export default GameOverModal;