import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Dashboard } from './pages/Dashboard';
import TableEditor from './pages/TableEditor';
import SchemaVisualizer from './pages/SchemaVisualizer';
import { useEffect } from 'react';

function App() {
  useEffect(() => {
    // Apply Supabase dark theme
    document.documentElement.classList.add('supabase-dark');
  }, []);

  return (
    <Router>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/editor" element={<TableEditor />} />
        <Route path="/SchemaVisualizer" element={<SchemaVisualizer />} />
      </Routes>
    </Router>
  );
}

export default App