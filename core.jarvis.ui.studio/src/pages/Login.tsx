import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Card } from '../components/ui/card';
import { mockUsers } from '../services/api/mockData';

export function Login() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const { login } = useAuth();

  const handleLogin = async (email: string) => {
    try {
      setIsLoading(true);
      setError('');
      await login({ email, password: 'mock-password' });
      navigate('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <Card className="w-full max-w-md p-6 space-y-6">
        <div className="text-center space-y-2">
          <h1 className="text-2xl font-bold">Welcome to Jarvis Studio</h1>
          <p className="text-muted-foreground">Select a user role to continue</p>
        </div>

        {error && (
          <div className="bg-destructive/10 text-destructive p-3 rounded-md text-sm">
            {error}
          </div>
        )}

        <div className="space-y-3">
          {mockUsers.map((user) => (
            <button
              key={user.id}
              onClick={() => handleLogin(user.email)}
              disabled={isLoading}
              className="w-full p-4 border border-border rounded-lg hover:bg-accent transition-colors text-left space-y-2 disabled:opacity-50"
            >
              <div className="flex items-center justify-between">
                <div>
                  <div className="font-semibold">{user.name}</div>
                  <div className="text-sm text-muted-foreground">{user.email}</div>
                </div>
                <div className="text-right">
                  <div className="text-xs font-medium text-muted-foreground">Role</div>
                  <div className="text-sm font-medium">
                    {user.roles.map(r => r.name).join(', ')}
                  </div>
                </div>
              </div>
              <div className="text-xs text-muted-foreground">
                {user.roles[0].description}
              </div>
            </button>
          ))}
        </div>

        <div className="text-center text-sm text-muted-foreground">
          <p>This is a mock authentication system for development.</p>
          <p>In production, this will be replaced with real authentication.</p>
        </div>
      </Card>
    </div>
  );
}