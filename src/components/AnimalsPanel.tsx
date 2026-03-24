import type { ISelectionCounters } from "../game/types";

interface AnimalsPanelProps {
  counters: ISelectionCounters;
}

const AnimalsPanel = ({ counters }: AnimalsPanelProps) => {
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
            disabled={true}
            className={`w-12 h-12 
            bg-gray-300
            rounded-lg
            `}
        >
            Hint
        </button>
        <span className={`font-bold text-[16px] text-gray-700`}>
            3
        </span>
        </div>
    </div>
  );
};

export default AnimalsPanel;