/**
 * React Query Provider
 * 
 * Configures React Query for the UIStudio application with proper
 * error handling, caching strategies, and development tools.
 * 
 * @module QueryProvider
 */

import React from 'react';
import { 
  QueryClient, 
  QueryClientProvider,
  useQueryClient as useReactQueryClient,
  type DefaultOptions 
} from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';

import { UIStudioError, isUIStudioError } from '../utils/uistudioErrors';

// ============================================================================
// Query Client Configuration
// ============================================================================

/** Default query options optimized for UIStudio operations */
const defaultQueryOptions: DefaultOptions = {
  queries: {
    // Stale time for different types of data
    staleTime: 5 * 60 * 1000, // 5 minutes default
    
    // Cache time
    gcTime: 10 * 60 * 1000, // 10 minutes (formerly cacheTime)
    
    // Retry configuration
    retry: (failureCount, error) => {
      // Don't retry auth errors or client errors (4xx)
      if (isUIStudioError(error)) {
        if (error.status && error.status >= 400 && error.status < 500) {
          return false;
        }
      }
      
      // Retry up to 3 times for other errors
      return failureCount < 3;
    },
    
    // Retry delay with exponential backoff
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
    
    // Refetch configuration
    refetchOnWindowFocus: true,
    refetchOnReconnect: true,
    refetchOnMount: true,
    
    // Network mode - fail fast on network errors
    networkMode: 'online',
  },
  
  mutations: {
    // Retry mutations more conservatively
    retry: (failureCount, error) => {
      // Don't retry mutations with client errors
      if (isUIStudioError(error)) {
        if (error.status && error.status >= 400 && error.status < 500) {
          return false;
        }
      }
      
      // Only retry once for mutations
      return failureCount < 1;
    },
    
    // Network mode for mutations
    networkMode: 'online',
  }
};

/** Create configured query client */
function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: defaultQueryOptions,
    
    // Global error handling removed - use error boundaries instead
  });
}

// ============================================================================
// Provider Component
// ============================================================================

interface QueryProviderProps {
  children: React.ReactNode;
  /** Optional custom query client */
  client?: QueryClient;
  /** Whether to show React Query DevTools */
  showDevtools?: boolean;
}

/** 
 * React Query provider with UIStudio-optimized configuration
 */
export function QueryProvider({ 
  children, 
  client, 
  showDevtools = false // Temporarily disable devtools to fix React hooks issue
}: QueryProviderProps) {
  // Use provided client or create default one
  const queryClient = React.useMemo(() => client || createQueryClient(), [client]);

  return (
    <QueryClientProvider client={queryClient}>
      {children}
      {showDevtools && (
        <ReactQueryDevtools 
          initialIsOpen={false}
          position={"bottom-right" as any}
          buttonPosition={"bottom-right" as const}
        />
      )}
    </QueryClientProvider>
  );
}

// ============================================================================
// Hook for Query Client Access
// ============================================================================

/**
 * Hook to access the query client instance
 * Useful for imperative operations outside of hooks
 */
export function useQueryClient() {
  const client = useReactQueryClient();
  
  if (!client) {
    throw new Error('useQueryClient must be used within a QueryProvider');
  }
  
  return client;
}

// ============================================================================
// Specialized Query Configurations
// ============================================================================

/** Query options for real-time data that updates frequently */
export const realtimeQueryOptions = {
  staleTime: 30 * 1000, // 30 seconds
  refetchInterval: 60 * 1000, // Refetch every minute
  refetchIntervalInBackground: true,
} as const;

/** Query options for static/slow-changing data */
export const staticQueryOptions = {
  staleTime: 30 * 60 * 1000, // 30 minutes
  gcTime: 60 * 60 * 1000, // 1 hour
  refetchOnWindowFocus: false,
} as const;

/** Query options for user-specific data */
export const userDataQueryOptions = {
  staleTime: 5 * 60 * 1000, // 5 minutes
  refetchOnWindowFocus: true,
  refetchOnMount: true,
} as const;

/** Query options for configuration data */
export const configQueryOptions = {
  staleTime: 15 * 60 * 1000, // 15 minutes
  gcTime: 30 * 60 * 1000, // 30 minutes
  refetchOnWindowFocus: false,
  refetchOnMount: false,
} as const;

// ============================================================================
// Error Boundary for React Query
// ============================================================================

interface QueryErrorBoundaryState {
  hasError: boolean;
  error?: Error;
}

class QueryErrorBoundary extends React.Component<
  { children: React.ReactNode; fallback?: React.ComponentType<{ error: Error; retry: () => void }> },
  QueryErrorBoundaryState
> {
  constructor(props: { children: React.ReactNode; fallback?: React.ComponentType<{ error: Error; retry: () => void }> }) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): QueryErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('React Query Error Boundary caught an error:', error, errorInfo);
  }

  render() {
    if (this.state.hasError && this.state.error) {
      if (this.props.fallback) {
        const FallbackComponent = this.props.fallback;
        return (
          <FallbackComponent 
            error={this.state.error} 
            retry={() => this.setState({ hasError: false, error: undefined })}
          />
        );
      }

      // Default error UI
      return (
        <div className="flex flex-col items-center justify-center min-h-[400px] p-8 text-center">
          <div className="max-w-md">
            <h2 className="text-xl font-semibold text-red-600 mb-4">
              Something went wrong
            </h2>
            <p className="text-gray-600 mb-6">
              An error occurred while loading data. Please try again.
            </p>
            <button
              onClick={() => this.setState({ hasError: false, error: undefined })}
              className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors"
            >
              Try Again
            </button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

/** 
 * Combined provider with error boundary
 */
export function UIStudioQueryProvider({ 
  children, 
  client,
  showDevtools,
  errorFallback
}: QueryProviderProps & { 
  errorFallback?: React.ComponentType<{ error: Error; retry: () => void }> 
}) {
  return (
    <QueryErrorBoundary fallback={errorFallback}>
      <QueryProvider client={client} showDevtools={showDevtools}>
        {children}
      </QueryProvider>
    </QueryErrorBoundary>
  );
}

// ============================================================================
// Development Utilities
// ============================================================================

/** Development helper to inspect query cache */
export function logQueryCache(client: QueryClient) {
  if (process.env.NODE_ENV === 'development') {
    console.log('Query Cache State:', client.getQueryCache().getAll());
  }
}

/** Development helper to clear all queries */
export function clearAllQueries(client: QueryClient) {
  if (process.env.NODE_ENV === 'development') {
    client.clear();
    console.log('All queries cleared');
  }
}

// Export the singleton query client for use outside React components
export const defaultQueryClient = createQueryClient();