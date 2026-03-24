import React, { useState } from 'react';

interface MainMenuProps {
  onStartGame: (difficulty: 'easy' | 'medium' | 'hard' | 'expert', loadFromSave: boolean) => void;
}

interface NeoButtonProps {
  onClick: () => void;
  children: React.ReactNode;
  variant?: 'primary' | 'secondary';
}

const NeoButton: React.FC<NeoButtonProps> = ({ onClick, children, variant = 'primary' }) => {
  const baseStyle = "w-64 py-4 rounded-3xl font-bold text-xl tracking-wide transition-all duration-200 active:scale-95 flex justify-center items-center";
  
  const variantStyle = variant === 'primary'
    ? "bg-cyan-400 text-white shadow-[6px_6px_12px_#b8b9be,-6px_-6px_12px_#ffffff] active:shadow-[inset_4px_4px_8px_rgba(0,0,0,0.1),inset_-4px_-4px_8px_rgba(255,255,255,0.3)]"
    : "bg-[#e0e5ec] text-gray-600 shadow-[6px_6px_12px_#b8cedc,-6px_-6px_12px_#ffffff] active:shadow-[inset_4px_4px_8px_#b8cedc,inset_-4px_-4px_8px_#ffffff]";

  return (
    <button onClick={onClick} className={`${baseStyle} ${variantStyle}`}>
      {children}
    </button>
  );
};

export const MainMenu: React.FC<MainMenuProps> = ({ onStartGame }) => {
  const [view, setView] = useState<'main' | 'difficulty'>('main');
  
  const [hasSave] = useState(() => {
    return localStorage.getItem('krugos_save_data') !== null;
  });

  return (
    <div className="absolute inset-0 z-50 flex flex-col items-center justify-center w-full h-full">
      <h1 className="text-6xl font-extrabold text-gray-700 mb-12 drop-shadow-md tracking-tighter">
        KRUGOS
      </h1>

      <div className="flex flex-col gap-6">
        
        {view === 'main' && (
          <>
            {hasSave && (
              <NeoButton variant="secondary" onClick={() => onStartGame('easy', true)}>
                ПРОДОЛЖИТЬ
              </NeoButton>
            )}
            
            <NeoButton variant="primary" onClick={() => setView('difficulty')}>
              НОВАЯ ИГРА
            </NeoButton>
          </>
        )}

        {view === 'difficulty' && (
          <div className="flex flex-col items-center gap-4 animate-fade-in">
            <p className="text-gray-500 font-semibold mb-2">ВЫБЕРИТЕ СЛОЖНОСТЬ</p>
            
            <NeoButton variant="primary" onClick={() => onStartGame('easy', false)}>ЛЕГКО</NeoButton>
            <NeoButton variant="primary" onClick={() => onStartGame('medium', false)}>НОРМА</NeoButton>
            <NeoButton variant="primary" onClick={() => onStartGame('hard', false)}>СЛОЖНО</NeoButton>
            <NeoButton variant="primary" onClick={() => onStartGame('expert', false)}>ЭКСПЕРТ</NeoButton>
            
            <div className="mt-4">
              <button 
                onClick={() => setView('main')}
                className="text-gray-400 font-bold hover:text-gray-600 transition-colors"
              >
                ОТМЕНА
              </button>
            </div>
          </div>
        )}

      </div>
    </div>
  );
};