import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Dashboard } from './pages/Dashboard';

import UserManagement from './pages/UserManagement';
import AccountEdit from './pages/AccountEdit';
import TableEditor from './pages/TableEditor';
import SchemaVisualizer from './pages/SchemaVisualizer';

import Templates from './pages/Templates';
import { Login } from './pages/Login';
import { AuthProvider } from './contexts/AuthContext';
import { ThemeProvider } from './contexts/ThemeContext';
import { ApiStatusProvider } from './contexts/ApiStatusContext';
import { EditModeProvider } from './contexts/EditModeContext';
import { QueryProvider } from './providers/QueryProvider';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { ApiStatusBanner } from './components/layout/ApiStatusBanner';
import { StatusFooter } from './components/layout/StatusFooter';
// import { UIStudioInterface } from './components/interfaces/UIStudioInterface';
import { UIStudioInterfaceSimple as UIStudioInterface } from './components/interfaces/UIStudioInterfaceSimple'; // TEMPORARY: Using simple version due to React 19 issues
import { PageBuilderInterface } from './components/interfaces/PageBuilderInterface';
import { SkipNavigation } from './components/accessibility/SkipNavigation';
import { KeyboardNavigationProvider } from './components/keyboard/KeyboardNavigationProvider';
import { ErrorBoundary } from './components/ErrorBoundary';
import CacheMonitor from './components/dev/CacheMonitor';

function App() {
  return (
    <ThemeProvider defaultTheme="supabase" defaultMode="dark">
      <QueryProvider>
        <Router>
          <KeyboardNavigationProvider>
            <ApiStatusProvider>
              <AuthProvider>
                <EditModeProvider>
              <div id="app-container" className="h-screen overflow-hidden" role="application" aria-label="Jarvis UI Studio">
                <SkipNavigation />
                <ApiStatusBanner />
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
            path="/templates" 
            element={
              <ProtectedRoute>
                <Templates />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/studio" 
            element={
              <ProtectedRoute>
                <ErrorBoundary>
                  <UIStudioInterface />
                </ErrorBoundary>
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/studio/page/:pageId" 
            element={
              <ProtectedRoute>
                <PageBuilderInterface />
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
              </div>
              </EditModeProvider>
            </AuthProvider>
            <StatusFooter />
            {/* Development Cache Monitor */}
            {import.meta.env.DEV && (
              <CacheMonitor 
                detailed={true}
                position="bottom-right"
                updateInterval={3000}
              />
            )}
          </ApiStatusProvider>
        </KeyboardNavigationProvider>
      </Router>
      </QueryProvider>
    </ThemeProvider>
  );
}

export default App