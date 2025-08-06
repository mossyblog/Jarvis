/**
 * useCollaboration - Real-time collaboration hook
 * 
 * Provides real-time collaboration features for page editing including:
 * - WebSocket connection management
 * - User presence tracking
 * - Conflict detection and resolution
 * - Real-time page updates
 * - Collaborative cursors and selections
 */

import { useState, useEffect, useCallback, useRef } from 'react';
import type { BentoPage } from '../types/bento';
import type { UIStudioEntityId } from '../types/uistudio';

// ============================================================================
// Types
// ============================================================================

export interface CollaborationUser {
  id: string;
  name: string;
  avatar?: string;
  color: string;
  cursor?: {
    x: number;
    y: number;
    component?: string;
  };
  lastSeen: string;
}

export interface PageUpdate {
  userId: string;
  timestamp: string;
  changeType: 'component_add' | 'component_update' | 'component_remove' | 'page_settings' | 'layout_change';
  data: Record<string, unknown>;
  version: number;
}

export interface CollaborationConfig {
  enabled: boolean;
  websocketUrl?: string;
  reconnectInterval?: number;
  maxReconnectAttempts?: number;
  onUserJoined?: (user: CollaborationUser) => void;
  onUserLeft?: (user: CollaborationUser) => void;
  onPageUpdate?: (update: PageUpdate) => void;
  onConnectionChange?: (connected: boolean) => void;
  onConflict?: (conflict: PageConflict) => void;
}

export interface PageConflict {
  conflictId: string;
  type: 'concurrent_edit' | 'version_mismatch' | 'component_conflict';
  localChange: PageUpdate;
  remoteChange: PageUpdate;
  resolution?: 'local' | 'remote' | 'merge';
}

interface CollaborationState {
  isConnected: boolean;
  users: CollaborationUser[];
  currentUserId: string;
  pageVersion: number;
  pendingUpdates: PageUpdate[];
  conflicts: PageConflict[];
  reconnectAttempts: number;
}

// ============================================================================
// Constants
// ============================================================================

const DEFAULT_CONFIG: Required<Omit<CollaborationConfig, 'onUserJoined' | 'onUserLeft' | 'onPageUpdate' | 'onConnectionChange' | 'onConflict'>> = {
  enabled: true,
  websocketUrl: import.meta.env.VITE_WS_URL || 'ws://localhost:8080/collaboration',
  reconnectInterval: 3000,
  maxReconnectAttempts: 5,
};

const USER_COLORS = [
  '#3B82F6', // Blue
  '#EF4444', // Red
  '#10B981', // Green
  '#F59E0B', // Amber
  '#8B5CF6', // Violet
  '#EC4899', // Pink
  '#14B8A6', // Teal
  '#F97316', // Orange
];

// ============================================================================
// Main Hook
// ============================================================================

export const useCollaboration = (
  pageId: UIStudioEntityId,
  config: CollaborationConfig = { enabled: true }
) => {
  const mergedConfig = { ...DEFAULT_CONFIG, ...config };
  const wsRef = useRef<WebSocket | null>(null);
  const reconnectTimeoutRef = useRef<number | undefined>(undefined);
  const heartbeatIntervalRef = useRef<number | undefined>(undefined);

  const [state, setState] = useState<CollaborationState>({
    isConnected: false,
    users: [],
    currentUserId: generateUserId(),
    pageVersion: 1,
    pendingUpdates: [],
    conflicts: [],
    reconnectAttempts: 0,
  });

  // ============================================================================
  // WebSocket Management
  // ============================================================================

  const connect = useCallback(() => {
    if (!mergedConfig.enabled || !pageId) return;

    try {
      const wsUrl = `${mergedConfig.websocketUrl}?pageId=${pageId}&userId=${state.currentUserId}`;
      wsRef.current = new WebSocket(wsUrl);

      wsRef.current.onopen = () => {
        setState(prev => ({
          ...prev,
          isConnected: true,
          reconnectAttempts: 0,
        }));

        config.onConnectionChange?.(true);

        // Send initial user info
        wsRef.current?.send(JSON.stringify({
          type: 'user_join',
          data: {
            userId: state.currentUserId,
            name: `User ${state.currentUserId.slice(0, 8)}`,
            color: USER_COLORS[Math.floor(Math.random() * USER_COLORS.length)],
            pageId,
          }
        }));

        // Start heartbeat
        heartbeatIntervalRef.current = window.setInterval(() => {
          if (wsRef.current?.readyState === WebSocket.OPEN) {
            wsRef.current.send(JSON.stringify({ type: 'ping' }));
          }
        }, 30000);
      };

      wsRef.current.onmessage = (event) => {
        try {
          const message = JSON.parse(event.data);
          handleWebSocketMessage(message);
        } catch (error) {
          console.error('Failed to parse WebSocket message:', error);
        }
      };

      wsRef.current.onclose = (event) => {
        setState(prev => ({ ...prev, isConnected: false }));
        config.onConnectionChange?.(false);
        
        if (heartbeatIntervalRef.current) {
          clearInterval(heartbeatIntervalRef.current);
        }

        // Attempt reconnection if not a clean close
        if (!event.wasClean && state.reconnectAttempts < mergedConfig.maxReconnectAttempts) {
          reconnectTimeoutRef.current = window.setTimeout(() => {
            setState(prev => ({ ...prev, reconnectAttempts: prev.reconnectAttempts + 1 }));
            connect();
          }, mergedConfig.reconnectInterval);
        }
      };

      wsRef.current.onerror = (error) => {
        console.error('WebSocket error:', error);
      };

    } catch (error) {
      console.error('Failed to establish WebSocket connection:', error);
    }
  }, [pageId, mergedConfig, state.currentUserId, state.reconnectAttempts, config]);

  const disconnect = useCallback(() => {
    if (wsRef.current) {
      wsRef.current.close();
      wsRef.current = null;
    }

    if (reconnectTimeoutRef.current) {
      clearTimeout(reconnectTimeoutRef.current);
    }

    if (heartbeatIntervalRef.current) {
      clearInterval(heartbeatIntervalRef.current);
    }

    setState(prev => ({
      ...prev,
      isConnected: false,
      users: [],
    }));
  }, []);

  // ============================================================================
  // Message Handling
  // ============================================================================

  const handleWebSocketMessage = useCallback((message: {
    type: string;
    data?: unknown;
    user?: CollaborationUser;
    update?: PageUpdate;
  }) => {
    switch (message.type) {
      case 'user_joined':
        setState(prev => ({
          ...prev,
          users: [...prev.users.filter(u => u.id !== message.data.userId), message.data]
        }));
        config.onUserJoined?.(message.data);
        break;

      case 'user_left':
        setState(prev => ({
          ...prev,
          users: prev.users.filter(u => u.id !== message.data.userId)
        }));
        config.onUserLeft?.(message.data);
        break;

      case 'user_cursor':
        setState(prev => ({
          ...prev,
          users: prev.users.map(user =>
            user.id === message.data.userId
              ? { ...user, cursor: message.data.cursor }
              : user
          )
        }));
        break;

      case 'page_update':
        const update: PageUpdate = message.data;
        
        // Check for conflicts
        const hasConflict = state.pendingUpdates.some(pending => 
          pending.changeType === update.changeType &&
          pending.timestamp > update.timestamp
        );

        if (hasConflict) {
          const conflict: PageConflict = {
            conflictId: generateConflictId(),
            type: 'concurrent_edit',
            localChange: state.pendingUpdates[0],
            remoteChange: update,
          };

          setState(prev => ({
            ...prev,
            conflicts: [...prev.conflicts, conflict]
          }));

          config.onConflict?.(conflict);
        } else {
          setState(prev => ({
            ...prev,
            pageVersion: Math.max(prev.pageVersion, update.version)
          }));

          config.onPageUpdate?.(update);
        }
        break;

      case 'version_sync':
        setState(prev => ({
          ...prev,
          pageVersion: message.data.version
        }));
        break;

      case 'pong':
        // Heartbeat response
        break;

      default:
        console.warn('Unknown WebSocket message type:', message.type);
    }
  }, [state.pendingUpdates, config]);

  // ============================================================================
  // Public Methods
  // ============================================================================

  const broadcastPageUpdate = useCallback((page: BentoPage) => {
    if (!state.isConnected || !wsRef.current) return;

    const update: PageUpdate = {
      userId: state.currentUserId,
      timestamp: new Date().toISOString(),
      changeType: 'page_settings',
      data: page,
      version: state.pageVersion + 1,
    };

    setState(prev => ({
      ...prev,
      pendingUpdates: [...prev.pendingUpdates, update],
      pageVersion: prev.pageVersion + 1,
    }));

    wsRef.current.send(JSON.stringify({
      type: 'page_update',
      data: update,
    }));
  }, [state.isConnected, state.currentUserId, state.pageVersion]);

  const broadcastCursor = useCallback((x: number, y: number, componentId?: string) => {
    if (!state.isConnected || !wsRef.current) return;

    wsRef.current.send(JSON.stringify({
      type: 'user_cursor',
      data: {
        userId: state.currentUserId,
        cursor: { x, y, component: componentId },
      },
    }));
  }, [state.isConnected, state.currentUserId]);

  const resolveConflict = useCallback((conflictId: string, resolution: 'local' | 'remote' | 'merge') => {
    setState(prev => ({
      ...prev,
      conflicts: prev.conflicts.filter(c => c.conflictId !== conflictId)
    }));

    // Apply resolution logic based on the chosen strategy
    // This would typically involve merging changes or choosing one version
  }, []);

  // ============================================================================
  // Effects
  // ============================================================================

  // Connect when enabled and pageId is available
  useEffect(() => {
    if (mergedConfig.enabled && pageId) {
      connect();
    }

    return () => {
      disconnect();
    };
  }, [mergedConfig.enabled, pageId, connect, disconnect]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      disconnect();
    };
  }, [disconnect]);

  return {
    // State
    isConnected: state.isConnected,
    users: state.users,
    currentUserId: state.currentUserId,
    pageVersion: state.pageVersion,
    conflicts: state.conflicts,
    reconnectAttempts: state.reconnectAttempts,

    // Methods
    connect,
    disconnect,
    broadcastPageUpdate,
    broadcastCursor,
    resolveConflict,

    // Configuration
    config: mergedConfig,
  };
};

// ============================================================================
// Utility Functions
// ============================================================================

function generateUserId(): string {
  return `user_${Math.random().toString(36).substr(2, 9)}`;
}

function generateConflictId(): string {
  return `conflict_${Date.now()}_${Math.random().toString(36).substr(2, 5)}`;
}

export default useCollaboration;