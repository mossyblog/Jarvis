import { Card } from '../components/ui/card';
import { LoginForm } from '../components/auth/LoginForm';
import { Shield } from 'lucide-react';

export function Login() {
  const useMockApi = import.meta.env.VITE_USE_MOCK_API === 'true';

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <Card className="w-full max-w-md p-6 space-y-6">
        <div className="text-center space-y-4">
          <div className="mx-auto w-16 h-16 bg-primary/10 rounded-full flex items-center justify-center transition-all duration-300 hover:bg-primary/20 hover:scale-105">
            <Shield className="w-8 h-8 text-primary" />
          </div>
          <div className="space-y-2">
            <h1 className="text-2xl font-bold">Welcome to Jarvis Studio</h1>
            <p className="text-muted-foreground">Sign in to your account</p>
          </div>
        </div>

        <LoginForm />

        {useMockApi && (
          <div className="text-center text-sm text-muted-foreground border-t pt-4">
            <p>Running in mock mode. Set VITE_USE_MOCK_API=false to use real API.</p>
          </div>
        )}
      </Card>
    </div>
  );
}