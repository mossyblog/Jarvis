/**
 * ComponentRenderer - Renders components based on their type
 * 
 * This component acts as a factory/registry that renders the appropriate
 * React component based on the component type from the grid configuration.
 * It handles component loading, error states, and provides a fallback
 * for unknown component types.
 */

import React, { Suspense, useMemo, useCallback } from 'react';
import { AlertCircle, Box, Loader2 } from 'lucide-react';

import type { GridComponent, Size } from '@/types/bento';
import { DeviceType } from '@/types/bento';
import { cn } from '@/lib/utils';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

// ============================================================================
// Types
// ============================================================================

export interface ComponentRendererProps {
  /** The grid component to render */
  component: GridComponent;
  
  /** Grid size information for responsive behavior */
  gridSize?: Size;
  
  /** Current device type */
  deviceType?: DeviceType;
  
  /** Whether the component is in loading state */
  loading?: boolean;
  
  /** Error state if component failed to load */
  error?: Error;
  
  /** Additional data passed to the component */
  data?: unknown;
  
  /** Event handlers */
  onReady?: () => void;
  onError?: (error: Error) => void;
  onAction?: (action: ComponentAction) => void;
}

export interface ComponentAction {
  action: string;
  payload?: unknown;
}

export interface BentoComponentProps {
  className?: string;
  style?: React.CSSProperties;
  gridSize?: Size;
  deviceType?: DeviceType;
  data?: unknown;
  loading?: boolean;
  error?: Error;
  onReady?: () => void;
  onError?: (error: Error) => void;
  onAction?: (action: ComponentAction) => void;
}

// ============================================================================
// Component Registry
// ============================================================================

/**
 * Registry of available component types
 * In a real implementation, this would be populated from a component registry
 * or loaded dynamically from a component library
 */
const COMPONENT_REGISTRY: Record<string, React.ComponentType<BentoComponentProps & Record<string, unknown>>> = {
  // Example components - these would be replaced with actual component implementations
  
  // Placeholder component for demonstration
  'placeholder': ({ className, gridSize }) => (
    <Card className={cn('h-full', className)}>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm">Placeholder</CardTitle>
      </CardHeader>
      <CardContent className="pt-0">
        <div className="flex items-center justify-center h-full min-h-[60px] text-muted-foreground">
          <div className="text-center">
            <Box className="h-8 w-8 mx-auto mb-2 opacity-50" />
            <p className="text-xs">
              {gridSize ? `${gridSize.w}×${gridSize.h}` : 'Component'}
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  ),
  
  // Text component
  'text': ({ className, ...props }) => {
    const text = (props as { text?: string }).text || 'Sample text';
    return (
      <Card className={cn('h-full', className)}>
        <CardContent className="flex items-center justify-center h-full p-4">
          <div className="text-center">
            <p className="text-sm">{text}</p>
          </div>
        </CardContent>
      </Card>
    );
  },
  
  // Metric component
  'metric': ({ className, ...props }) => {
    const { title = 'Metric', value = '0', change = '+0%' } = props as {
      title?: string;
      value?: string;
      change?: string;
    };
    
    const isPositive = change.startsWith('+');
    
    return (
      <Card className={cn('h-full', className)}>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        </CardHeader>
        <CardContent className="pt-0 flex flex-col justify-center h-[calc(100%-50px)]">
          <div className="space-y-2">
            <div className="text-3xl font-bold">{value}</div>
            <div className={cn(
              "text-sm font-medium flex items-center",
              isPositive ? "text-green-600 dark:text-green-400" : "text-red-600 dark:text-red-400"
            )}>
              <span className="inline-block mr-1">
                {isPositive ? '↑' : '↓'}
              </span>
              {change}
            </div>
          </div>
        </CardContent>
      </Card>
    );
  },
  
  // Chart component placeholder
  'chart': ({ className, ...props }) => {
    const { title = 'Sales Overview' } = props as { title?: string };
    return (
      <Card className={cn('h-full bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-950/20 dark:to-indigo-950/20', className)}>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">{title}</CardTitle>
        </CardHeader>
        <CardContent className="pt-0 h-[calc(100%-60px)]">
          <div className="flex items-center justify-center h-full min-h-[150px] text-muted-foreground">
            <div className="w-full h-full flex flex-col justify-end">
              <div className="flex items-end justify-around h-full p-4">
                {[65, 45, 78, 52, 88, 62, 95, 71].map((height, i) => (
                  <div
                    key={i}
                    className="bg-gradient-to-t from-blue-500 to-blue-400 rounded-t-sm flex-1 mx-1"
                    style={{
                      height: `${height}%`,
                      maxWidth: '40px',
                    }}
                  />
                ))}
              </div>
              <p className="text-xs text-center pb-2">Chart Data</p>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  },
};

// ============================================================================
// Error Boundary Component
// ============================================================================

interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
}

class ComponentErrorBoundary extends React.Component<
  { children: React.ReactNode; onError?: (error: Error) => void },
  ErrorBoundaryState
> {
  constructor(props: { children: React.ReactNode; onError?: (error: Error) => void }) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Component render error:', error, errorInfo);
    this.props.onError?.(error);
  }

  render() {
    if (this.state.hasError) {
      return <ComponentErrorFallback error={this.state.error} />;
    }

    return this.props.children;
  }
}

// ============================================================================
// Fallback Components
// ============================================================================

/**
 * Loading fallback component
 */
const ComponentLoadingFallback: React.FC<{ className?: string }> = ({ className }) => (
  <Card className={cn('h-full', className)}>
    <CardContent className="flex items-center justify-center h-full">
      <div className="text-center">
        <Loader2 className="h-6 w-6 animate-spin mx-auto mb-2 text-muted-foreground" />
        <p className="text-xs text-muted-foreground">Loading...</p>
      </div>
    </CardContent>
  </Card>
);

/**
 * Error fallback component
 */
const ComponentErrorFallback: React.FC<{ 
  error?: Error; 
  className?: string;
  onRetry?: () => void;
}> = ({ error, className, onRetry }) => (
  <Card className={cn('h-full border-destructive', className)}>
    <CardContent className="flex items-center justify-center h-full p-4">
      <div className="text-center">
        <AlertCircle className="h-6 w-6 mx-auto mb-2 text-destructive" />
        <p className="text-xs text-destructive mb-2">
          {error?.message || 'Component failed to load'}
        </p>
        {onRetry && (
          <Button
            variant="outline"
            size="sm"
            onClick={onRetry}
            className="text-xs"
          >
            Retry
          </Button>
        )}
      </div>
    </CardContent>
  </Card>
);

/**
 * Unknown component type fallback
 */
const ComponentNotFoundFallback: React.FC<{ 
  componentType: string; 
  className?: string;
}> = ({ componentType, className }) => (
  <Card className={cn('h-full border-dashed', className)}>
    <CardContent className="flex items-center justify-center h-full p-4">
      <div className="text-center">
        <Box className="h-6 w-6 mx-auto mb-2 text-muted-foreground" />
        <p className="text-xs text-muted-foreground mb-1">
          Unknown component
        </p>
        <p className="text-xs text-muted-foreground font-mono">
          {componentType}
        </p>
      </div>
    </CardContent>
  </Card>
);

// ============================================================================
// Main Component
// ============================================================================

export const ComponentRenderer: React.FC<ComponentRendererProps> = ({
  component,
  gridSize,
  deviceType = DeviceType.Desktop,
  loading = false,
  error,
  data,
  onReady,
  onError,
  onAction,
}) => {
  // Get the component from registry
  const ComponentToRender = useMemo(() => {
    return COMPONENT_REGISTRY[component.componentType];
  }, [component.componentType]);

  // Handle component ready callback
  const handleReady = useCallback(() => {
    onReady?.();
  }, [onReady]);

  // Handle component error
  const handleError = useCallback((componentError: Error) => {
    console.error(`Error in component ${component.id}:`, componentError);
    onError?.(componentError);
  }, [component.id, onError]);

  // Handle component action
  const handleAction = useCallback((action: ComponentAction) => {
    onAction?.({
      ...action,
      // Add component context to action
      payload: {
        ...(action.payload as Record<string, unknown> || {}),
        componentId: component.id,
        componentType: component.componentType,
      },
    });
  }, [component.id, component.componentType, onAction]);

  // Show loading state
  if (loading) {
    return <ComponentLoadingFallback />;
  }

  // Show error state
  if (error) {
    return <ComponentErrorFallback error={error} />;
  }

  // Show unknown component fallback
  if (!ComponentToRender) {
    return <ComponentNotFoundFallback componentType={component.componentType} />;
  }

  // Common props passed to all components
  const componentProps: BentoComponentProps = {
    gridSize,
    deviceType,
    data,
    loading,
    error,
    onReady: handleReady,
    onError: handleError,
    onAction: handleAction,
  };

  return (
    <ComponentErrorBoundary onError={handleError}>
      <Suspense fallback={<ComponentLoadingFallback />}>
        <ComponentToRender
          {...componentProps}
          {...component.props}
        />
      </Suspense>
    </ComponentErrorBoundary>
  );
};

ComponentRenderer.displayName = 'ComponentRenderer';