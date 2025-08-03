/**
 * useSelectionState Hook - Comprehensive selection state management for bulk operations
 * 
 * Provides state management for multi-select functionality with:
 * - Individual item selection/deselection
 * - Select all / deselect all functionality
 * - Keyboard selection support (Space, Shift+click, Ctrl+click)
 * - Bulk operations toolbar integration
 * - Range selection with Shift+click
 * - Accessibility compliance with ARIA states
 * 
 * This implements the selection state requirement from TASK.md:
 * "Implement selection state for bulk operations"
 * 
 * @module UseSelectionState
 */

import { useState, useCallback, useMemo, useEffect, useRef } from 'react';

// ============================================================================
// Types and Interfaces
// ============================================================================

/** Selection mode options */
export type SelectionMode = 'single' | 'multiple';

/** Keyboard modifiers for selection */
export interface SelectionModifiers {
  /** Ctrl/Cmd key pressed (for toggle selection) */
  ctrlKey: boolean;
  /** Shift key pressed (for range selection) */
  shiftKey: boolean;
  /** Alt key pressed (for alternative actions) */
  altKey: boolean;
}

/** Selection change event details */
export interface SelectionChangeEvent<T = unknown> {
  /** Type of selection change */
  type: 'select' | 'deselect' | 'selectAll' | 'deselectAll' | 'toggle' | 'range';
  /** Items affected by the change */
  items: T[];
  /** All currently selected items after the change */
  selectedItems: T[];
  /** Keyboard modifiers during the event */
  modifiers?: SelectionModifiers;
  /** Original event that triggered the change */
  originalEvent?: React.MouseEvent | React.KeyboardEvent;
}

/** Bulk action definition */
export interface BulkAction<T = unknown> {
  /** Unique identifier for the action */
  id: string;
  /** Display label for the action */
  label: string;
  /** Icon component or icon name */
  icon?: React.ComponentType<{ className?: string }> | string;
  /** Action handler that receives selected items */
  action: (selectedItems: T[]) => void | Promise<void>;
  /** Whether this action is destructive (requires confirmation) */
  destructive?: boolean;
  /** Whether the action is currently disabled */
  disabled?: boolean;
  /** Tooltip text for the action */
  tooltip?: string;
  /** Keyboard shortcut for the action */
  shortcut?: string;
  /** Minimum number of items required for this action */
  minItems?: number;
  /** Maximum number of items allowed for this action */
  maxItems?: number;
  /** Custom validation function */
  validate?: (selectedItems: T[]) => boolean;
}

/** Selection state options */
export interface SelectionStateOptions<T = unknown> {
  /** Selection mode - single or multiple */
  mode?: SelectionMode;
  /** Function to extract unique ID from items */
  getId?: (item: T) => string;
  /** Enable keyboard selection support */
  enableKeyboard?: boolean;
  /** Enable range selection with Shift+click */
  enableRangeSelection?: boolean;
  /** Enable select all functionality */
  enableSelectAll?: boolean;
  /** Preserve selection when items change */
  preserveSelection?: boolean;
  /** Maximum number of items that can be selected */
  maxSelection?: number;
  /** Available bulk actions */
  bulkActions?: BulkAction<T>[];
  /** Called when selection changes */
  onSelectionChange?: (event: SelectionChangeEvent<T>) => void;
  /** Called when bulk action is triggered */
  onBulkAction?: (action: BulkAction<T>, selectedItems: T[]) => void;
}

/** Selection state result */
export interface UseSelectionStateResult<T = unknown> {
  // Selection state
  selectedItems: T[];
  selectedIds: Set<string>;
  isAllSelected: boolean;
  isPartiallySelected: boolean;
  hasSelection: boolean;
  selectionCount: number;
  
  // Selection actions
  selectItem: (item: T, modifiers?: SelectionModifiers, event?: React.MouseEvent) => void;
  deselectItem: (item: T) => void;
  toggleItem: (item: T, modifiers?: SelectionModifiers, event?: React.MouseEvent) => void;
  selectAll: () => void;
  deselectAll: () => void;
  toggleAll: () => void;
  selectRange: (fromItem: T, toItem: T) => void;
  
  // Utility functions
  isSelected: (item: T) => boolean;
  getSelectedItems: () => T[];
  getAvailableBulkActions: () => BulkAction<T>[];
  canExecuteBulkAction: (actionId: string) => boolean;
  executeBulkAction: (actionId: string) => void;
  
  // Event handlers for components
  getSelectAllProps: () => {
    checked: boolean;
    indeterminate: boolean;
    onChange: (checked: boolean, modifiers?: SelectionModifiers, event?: React.MouseEvent | React.KeyboardEvent) => void;
    'aria-label': string;
  };
  getItemProps: (item: T) => {
    checked: boolean;
    onChange: (checked: boolean, modifiers?: SelectionModifiers, event?: React.MouseEvent | React.KeyboardEvent) => void;
    'aria-label': string;
  };
  getTableRowProps: (item: T) => {
    onClick: (event: React.MouseEvent) => void;
    onKeyDown: (event: React.KeyboardEvent) => void;
    'aria-selected': boolean;
    'data-selected': boolean;
    className?: string;
  };
  
  // Bulk actions toolbar
  showBulkToolbar: boolean;
  bulkToolbarActions: BulkAction<T>[];
}

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Default ID extractor - uses 'id' property or converts to string
 */
function defaultGetId<T>(item: T): string {
  if (typeof item === 'string' || typeof item === 'number') {
    return String(item);
  }
  if (item && typeof item === 'object' && 'id' in item) {
    return String((item as Record<string, unknown>).id);
  }
  return JSON.stringify(item);
}

/**
 * Extract keyboard modifiers from an event
 */
function getModifiers(event?: React.MouseEvent | React.KeyboardEvent): SelectionModifiers {
  return {
    ctrlKey: event?.ctrlKey || event?.metaKey || false,
    shiftKey: event?.shiftKey || false,
    altKey: event?.altKey || false
  };
}

/**
 * Find index of item in array by ID
 */
function findItemIndex<T>(items: T[], item: T, getId: (item: T) => string): number {
  const targetId = getId(item);
  return items.findIndex(i => getId(i) === targetId);
}

// ============================================================================
// Main Hook Implementation
// ============================================================================

/**
 * Comprehensive selection state management hook for bulk operations
 */
export function useSelectionState<T = unknown>(
  items: T[] = [],
  options: SelectionStateOptions<T> = {}
): UseSelectionStateResult<T> {
  const {
    mode = 'multiple',
    getId = defaultGetId,
    enableKeyboard = true,
    enableRangeSelection = true,
    // enableSelectAll = true, // Not used in implementation
    preserveSelection = false,
    maxSelection,
    bulkActions = [],
    onSelectionChange,
    onBulkAction
  } = options;

  // ============================================================================
  // State Management
  // ============================================================================

  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const lastSelectedRef = useRef<T | null>(null);
  const itemsRef = useRef<T[]>(items);

  // Update items ref when items change
  useEffect(() => {
    itemsRef.current = items;
    
    // Clean up selection if items changed and preserve is disabled
    if (!preserveSelection) {
      const currentItemIds = new Set(items.map(getId));
      setSelectedIds(prev => {
        const filtered = new Set(Array.from(prev).filter(id => currentItemIds.has(id)));
        return filtered.size !== prev.size ? filtered : prev;
      });
    }
  }, [items, getId, preserveSelection]);

  // ============================================================================
  // Computed Values
  // ============================================================================

  const selectedItems = useMemo(() => {
    return items.filter(item => selectedIds.has(getId(item)));
  }, [items, selectedIds, getId]);

  const selectionCount = selectedIds.size;
  const hasSelection = selectionCount > 0;
  const isAllSelected = items.length > 0 && selectedItems.length === items.length;
  const isPartiallySelected = hasSelection && !isAllSelected;

  // ============================================================================
  // Selection Actions
  // ============================================================================

  const fireSelectionChange = useCallback((
    type: SelectionChangeEvent<T>['type'],
    affectedItems: T[],
    modifiers?: SelectionModifiers,
    originalEvent?: React.MouseEvent | React.KeyboardEvent
  ) => {
    if (onSelectionChange) {
      onSelectionChange({
        type,
        items: affectedItems,
        selectedItems: items.filter(item => selectedIds.has(getId(item))),
        modifiers,
        originalEvent
      });
    }
  }, [onSelectionChange, items, selectedIds, getId]);

  const selectItem = useCallback((item: T, modifiers?: SelectionModifiers, event?: React.MouseEvent) => {
    const itemId = getId(item);
    
    setSelectedIds(prev => {
      const newSet = new Set(prev);
      
      // Handle different selection modes
      if (mode === 'single') {
        newSet.clear();
        newSet.add(itemId);
      } else {
        // Multiple selection mode
        if (maxSelection && newSet.size >= maxSelection && !newSet.has(itemId)) {
          return prev; // Don't exceed max selection
        }
        newSet.add(itemId);
      }
      
      lastSelectedRef.current = item;
      return newSet;
    });

    fireSelectionChange('select', [item], modifiers, event);
  }, [getId, mode, maxSelection, fireSelectionChange]);

  const deselectItem = useCallback((item: T) => {
    const itemId = getId(item);
    
    setSelectedIds(prev => {
      const newSet = new Set(prev);
      newSet.delete(itemId);
      return newSet;
    });

    fireSelectionChange('deselect', [item]);
  }, [getId, fireSelectionChange]);

  const toggleItem = useCallback((item: T, modifiers?: SelectionModifiers, event?: React.MouseEvent) => {
    const itemId = getId(item);
    const isCurrentlySelected = selectedIds.has(itemId);
    
    if (isCurrentlySelected) {
      deselectItem(item);
    } else {
      selectItem(item, modifiers, event);
    }
  }, [selectedIds, getId, selectItem, deselectItem]);

  const selectAll = useCallback(() => {
    const allIds = new Set(items.map(getId));
    setSelectedIds(allIds);
    lastSelectedRef.current = null;
    fireSelectionChange('selectAll', items);
  }, [items, getId, fireSelectionChange]);

  const deselectAll = useCallback(() => {
    setSelectedIds(new Set());
    lastSelectedRef.current = null;
    fireSelectionChange('deselectAll', selectedItems);
  }, [selectedItems, fireSelectionChange]);

  const toggleAll = useCallback(() => {
    if (isAllSelected) {
      deselectAll();
    } else {
      selectAll();
    }
  }, [isAllSelected, selectAll, deselectAll]);

  const selectRange = useCallback((fromItem: T, toItem: T) => {
    if (!enableRangeSelection) return;
    
    const fromIndex = findItemIndex(items, fromItem, getId);
    const toIndex = findItemIndex(items, toItem, getId);
    
    if (fromIndex === -1 || toIndex === -1) return;
    
    const startIndex = Math.min(fromIndex, toIndex);
    const endIndex = Math.max(fromIndex, toIndex);
    const rangeItems = items.slice(startIndex, endIndex + 1);
    
    setSelectedIds(prev => {
      const newSet = new Set(prev);
      rangeItems.forEach(item => {
        const itemId = getId(item);
        if (!maxSelection || newSet.size < maxSelection || newSet.has(itemId)) {
          newSet.add(itemId);
        }
      });
      return newSet;
    });

    fireSelectionChange('range', rangeItems);
  }, [items, getId, enableRangeSelection, maxSelection, fireSelectionChange]);

  // ============================================================================
  // Utility Functions
  // ============================================================================

  const isSelected = useCallback((item: T) => {
    return selectedIds.has(getId(item));
  }, [selectedIds, getId]);

  const getSelectedItems = useCallback(() => {
    return selectedItems;
  }, [selectedItems]);

  const getAvailableBulkActions = useCallback(() => {
    return bulkActions.filter(action => {
      if (action.disabled) return false;
      if (action.minItems && selectionCount < action.minItems) return false;
      if (action.maxItems && selectionCount > action.maxItems) return false;
      if (action.validate && !action.validate(selectedItems)) return false;
      return true;
    });
  }, [bulkActions, selectionCount, selectedItems]);

  const canExecuteBulkAction = useCallback((actionId: string) => {
    return getAvailableBulkActions().some(action => action.id === actionId);
  }, [getAvailableBulkActions]);

  const executeBulkAction = useCallback((actionId: string) => {
    const action = bulkActions.find(a => a.id === actionId);
    if (!action || !canExecuteBulkAction(actionId)) return;
    
    onBulkAction?.(action, selectedItems);
    action.action(selectedItems);
  }, [bulkActions, canExecuteBulkAction, selectedItems, onBulkAction]);

  // ============================================================================
  // Component Props Generators
  // ============================================================================

  const getSelectAllProps = useCallback(() => ({
    checked: isAllSelected,
    indeterminate: isPartiallySelected,
    onChange: (checked: boolean, modifiers?: SelectionModifiers, event?: React.MouseEvent | React.KeyboardEvent) => {
      if (checked) {
        selectAll();
      } else {
        deselectAll();
      }
    },
    'aria-label': `Select all ${items.length} items`
  }), [isAllSelected, isPartiallySelected, selectAll, deselectAll, items.length]);

  const getItemProps = useCallback((item: T) => ({
    checked: isSelected(item),
    onChange: (checked: boolean, modifiers?: SelectionModifiers, event?: React.MouseEvent | React.KeyboardEvent) => {
      if (checked) {
        selectItem(item, modifiers, event as React.MouseEvent);
      } else {
        deselectItem(item);
      }
    },
    'aria-label': `Select item ${getId(item)}`
  }), [isSelected, selectItem, deselectItem, getId]);

  const getTableRowProps = useCallback((item: T) => ({
    onClick: (event: React.MouseEvent) => {
      if (!enableKeyboard) return;
      
      const modifiers = getModifiers(event);
      
      if (modifiers.shiftKey && enableRangeSelection && lastSelectedRef.current) {
        // Range selection
        selectRange(lastSelectedRef.current, item);
      } else if (modifiers.ctrlKey) {
        // Toggle selection
        toggleItem(item, modifiers, event);
      } else {
        // Single selection
        if (mode === 'single') {
          deselectAll();
          selectItem(item, modifiers, event);
        } else {
          toggleItem(item, modifiers, event);
        }
      }
    },
    onKeyDown: (event: React.KeyboardEvent) => {
      if (!enableKeyboard) return;
      
      if (event.key === ' ' || event.key === 'Enter') {
        event.preventDefault();
        const modifiers = getModifiers(event);
        toggleItem(item, modifiers);
      }
    },
    'aria-selected': isSelected(item),
    'data-selected': isSelected(item),
    className: isSelected(item) ? 'selected' : undefined
  }), [
    enableKeyboard,
    enableRangeSelection,
    mode,
    isSelected,
    selectRange,
    toggleItem,
    selectItem,
    deselectAll
  ]);

  // ============================================================================
  // Bulk Actions Toolbar
  // ============================================================================

  const showBulkToolbar = hasSelection;
  const bulkToolbarActions = getAvailableBulkActions();

  // ============================================================================
  // Keyboard Shortcuts Setup
  // ============================================================================

  useEffect(() => {
    if (!enableKeyboard) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      // Only handle if focus is within a selectable container
      const target = event.target as HTMLElement;
      if (!target.closest('[data-selectable-container]')) return;

      switch (event.key) {
        case 'a':
          if (event.ctrlKey || event.metaKey) {
            event.preventDefault();
            selectAll();
          }
          break;
        case 'Escape':
          if (hasSelection) {
            event.preventDefault();
            deselectAll();
          }
          break;
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [enableKeyboard, selectAll, deselectAll, hasSelection]);

  // ============================================================================
  // Return Selection State
  // ============================================================================

  return {
    // Selection state
    selectedItems,
    selectedIds,
    isAllSelected,
    isPartiallySelected,
    hasSelection,
    selectionCount,
    
    // Selection actions
    selectItem,
    deselectItem,
    toggleItem,
    selectAll,
    deselectAll,
    toggleAll,
    selectRange,
    
    // Utility functions
    isSelected,
    getSelectedItems,
    getAvailableBulkActions,
    canExecuteBulkAction,
    executeBulkAction,
    
    // Component props generators
    getSelectAllProps,
    getItemProps,
    getTableRowProps,
    
    // Bulk actions toolbar
    showBulkToolbar,
    bulkToolbarActions
  };
}

// ============================================================================
// Export Default
// ============================================================================

export default useSelectionState;