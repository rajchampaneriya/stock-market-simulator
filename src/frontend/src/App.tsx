import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Navigation } from './components/Navigation';
import { Dashboard } from './pages/Dashboard';
import { RedisMonitoring } from './pages/RedisMonitoring';
import { OptionsRisk } from './pages/OptionsRisk';
import './App.css';

function App() {
  return (
    <Router>
      <Navigation />
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/redis-monitoring" element={<RedisMonitoring />} />
        <Route path="/options" element={<OptionsRisk />} />
      </Routes>
    </Router>
  );
}

export default App;

