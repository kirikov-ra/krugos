interface NeoButtonProps {
  onClick: () => void;
  children: React.ReactNode;
  variant?: 'primary' | 'secondary';
}

const NeoButton = ({ onClick, children, variant = 'primary' } : NeoButtonProps) => {
  const baseStyle = "cursor-pointer w-64 py-4 rounded-[26px] font-bold border-3 border-[#dafcfb]/50 text-black text-xl tracking-wide transition-all duration-200 active:scale-95 flex justify-center items-center";
  
  const variantStyle = variant === 'primary'
    ? "bg-[#8bbbbb] text-white inset-shadow-[2px_2px_2px_#cff6f5,-2px_-2px_2px_#4a999e] shadow-[6px_6px_12px_#b8b9be,-6px_-6px_12px_#ffffff] active:shadow-[inset_4px_4px_8px_rgba(0,0,0,0.1),inset_-4px_-4px_8px_rgba(255,255,255,0.3)]"
    : "bg-[#e0e5ec] text-gray-600 shadow-[6px_6px_12px_#b8cedc,-6px_-6px_12px_#ffffff] active:shadow-[inset_4px_4px_8px_#306180,inset_-4px_-4px_8px_#ffffff]";

  return (
    <button onClick={onClick} className={`${baseStyle} ${variantStyle}`}>
      {children}
    </button>
  );
};

export default NeoButton;