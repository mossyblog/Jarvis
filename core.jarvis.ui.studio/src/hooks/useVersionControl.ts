/**
 * useVersionControl - Version control and conflict resolution hook
 * 
 * Provides version control features for collaborative editing including:
 * - Automatic snapshot creation
 * - Version history tracking
 * - Conflict detection and resolution
 * - Rollback capabilities
 * - Merge strategies
 */

import { useState, useEffect, useCallback, useRef } from 'react';
import { useCreateUIStudioVersionSnapshot, useUIStudioVersionHistory } from './useUIStudio';
import type { BentoPage } from '../types/bento';
import type { UIStudioEntityId, UIStudioVersion, CreateVersionRequest } from '../types/uistudio';

// ============================================================================
// Types
// ============================================================================

export interface VersionControlConfig {
  autoSnapshot: boolean;
  snapshotInterval: number; // milliseconds
  maxVersions?: number;
  enableConflictDetection?: boolean;
  onSnapshot?: (version: UIStudioVersion) => void;
  onConflict?: (conflict: VersionConflict) => void;
  onRestore?: (version: UIStudioVersion) => void;
}

export interface VersionConflict {
  id: string;
  type: 'merge_conflict' | 'version_mismatch' | 'concurrent_edit';
  baseVersion: number;
  localVersion: number;
  remoteVersion: number;
  conflictingChanges: ConflictingChange[];
  timestamp: string;
}

export interface ConflictingChange {
  path: string;
  localValue: any;
  remoteValue: any;
  changeType: 'added' | 'modified' | 'removed';
}

export interface VersionSnapshot {
  id: string;
  version: number;
  timestamp: string;
  message: string;
  data: BentoPage | Record<string, unknown>;
  author: string;
  isAutoSnapshot: boolean;
}

interface VersionControlState {
  currentVersion: number;
  isSnapshotting: boolean;
  lastSnapshot: string | null;
  conflicts: VersionConflict[];
  snapshots: VersionSnapshot[];
  isRestoring: boolean;
}

// ============================================================================
// Constants
// ============================================================================

const DEFAULT_CONFIG: Required<Omit<VersionControlConfig, 'onSnapshot' | 'onConflict' | 'onRestore'>> = {
  autoSnapshot: true,
  snapshotInterval: 30000, // 30 seconds
  maxVersions: 50,
  enableConflictDetection: true,
};

// ============================================================================
// Main Hook
// ============================================================================

export const useVersionControl = (
  resourceId: UIStudioEntityId,
  config: VersionControlConfig = { autoSnapshot: true, snapshotInterval: 30000 }
) => {
  const mergedConfig = { ...DEFAULT_CONFIG, ...config };
  const snapshotTimeoutRef = useRef<number | undefined>(undefined);
  const lastChangeRef = useRef<string | undefined>(undefined);

  const [state, setState] = useState<VersionControlState>({
    currentVersion: 1,
    isSnapshotting: false,
    lastSnapshot: null,
    conflicts: [],
    snapshots: [],
    isRestoring: false,
  });

  // API hooks
  const createSnapshot = useCreateUIStudioVersionSnapshot();
  const { data: versionHistory, refetch: refetchVersions } = useUIStudioVersionHistory(
    resourceId,
    { limit: mergedConfig.maxVersions }
  );

  // ============================================================================
  // Snapshot Management
  // ============================================================================

  const createVersionSnapshot = useCallback(async (
    page: BentoPage,
    message?: string,
    isAuto = false
  ) => {
    if (state.isSnapshotting) return;

    setState(prev => ({ ...prev, isSnapshotting: true }));

    try {
      const snapshotData: CreateVersionRequest = {
        resourceEntityId: resourceId,
        resourceType: 'page', // Required by CreateVersionRequest
        versionLabel: `v${state.currentVersion + 1}`, // Required by CreateVersionRequest
        changeDescription: message || (isAuto ? 'Auto-snapshot' : 'Manual snapshot'),
        createdByEntityId: 'current-user', // TODO: Get from auth context
      };

      const result = await createSnapshot.mutateAsync(snapshotData);

      if (result && result.length > 0) {
        const newVersion = result[0];
        const snapshot: VersionSnapshot = {
          id: newVersion.id,
          version: state.currentVersion + 1, // Use local version since versionNumber doesn't exist on UIStudioVersion
          timestamp: newVersion.lastUpdated, // Use lastUpdated since createdAt doesn't exist on UIStudioVersion
          message: newVersion.changeDescription || '',
          data: page,
          author: newVersion.createdByEntityId,
          isAutoSnapshot: isAuto,
        };

        setState(prev => ({
          ...prev,
          currentVersion: snapshot.version,
          lastSnapshot: snapshot.timestamp,
          snapshots: [snapshot, ...prev.snapshots].slice(0, mergedConfig.maxVersions),
        }));

        config.onSnapshot?.(newVersion);
        refetchVersions();
      }
    } catch (error) {
      console.error('Failed to create version snapshot:', error);
    } finally {
      setState(prev => ({ ...prev, isSnapshotting: false }));
    }
  }, [
    resourceId,
    state.currentVersion,
    state.isSnapshotting,
    createSnapshot,
    mergedConfig.maxVersions,
    config,
    refetchVersions
  ]);

  // Auto-snapshot with debouncing
  const scheduleAutoSnapshot = useCallback((page: BentoPage) => {
    if (!mergedConfig.autoSnapshot) return;

    // Clear existing timeout
    if (snapshotTimeoutRef.current) {
      clearTimeout(snapshotTimeoutRef.current);
    }

    // Schedule new snapshot
    snapshotTimeoutRef.current = window.setTimeout(() => {
      const currentChangeId = JSON.stringify(page);
      if (lastChangeRef.current !== currentChangeId) {
        lastChangeRef.current = currentChangeId;
        createVersionSnapshot(page, undefined, true);
      }
    }, mergedConfig.snapshotInterval);
  }, [mergedConfig.autoSnapshot, mergedConfig.snapshotInterval, createVersionSnapshot]);

  // ============================================================================
  // Conflict Detection and Resolution
  // ============================================================================

  const detectConflicts = useCallback((
    localPage: BentoPage,
    remotePage: BentoPage,
    baseVersion: number
  ): VersionConflict | null => {
    if (!mergedConfig.enableConflictDetection) return null;

    const conflicts = findConflictingChanges(localPage, remotePage);
    
    if (conflicts.length === 0) return null;

    const conflict: VersionConflict = {
      id: generateConflictId(),
      type: 'merge_conflict',
      baseVersion,
      localVersion: state.currentVersion,
      remoteVersion: state.currentVersion + 1,
      conflictingChanges: conflicts,
      timestamp: new Date().toISOString(),
    };

    setState(prev => ({
      ...prev,
      conflicts: [...prev.conflicts, conflict]
    }));

    config.onConflict?.(conflict);
    return conflict;
  }, [mergedConfig.enableConflictDetection, state.currentVersion, config]);

  const resolveConflict = useCallback((
    conflictId: string,
    resolution: 'accept_local' | 'accept_remote' | 'manual_merge',
    mergedData?: BentoPage
  ) => {
    const conflict = state.conflicts.find(c => c.id === conflictId);
    if (!conflict) return;

    setState(prev => ({
      ...prev,
      conflicts: prev.conflicts.filter(c => c.id !== conflictId)
    }));

    // Apply resolution
    if (resolution === 'manual_merge' && mergedData) {
      createVersionSnapshot(mergedData, `Merged conflict ${conflictId.slice(0, 8)}`);
    }

    // TODO: Implement specific resolution strategies
  }, [state.conflicts, createVersionSnapshot]);

  // ============================================================================
  // Version Restoration
  // ============================================================================

  const restoreVersion = useCallback(async (versionId: string) => {
    setState(prev => ({ ...prev, isRestoring: true }));

    try {
      const snapshot = state.snapshots.find(s => s.id === versionId);
      if (snapshot) {
        // Create a new snapshot before restoring
        await createVersionSnapshot(
          snapshot.data as BentoPage,
          `Restored from version ${snapshot.version}`
        );

        config.onRestore?.(snapshot as any); // Type assertion for UIStudioVersion
      }
    } catch (error) {
      console.error('Failed to restore version:', error);
    } finally {
      setState(prev => ({ ...prev, isRestoring: false }));
    }
  }, [state.snapshots, createVersionSnapshot, config]);

  // ============================================================================
  // Effects
  // ============================================================================

  // Load version history on mount
  useEffect(() => {
    if (versionHistory && Array.isArray(versionHistory)) {
      const snapshots: VersionSnapshot[] = versionHistory.map(version => ({
        id: version.id,
        version: 1, // Use default since versionNumber doesn't exist on UIStudioVersion
        timestamp: version.lastUpdated, // Use lastUpdated since createdAt doesn't exist on UIStudioVersion
        message: version.changeDescription || '',
        data: version.versionData as BentoPage | Record<string, unknown>, // Cast to expected union type
        author: version.createdByEntityId,
        isAutoSnapshot: false, // TODO: Determine from snapshot metadata
      }));

      setState(prev => ({
        ...prev,
        snapshots,
        currentVersion: Math.max(...snapshots.map(s => s.version), 1),
      }));
    }
  }, [versionHistory]);

  // Cleanup timeouts on unmount
  useEffect(() => {
    return () => {
      if (snapshotTimeoutRef.current) {
        clearTimeout(snapshotTimeoutRef.current);
      }
    };
  }, []);

  return {
    // State
    currentVersion: state.currentVersion,
    isSnapshotting: state.isSnapshotting,
    isRestoring: state.isRestoring,
    lastSnapshot: state.lastSnapshot,
    conflicts: state.conflicts,
    snapshots: state.snapshots,

    // Methods
    createSnapshot: createVersionSnapshot,
    scheduleAutoSnapshot,
    detectConflicts,
    resolveConflict,
    restoreVersion,
    refetchVersions,

    // Configuration
    config: mergedConfig,
  };
};

// ============================================================================
// Utility Functions
// ============================================================================

function findConflictingChanges(
  localPage: BentoPage,
  remotePage: BentoPage
): ConflictingChange[] {
  const conflicts: ConflictingChange[] = [];

  // Simple conflict detection - in a real implementation, this would be more sophisticated
  if (localPage.displayName !== remotePage.displayName) {
    conflicts.push({
      path: 'displayName',
      localValue: localPage.displayName,
      remoteValue: remotePage.displayName,
      changeType: 'modified',
    });
  }

  if (localPage.route !== remotePage.route) {
    conflicts.push({
      path: 'route',
      localValue: localPage.route,
      remoteValue: remotePage.route,
      changeType: 'modified',
    });
  }

  if (localPage.layoutId !== remotePage.layoutId) {
    conflicts.push({
      path: 'layoutId',
      localValue: localPage.layoutId,
      remoteValue: remotePage.layoutId,
      changeType: 'modified',
    });
  }

  // TODO: Add more sophisticated conflict detection for grid components, bindings, etc.

  return conflicts;
}

function generateConflictId(): string {
  return `conflict_${Date.now()}_${Math.random().toString(36).substr(2, 5)}`;
}

export default useVersionControl;