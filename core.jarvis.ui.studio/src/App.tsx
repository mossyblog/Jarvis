import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Dashboard } from './pages/Dashboard';
import UserManagement from './pages/UserManagement';
import TableEditor from './pages/TableEditor';
import SchemaVisualizer from './pages/SchemaVisualizer';
import { Login } from './pages/Login';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { useEffect } from 'react';

function App() {
  useEffect(() => {
    // Apply Supabase dark theme
    document.documentElement.classList.add('supabase-dark');
  }, []);

  return (
    <Router>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/UserManagement" element={<UserManagement />} />
          <Route 
            path="/" 
            element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/editor" 
            element={
              <ProtectedRoute requiredPermission="table-editor">
                <TableEditor />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/SchemaVisualizer" 
            element={
              <ProtectedRoute requiredPermission="schema-visualizer">
                <SchemaVisualizer />
              </ProtectedRoute>
            } 
          />
        </Routes>
      </AuthProvider>
    </Router>
  );
}

export default App