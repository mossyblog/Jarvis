/**
 * ComponentRenderer - Renders components based on their type
 * 
 * Enhanced version with UIStudio integration for live data binding.
 * Supports real-time data updates, component bindings, and ECS integration.
 * 
 * Features:
 * - Live data binding to ECS components
 * - Real-time data updates via React Query
 * - Field mapping configuration
 * - Component state synchronization
 * - Error boundaries with recovery
 * - Performance optimizations with memo
 */

import React, { Suspense, useMemo, useCallback, useState, useEffect } from 'react';
import { Box, Loader2, Wifi, WifiOff, AlertCircle } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { toast } from 'sonner';

import type { GridComponent, Size } from '@/types/bento';
import { DeviceType } from '@/types/bento';
import type { UIStudioComponentBinding } from '@/types/uistudio';
import { cn } from '@/lib/utils';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';

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
  data?: {
    binding?: UIStudioComponentBinding;
    liveData?: boolean;
    [key: string]: unknown;
  };
  
  /** Enable real-time data updates */
  enableLiveData?: boolean;
  
  /** Refresh interval for live data (ms) */
  refreshInterval?: number;
  
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
  liveData?: Record<string, unknown>;
  binding?: UIStudioComponentBinding;
  loading?: boolean;
  error?: Error;
  onReady?: () => void;
  onError?: (error: Error) => void;
  onAction?: (action: ComponentAction) => void;
  onDataUpdate?: (data: unknown) => void;
}

interface LiveDataState {
  isConnected: boolean;
  lastUpdate: Date | null;
  updateCount: number;
  errors: string[];
}

// ============================================================================
// Component Registry
// ============================================================================

/**
 * Enhanced registry of available component types with live data support
 * Components now receive live data, bindings, and can react to real-time updates
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
  
  // Enhanced metric component with live data support
  'metric': ({ className, liveData, binding, ...props }) => {
    // Use live data if available, fallback to static props
    const title = liveData?.title || props.title || 'Metric';
    const value = liveData?.value || props.value || '0';
    const change = liveData?.change || props.change || '+0%';
    
    const isPositive = String(change).startsWith('+');
    const hasLiveData = !!liveData;
    
    return (
      <Card className={cn('h-full', hasLiveData && 'ring-2 ring-blue-200', className)}>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium text-muted-foreground flex items-center justify-between">
            {String(title)}
            {hasLiveData && (
              <Badge variant="secondary" className="text-xs">
                Live
              </Badge>
            )}
          </CardTitle>
        </CardHeader>
        <CardContent className="pt-0 flex flex-col justify-center h-[calc(100%-50px)]">
          <div className="space-y-2">
            <div className={cn(
              'text-3xl font-bold transition-all duration-500',
              hasLiveData && 'text-blue-600'
            )}>
              {String(value)}
            </div>
            <div className={cn(
              "text-sm font-medium flex items-center transition-all duration-300",
              isPositive ? "text-green-600 dark:text-green-400" : "text-red-600 dark:text-red-400"
            )}>
              <span className="inline-block mr-1">
                {isPositive ? '↑' : '↓'}
              </span>
              {String(change)}
            </div>
            {binding?.dataSourceConfig && (
              <div className="text-xs text-muted-foreground">
                Source: {JSON.stringify(binding.dataSourceConfig)}
              </div>
            )}
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
 * Loading fallback component with personality
 */
const ComponentLoadingFallback: React.FC<{ 
  className?: string;
  isLiveData?: boolean;
  connectionStatus?: boolean;
}> = ({ className, isLiveData = false, connectionStatus = false }) => {
  const loadingMessages = isLiveData ? [
    '📡 Connecting to live data...',
    '🔄 Syncing with data source...',
    '⚡ Establishing real-time connection...',
    '🌐 Loading fresh data...',
    '📊 Preparing live updates...',
  ] : [
    '🎨 Crafting something beautiful...',
    '✨ Sprinkling some magic...',
    '🚀 Getting ready for launch...',
    '🎯 Preparing the perfect component...',
    '🔮 Consulting the component oracle...',
  ];
  
  const [message] = React.useState(() => 
    loadingMessages[Math.floor(Math.random() * loadingMessages.length)]
  );
  
  return (
    <Card className={cn('h-full loading-shimmer', className)}>
      <CardContent className="flex items-center justify-center h-full">
        <div className="text-center">
          <div className="relative">
            <Loader2 className="h-8 w-8 animate-spin mx-auto mb-3 text-blue-500" />
            <div className="absolute inset-0 h-8 w-8 mx-auto mb-3 border-2 border-transparent border-t-purple-500 rounded-full animate-spin" style={{ animationDirection: 'reverse', animationDuration: '0.8s' }}></div>
          </div>
          <p className="text-sm text-muted-foreground animate-pulse">{message}</p>
          
          {/* Live data connection indicator */}
          {isLiveData && (
            <div className="mt-2 flex items-center justify-center gap-2">
              <div className={cn(
                'w-2 h-2 rounded-full',
                connectionStatus ? 'bg-green-500 animate-pulse' : 'bg-gray-400'
              )}></div>
              <span className="text-xs text-muted-foreground">
                {connectionStatus ? 'Connected' : 'Connecting...'}
              </span>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
};

/**
 * Error fallback component with friendly personality
 */
const ComponentErrorFallback: React.FC<{ 
  error?: Error; 
  className?: string;
  onRetry?: () => void;
  isLiveDataError?: boolean;
}> = ({ error, className, onRetry, isLiveDataError = false }) => {
  const errorPersonalities = isLiveDataError ? [
    { emoji: '📡', message: 'Lost connection to data source.' },
    { emoji: '🌐', message: 'Network hiccup detected!' },
    { emoji: '⚠️', message: 'Data sync temporarily unavailable.' },
    { emoji: '🔄', message: 'Retrying connection...' },
    { emoji: '📊', message: 'Data pipeline needs attention.' },
  ] : [
    { emoji: '🤖', message: 'Oops! This component had a robot malfunction.' },
    { emoji: '🎭', message: 'This component is having stage fright.' },
    { emoji: '🔧', message: 'Something needs a little fixing here.' },
    { emoji: '🎪', message: 'The show must go on, but this act needs work.' },
    { emoji: '🎨', message: 'This masterpiece is still in progress.' },
  ];
  
  const [personality] = React.useState(() => 
    errorPersonalities[Math.floor(Math.random() * errorPersonalities.length)]
  );
  
  return (
    <Card className={cn(
      'h-full border-orange-300 bg-orange-50/50',
      isLiveDataError && 'border-red-300 bg-red-50/50',
      className
    )}>
      <CardContent className="flex items-center justify-center h-full p-4">
        <div className="text-center">
          <div className="text-4xl mb-2 animate-bounce">{personality.emoji}</div>
          <p className={cn(
            'text-sm font-medium mb-1',
            isLiveDataError ? 'text-red-700' : 'text-orange-700'
          )}>
            {personality.message}
          </p>
          <p className={cn(
            'text-xs mb-3',
            isLiveDataError ? 'text-red-600' : 'text-orange-600'
          )}>
            {error?.message || (isLiveDataError ? 'Data connection issue' : 'No worries, these things happen!')}
          </p>
          {onRetry && (
            <Button
              variant="outline"
              size="sm"
              onClick={onRetry}
              className={cn(
                'text-xs',
                isLiveDataError 
                  ? 'border-red-300 text-red-700 hover:bg-red-100'
                  : 'border-orange-300 text-orange-700 hover:bg-orange-100'
              )}
            >
              {isLiveDataError ? '🔄 Reconnect' : '✨ Try Again'}
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
};

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
  enableLiveData = false,
  refreshInterval = 30000, // 30 seconds default
  onReady,
  onError,
  onAction,
}) => {
  // Live data state management
  const [liveDataState, setLiveDataState] = useState<LiveDataState>({
    isConnected: false,
    lastUpdate: null,
    updateCount: 0,
    errors: []
  });
  
  // Extract binding information from data
  const binding = data?.binding as UIStudioComponentBinding | undefined;
  const shouldFetchLiveData = enableLiveData && binding?.dataSourceConfig && data?.liveData;
  
  // Live data query (mock implementation - replace with actual ECS data fetching)
  const liveDataQuery = useQuery({
    queryKey: ['component-data', component.id, binding?.dataSourceConfig],
    queryFn: async () => {
      if (!binding?.dataSourceConfig) return null;
      
      // Mock live data fetching - replace with actual ECS query
      // This would typically fetch from your ECS system based on the binding configuration
      const response = await fetch(`/api/ecs/components/${JSON.stringify(binding.dataSourceConfig)}`);
      if (!response.ok) throw new Error('Failed to fetch component data');
      
      return response.json();
    },
    enabled: shouldFetchLiveData,
    refetchInterval: shouldFetchLiveData ? refreshInterval : false,
    staleTime: refreshInterval / 2, // Consider data stale after half the refresh interval
    // React Query v5 removed onSuccess/onError - handle via useEffect
  });
  
  // Handle live data query success/error states
  useEffect(() => {
    if (liveDataQuery.isSuccess) {
      setLiveDataState(prev => ({
        ...prev,
        isConnected: true,
        lastUpdate: new Date(),
        updateCount: prev.updateCount + 1,
        errors: []
      }));
    }
    
    if (liveDataQuery.isError) {
      const errorMessage = liveDataQuery.error instanceof Error ? liveDataQuery.error.message : 'Failed to fetch live data';
      setLiveDataState(prev => ({
        ...prev,
        isConnected: false,
        errors: [...prev.errors.slice(-4), errorMessage] // Keep last 5 errors
      }));
    }
  }, [liveDataQuery.isSuccess, liveDataQuery.isError, liveDataQuery.error]);
  
  // Extract data from query result
  const liveData = liveDataQuery.data;
  const liveDataLoading = liveDataQuery.isLoading;
  const liveDataError = liveDataQuery.error;
  const liveDataSuccess = liveDataQuery.isSuccess;
  
  // Apply field mappings to live data
  const mappedData = useMemo(() => {
    if (!liveData || !binding?.fieldMappings) return liveData;
    
    const mapped: Record<string, unknown> = {};
    Object.entries(binding.fieldMappings).forEach(([targetField, sourceField]) => {
      if (typeof sourceField === 'string' && liveData[sourceField] !== undefined) {
        mapped[targetField] = liveData[sourceField];
      }
    });
    
    return { ...liveData, ...mapped };
  }, [liveData, binding?.fieldMappings]);
  // Get the component from registry
  const ComponentToRender = useMemo(() => {
    return COMPONENT_REGISTRY[component.componentType];
  }, [component.componentType]);
  
  // Enhanced error handling for live data
  const effectiveError = error || liveDataError || undefined;
  const effectiveLoading = loading || (shouldFetchLiveData && liveDataLoading);

  // Handle component ready callback
  const handleReady = useCallback(() => {
    onReady?.();
  }, [onReady]);
  
  // Notify parent of data updates
  useEffect(() => {
    if (liveDataSuccess && mappedData) {
      // Could call onAction with data update event
      onAction?.({
        action: 'data_updated',
        payload: {
          componentId: component.id,
          data: mappedData,
          timestamp: new Date().toISOString()
        }
      });
    }
  }, [liveDataSuccess, mappedData, component.id, onAction]);

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
  if (effectiveLoading) {
    return (
      <ComponentLoadingFallback 
        isLiveData={shouldFetchLiveData}
        connectionStatus={liveDataState.isConnected}
      />
    );
  }

  // Show error state
  if (effectiveError) {
    return (
      <ComponentErrorFallback 
        error={effectiveError} 
        isLiveDataError={!!liveDataError}
        onRetry={() => {
          if (liveDataError) {
            // Retry live data query
            window.location.reload(); // Simple retry - could be more sophisticated
          }
        }}
      />
    );
  }

  // Show unknown component fallback
  if (!ComponentToRender) {
    return <ComponentNotFoundFallback componentType={component.componentType} />;
  }

  // Common props passed to all components
  const componentProps: BentoComponentProps = {
    gridSize,
    deviceType,
    data: data || {},
    liveData: mappedData,
    binding,
    loading: effectiveLoading,
    error: effectiveError,
    onReady: handleReady,
    onError: handleError,
    onAction: handleAction,
  };

  return (
    <ComponentErrorBoundary onError={handleError}>
      <div className="relative">
        {/* Live data indicators */}
        {shouldFetchLiveData && (
          <div className="absolute top-2 right-2 z-10 flex items-center gap-2">
            {/* Connection status */}
            <Badge 
              variant={liveDataState.isConnected ? 'default' : 'destructive'} 
              className="text-xs px-2 py-1"
            >
              {liveDataState.isConnected ? (
                <><Wifi className="w-3 h-3 mr-1" />Live</>
              ) : (
                <><WifiOff className="w-3 h-3 mr-1" />Offline</>
              )}
            </Badge>
            
            {/* Update counter */}
            {liveDataState.updateCount > 0 && (
              <Badge variant="secondary" className="text-xs px-2 py-1">
                {liveDataState.updateCount} updates
              </Badge>
            )}
            
            {/* Error indicator */}
            {liveDataState.errors.length > 0 && (
              <Badge variant="destructive" className="text-xs px-2 py-1" title={liveDataState.errors.join(', ')}>
                <AlertCircle className="w-3 h-3" />
              </Badge>
            )}
          </div>
        )}
        
        <Suspense fallback={<ComponentLoadingFallback isLiveData={shouldFetchLiveData} />}>
          <ComponentToRender
            {...componentProps}
            {...component.props}
          />
        </Suspense>
      </div>
    </ComponentErrorBoundary>
  );
};

ComponentRenderer.displayName = 'ComponentRenderer';