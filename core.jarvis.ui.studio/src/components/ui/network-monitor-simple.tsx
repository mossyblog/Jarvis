import React, { useState, useEffect } from 'react';
import { Activity } from 'lucide-react';

export function NetworkMonitor() {
  const [requestCount, setRequestCount] = useState(0);
  const [responseCount, setResponseCount] = useState(0);

  useEffect(() => {
    console.log('NetworkMonitor component mounted');
    
    // Simple fetch interceptor for testing
    const originalFetch = window.fetch;
    
    window.fetch = async (...args) => {
      console.log('Fetch intercepted:', args[0]);
      setRequestCount(prev => prev + 1);
      
      const response = await originalFetch(...args);
      setResponseCount(prev => prev + 1);
      return response;
    };

    return () => {
      window.fetch = originalFetch;
    };
  }, []);

  return (
    <div className="fixed bottom-4 right-4 z-50 bg-card border border-border rounded-lg shadow-lg p-2">
      <div className="flex items-center gap-2 text-xs">
        <span className="text-muted-foreground">Network Req</span>
        <Activity size={12} className="text-primary" />
        <span className="text-muted-foreground">:</span>
        <div className="flex items-center gap-1">
          <span className="font-mono">{requestCount}</span>
          <span className="text-blue-500">↑</span>
        </div>
        <span className="text-muted-foreground">|</span>
        <div className="flex items-center gap-1">
          <span className="font-mono">{responseCount}</span>
          <span className="text-green-500">↓</span>
        </div>
      </div>
    </div>
  );
}