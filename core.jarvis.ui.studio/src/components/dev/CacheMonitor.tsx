/**
 * Cache Performance Monitor
 * 
 * Development component for monitoring cache performance and health
 * in UIStudio applications. Only renders in development mode.
 * 
 * @module CacheMonitor
 */

import React, { useState, useEffect } from 'react';
import { useCacheStrategy } from '../../hooks/useCacheStrategy';

// ============================================================================
// Types
// ============================================================================

interface CacheMonitorProps {
  /** Whether to show detailed metrics */
  detailed?: boolean;
  /** Update interval in milliseconds */
  updateInterval?: number;
  /** Position of the monitor */
  position?: 'top-left' | 'top-right' | 'bottom-left' | 'bottom-right';
  /** Whether to auto-hide in production */
  autoHide?: boolean;
}

// ============================================================================
// Cache Monitor Component
// ============================================================================

export function CacheMonitor({
  detailed = false,
  updateInterval = 5000, // 5 seconds
  position = 'bottom-right',
  autoHide = true,
}: CacheMonitorProps) {
  const { getPerformanceMetrics, cacheManager, logPerformanceReport } = useCacheStrategy();
  const [metrics, setMetrics] = useState(getPerformanceMetrics());
  const [cacheStats, setCacheStats] = useState(cacheManager.getCacheStats());
  const [cacheHealth, setCacheHealth] = useState(cacheManager.getCacheHealth());
  const [isExpanded, setIsExpanded] = useState(false);

  // Hide in production if autoHide is enabled
  if (autoHide && import.meta.env.PROD) {
    return null;
  }

  // -------------------------------------------------------------------------
  // Effects
  // -------------------------------------------------------------------------

  useEffect(() => {
    const interval = setInterval(() => {
      setMetrics(getPerformanceMetrics());
      setCacheStats(cacheManager.getCacheStats());
      setCacheHealth(cacheManager.getCacheHealth());
    }, updateInterval);

    return () => clearInterval(interval);
  }, [getPerformanceMetrics, cacheManager, updateInterval]);

  // -------------------------------------------------------------------------
  // Handlers
  // -------------------------------------------------------------------------

  const handleClearCache = () => {
    cacheManager.clearAll();
    setMetrics(getPerformanceMetrics());
    setCacheStats(cacheManager.getCacheStats());
  };

  const handleLogReport = () => {
    logPerformanceReport();
  };

  const handleOptimize = async () => {
    await cacheManager.backgroundRefresh();
    setMetrics(getPerformanceMetrics());
    setCacheStats(cacheManager.getCacheStats());
  };

  // -------------------------------------------------------------------------
  // Styling
  // -------------------------------------------------------------------------

  const getPositionClasses = () => {
    switch (position) {
      case 'top-left':
        return 'top-4 left-4';
      case 'top-right':
        return 'top-4 right-4';
      case 'bottom-left':
        return 'bottom-4 left-4';
      case 'bottom-right':
      default:
        return 'bottom-4 right-4';
    }
  };

  const getHealthColor = () => {
    switch (cacheHealth.status) {
      case 'healthy':
        return 'text-green-500';
      case 'warning':
        return 'text-yellow-500';
      case 'error':
        return 'text-red-500';
      default:
        return 'text-gray-500';
    }
  };

  const getHealthIcon = () => {
    switch (cacheHealth.status) {
      case 'healthy':
        return '🟢';
      case 'warning':
        return '🟡';
      case 'error':
        return '🔴';
      default:
        return '⚫';
    }
  };

  // -------------------------------------------------------------------------
  // Render
  // -------------------------------------------------------------------------

  return (
    <div
      className={`fixed ${getPositionClasses()} z-50 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg transition-all duration-200 ${
        isExpanded ? 'w-80' : 'w-12 h-12'
      }`}
    >
      {/* Collapsed State */}
      {!isExpanded && (
        <button
          onClick={() => setIsExpanded(true)}
          className="w-full h-full flex items-center justify-center text-lg hover:bg-gray-50 dark:hover:bg-gray-700 rounded-lg transition-colors"
          title="Cache Monitor - Click to expand"
        >
          {getHealthIcon()}
        </button>
      )}

      {/* Expanded State */}
      {isExpanded && (
        <div className="p-4">
          {/* Header */}
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-semibold text-gray-900 dark:text-white flex items-center gap-2">
              {getHealthIcon()}
              Cache Monitor
            </h3>
            <button
              onClick={() => setIsExpanded(false)}
              className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
              title="Collapse"
            >
              ×
            </button>
          </div>

          {/* Quick Stats */}
          <div className="space-y-2 mb-3">
            <div className="flex justify-between text-xs">
              <span className="text-gray-600 dark:text-gray-400">Hit Rate:</span>
              <span className={`font-mono ${metrics.hitRate > 80 ? 'text-green-600' : metrics.hitRate > 60 ? 'text-yellow-600' : 'text-red-600'}`}>
                {metrics.hitRate.toFixed(1)}%
              </span>
            </div>
            
            <div className="flex justify-between text-xs">
              <span className="text-gray-600 dark:text-gray-400">Stale Rate:</span>
              <span className={`font-mono ${metrics.staleRate < 20 ? 'text-green-600' : metrics.staleRate < 40 ? 'text-yellow-600' : 'text-red-600'}`}>
                {metrics.staleRate.toFixed(1)}%
              </span>
            </div>

            <div className="flex justify-between text-xs">
              <span className="text-gray-600 dark:text-gray-400">Cached:</span>
              <span className="font-mono text-gray-900 dark:text-white">
                {cacheStats.cachedQueries}
              </span>
            </div>

            <div className="flex justify-between text-xs">
              <span className="text-gray-600 dark:text-gray-400">Health:</span>
              <span className={`font-mono ${getHealthColor()}`}>
                {cacheHealth.status}
              </span>
            </div>
          </div>

          {/* Detailed Stats */}
          {detailed && (
            <div className="space-y-2 mb-3 text-xs border-t border-gray-200 dark:border-gray-700 pt-3">
              <div className="flex justify-between">
                <span className="text-gray-600 dark:text-gray-400">Total Queries:</span>
                <span className="font-mono text-gray-900 dark:text-white">
                  {cacheStats.totalQueries}
                </span>
              </div>
              
              <div className="flex justify-between">
                <span className="text-gray-600 dark:text-gray-400">Active:</span>
                <span className="font-mono text-gray-900 dark:text-white">
                  {cacheStats.activeQueries}
                </span>
              </div>

              <div className="flex justify-between">
                <span className="text-gray-600 dark:text-gray-400">Stale:</span>
                <span className="font-mono text-gray-900 dark:text-white">
                  {cacheStats.staleQueries}
                </span>
              </div>

              <div className="flex justify-between">
                <span className="text-gray-600 dark:text-gray-400">Error Rate:</span>
                <span className={`font-mono ${metrics.errorRate < 5 ? 'text-green-600' : metrics.errorRate < 15 ? 'text-yellow-600' : 'text-red-600'}`}>
                  {metrics.errorRate.toFixed(1)}%
                </span>
              </div>
            </div>
          )}

          {/* Health Issues */}
          {cacheHealth.issues.length > 0 && (
            <div className="mb-3 text-xs">
              <div className="text-red-600 dark:text-red-400 font-medium mb-1">
                Issues:
              </div>
              {cacheHealth.issues.slice(0, 2).map((issue, index) => (
                <div key={index} className="text-gray-600 dark:text-gray-400 truncate">
                  • {issue}
                </div>
              ))}
              {cacheHealth.issues.length > 2 && (
                <div className="text-gray-500 dark:text-gray-500">
                  +{cacheHealth.issues.length - 2} more
                </div>
              )}
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex gap-2">
            <button
              onClick={handleLogReport}
              className="flex-1 px-2 py-1 text-xs bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded hover:bg-blue-200 dark:hover:bg-blue-800 transition-colors"
              title="Log detailed report to console"
            >
              Log
            </button>
            
            <button
              onClick={handleOptimize}
              className="flex-1 px-2 py-1 text-xs bg-green-100 dark:bg-green-900 text-green-700 dark:text-green-300 rounded hover:bg-green-200 dark:hover:bg-green-800 transition-colors"
              title="Refresh stale data"
            >
              Refresh
            </button>
            
            <button
              onClick={handleClearCache}
              className="flex-1 px-2 py-1 text-xs bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-300 rounded hover:bg-red-200 dark:hover:bg-red-800 transition-colors"
              title="Clear all cache"
            >
              Clear
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

// ============================================================================
// Cache Health Badge Component
// ============================================================================

interface CacheHealthBadgeProps {
  /** Show detailed tooltip */
  detailed?: boolean;
  /** Size of the badge */
  size?: 'sm' | 'md' | 'lg';
}

export function CacheHealthBadge({ 
  detailed = false, 
  size = 'md' 
}: CacheHealthBadgeProps) {
  const { cacheManager } = useCacheStrategy();
  const [health, setHealth] = useState(cacheManager.getCacheHealth());

  useEffect(() => {
    const interval = setInterval(() => {
      setHealth(cacheManager.getCacheHealth());
    }, 10000); // Update every 10 seconds

    return () => clearInterval(interval);
  }, [cacheManager]);

  const sizeClasses = {
    sm: 'w-2 h-2',
    md: 'w-3 h-3',
    lg: 'w-4 h-4',
  };

  const getStatusColor = () => {
    switch (health.status) {
      case 'healthy':
        return 'bg-green-500';
      case 'warning':
        return 'bg-yellow-500';
      case 'error':
        return 'bg-red-500';
      default:
        return 'bg-gray-500';
    }
  };

  const tooltip = detailed ? (
    <div className="text-xs">
      <div className="font-medium">Cache Health: {health.status}</div>
      {health.issues.length > 0 && (
        <div className="mt-1">
          <div className="text-red-400">Issues:</div>
          {health.issues.map((issue, index) => (
            <div key={index}>• {issue}</div>
          ))}
        </div>
      )}
      {health.recommendations.length > 0 && (
        <div className="mt-1">
          <div className="text-yellow-400">Recommendations:</div>
          {health.recommendations.map((rec, index) => (
            <div key={index}>• {rec}</div>
          ))}
        </div>
      )}
    </div>
  ) : `Cache Health: ${health.status}`;

  return (
    <div
      className={`${sizeClasses[size]} ${getStatusColor()} rounded-full`}
      title={typeof tooltip === 'string' ? tooltip : undefined}
    >
      {typeof tooltip !== 'string' && (
        <div className="sr-only">{health.status}</div>
      )}
    </div>
  );
}

// ============================================================================
// Export Default
// ============================================================================

export default CacheMonitor;