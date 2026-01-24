import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Navigation } from './components/Navigation';
import { Dashboard } from './pages/Dashboard';
import { RedisMonitoring } from './pages/RedisMonitoring';
import './App.css';

function App() {
  return (
    <Router>
      <Navigation />
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/redis-monitoring" element={<RedisMonitoring />} />
      </Routes>
    </Router>
  );
}

export default App;

