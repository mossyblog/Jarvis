/**
 * Loading Components
 * 
 * Comprehensive loading indicators and states for different scenarios
 * with consistent styling and behavior patterns.
 * 
 * @module LoadingSpinner
 */

import React from 'react';
import { RefreshCw, Loader2, MoreHorizontal } from 'lucide-react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/utils';
import { Button } from './button';

// ============================================================================
// Loading Spinner Variants
// ============================================================================

const loadingSpinnerVariants = cva(
  "animate-spin",
  {
    variants: {
      variant: {
        default: "text-muted-foreground",
        primary: "text-primary",
        secondary: "text-secondary-foreground",
        muted: "text-muted-foreground/60",
        accent: "text-accent-foreground"
      },
      size: {
        xs: "h-xs w-xs",
        sm: "h-xs w-xs", 
        default: "h-sm w-sm",
        lg: "h-md w-md",
        xl: "h-lg w-lg"
      }
    },
    defaultVariants: {
      variant: "default",
      size: "default"
    }
  }
);

// ============================================================================
// Basic Loading Spinner
// ============================================================================

interface LoadingSpinnerProps extends VariantProps<typeof loadingSpinnerVariants> {
  className?: string;
  icon?: 'spinner' | 'refresh' | 'dots';
}

export function LoadingSpinner({ 
  variant, 
  size, 
  className, 
  icon = 'spinner' 
}: LoadingSpinnerProps) {
  const IconComponent = icon === 'refresh' ? RefreshCw : 
                       icon === 'dots' ? MoreHorizontal : 
                       Loader2;
  
  return (
    <IconComponent 
      className={cn(loadingSpinnerVariants({ variant, size }), className)} 
    />
  );
}

// ============================================================================
// Loading State Components
// ============================================================================

interface LoadingStateProps {
  children?: React.ReactNode;
  className?: string;
  size?: 'sm' | 'default' | 'lg';
  variant?: 'default' | 'minimal' | 'card';
  showSpinner?: boolean;
}

export function LoadingState({ 
  children, 
  className, 
  size = 'default',
  variant = 'default',
  showSpinner = true 
}: LoadingStateProps) {
  const sizeClasses = {
    sm: 'py-md',
    default: 'py-lg', 
    lg: 'py-xl'
  };

  const variantClasses = {
    default: 'flex items-center justify-center',
    minimal: 'flex items-center gap-xs',
    card: 'flex flex-col items-center justify-center p-lg bg-muted/30 rounded-lg border-2 border-dashed border-muted'
  };

  return (
    <div className={cn(
      variantClasses[variant],
      sizeClasses[size],
      className
    )}>
      <div className={cn(
        "flex items-center gap-xs text-muted-foreground",
        variant === 'default' && "flex-col text-center"
      )}>
        {showSpinner && (
          <LoadingSpinner 
            size={size === 'sm' ? 'sm' : size === 'lg' ? 'lg' : 'default'} 
          />
        )}
        {children && (
          <span className={cn(
            "text-sm",
            variant === 'default' && "mt-xs"
          )}>
            {children}
          </span>
        )}
      </div>
    </div>
  );
}

// ============================================================================
// Loading Overlay
// ============================================================================

interface LoadingOverlayProps {
  isLoading: boolean;
  children: React.ReactNode;
  message?: string;
  className?: string;
  spinnerSize?: 'sm' | 'default' | 'lg';
}

export function LoadingOverlay({
  isLoading,
  children,
  message = 'Loading...',
  className,
  spinnerSize = 'default'
}: LoadingOverlayProps) {
  return (
    <div className={cn("relative", className)}>
      {children}
      {isLoading && (
        <div className="absolute inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center z-10">
          <div className="flex flex-col items-center gap-md p-2xl bg-background border rounded-lg shadow-lg">
            <LoadingSpinner size={spinnerSize} />
            <span className="text-sm text-muted-foreground">{message}</span>
          </div>
        </div>
      )}
    </div>
  );
}

// ============================================================================
// Button Loading States
// ============================================================================

interface LoadingButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  isLoading?: boolean;
  loadingText?: string;
  variant?: 'default' | 'destructive' | 'outline' | 'secondary' | 'ghost' | 'link';
  size?: 'default' | 'sm' | 'lg' | 'icon';
}

export function LoadingButton({
  isLoading = false,
  loadingText,
  children,
  disabled,
  className,
  ...props
}: LoadingButtonProps) {
  return (
    <Button
      {...props}
      disabled={disabled || isLoading}
      className={className}
    >
      {isLoading && (
        <LoadingSpinner size="sm" className="mr-2" />
      )}
      {isLoading && loadingText ? loadingText : children}
    </Button>
  );
}

// ============================================================================
// Pulse Animation Component
// ============================================================================

interface PulseProps {
  children: React.ReactNode;
  className?: string;
  duration?: 'fast' | 'normal' | 'slow';
}

export function Pulse({ children, className, duration = 'normal' }: PulseProps) {
  const durationClasses = {
    fast: 'animate-pulse [animation-duration:1s]',
    normal: 'animate-pulse',
    slow: 'animate-pulse [animation-duration:3s]'
  };

  return (
    <div className={cn(durationClasses[duration], className)}>
      {children}
    </div>
  );
}

// ============================================================================
// Loading Dots Animation
// ============================================================================

interface LoadingDotsProps {
  className?: string;
  size?: 'sm' | 'default' | 'lg';
}

export function LoadingDots({ className, size = 'default' }: LoadingDotsProps) {
  const sizeClasses = {
    sm: 'h-xs w-xs',
    default: 'h-xs w-xs',
    lg: 'h-xs w-xs'
  };

  const dotClass = cn(
    "bg-current rounded-full animate-pulse",
    sizeClasses[size]
  );

  return (
    <div className={cn("flex items-center gap-0.5", className)}>
      <div className={cn(dotClass, "[animation-delay:0ms]")} />
      <div className={cn(dotClass, "[animation-delay:150ms]")} />
      <div className={cn(dotClass, "[animation-delay:300ms]")} />
    </div>
  );
}

// ============================================================================
// Progress Indicator
// ============================================================================

interface ProgressIndicatorProps {
  progress: number; // 0-100
  message?: string;
  className?: string;
  showPercentage?: boolean;
}

export function ProgressIndicator({
  progress,
  message,
  className,
  showPercentage = true
}: ProgressIndicatorProps) {
  return (
    <div className={cn("w-full space-y-xs", className)}>
      {(message || showPercentage) && (
        <div className="flex justify-between items-center text-sm">
          {message && <span className="text-muted-foreground">{message}</span>}
          {showPercentage && (
            <span className="text-muted-foreground font-medium">
              {Math.round(progress)}%
            </span>
          )}
        </div>
      )}
      <div className="h-xs bg-muted rounded-full overflow-hidden">
        <div 
          className="h-full bg-primary transition-all duration-300 ease-out"
          style={{ width: `${Math.min(100, Math.max(0, progress))}%` }}
        />
      </div>
    </div>
  );
}

// ============================================================================
// Inline Loading Components
// ============================================================================

export function InlineLoading({ 
  size = 'sm', 
  className 
}: { 
  size?: 'xs' | 'sm' | 'default';
  className?: string; 
}) {
  return (
    <LoadingSpinner 
      size={size} 
      className={cn("inline", className)} 
    />
  );
}

export function LoadingText({ 
  children, 
  className 
}: { 
  children: React.ReactNode;
  className?: string; 
}) {
  return (
    <span className={cn("flex items-center gap-xs text-muted-foreground", className)}>
      <LoadingSpinner size="xs" />
      {children}
    </span>
  );
}