import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Dashboard } from './pages/Dashboard';
import UserManagement from './pages/UserManagement';
import AccountEdit from './pages/AccountEdit';
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
          <Route 
            path="/" 
            element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/accounts" 
            element={
              <ProtectedRoute>
                <UserManagement />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/accounts/:id/edit" 
            element={
              <ProtectedRoute>
                <AccountEdit />
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
            path="/schema" 
            element={
              <ProtectedRoute requiredPermission="schema-visualizer">
                <SchemaVisualizer />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/roles" 
            element={
              <ProtectedRoute>
                <div className="p-8">
                  <h1 className="text-2xl font-bold">Roles Management</h1>
                  <p className="text-muted-foreground mt-2">Roles page coming soon...</p>
                </div>
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/settings" 
            element={
              <ProtectedRoute>
                <div className="p-8">
                  <h1 className="text-2xl font-bold">Settings</h1>
                  <p className="text-muted-foreground mt-2">Settings page coming soon...</p>
                </div>
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