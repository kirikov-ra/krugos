import React from 'react';
import NeoButton from './NeoButton';

const PauseModal: React.FC = () => {
  const handleResume = () => {
    window.dispatchEvent(new CustomEvent('krugos-resume-game'));
  };

  const handleMenu = () => {
    window.dispatchEvent(new CustomEvent('krugos-exit-to-menu'));
  };

  return (
    <div className="absolute inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
      <div className="w-[80%] max-w-[300px] bg-[#e0e5ec] rounded-2xl p-6 flex flex-col gap-4 items-center border border-white/40">
        
        <h2 className="text-3xl font-black text-gray-700 mb-6 drop-shadow-sm">
          ПАУЗА
        </h2>

        <NeoButton 
            onClick={handleResume}
            variant="primary"
        >
          ПРОДОЛЖИТЬ
        </NeoButton>

        <NeoButton 
          onClick={handleMenu}
           variant="secondary"
        >
          В МЕНЮ
        </NeoButton>
        
      </div>
    </div>
  );
};

export default PauseModal;