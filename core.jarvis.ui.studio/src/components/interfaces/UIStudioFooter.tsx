/**
 * UIStudioFooter Component
 * 
 * Footer component with status indicators for the UIStudio interface.
 * Shows build status, version info, connection status, and other system indicators.
 * 
 * Features:
 * - Build status indicator
 * - Version information
 * - Connection status (API/Database)
 * - Responsive design
 * - Real-time status updates
 * - User session info
 * - Performance metrics
 * 
 * @module UIStudioFooter
 */

import React, { useState, useEffect, useCallback, useMemo } from 'react';

// Shadcn/ui components
import { Badge } from '../ui/badge';
import { Button } from '../ui/button';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '../ui/tooltip';

// Icons
import { 
  Activity,
  CheckCircle2,
  XCircle,
  AlertCircle,
  Clock,
  WifiOff,
  RefreshCw,
  Info,
  User
} from 'lucide-react';

// Types
import type { UIStudioEntityId } from '../../types/uistudio';

// ============================================================================
// Types and Interfaces
// ============================================================================

/**
 * Connection status types
 */
export type ConnectionStatus = 'connected' | 'disconnected' | 'reconnecting' | 'error';

/**
 * Build status types
 */
export type BuildStatus = 'success' | 'failed' | 'building' | 'pending' | 'unknown';

/**
 * System health status types
 */
export type HealthStatus = 'healthy' | 'warning' | 'critical' | 'unknown';

/**
 * Status indicator configuration
 */
export interface StatusIndicator {
  id: string;
  label: string;
  status: ConnectionStatus | BuildStatus | HealthStatus;
  value?: string | number;
  timestamp?: Date;
  details?: string;
  isLoading?: boolean;
}

/**
 * System metrics interface
 */
export interface SystemMetrics {
  responseTime: number;
  uptime: string;
  memoryUsage?: number;
  cpuUsage?: number;
  activeUsers?: number;
  buildNumber?: string;
  lastDeployment?: Date;
}

/**
 * Footer component props
 */
export interface UIStudioFooterProps {
  /** Current user entity ID */
  userEntityId?: UIStudioEntityId;
  
  /** Show/hide specific status indicators */
  showIndicators?: {
    build: boolean;
    connection: boolean;
    database: boolean;
    api: boolean;
    session: boolean;
    performance: boolean;
  };
  
  /** Override version information */
  version?: string;
  
  /** Override build information */
  buildInfo?: {
    number: string;
    timestamp: Date;
    branch?: string;
    commit?: string;
  };
  
  /** Callback for status refresh */
  onRefreshStatus?: () => void;
  
  /** Callback for opening system info */
  onOpenSystemInfo?: () => void;
  
  /** Custom CSS classes */
  className?: string;
  
  /** Compact mode for mobile */
  compact?: boolean;
}

/**
 * Use system status hook interface
 */
export interface UseSystemStatusReturn {
  indicators: StatusIndicator[];
  metrics: SystemMetrics;
  isLoading: boolean;
  error: string | null;
  refresh: () => void;
  lastUpdated: Date | null;
}

// ============================================================================
// Custom Hook for System Status
// ============================================================================

/**
 * Custom hook for managing system status and metrics
 */
const useSystemStatus = (userEntityId?: UIStudioEntityId): UseSystemStatusReturn => {
  const [indicators, setIndicators] = useState<StatusIndicator[]>([]);
  const [metrics, setMetrics] = useState<SystemMetrics>({
    responseTime: 0,
    uptime: '0s'
  });
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  /**
   * Simulate API health check
   */
  const checkApiHealth = useCallback(async (): Promise<StatusIndicator> => {
    try {
      const start = Date.now();
      
      // Simulate API call with random delay
      await new Promise(resolve => setTimeout(resolve, Math.random() * 200 + 50));
      
      const responseTime = Date.now() - start;
      const isHealthy = responseTime < 500 && Math.random() > 0.1; // 90% success rate
      
      return {
        id: 'api',
        label: 'API',
        status: isHealthy ? 'connected' : 'error',
        value: `${responseTime}ms`,
        timestamp: new Date(),
        details: isHealthy ? 'API responding normally' : 'API response time degraded'
      };
    } catch (err) {
      return {
        id: 'api',
        label: 'API',
        status: 'disconnected',
        timestamp: new Date(),
        details: 'API connection failed'
      };
    }
  }, []);

  /**
   * Simulate database health check
   */
  const checkDatabaseHealth = useCallback(async (): Promise<StatusIndicator> => {
    try {
      // Simulate database check
      await new Promise(resolve => setTimeout(resolve, Math.random() * 100 + 25));
      
      const isHealthy = Math.random() > 0.05; // 95% success rate
      const connections = Math.floor(Math.random() * 50) + 10;
      
      return {
        id: 'database',
        label: 'Database',
        status: isHealthy ? 'connected' : 'warning',
        value: `${connections} conn`,
        timestamp: new Date(),
        details: isHealthy ? 'Database operating normally' : 'Database under heavy load'
      };
    } catch (err) {
      return {
        id: 'database',
        label: 'Database',
        status: 'error',
        timestamp: new Date(),
        details: 'Database connection failed'
      };
    }
  }, []);

  /**
   * Get build status
   */
  const getBuildStatus = useCallback((): StatusIndicator => {
    const buildStatuses: BuildStatus[] = ['success', 'success', 'success', 'failed', 'building'];
    const randomStatus = buildStatuses[Math.floor(Math.random() * buildStatuses.length)];
    
    return {
      id: 'build',
      label: 'Build',
      status: randomStatus,
      value: randomStatus === 'building' ? 'In Progress' : `#${Math.floor(Math.random() * 1000) + 100}`,
      timestamp: new Date(),
      details: randomStatus === 'success' ? 'Last build completed successfully' :
               randomStatus === 'failed' ? 'Build failed - check logs' :
               randomStatus === 'building' ? 'Build in progress' : 'Build status unknown'
    };
  }, []);

  /**
   * Get session status
   */
  const getSessionStatus = useCallback((): StatusIndicator => {
    const sessionValid = Math.random() > 0.05; // 95% session validity
    const expiresIn = Math.floor(Math.random() * 120) + 30; // 30-150 minutes
    
    return {
      id: 'session',
      label: 'Session',
      status: sessionValid ? 'connected' : 'warning',
      value: sessionValid ? `${expiresIn}m` : 'Expired',
      timestamp: new Date(),
      details: sessionValid ? `Session expires in ${expiresIn} minutes` : 'Session expired - please refresh'
    };
  }, []);

  /**
   * Get performance metrics
   */
  const getPerformanceStatus = useCallback((): StatusIndicator => {
    const responseTime = Math.floor(Math.random() * 200) + 50;
    const status: ConnectionStatus = responseTime < 100 ? 'connected' : 
                                   responseTime < 300 ? 'reconnecting' : 'error';
    
    return {
      id: 'performance',
      label: 'Performance',
      status,
      value: `${responseTime}ms`,
      timestamp: new Date(),
      details: `Average response time: ${responseTime}ms`
    };
  }, []);

  /**
   * Refresh all status indicators
   */
  const refresh = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    
    try {
      const [apiStatus, dbStatus] = await Promise.all([
        checkApiHealth(),
        checkDatabaseHealth()
      ]);
      
      const buildStatus = getBuildStatus();
      const sessionStatus = getSessionStatus();
      const performanceStatus = getPerformanceStatus();
      
      const newIndicators = [apiStatus, dbStatus, buildStatus, sessionStatus, performanceStatus];
      setIndicators(newIndicators);
      
      // Update metrics
      const responseTime = parseInt(apiStatus.value?.toString().replace('ms', '') || '0');
      const uptime = `${Math.floor(Math.random() * 24)}h ${Math.floor(Math.random() * 60)}m`;
      
      setMetrics({
        responseTime,
        uptime,
        memoryUsage: Math.floor(Math.random() * 40) + 30, // 30-70%
        cpuUsage: Math.floor(Math.random() * 30) + 10, // 10-40%
        activeUsers: Math.floor(Math.random() * 50) + 5,
        buildNumber: `${Math.floor(Math.random() * 1000) + 100}`,
        lastDeployment: new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000) // Last 7 days
      });
      
      setLastUpdated(new Date());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to refresh status');
    } finally {
      setIsLoading(false);
    }
  }, [checkApiHealth, checkDatabaseHealth, getBuildStatus, getSessionStatus, getPerformanceStatus]);

  /**
   * Initial load and periodic updates
   */
  useEffect(() => {
    refresh();
    
    // Update every 30 seconds
    const interval = setInterval(refresh, 30000);
    
    return () => clearInterval(interval);
  }, [refresh]);

  return {
    indicators,
    metrics,
    isLoading,
    error,
    refresh,
    lastUpdated
  };
};

// ============================================================================
// Status Indicator Components
// ============================================================================

/**
 * Individual status indicator component
 */
interface StatusIndicatorItemProps {
  indicator: StatusIndicator;
  compact?: boolean;
}

const StatusIndicatorItem: React.FC<StatusIndicatorItemProps> = ({ indicator, compact = false }) => {
  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'connected':
      case 'success':
      case 'healthy':
        return <CheckCircle2 className="h-2xs w-2xs text-green-500" />;
      case 'warning':
      case 'reconnecting':
      case 'building':
        return <AlertCircle className="h-2xs w-2xs text-yellow-sm00" />;
      case 'error':
      case 'failed':
      case 'critical':
        return <XCircle className="h-2xs w-2xs text-red-500" />;
      case 'disconnected':
        return <WifiOff className="h-2xs w-2xs text-gray-500" />;
      default:
        return <Activity className="h-2xs w-2xs text-gray-400" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'connected':
      case 'success':
      case 'healthy':
        return 'text-green-600 border-green-200 bg-green-50';
      case 'warning':
      case 'reconnecting':
      case 'building':
        return 'text-yellow-md00 border-yellow-xs00 bg-yellow-sm0';
      case 'error':
      case 'failed':
      case 'critical':
        return 'text-red-600 border-red-200 bg-red-50';
      case 'disconnected':
        return 'text-gray-600 border-gray-200 bg-gray-50';
      default:
        return 'text-gray-600 border-gray-200 bg-gray-50';
    }
  };

  if (compact) {
    return (
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <div className="flex items-center space-x-1">
              {getStatusIcon(indicator.status)}
              {indicator.value && (
                <span className="text-xs text-muted-foreground">
                  {indicator.value}
                </span>
              )}
            </div>
          </TooltipTrigger>
          <TooltipContent side="top" className="max-w-xs">
            <div className="space-y-1">
              <div className="font-medium">{indicator.label}</div>
              <div className="text-xs text-muted-foreground">{indicator.details}</div>
              {indicator.timestamp && (
                <div className="text-xs text-muted-foreground">
                  Updated: {indicator.timestamp.toLocaleTimeString()}
                </div>
              )}
            </div>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    );
  }

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <Badge variant="outline" className={`${getStatusColor(indicator.status)} cursor-help`}>
            <div className="flex items-center space-x-1">
              {getStatusIcon(indicator.status)}
              <span className="text-xs font-medium">{indicator.label}</span>
              {indicator.value && (
                <span className="text-xs opacity-75">
                  {indicator.value}
                </span>
              )}
            </div>
          </Badge>
        </TooltipTrigger>
        <TooltipContent side="top" className="max-w-xs">
          <div className="space-y-1">
            <div className="font-medium">{indicator.label} Status</div>
            <div className="text-xs text-muted-foreground">{indicator.details}</div>
            {indicator.timestamp && (
              <div className="text-xs text-muted-foreground">
                Last updated: {indicator.timestamp.toLocaleTimeString()}
              </div>
            )}
          </div>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
};

// ============================================================================
// Main Footer Component
// ============================================================================

/**
 * UIStudioFooter - Footer component with status indicators
 */
export const UIStudioFooter: React.FC<UIStudioFooterProps> = ({
  userEntityId,
  showIndicators = {
    build: true,
    connection: true,
    database: true,
    api: true,
    session: true,
    performance: true
  },
  version = '1.0.0',
  buildInfo,
  onRefreshStatus,
  onOpenSystemInfo,
  className = '',
  compact = false
}) => {
  const { indicators, metrics, isLoading, error, refresh, lastUpdated } = useSystemStatus(userEntityId);

  /**
   * Handle refresh button click
   */
  const handleRefresh = useCallback(() => {
    refresh();
    onRefreshStatus?.();
  }, [refresh, onRefreshStatus]);

  /**
   * Filter indicators based on showIndicators prop
   */
  const visibleIndicators = useMemo(() => {
    return indicators.filter(indicator => {
      switch (indicator.id) {
        case 'api':
          return showIndicators.api;
        case 'database':
          return showIndicators.database;
        case 'build':
          return showIndicators.build;
        case 'session':
          return showIndicators.session;
        case 'performance':
          return showIndicators.performance;
        default:
          return true;
      }
    });
  }, [indicators, showIndicators]);

  /**
   * Get build info display
   */
  const buildDisplay = useMemo(() => {
    if (buildInfo) {
      return `v${version} (${buildInfo.number})`;
    }
    return `v${version}`;
  }, [version, buildInfo]);

  if (compact) {
    return (
      <footer className={`bg-background border-t border-border px-4 py-2 ${className}`}>
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          {/* Left side - Version */}
          <div className="flex items-center space-x-2">
            <span>{buildDisplay}</span>
            {lastUpdated && (
              <span>•</span>
            )}
            {lastUpdated && (
              <span>{lastUpdated.toLocaleTimeString()}</span>
            )}
          </div>

          {/* Right side - Status indicators */}
          <div className="flex items-center space-x-2">
            {visibleIndicators.slice(0, 3).map((indicator) => (
              <StatusIndicatorItem
                key={indicator.id}
                indicator={indicator}
                compact={true}
              />
            ))}
            {isLoading && (
              <RefreshCw className="h-2xs w-2xs animate-spin text-muted-foreground" />
            )}
          </div>
        </div>
      </footer>
    );
  }

  return (
    <footer className={`bg-background border-t border-border ${className}`}>
      <div className="px-4 py-3 lg:px-6">
        {/* Main footer content */}
        <div className="flex flex-col space-y-3 lg:flex-row lg:items-center lg:justify-between lg:space-y-0">
          {/* Left section - Version and system info */}
          <div className="flex flex-col space-y-2 sm:flex-row sm:items-center sm:space-y-0 sm:space-x-4">
            <div className="flex items-center space-x-2">
              <div className="text-sm font-medium text-foreground">UIStudio</div>
              <Badge variant="secondary" className="text-xs">
                {buildDisplay}
              </Badge>
              {buildInfo?.timestamp && (
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <div className="text-xs text-muted-foreground cursor-help">
                        Built {buildInfo.timestamp.toLocaleDateString()}
                      </div>
                    </TooltipTrigger>
                    <TooltipContent>
                      <div className="space-y-1">
                        <div>Build: {buildInfo.number}</div>
                        {buildInfo.branch && <div>Branch: {buildInfo.branch}</div>}
                        {buildInfo.commit && <div>Commit: {buildInfo.commit.substring(0, 8)}</div>}
                        <div>Timestamp: {buildInfo.timestamp.toLocaleString()}</div>
                      </div>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              )}
            </div>

            {/* System metrics */}
            <div className="flex items-center space-x-3 text-xs text-muted-foreground">
              {metrics.uptime && (
                <div className="flex items-center space-x-1">
                  <Clock className="h-2xs w-2xs" />
                  <span>Uptime: {metrics.uptime}</span>
                </div>
              )}
              {metrics.activeUsers && (
                <div className="flex items-center space-x-1">
                  <User className="h-2xs w-2xs" />
                  <span>{metrics.activeUsers} users</span>
                </div>
              )}
            </div>
          </div>

          {/* Right section - Status indicators and actions */}
          <div className="flex flex-col space-y-2 sm:flex-row sm:items-center sm:space-y-0 sm:space-x-3">
            {/* Status indicators */}
            <div className="flex flex-wrap items-center gap-2">
              {visibleIndicators.map((indicator) => (
                <StatusIndicatorItem
                  key={indicator.id}
                  indicator={indicator}
                  compact={false}
                />
              ))}
            </div>

            {/* Action buttons */}
            <div className="flex items-center space-x-2">
              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={handleRefresh}
                      disabled={isLoading}
                      className="h-lg w-lg p-0"
                    >
                      <RefreshCw className={`h-2xs w-2xs ${isLoading ? 'animate-spin' : ''}`} />
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>
                    <div>Refresh status</div>
                  </TooltipContent>
                </Tooltip>
              </TooltipProvider>

              {onOpenSystemInfo && (
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={onOpenSystemInfo}
                        className="h-lg w-lg p-0"
                      >
                        <Info className="h-2xs w-2xs" />
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>
                      <div>System information</div>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              )}
            </div>
          </div>
        </div>

        {/* Error state */}
        {error && (
          <div className="mt-2 text-xs text-red-600 bg-red-50 border border-red-200 rounded px-2 py-1">
            <div className="flex items-center space-x-1">
              <AlertCircle className="h-2xs w-2xs" />
              <span>Status update failed: {error}</span>
            </div>
          </div>
        )}

        {/* Last updated timestamp */}
        {lastUpdated && (
          <div className="mt-2 text-xs text-muted-foreground text-center">
            Last updated: {lastUpdated.toLocaleString()}
          </div>
        )}
      </div>
    </footer>
  );
};

// ============================================================================
// Default Export
// ============================================================================

export default UIStudioFooter;