import type { ISelectionCounters } from "../game/types";

interface AnimalsPanelProps {
  counters: ISelectionCounters;
  hints: number;
}

const AnimalsPanel = ({ counters, hints }: AnimalsPanelProps) => {
  return (
    <div className="flex flex-wrap justify-center gap-4 p-4 bg-white/30 rounded-lg border border-white/50 shadow-lg backdrop-blur-md max-w-sm">
      {[1, 2, 3, 4, 5, 6, 7, 8, 9].map((id) => {
        const remaining = counters[id] || 0;
        const isDisabled = remaining <= 0;

        return (
          <div key={id} className="flex flex-col items-center gap-1">
            <button
              disabled={isDisabled}
              onClick={() => window.dispatchEvent(new CustomEvent('place-krugos-ball', { detail: id }))}
              className={`w-12 h-12 
                transition-transform 
                active:scale-90 
                hover:scale-110 
                ${isDisabled ? 'opacity-30' : 'opacity-100'}
                bg-gray-300
                rounded-lg
                relative
                `}
            >
              <img src={`/assets/balls/ball_${id}.png`} alt={`ball_${id}`} className="w-full h-full object-contain drop-shadow-md  relative z-1" />
              <img src={`/assets/ui/shadow.png`} alt={`ball_${id}`} className="w-full h-full object-contain absolute z-0 left-0 top-0" />
            </button>
            <span className={`font-bold text-[16px] ${isDisabled ? 'text-gray-400' : 'text-gray-700'}`}>
              {remaining}
            </span>
          </div>
        );
      })}
      <div key={1231245124} className="flex flex-col items-center gap-1">

              <button
                disabled={hints <= 0}
                onClick={() => window.dispatchEvent(new CustomEvent('use-krugos-hint'))}
                className={`relative w-12 h-12 border-2 rounded-lg font-black shadow-md transition-transform flex items-center justify-center text-2xl
                  ${hints > 0 
                    ? 'bg-white/40 border-white/60 text-gray-700 active:scale-95 hover:bg-white/60' 
                    : 'bg-gray-300/40 border-gray-400/50 text-gray-400 opacity-70 cursor-not-allowed'}`}
                title="Подсказка"
              >
                💡
                
              </button>
            <span className={`font-bold text-[16px] text-gray-700`}>
                {hints}
            </span>
        </div>
    </div>
  );
};

export default AnimalsPanel;