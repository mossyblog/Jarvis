import React, { useState, useEffect } from 'react';
import { Activity, CheckCircle, XCircle, Clock, Wifi, WifiOff, AlertCircle, Code, Database, Globe } from 'lucide-react';
import { useApiStatus } from '../../contexts/ApiStatusContext';
import { cn } from '../../lib/utils';

interface BuildInfo {
  version: string;
  buildTime: string;
  commitHash: string;
  environment: string;
}

interface NetworkStats {
  requestCount: number;
  responseCount: number;
  errorCount: number;
  avgResponseTime: number;
}

export function StatusFooter() {
  const { isApiReachable, lastError } = useApiStatus();
  const [buildInfo, setBuildInfo] = useState<BuildInfo>({
    version: '0.0.0',
    buildTime: new Date().toISOString(),
    commitHash: 'dev',
    environment: 'development'
  });
  
  const [networkStats, setNetworkStats] = useState<NetworkStats>({
    requestCount: 0,
    responseCount: 0,
    errorCount: 0,
    avgResponseTime: 0
  });

  const [isOnline, setIsOnline] = useState(navigator.onLine);

  // Monitor online/offline status
  useEffect(() => {
    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  // Load build info on mount
  useEffect(() => {
    // Try to load build info from package.json or build artifacts
    const loadBuildInfo = async () => {
      try {
        // In a real app, this would come from a build-time generated file
        const packageResponse = await fetch('/package.json');
        if (packageResponse.ok) {
          const packageData = await packageResponse.json();
          setBuildInfo(prev => ({
            ...prev,
            version: packageData.version || '0.0.0'
          }));
        }
      } catch (error) {
        console.warn('Could not load build info:', error);
      }
    };

    loadBuildInfo();
  }, []);

  // Network monitoring
  useEffect(() => {
    const originalFetch = window.fetch;
    const responseTimes: number[] = [];

    window.fetch = async (...args) => {
      const startTime = performance.now();
      setNetworkStats(prev => ({ ...prev, requestCount: prev.requestCount + 1 }));

      try {
        const response = await originalFetch(...args);
        const endTime = performance.now();
        const responseTime = endTime - startTime;
        
        responseTimes.push(responseTime);
        if (responseTimes.length > 10) responseTimes.shift(); // Keep last 10
        
        const avgResponseTime = responseTimes.reduce((sum, time) => sum + time, 0) / responseTimes.length;
        
        setNetworkStats(prev => ({
          ...prev,
          responseCount: prev.responseCount + 1,
          avgResponseTime: Math.round(avgResponseTime)
        }));

        if (!response.ok) {
          setNetworkStats(prev => ({ ...prev, errorCount: prev.errorCount + 1 }));
        }

        return response;
      } catch (error) {
        setNetworkStats(prev => ({ ...prev, errorCount: prev.errorCount + 1 }));
        throw error;
      }
    };

    return () => {
      window.fetch = originalFetch;
    };
  }, []);

  const getConnectionStatus = () => {
    if (!isOnline) return { icon: WifiOff, color: 'text-destructive', text: 'Offline' };
    if (!isApiReachable) return { icon: XCircle, color: 'text-destructive', text: 'API Unreachable' };
    return { icon: CheckCircle, color: 'text-success', text: 'Connected' };
  };

  const connectionStatus = getConnectionStatus();

  return (
    <footer className="fixed bottom-0 left-0 right-0 bg-background/95 backdrop-blur-sm border-t border-border z-40" role="contentinfo" aria-label="Application status information">
      <div className="flex items-center justify-between px-4 py-2 text-xs">
        {/* Left section - Build & Version Info */}
        <div className="flex items-center gap-4" role="region" aria-label="Build and version information">
          <div className="flex items-center gap-2">
            <Code size={14} className="text-muted-foreground" />
            <span className="text-muted-foreground">v{buildInfo.version}</span>
            <span className="text-muted-foreground">•</span>
            <span className="text-muted-foreground">{buildInfo.environment}</span>
          </div>
          
          {buildInfo.commitHash !== 'dev' && (
            <div className="flex items-center gap-1">
              <span className="text-muted-foreground">@</span>
              <span className="font-mono text-muted-foreground text-[10px]">
                {buildInfo.commitHash.substring(0, 7)}
              </span>
            </div>
          )}
        </div>

        {/* Center section - Network Statistics */}
        <div className="hidden md:flex items-center gap-4" role="region" aria-label="Network statistics">
          <div className="flex items-center gap-2">
            <Activity size={14} className="text-primary" />
            <span className="text-muted-foreground">Requests:</span>
            <span className="font-mono">{networkStats.requestCount}</span>
            <span className="text-primary">↑</span>
            <span className="font-mono">{networkStats.responseCount}</span>
            <span className="text-success">↓</span>
          </div>

          {networkStats.errorCount > 0 && (
            <div className="flex items-center gap-1">
              <AlertCircle size={14} className="text-destructive" />
              <span className="text-destructive font-mono">{networkStats.errorCount}</span>
            </div>
          )}

          {networkStats.avgResponseTime > 0 && (
            <div className="flex items-center gap-1">
              <Clock size={14} className="text-muted-foreground" />
              <span className="text-muted-foreground">{networkStats.avgResponseTime}ms</span>
            </div>
          )}
        </div>

        {/* Right section - Connection Status */}
        <div className="flex items-center gap-4" role="region" aria-label="Connection status">
          {/* API Status */}
          <div className="flex items-center gap-2">
            <Database size={14} className={cn(connectionStatus.color)} />
            <span className={cn('text-xs', connectionStatus.color)}>
              {connectionStatus.text}
            </span>
          </div>

          {/* Network Status */}
          <div className="flex items-center gap-2">
            {isOnline ? (
              <>
                <Wifi size={14} className="text-success" />
                <span className="text-success">Online</span>
              </>
            ) : (
              <>
                <WifiOff size={14} className="text-destructive" />
                <span className="text-destructive">Offline</span>
              </>
            )}
          </div>

          {/* Error indicator */}
          {lastError && (
            <div className="flex items-center gap-1 max-w-48">
              <AlertCircle size={14} className="text-destructive flex-shrink-0" />
              <span className="text-destructive truncate" title={lastError}>
                {lastError}
              </span>
            </div>
          )}
        </div>
      </div>

      {/* Mobile-optimized bottom row */}
      <div className="md:hidden flex items-center justify-center gap-4 px-4 py-1 text-[10px] text-muted-foreground border-t border-border/50">
        <span>Reqs: {networkStats.requestCount}↑ {networkStats.responseCount}↓</span>
        {networkStats.errorCount > 0 && (
          <span className="text-destructive">Errors: {networkStats.errorCount}</span>
        )}
        {networkStats.avgResponseTime > 0 && (
          <span>Avg: {networkStats.avgResponseTime}ms</span>
        )}
      </div>
    </footer>
  );
}