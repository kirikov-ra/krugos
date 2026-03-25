const Logotype = ({ height } : {height : number}) => {
  return (
    <div className='flex flex-nowrap gap-x-1'>
        {['K','R','U','G','O','S'].map((i, index) => {
            return (
                <div 
                    key={index} 
                >
                    <img 
                        src={`/assets/ui/logo/${i}.png`}
                        alt={i}
                        style={{ height: `${height * 0.25}rem` }}
                        className='object-contain drop-shadow-[2px_3px_2px_rgba(0,0,0,0.25)]'
                    />
                </div>
            );
        })}
    </div>
  )
}

export default Logotype