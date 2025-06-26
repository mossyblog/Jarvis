import { Card } from '../components/ui/card';
import { LoginForm } from '../components/auth/LoginForm';

export function Login() {
  const useMockApi = import.meta.env.VITE_USE_MOCK_API === 'true';

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <Card className="w-full max-w-md p-6 space-y-6">
        <div className="text-center space-y-2">
          <h1 className="text-2xl font-bold">Welcome to Jarvis Studio</h1>
          <p className="text-muted-foreground">Sign in to your account</p>
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