import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';

interface ApiStatusContextType {
  isApiReachable: boolean;
  lastError: string | null;
  checkApiStatus: () => Promise<void>;
  setApiError: (error: string | null) => void;
}

const ApiStatusContext = createContext<ApiStatusContextType | null>(null);

export const useApiStatus = () => {
  const context = useContext(ApiStatusContext);
  if (!context) {
    throw new Error('useApiStatus must be used within ApiStatusProvider');
  }
  return context;
};

interface ApiStatusProviderProps {
  children: React.ReactNode;
}

export const ApiStatusProvider: React.FC<ApiStatusProviderProps> = ({ children }) => {
  const [isApiReachable, setIsApiReachable] = useState(true);
  const [lastError, setLastError] = useState<string | null>(null);

  const checkApiStatus = useCallback(async () => {
    try {
      const response = await fetch('/api/health', {
        method: 'GET',
        signal: AbortSignal.timeout(5000), // 5 second timeout
      });
      
      if (response.ok) {
        setIsApiReachable(true);
        setLastError(null);
      } else {
        setIsApiReachable(false);
        setLastError(`API returned status ${response.status}`);
      }
    } catch (error) {
      setIsApiReachable(false);
      if (error instanceof Error) {
        if (error.name === 'TypeError' && error.message.includes('fetch')) {
          setLastError('Cannot connect to API server');
        } else if (error.name === 'AbortError') {
          setLastError('API request timed out');
        } else {
          setLastError(error.message);
        }
      } else {
        setLastError('Unknown error connecting to API');
      }
    }
  }, []);

  const setApiError = useCallback((error: string | null) => {
    if (error) {
      setIsApiReachable(false);
      setLastError(error);
    } else {
      setIsApiReachable(true);
      setLastError(null);
    }
  }, []);

  // Check API status on mount and every 30 seconds
  useEffect(() => {
    checkApiStatus();
    const interval = setInterval(checkApiStatus, 30000);
    return () => clearInterval(interval);
  }, [checkApiStatus]);

  // Also set up a global fetch interceptor to catch API errors
  useEffect(() => {
    const originalFetch = window.fetch;
    
    window.fetch = async (...args) => {
      try {
        const response = await originalFetch(...args);
        
        // Check if this is an API call
        const url = args[0] as string;
        if (url?.startsWith('/api')) {
          if (!response.ok && response.status >= 500) {
            setApiError(`API server error (${response.status})`);
          } else if (response.ok && !isApiReachable) {
            // API is back online
            setApiError(null);
          }
        }
        
        return response;
      } catch (error) {
        // Check if this is an API call
        const url = args[0] as string;
        if (url?.startsWith('/api')) {
          if (error instanceof Error && error.name === 'TypeError') {
            setApiError('Cannot connect to API server');
          }
        }
        throw error;
      }
    };

    return () => {
      window.fetch = originalFetch;
    };
  }, [isApiReachable, setApiError]);

  return (
    <ApiStatusContext.Provider value={{ isApiReachable, lastError, checkApiStatus, setApiError }}>
      {children}
    </ApiStatusContext.Provider>
  );
};