import type { IHudData } from "../game/types";
import Logotype from "./Logotype";

const InfoPanel = ({ hud }: { hud: IHudData }) => {

  const formatTime = (seconds: number) => {
    const m = Math.floor(Math.max(0, seconds) / 60);
    const s = Math.max(0, seconds) % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  return (
    <div className="w-full max-w-sm">
        <div className="w-full flex justify-between items-center ">
          <button
              onClick={() => window.dispatchEvent(new CustomEvent('pause-krugos-game'))}
              className="w-12 h-12 bg-white/40 border-2 border-white/60 rounded-full font-black text-gray-700 shadow-md active:scale-95 transition-transform flex items-center justify-center text-2xl"
          >
              II
          </button>

          <Logotype height={6}/>

          <button
            disabled={true}
            className="w-12 h-12 opacity-0 bg-white/40 border-2 border-white/60 rounded-xl font-black text-gray-700 shadow-md active:scale-95 transition-transform flex items-center justify-center text-xl"
          >
              
          </button>
        </div>

        <div className="w-full flex justify-between gap-2 mt-4 p-2 bg-transparent rounded-xl border border-white/50 shadow-lg max-w-sm">
            <div className="flex items-center gap-2">
              <img 
                src={`/assets/ui/clock.png`} 
                alt={`clock`} 
                className="w-7 h-7 object-contain drop-shadow-md  relative z-1" 
              />
              <span className="text-red-300 font-extrabold text-2xl tracking-wide">
                {formatTime(hud.time)}
              </span>
            </div>
            
            <div className="flex gap-1 items-center">
            {[1, 2, 3].map((i) => {
                const isLost = i > hud.lives;
                
                return (
                <div 
                    key={i} 
                    className={`w-7 h-7 transition-all duration-300 ${isLost ? 'opacity-30 grayscale scale-90' : 'opacity-100 scale-100'}`}
                >
                    <img 
                    src="/assets/ui/heart.png" 
                    alt="heart" 
                    className="w-full h-full object-contain drop-shadow-[3px_3px_3px] drop-shadow-rose-500/30" 
                    />
                </div>
                );
            })}
            </div>
        </div>
        </div>
  )
}

export default InfoPanel