/**
 * Error Display Components
 * 
 * User-friendly error display components for different scenarios
 * with consistent styling and behavior patterns.
 * 
 * @module ErrorDisplay
 */

import React from 'react';
import { 
  AlertTriangle, 
  RefreshCw, 
  X, 
  AlertCircle,
  Wifi,
  Lock,
  Database,
  Server
} from 'lucide-react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/utils';
import { Button } from './button';
import { 
  UIStudioError, 
  getUserFriendlyMessage,
  isUIStudioError,
  isAuthError,
  isNetworkError,
  isPermissionError,
  isValidationError,
  isConflictError,
  UIStudioServerError
} from '../../utils/uistudioErrors';

// ============================================================================
// Error Alert Variants
// ============================================================================

const errorAlertVariants = cva(
  "relative w-full rounded-lg border px-4 py-3 text-sm",
  {
    variants: {
      variant: {
        error: "border-destructive/50 text-destructive bg-destructive/5 [&>svg]:text-destructive",
        warning: "border-yellow-500/50 text-yellow-700 bg-yellow-50 dark:bg-yellow-950/10 dark:text-yellow-400 [&>svg]:text-yellow-600 dark:[&>svg]:text-yellow-400",
        info: "border-blue-500/50 text-blue-700 bg-blue-50 dark:bg-blue-950/10 dark:text-blue-400 [&>svg]:text-blue-600 dark:[&>svg]:text-blue-400",
        success: "border-green-500/50 text-green-700 bg-green-50 dark:bg-green-950/10 dark:text-green-400 [&>svg]:text-green-600 dark:[&>svg]:text-green-400"
      },
      size: {
        sm: "px-3 py-2 text-xs",
        default: "px-4 py-3 text-sm",
        lg: "px-6 py-4 text-base"
      }
    },
    defaultVariants: {
      variant: "error",
      size: "default"
    }
  }
);

// ============================================================================
// Error Icon Mapping
// ============================================================================

function getErrorIcon(error: Error) {
  if (!isUIStudioError(error)) {
    return AlertTriangle;
  }

  const uiError = error as UIStudioError;
  
  if (isNetworkError(uiError)) return Wifi;
  if (isAuthError(uiError)) return Lock;
  if (isPermissionError(uiError)) return Lock;
  if (isValidationError(uiError)) return AlertCircle;
  if (isConflictError(uiError)) return RefreshCw;
  if (typeof uiError === 'object' && uiError !== null && 'status' in uiError && (uiError as any).status >= 500) return Server;
  
  return AlertTriangle;
}

function getErrorVariant(error: Error): 'error' | 'warning' | 'info' {
  if (!isUIStudioError(error)) return 'error';
  
  const uiError = error as UIStudioError;
  
  if (isValidationError(uiError)) return 'warning';
  if (isConflictError(uiError)) return 'info';
  
  return 'error';
}

// ============================================================================
// Basic Error Alert Component
// ============================================================================

interface ErrorAlertProps extends VariantProps<typeof errorAlertVariants> {
  error: Error | string;
  onRetry?: () => void;
  onDismiss?: () => void;
  canRetry?: boolean;
  retryCount?: number;
  className?: string;
  title?: string;
  children?: React.ReactNode;
}

export function ErrorAlert({
  error,
  onRetry,
  onDismiss,
  canRetry = false,
  retryCount = 0,
  variant,
  size,
  className,
  title,
  children,
  ...props
}: ErrorAlertProps) {
  const errorObj = typeof error === 'string' ? new Error(error) : error;
  const Icon = getErrorIcon(errorObj);
  const autoVariant = variant || getErrorVariant(errorObj);
  const userMessage = isUIStudioError(errorObj) 
    ? getUserFriendlyMessage(errorObj as UIStudioError)
    : errorObj.message;

  return (
    <div className={cn(errorAlertVariants({ variant: autoVariant, size }), className)} {...props}>
      <div className="flex items-start gap-3">
        <Icon className="h-4 w-4 mt-0.5 flex-shrink-0" />
        
        <div className="flex-1 min-w-0">
          {title && (
            <div className="font-medium mb-1">{title}</div>
          )}
          
          <div className="text-sm">
            {userMessage}
            {retryCount > 0 && (
              <span className="block text-xs mt-1 opacity-75">
                Retry attempt {retryCount}
              </span>
            )}
          </div>
          
          {children && (
            <div className="mt-2">
              {children}
            </div>
          )}
        </div>

        <div className="flex items-center gap-2 flex-shrink-0">
          {canRetry && onRetry && (
            <Button 
              variant="ghost" 
              size="sm" 
              onClick={onRetry}
              className="h-8 px-2 hover:bg-current hover:bg-opacity-10"
            >
              <RefreshCw className="h-3 w-3" />
            </Button>
          )}
          
          {onDismiss && (
            <Button 
              variant="ghost" 
              size="sm" 
              onClick={onDismiss}
              className="h-8 px-2 hover:bg-current hover:bg-opacity-10"
            >
              <X className="h-3 w-3" />
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// Inline Error Component
// ============================================================================

interface InlineErrorProps {
  error: Error | string;
  onRetry?: () => void;
  canRetry?: boolean;
  size?: 'sm' | 'default';
  className?: string;
}

export function InlineError({ 
  error, 
  onRetry, 
  canRetry = false, 
  size = 'default',
  className 
}: InlineErrorProps) {
  const errorObj = typeof error === 'string' ? new Error(error) : error;
  const Icon = getErrorIcon(errorObj);
  const userMessage = isUIStudioError(errorObj) 
    ? getUserFriendlyMessage(errorObj as UIStudioError)
    : errorObj.message;

  const sizeClasses = size === 'sm' 
    ? 'text-xs py-1' 
    : 'text-sm py-2';

  return (
    <div className={cn(
      "flex items-center gap-2 text-destructive bg-destructive/5 px-3 rounded border border-destructive/20",
      sizeClasses,
      className
    )}>
      <Icon className={cn("flex-shrink-0", size === 'sm' ? 'h-3 w-3' : 'h-4 w-4')} />
      <span className="flex-1 min-w-0 truncate">{userMessage}</span>
      {canRetry && onRetry && (
        <Button 
          variant="ghost" 
          size="sm" 
          onClick={onRetry}
          className="h-6 w-6 p-0 hover:bg-destructive/10"
        >
          <RefreshCw className="h-3 w-3" />
        </Button>
      )}
    </div>
  );
}

// ============================================================================
// Error Page Component
// ============================================================================

interface ErrorPageProps {
  error: Error | string;
  onRetry?: () => void;
  onGoHome?: () => void;
  canRetry?: boolean;
  retryCount?: number;
  title?: string;
  description?: string;
}

export function ErrorPage({
  error,
  onRetry,
  onGoHome,
  canRetry = false,
  retryCount = 0,
  title,
  description
}: ErrorPageProps) {
  const errorObj = typeof error === 'string' ? new Error(error) : error;
  const Icon = getErrorIcon(errorObj);
  const userMessage = isUIStudioError(errorObj) 
    ? getUserFriendlyMessage(errorObj as UIStudioError)
    : errorObj.message;

  const isAuth = isUIStudioError(errorObj) && isAuthError(errorObj as UIStudioError);
  const isNetwork = isUIStudioError(errorObj) && isNetworkError(errorObj as UIStudioError);

  const defaultTitle = isAuth ? 'Authentication Required' :
                      isNetwork ? 'Connection Problem' :
                      'Something went wrong';

  const defaultDescription = isAuth ? 'Please log in to continue accessing this page.' :
                            isNetwork ? 'Please check your internet connection and try again.' :
                            'An unexpected error occurred. Our team has been notified.';

  return (
    <div className="min-h-[400px] flex items-center justify-center p-8">
      <div className="max-w-md w-full text-center">
        <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-destructive/10 mb-6">
          <Icon className="h-8 w-8 text-destructive" />
        </div>

        <h2 className="text-2xl font-bold text-foreground mb-4">
          {title || defaultTitle}
        </h2>

        <p className="text-muted-foreground mb-2">
          {description || defaultDescription}
        </p>

        <p className="text-sm text-muted-foreground mb-8">
          {userMessage}
          {retryCount > 0 && (
            <span className="block mt-1">
              Retry attempt {retryCount}
            </span>
          )}
        </p>

        <div className="flex flex-col gap-3 sm:flex-row sm:justify-center">
          {canRetry && onRetry && (
            <Button onClick={onRetry} className="sm:w-auto">
              <RefreshCw className="h-4 w-4 mr-2" />
              Try Again
            </Button>
          )}
          
          {onGoHome && (
            <Button 
              variant="outline" 
              onClick={onGoHome}
              className="sm:w-auto"
            >
              Go to Home
            </Button>
          )}
          
          {isAuth && (
            <Button 
              variant="outline" 
              onClick={() => window.location.href = '/login'}
              className="sm:w-auto"
            >
              Sign In
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// Error Toast/Notification Component
// ============================================================================

interface ErrorToastProps {
  error: Error | string;
  onRetry?: () => void;
  onDismiss?: () => void;
  canRetry?: boolean;
  autoClose?: boolean;
  duration?: number;
}

export function ErrorToast({
  error,
  onRetry,
  onDismiss,
  canRetry = false,
  autoClose = true,
  duration = 5000
}: ErrorToastProps) {
  const [isVisible, setIsVisible] = React.useState(true);

  React.useEffect(() => {
    if (autoClose && !canRetry) {
      const timer = setTimeout(() => {
        setIsVisible(false);
        onDismiss?.();
      }, duration);

      return () => clearTimeout(timer);
    }
  }, [autoClose, canRetry, duration, onDismiss]);

  if (!isVisible) return null;

  const errorObj = typeof error === 'string' ? new Error(error) : error;
  const Icon = getErrorIcon(errorObj);
  const userMessage = isUIStudioError(errorObj) 
    ? getUserFriendlyMessage(errorObj as UIStudioError)
    : errorObj.message;

  return (
    <div className="bg-background border border-destructive/20 rounded-lg shadow-lg p-4 min-w-[300px] max-w-[400px]">
      <div className="flex items-start gap-3">
        <Icon className="h-5 w-5 text-destructive mt-0.5 flex-shrink-0" />
        
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-foreground mb-1">Error</p>
          <p className="text-sm text-muted-foreground">{userMessage}</p>
        </div>

        <div className="flex items-center gap-1 flex-shrink-0">
          {canRetry && onRetry && (
            <Button 
              variant="ghost" 
              size="sm" 
              onClick={onRetry}
              className="h-8 w-8 p-0"
            >
              <RefreshCw className="h-4 w-4" />
            </Button>
          )}
          
          <Button 
            variant="ghost" 
            size="sm" 
            onClick={() => {
              setIsVisible(false);
              onDismiss?.();
            }}
            className="h-8 w-8 p-0"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// Form Field Error Component
// ============================================================================

interface FieldErrorProps {
  error?: string | string[];
  className?: string;
}

export function FieldError({ error, className }: FieldErrorProps) {
  if (!error) return null;

  const errors = Array.isArray(error) ? error : [error];

  return (
    <div className={cn("text-sm text-destructive space-y-1", className)}>
      {errors.map((err, index) => (
        <div key={index} className="flex items-center gap-1">
          <AlertCircle className="h-3 w-3 flex-shrink-0" />
          <span>{err}</span>
        </div>
      ))}
    </div>
  );
}

// ============================================================================
// Empty State with Error Option
// ============================================================================

interface EmptyStateProps {
  title: string;
  description: string;
  error?: Error | string;
  onRetry?: () => void;
  action?: React.ReactNode;
  icon?: React.ComponentType<{ className?: string }>;
  className?: string;
}

export function EmptyState({
  title,
  description,
  error,
  onRetry,
  action,
  icon: Icon = Database,
  className
}: EmptyStateProps) {
  return (
    <div className={cn("flex flex-col items-center justify-center p-8 text-center", className)}>
      <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-muted mb-6">
        <Icon className="h-8 w-8 text-muted-foreground" />
      </div>

      <h3 className="text-lg font-semibold text-foreground mb-2">
        {title}
      </h3>

      <p className="text-muted-foreground mb-6 max-w-sm">
        {description}
      </p>

      {error && (
        <div className="mb-6 w-full max-w-md">
          <InlineError 
            error={error} 
            onRetry={onRetry}
            canRetry={!!onRetry}
          />
        </div>
      )}

      {action && <div>{action}</div>}
    </div>
  );
}

export {
  errorAlertVariants,
  getErrorIcon,
  getErrorVariant
};