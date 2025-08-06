/**
 * Error Boundary Components
 * 
 * Comprehensive error boundary components for graceful error handling
 * with retry functionality and user-friendly error messages.
 * 
 * @module ErrorBoundary
 */

import React from 'react';
import { AlertTriangle, RefreshCw, Home, Bug } from 'lucide-react';
import { Button } from './button';
import { 
  UIStudioError, 
  getUserFriendlyMessage,
  isUIStudioError,
  isAuthError,
  isNetworkError,
  isPermissionError 
} from '../../utils/uistudioErrors';

// ============================================================================
// Error Boundary State & Types
// ============================================================================

interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
  errorInfo?: React.ErrorInfo;
  retryCount: number;
}

interface ErrorBoundaryProps {
  children: React.ReactNode;
  fallback?: React.ComponentType<ErrorFallbackProps>;
  onError?: (error: Error, errorInfo: React.ErrorInfo) => void;
  isolate?: boolean;
  level?: 'page' | 'section' | 'component';
  maxRetries?: number;
}

interface ErrorFallbackProps {
  error: Error;
  retry: () => void;
  canRetry: boolean;
  retryCount: number;
  level: 'page' | 'section' | 'component';
}

// ============================================================================
// Error Boundary Class Component
// ============================================================================

export class ErrorBoundary extends React.Component<ErrorBoundaryProps, ErrorBoundaryState> {
  private resetTimeoutId: NodeJS.Timeout | null = null;

  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = {
      hasError: false,
      retryCount: 0
    };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return {
      hasError: true,
      error
    };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('ErrorBoundary caught an error:', error, errorInfo);
    
    this.setState({
      error,
      errorInfo
    });

    // Call custom error handler if provided
    this.props.onError?.(error, errorInfo);

    // Auto-retry for certain types of errors
    if (this.shouldAutoRetry(error)) {
      this.scheduleAutoRetry();
    }
  }

  private shouldAutoRetry(error: Error): boolean {
    if (!isUIStudioError(error)) return false;
    
    // Auto-retry for network errors and server errors
    return isNetworkError(error) || Boolean(error.status && Number(error.status) >= 500);
  }

  private scheduleAutoRetry() {
    if (this.resetTimeoutId) {
      clearTimeout(this.resetTimeoutId);
    }

    // Auto-retry after 3 seconds for the first retry, then manual only
    if (this.state.retryCount === 0) {
      this.resetTimeoutId = setTimeout(() => {
        this.handleRetry();
      }, 3000);
    }
  }

  private handleRetry = () => {
    const maxRetries = this.props.maxRetries || 3;
    
    if (this.state.retryCount < maxRetries) {
      this.setState(prevState => ({
        hasError: false,
        error: undefined,
        errorInfo: undefined,
        retryCount: prevState.retryCount + 1
      }));
    }
  };

  componentWillUnmount() {
    if (this.resetTimeoutId) {
      clearTimeout(this.resetTimeoutId);
    }
  }

  render() {
    if (this.state.hasError && this.state.error) {
      const FallbackComponent = this.props.fallback || DefaultErrorFallback;
      const maxRetries = this.props.maxRetries || 3;
      
      return (
        <FallbackComponent
          error={this.state.error}
          retry={this.handleRetry}
          canRetry={this.state.retryCount < maxRetries}
          retryCount={this.state.retryCount}
          level={this.props.level || 'component'}
        />
      );
    }

    return this.props.children;
  }
}

// ============================================================================
// Default Error Fallback Components
// ============================================================================

function DefaultErrorFallback({
  error,
  retry,
  canRetry,
  retryCount,
  level
}: ErrorFallbackProps) {
  const isUIStudio = isUIStudioError(error);
  const userMessage = isUIStudio ? getUserFriendlyMessage(error as UIStudioError) : error.message;

  // Different layouts based on error level
  if (level === 'page') {
    return <PageErrorFallback 
      error={error} 
      retry={retry} 
      canRetry={canRetry} 
      retryCount={retryCount} 
      level={level}
    />;
  }

  if (level === 'section') {
    return <SectionErrorFallback 
      error={error} 
      retry={retry} 
      canRetry={canRetry} 
      retryCount={retryCount} 
      level={level}
    />;
  }

  // Component level (inline)
  return (
    <div className="my-4 p-3 border border-destructive/20 bg-destructive/5 rounded-md">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <AlertTriangle className="h-xs w-xs text-destructive" />
          <span className="text-sm text-destructive">{userMessage}</span>
        </div>
        {canRetry && (
          <Button
            variant="outline"
            size="sm"
            onClick={retry}
            className="ml-4 h-md px-3"
          >
            <RefreshCw className="h-xs w-xs mr-1" />
            Retry
          </Button>
        )}
      </div>
    </div>
  );
}

function PageErrorFallback({
  error,
  retry,
  canRetry,
  retryCount
}: ErrorFallbackProps) {
  const isUIStudio = isUIStudioError(error);
  const userMessage = isUIStudio ? getUserFriendlyMessage(error as UIStudioError) : error.message;
  
  // Different handling for different error types
  const isAuth = isUIStudio && isAuthError(error as UIStudioError);
  const isPermission = isUIStudio && isPermissionError(error as UIStudioError);
  const isNetwork = isUIStudio && isNetworkError(error as UIStudioError);

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="max-w-md w-full mx-auto p-6">
        <div className="text-center">
          {/* Error Icon */}
          <div className="mx-auto flex items-center justify-center h-3xl w-3xl rounded-full bg-destructive/10 mb-6">
            {isAuth ? (
              <Home className="h-md w-md text-destructive" />
            ) : isNetwork ? (
              <RefreshCw className="h-md w-md text-destructive" />
            ) : (
              <AlertTriangle className="h-md w-md text-destructive" />
            )}
          </div>

          {/* Error Title */}
          <h1 className="text-2xl font-bold text-foreground mb-4">
            {isAuth ? 'Authentication Required' :
             isPermission ? 'Access Denied' :
             isNetwork ? 'Connection Problem' :
             'Something went wrong'}
          </h1>

          {/* Error Message */}
          <p className="text-muted-foreground mb-8">
            {userMessage}
            {retryCount > 0 && (
              <span className="block text-sm mt-2 text-muted-foreground">
                Retry attempt {retryCount}
              </span>
            )}
          </p>

          {/* Actions */}
          <div className="flex flex-col gap-3">
            {canRetry && (
              <Button onClick={retry} className="w-full">
                <RefreshCw className="h-xs w-xs mr-2" />
                Try Again
              </Button>
            )}
            
            {isAuth && (
              <Button 
                variant="outline" 
                onClick={() => window.location.href = '/login'}
                className="w-full"
              >
                <Home className="h-xs w-xs mr-2" />
                Go to Login
              </Button>
            )}
            
            {!isAuth && (
              <Button 
                variant="outline" 
                onClick={() => window.location.href = '/'}
                className="w-full"
              >
                <Home className="h-xs w-xs mr-2" />
                Go to Home
              </Button>
            )}
          </div>

          {/* Debug info in development */}
          {import.meta.env.DEV && (
            <details className="mt-8 text-left">
              <summary className="cursor-pointer text-sm text-muted-foreground hover:text-foreground">
                <Bug className="inline h-xs w-xs mr-1" />
                Debug Information
              </summary>
              <pre className="mt-2 p-3 bg-muted rounded text-xs overflow-auto">
                {error.stack}
              </pre>
            </details>
          )}
        </div>
      </div>
    </div>
  );
}

function SectionErrorFallback({
  error,
  retry,
  canRetry,
  retryCount
}: ErrorFallbackProps) {
  const isUIStudio = isUIStudioError(error);
  const userMessage = isUIStudio ? getUserFriendlyMessage(error as UIStudioError) : error.message;

  return (
    <div className="flex items-center justify-center min-h-[200px] p-6 border-2 border-dashed border-destructive/20 rounded-lg bg-destructive/5">
      <div className="text-center max-w-sm">
        <AlertTriangle className="h-md w-md text-destructive mx-auto mb-4" />
        <h3 className="text-lg font-semibold text-foreground mb-2">
          Unable to load content
        </h3>
        <p className="text-sm text-muted-foreground mb-4">
          {userMessage}
          {retryCount > 0 && (
            <span className="block text-xs mt-1">
              Retry attempt {retryCount}
            </span>
          )}
        </p>
        {canRetry && (
          <Button onClick={retry} size="sm">
            <RefreshCw className="h-xs w-xs mr-2" />
            Retry
          </Button>
        )}
      </div>
    </div>
  );
}

// ============================================================================
// Specialized Error Boundary HOCs
// ============================================================================

export function withErrorBoundary<P extends object>(
  Component: React.ComponentType<P>,
  errorBoundaryProps?: Omit<ErrorBoundaryProps, 'children'>
) {
  const WrappedComponent = (props: P) => (
    <ErrorBoundary {...errorBoundaryProps}>
      <Component {...props} />
    </ErrorBoundary>
  );
  
  WrappedComponent.displayName = `withErrorBoundary(${Component.displayName || Component.name})`;
  return WrappedComponent;
}

// ============================================================================
// Hook for Error Boundary
// ============================================================================

export function useErrorHandler() {
  return (error: Error) => {
    // Throw the error to be caught by the nearest error boundary
    throw error;
  };
}

// ============================================================================
// Specialized Error Boundaries
// ============================================================================

export function PageErrorBoundary({ children }: { children: React.ReactNode }) {
  return (
    <ErrorBoundary level="page" maxRetries={3}>
      {children}
    </ErrorBoundary>
  );
}

export function SectionErrorBoundary({ children }: { children: React.ReactNode }) {
  return (
    <ErrorBoundary level="section" maxRetries={2}>
      {children}
    </ErrorBoundary>
  );
}

export function ComponentErrorBoundary({ children }: { children: React.ReactNode }) {
  return (
    <ErrorBoundary level="component" maxRetries={1}>
      {children}
    </ErrorBoundary>
  );
}

// ============================================================================
// React Query Error Boundary
// ============================================================================

export function QueryErrorBoundary({ 
  children, 
  fallback 
}: { 
  children: React.ReactNode;
  fallback?: React.ComponentType<ErrorFallbackProps>;
}) {
  return (
    <ErrorBoundary 
      level="section" 
      fallback={fallback}
      onError={(error, errorInfo) => {
        console.error('React Query Error:', error, errorInfo);
      }}
    >
      {children}
    </ErrorBoundary>
  );
}

export { ErrorBoundary as default };