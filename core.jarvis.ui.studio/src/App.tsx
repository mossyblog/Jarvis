import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Dashboard } from './pages/Dashboard';
import UserManagement from './pages/UserManagement';
import TableEditor from './pages/TableEditor';
import SchemaVisualizer from './pages/SchemaVisualizer';
import { Login } from './pages/Login';
import { AuthProvider } from './contexts/AuthContext';
import { ThemeProvider } from './contexts/ThemeContext';
import { ProtectedRoute } from './components/auth/ProtectedRoute';

function App() {
  return (
    <ThemeProvider defaultTheme="supabase" defaultMode="dark">
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
  </ThemeProvider>
  );
}

export default App