import React from 'react';

const PauseModal: React.FC = () => {
  const handleResume = () => {
    window.dispatchEvent(new CustomEvent('krugos-resume-game'));
  };

  const handleMenu = () => {
    window.dispatchEvent(new CustomEvent('krugos-exit-to-menu'));
  };

  return (
    <div className="absolute inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm rounded-xl">
      <div className="w-[80%] max-w-[300px] bg-[#e0e5ec] rounded-2xl p-6 flex flex-col items-center border border-white/40">
        
        <h2 className="text-3xl font-black text-gray-700 mb-6 drop-shadow-sm">
          ПАУЗА
        </h2>

        <button 
          onClick={handleResume}
          className="w-full py-3 mb-4 rounded-xl bg-[#4dd0e1] font-bold text-gray-800 shadow-neo-flat active:shadow-neo-inset transition-all"
        >
          ПРОДОЛЖИТЬ
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

export default PauseModal;