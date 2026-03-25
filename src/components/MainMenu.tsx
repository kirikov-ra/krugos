import { useState } from 'react';
import Logotype from './Logotype';
import NeoButton from './NeoButton';

interface MainMenuProps {
  onStartGame: (difficulty: 'easy' | 'medium' | 'hard' | 'expert', loadFromSave: boolean) => void;
}

export const MainMenu = ({ onStartGame } : MainMenuProps) => {
  const [view, setView] = useState<'main' | 'difficulty'>('main');
  
  const [hasSave] = useState(() => {
    return localStorage.getItem('krugos_save_data') !== null;
  });

  return (
    <div className="flex flex-col items-center justify-center w-full h-full overflow-y-auto gap-6">
      <Logotype height={8}/>

      <div className="flex flex-col items-center gap-6">

        
        
        {view === 'main' && (
          <>
            <img src="/assets/ui/banner.png" alt="banner" />
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
            <NeoButton variant="primary" onClick={() => onStartGame('easy', false)}>ЛЕГКО</NeoButton>
            <NeoButton variant="primary" onClick={() => onStartGame('medium', false)}>НОРМА</NeoButton>
            <NeoButton variant="primary" onClick={() => onStartGame('hard', false)}>СЛОЖНО</NeoButton>
            <NeoButton variant="primary" onClick={() => onStartGame('expert', false)}>ЭКСПЕРТ</NeoButton>
            
            <div>
              <NeoButton 
                variant="secondary"
                onClick={() => setView('main')}
              >
                ОТМЕНА
              </NeoButton>
            </div>
          </div>
        )}

      </div>
    </div>
  );
};