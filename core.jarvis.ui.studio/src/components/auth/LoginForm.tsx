import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { Input } from '../ui/input';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '../ui/form';
import { 
  LoadingButton, 
  ErrorAlert, 
  useSimpleRetry 
} from '../ui/loading-and-error';

const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(1, 'Password is required'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

export function LoginForm() {
  const navigate = useNavigate();
  const { login } = useAuth();

  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: 'test@example.com',
      password: 'TestPassword123!',
    },
  });

  const loginWithRetry = useSimpleRetry(
    async (...args: unknown[]) => {
      const values = args[0] as LoginFormValues;
      console.log('LoginForm: Starting login...');
      await login(values);
      console.log('LoginForm: Login successful, navigating to /');
      
      // Add a small delay to ensure state updates have propagated
      setTimeout(() => {
        navigate('/');
      }, 100);
    },
    { maxRetries: 2, delay: 1000 }
  );

  const onSubmit = async (values: LoginFormValues) => {
    try {
      await loginWithRetry.execute(values);
    } catch (err) {
      console.error('LoginForm: Login failed', err);
    }
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {loginWithRetry.error && (
          <ErrorAlert 
            error={loginWithRetry.error}
            onRetry={() => {
              if (form.formState.isValid) {
                onSubmit(form.getValues());
              }
            }}
            canRetry={loginWithRetry.canRetry}
            retryCount={loginWithRetry.retryCount}
            onDismiss={loginWithRetry.reset}
          />
        )}

        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl>
                <Input
                  type="email"
                  placeholder="Enter your email"
                  autoComplete="email"
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="password"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Password</FormLabel>
              <FormControl>
                <Input
                  type="password"
                  placeholder="Enter your password"
                  autoComplete="current-password"
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <LoadingButton 
          type="submit" 
          className="w-full" 
          isLoading={loginWithRetry.isRetrying}
          loadingText="Signing in..."
          disabled={!form.formState.isValid}
        >
          Sign in
        </LoadingButton>
      </form>
    </Form>
  );
}