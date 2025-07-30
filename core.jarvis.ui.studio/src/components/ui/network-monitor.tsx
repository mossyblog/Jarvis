import React, { useState, useEffect, useRef } from 'react';
import { ArrowDown, ArrowUp, Activity } from 'lucide-react';
import { cn } from '../../lib/utils';

interface NetworkStats {
  requestsIn: number;
  requestsOut: number;
  bytesIn: number;
  bytesOut: number;
  avgResponseTime: number;
  activeRequests: number;
}

interface NetworkActivity {
  timestamp: number;
  type: 'request' | 'response';
  url: string;
  method: string;
  size: number;
  duration?: number;
}

export function NetworkMonitor() {
  const [stats, setStats] = useState<NetworkStats>({
    requestsIn: 0,
    requestsOut: 0,
    bytesIn: 0,
    bytesOut: 0,
    avgResponseTime: 0,
    activeRequests: 0
  });
  const [recentActivity, setRecentActivity] = useState<NetworkActivity[]>([]);
  const [isVisible, setIsVisible] = useState(false);
  const originalFetch = useRef<typeof fetch>();
  const startTime = useRef<{ [key: string]: number }>({});

  useEffect(() => {
    // Store original fetch
    originalFetch.current = window.fetch;

    // Intercept fetch requests
    window.fetch = async (...args) => {
      const url = typeof args[0] === 'string' ? args[0] : args[0].url;
      const method = args[1]?.method || 'GET';
      const requestId = `${Date.now()}-${Math.random()}`;
      
      // Track request start
      startTime.current[requestId] = Date.now();
      
      // Update stats for outgoing request
      setStats(prev => ({
        ...prev,
        requestsOut: prev.requestsOut + 1,
        activeRequests: prev.activeRequests + 1
      }));

      // Add request activity
      const requestActivity: NetworkActivity = {
        timestamp: Date.now(),
        type: 'request',
        url,
        method,
        size: 0 // We don't have easy access to request body size
      };

      setRecentActivity(prev => [requestActivity, ...prev.slice(0, 9)]);

      try {
        const response = await originalFetch.current!(...args);
        const endTime = Date.now();
        const duration = endTime - startTime.current[requestId];
        delete startTime.current[requestId];

        // Estimate response size (not exact, but good approximation)
        const responseSize = parseInt(response.headers.get('content-length') || '0') || 
                           (response.clone().text().then(text => text.length).catch(() => 0));

        // Update stats for incoming response
        setStats(prev => {
          const newAvgTime = prev.requestsIn === 0 ? duration : 
            (prev.avgResponseTime * prev.requestsIn + duration) / (prev.requestsIn + 1);
          
          return {
            ...prev,
            requestsIn: prev.requestsIn + 1,
            bytesIn: prev.bytesIn + (typeof responseSize === 'number' ? responseSize : 0),
            avgResponseTime: newAvgTime,
            activeRequests: Math.max(0, prev.activeRequests - 1)
          };
        });

        // Add response activity
        const responseActivity: NetworkActivity = {
          timestamp: Date.now(),
          type: 'response',
          url,
          method,
          size: typeof responseSize === 'number' ? responseSize : 0,
          duration
        };

        setRecentActivity(prev => [responseActivity, ...prev.slice(0, 9)]);

        return response;
      } catch (error) {
        // Handle request error
        delete startTime.current[requestId];
        setStats(prev => ({
          ...prev,
          activeRequests: Math.max(0, prev.activeRequests - 1)
        }));
        throw error;
      }
    };

    return () => {
      // Restore original fetch
      if (originalFetch.current) {
        window.fetch = originalFetch.current;
      }
    };
  }, []);

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
  };

  const formatSpeed = (bytesPerSecond: number): string => {
    return `${formatBytes(bytesPerSecond)}/s`;
  };

  const getNetworkSpeed = (): number => {
    const recentRequests = recentActivity.filter(
      activity => Date.now() - activity.timestamp < 5000 // Last 5 seconds
    );
    const totalBytes = recentRequests.reduce((sum, activity) => sum + activity.size, 0);
    return totalBytes / 5; // bytes per second over last 5 seconds
  };

  return (
    <div className="fixed bottom-4 right-4 z-50">
      <div
        className={cn(
          "bg-card/95 backdrop-blur-sm border border-border rounded-lg shadow-lg transition-all duration-200",
          isVisible ? "w-80 p-3" : "w-auto p-2 cursor-pointer hover:bg-accent/50"
        )}
        onClick={() => !isVisible && setIsVisible(true)}
      >
        {isVisible ? (
          <div className="space-y-2">
            {/* Header */}
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Activity size={14} className="text-primary" />
                <span className="text-xs font-medium">Network Activity</span>
              </div>
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  setIsVisible(false);
                }}
                className="text-xs text-muted-foreground hover:text-foreground"
              >
                ×
              </button>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-2 gap-2 text-xs">
              <div className="space-y-1">
                <div className="flex items-center gap-1">
                  <ArrowUp size={10} className="text-blue-500" />
                  <span className="text-muted-foreground">Out:</span>
                  <span className="font-mono">{stats.requestsOut}</span>
                </div>
                <div className="flex items-center gap-1">
                  <ArrowDown size={10} className="text-green-500" />
                  <span className="text-muted-foreground">In:</span>
                  <span className="font-mono">{stats.requestsIn}</span>
                </div>
              </div>
              <div className="space-y-1">
                <div className="flex items-center gap-1">
                  <span className="text-muted-foreground">Speed:</span>
                  <span className="font-mono text-xs">{formatSpeed(getNetworkSpeed())}</span>
                </div>
                <div className="flex items-center gap-1">
                  <span className="text-muted-foreground">Avg:</span>
                  <span className="font-mono text-xs">{Math.round(stats.avgResponseTime)}ms</span>
                </div>
              </div>
            </div>

            {/* Active indicator */}
            {stats.activeRequests > 0 && (
              <div className="flex items-center gap-2 text-xs text-amber-500">
                <div className="animate-pulse w-1.5 h-1.5 bg-amber-500 rounded-full" />
                <span>{stats.activeRequests} active request{stats.activeRequests > 1 ? 's' : ''}</span>
              </div>
            )}

            {/* Recent activity */}
            <div className="space-y-1 max-h-32 overflow-y-auto">
              {recentActivity.slice(0, 5).map((activity, index) => (
                <div key={index} className="flex items-center gap-2 text-xs">
                  {activity.type === 'request' ? (
                    <ArrowUp size={8} className="text-blue-500 flex-shrink-0" />
                  ) : (
                    <ArrowDown size={8} className="text-green-500 flex-shrink-0" />
                  )}
                  <span className="font-mono text-muted-foreground text-xs">
                    {activity.method}
                  </span>
                  <span className="text-xs truncate flex-1">
                    {activity.url.replace(/^https?:\/\/[^\/]+/, '')}
                  </span>
                  {activity.duration && (
                    <span className="font-mono text-xs text-muted-foreground">
                      {activity.duration}ms
                    </span>
                  )}
                </div>
              ))}
            </div>
          </div>
        ) : (
          <div className="flex items-center gap-2">
            <Activity size={14} className={cn(
              "transition-colors",
              stats.activeRequests > 0 ? "text-amber-500 animate-pulse" : "text-muted-foreground"
            )} />
            <div className="flex items-center gap-1 text-xs font-mono">
              <ArrowDown size={10} className="text-green-500" />
              <span>{stats.requestsIn}</span>
              <ArrowUp size={10} className="text-blue-500" />
              <span>{stats.requestsOut}</span>
            </div>
            {stats.activeRequests > 0 && (
              <div className="text-xs text-amber-500">
                ({stats.activeRequests})
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}