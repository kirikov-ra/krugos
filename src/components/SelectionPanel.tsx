
export const SelectionPanel = ({ onSelect }: { onSelect: (id: number) => void }) => {
  const balls = [1, 2, 3, 4, 5, 6, 7, 8, 9];
  return (
    <div className="flex gap-2 p-4 bg-white/30 rounded-full shadow-lg backdrop-blur-md border border-white/50 mb-8">
      {balls.map(id => (
        <button 
          key={id}
          onClick={() => onSelect(id)}
          className="w-10 h-10 transition-transform active:scale-90 hover:scale-110"
        >
          <img src={`/assets/balls/ball_${id}.png`} alt={`ball-${id}`} className="w-full h-full object-contain" />
        </button>
      ))}
    </div>
  );
};