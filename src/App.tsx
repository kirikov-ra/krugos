import { PhaserGame } from './components/PhaserGame';
import './App.css';

export default function App() {
  return (
    <div style={{ width: '100vw', height: '100vh', position: 'relative', overflow: 'hidden' }}>
      <PhaserGame />
      <div 
        style={{ 
          position: 'absolute', 
          top: 0, 
          left: 0, 
          width: '100%', 
          height: '100%', 
          pointerEvents: 'none',
          display: 'flex',
          flexDirection: 'column'
        }}
      >
      </div>

    </div>
  );
}