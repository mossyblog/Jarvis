/**
 * Retry Components
 * 
 * Components for displaying retry states and actions with consistent
 * styling and behavior patterns.
 * 
 * @module Retry
 */

import React from 'react';
import { RefreshCw, AlertTriangle, CheckCircle, Clock } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from './button';
import { LoadingSpinner, ProgressIndicator } from './loading-spinner';
import { ErrorAlert } from './error-display';
import { 
  UIStudioError, 
  getUserFriendlyMessage, 
  isUIStudioError 
} from '../../utils/uistudioErrors';

// ============================================================================
// Retry Button Component
// ============================================================================

interface RetryButtonProps {
  onRetry: () => void | Promise<void>;
  isRetrying?: boolean;
  canRetry?: boolean;
  retryCount?: number;
  maxRetries?: number;
  error?: Error | string;
  className?: string;
  variant?: 'default' | 'outline' | 'ghost';
  size?: 'sm' | 'default' | 'lg';
  showRetryCount?: boolean;
  disabled?: boolean;
}

export function RetryButton({
  onRetry,
  isRetrying = false,
  canRetry = true,
  retryCount = 0,
  maxRetries,
  className,
  variant = 'outline',
  size = 'default',
  showRetryCount = true,
  disabled = false
}: RetryButtonProps) {
  const handleRetry = async () => {
    if (disabled || isRetrying || !canRetry) return;
    await onRetry();
  };

  const isDisabled = disabled || isRetrying || !canRetry;
  
  const buttonText = isRetrying ? 'Retrying...' : 
                    retryCount > 0 && showRetryCount ? `Retry (${retryCount + 1})` :
                    'Retry';

  return (
    <Button
      variant={variant}
      size={size}
      onClick={handleRetry}
      disabled={isDisabled}
      className={cn(className)}
    >
      {isRetrying ? (
        <LoadingSpinner size="sm" className="mr-2" />
      ) : (
        <RefreshCw className="h-4 w-4 mr-2" />
      )}
      {buttonText}
      {maxRetries && retryCount >= maxRetries && (
        <span className="ml-2 text-xs text-muted-foreground">
          (Max reached)
        </span>
      )}
    </Button>
  );
}

// ============================================================================
// Retry Status Component
// ============================================================================

interface RetryStatusProps {
  isRetrying: boolean;
  retryCount: number;
  maxRetries?: number;
  error?: Error | string;
  onRetry?: () => void | Promise<void>;
  canRetry?: boolean;
  className?: string;
}

export function RetryStatus({
  isRetrying,
  retryCount,
  maxRetries,
  error,
  onRetry,
  canRetry = true,
  className
}: RetryStatusProps) {
  const errorObj = typeof error === 'string' ? new Error(error) : error;
  const hasMaxRetries = maxRetries !== undefined;
  const isMaxReached = hasMaxRetries && retryCount >= maxRetries;

  if (isRetrying) {
    return (
      <div className={cn("flex items-center gap-2 text-sm text-muted-foreground", className)}>
        <LoadingSpinner size="sm" />
        <span>
          Retrying{retryCount > 0 ? ` (attempt ${retryCount + 1})` : ''}...
        </span>
      </div>
    );
  }

  if (errorObj && isMaxReached) {
    return (
      <div className={cn("space-y-2", className)}>
        <div className="flex items-center gap-2 text-sm text-destructive">
          <AlertTriangle className="h-4 w-4" />
          <span>Maximum retry attempts reached</span>
        </div>
        {errorObj && (
          <ErrorAlert 
            error={errorObj} 
            size="sm"
            className="mb-0"
          />
        )}
      </div>
    );
  }

  if (errorObj && canRetry && onRetry) {
    return (
      <div className={cn("space-y-2", className)}>
        <ErrorAlert 
          error={errorObj}
          onRetry={onRetry}
          canRetry={canRetry}
          retryCount={retryCount}
          size="sm"
        />
      </div>
    );
  }

  if (retryCount > 0 && !errorObj) {
    return (
      <div className={cn("flex items-center gap-2 text-sm text-green-600", className)}>
        <CheckCircle className="h-4 w-4" />
        <span>Succeeded after {retryCount} retry{retryCount > 1 ? 's' : ''}</span>
      </div>
    );
  }

  return null;
}

// ============================================================================
// Retry Progress Component
// ============================================================================

interface RetryProgressProps {
  currentAttempt: number;
  maxAttempts: number;
  isRetrying: boolean;
  nextRetryIn?: number; // seconds
  className?: string;
}

export function RetryProgress({
  currentAttempt,
  maxAttempts,
  isRetrying,
  nextRetryIn,
  className
}: RetryProgressProps) {
  const progress = (currentAttempt / maxAttempts) * 100;

  return (
    <div className={cn("space-y-2", className)}>
      <div className="flex items-center justify-between text-sm">
        <span className="text-muted-foreground">
          {isRetrying ? 'Retrying...' : 'Retry Progress'}
        </span>
        <span className="text-muted-foreground">
          {currentAttempt} / {maxAttempts}
        </span>
      </div>
      
      <ProgressIndicator
        progress={progress}
        showPercentage={false}
        className="mb-2"
      />

      {nextRetryIn !== undefined && nextRetryIn > 0 && (
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          <Clock className="h-3 w-3" />
          <span>Next retry in {nextRetryIn}s</span>
        </div>
      )}
    </div>
  );
}

// ============================================================================
// Retry Panel Component
// ============================================================================

interface RetryPanelProps {
  title?: string;
  error: Error | string;
  isRetrying: boolean;
  retryCount: number;
  maxRetries?: number;
  onRetry?: () => void | Promise<void>;
  onCancel?: () => void;
  canRetry?: boolean;
  showDetails?: boolean;
  className?: string;
}

export function RetryPanel({
  title = 'Operation Failed',
  error,
  isRetrying,
  retryCount,
  maxRetries,
  onRetry,
  onCancel,
  canRetry = true,
  showDetails = false,
  className
}: RetryPanelProps) {
  const errorObj = typeof error === 'string' ? new Error(error) : error;
  const userMessage = isUIStudioError(errorObj) 
    ? getUserFriendlyMessage(errorObj as UIStudioError)
    : errorObj.message;

  const isMaxReached = maxRetries !== undefined && retryCount >= maxRetries;

  return (
    <div className={cn(
      "border rounded-lg p-4 space-y-4 bg-background",
      className
    )}>
      {/* Header */}
      <div className="flex items-center gap-3">
        <div className="p-2 rounded-full bg-destructive/10">
          <AlertTriangle className="h-5 w-5 text-destructive" />
        </div>
        <div className="flex-1">
          <h3 className="font-medium text-foreground">{title}</h3>
          <p className="text-sm text-muted-foreground">{userMessage}</p>
        </div>
      </div>

      {/* Retry Progress */}
      {maxRetries && (
        <RetryProgress
          currentAttempt={retryCount}
          maxAttempts={maxRetries}
          isRetrying={isRetrying}
        />
      )}

      {/* Status */}
      <RetryStatus
        isRetrying={isRetrying}
        retryCount={retryCount}
        maxRetries={maxRetries}
        error={errorObj}
        onRetry={onRetry}
        canRetry={canRetry}
      />

      {/* Details */}
      {showDetails && import.meta.env.DEV && (
        <details className="text-xs">
          <summary className="cursor-pointer text-muted-foreground hover:text-foreground">
            Error Details
          </summary>
          <pre className="mt-2 p-2 bg-muted rounded text-xs overflow-auto">
            {errorObj.stack || errorObj.message}
          </pre>
        </details>
      )}

      {/* Actions */}
      <div className="flex items-center gap-2 pt-2">
        {canRetry && onRetry && !isMaxReached && (
          <RetryButton
            onRetry={onRetry}
            isRetrying={isRetrying}
            canRetry={canRetry}
            retryCount={retryCount}
            maxRetries={maxRetries}
            size="sm"
          />
        )}
        
        {onCancel && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onCancel}
            disabled={isRetrying}
          >
            {isMaxReached ? 'Close' : 'Cancel'}
          </Button>
        )}
      </div>
    </div>
  );
}

// ============================================================================
// Retry Wrapper Component
// ============================================================================

interface RetryWrapperProps {
  children: React.ReactNode;
  error?: Error | string | null;
  isRetrying?: boolean;
  retryCount?: number;
  maxRetries?: number;
  onRetry?: () => void | Promise<void>;
  canRetry?: boolean;
  showInline?: boolean;
  className?: string;
}

export function RetryWrapper({
  children,
  error,
  isRetrying = false,
  retryCount = 0,
  maxRetries,
  onRetry,
  canRetry = true,
  showInline = true,
  className
}: RetryWrapperProps) {
  if (!error) {
    return <div className={className}>{children}</div>;
  }

  if (showInline) {
    return (
      <div className={cn("space-y-4", className)}>
        {children}
        <RetryStatus
          isRetrying={isRetrying}
          retryCount={retryCount}
          maxRetries={maxRetries}
          error={error}
          onRetry={onRetry}
          canRetry={canRetry}
        />
      </div>
    );
  }

  return (
    <div className={className}>
      <RetryPanel
        error={error}
        isRetrying={isRetrying}
        retryCount={retryCount}
        maxRetries={maxRetries}
        onRetry={onRetry}
        canRetry={canRetry}
      />
    </div>
  );
}

// ============================================================================
// Auto Retry Component
// ============================================================================

interface AutoRetryProps {
  isRetrying: boolean;
  retryCount: number;
  maxRetries: number;
  nextRetryIn: number;
  error?: Error | string;
  onCancel?: () => void;
  className?: string;
}

export function AutoRetry({
  isRetrying,
  retryCount,
  maxRetries,
  nextRetryIn,
  onCancel,
  className
}: AutoRetryProps) {
  return (
    <div className={cn(
      "flex items-center justify-between p-3 bg-yellow-50 dark:bg-yellow-950/10 border border-yellow-200 dark:border-yellow-800 rounded-lg",
      className
    )}>
      <div className="flex items-center gap-3">
        <LoadingSpinner size="sm" className="text-yellow-600" />
        <div className="text-sm">
          <div className="font-medium text-yellow-800 dark:text-yellow-200">
            {isRetrying ? 'Retrying...' : 'Auto-retry in progress'}
          </div>
          <div className="text-yellow-600 dark:text-yellow-400">
            Attempt {retryCount + 1} of {maxRetries}
            {!isRetrying && nextRetryIn > 0 && ` • Next retry in ${nextRetryIn}s`}
          </div>
        </div>
      </div>
      
      {onCancel && (
        <Button
          variant="ghost"
          size="sm"
          onClick={onCancel}
          className="text-yellow-800 hover:bg-yellow-100 dark:text-yellow-200 dark:hover:bg-yellow-900/20"
        >
          Cancel
        </Button>
      )}
    </div>
  );
}

export type {
  RetryButtonProps,
  RetryStatusProps,
  RetryProgressProps,
  RetryPanelProps,
  RetryWrapperProps,
  AutoRetryProps
};