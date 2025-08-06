/**
 * useKeyboardNavigation - Comprehensive keyboard navigation support
 * 
 * Provides a centralized keyboard navigation system with support for:
 * - Tab/Shift+Tab navigation
 * - Enter/Space activation
 * - Escape dismissal
 * - Arrow key navigation
 * - Custom keyboard shortcuts
 * - Focus management
 * - Accessibility compliance
 */

import { useEffect, useCallback, useRef, useState } from 'react';

// ============================================================================
// Types
// ============================================================================

export interface KeyboardShortcut {
  key: string;
  ctrlKey?: boolean;
  metaKey?: boolean;
  shiftKey?: boolean;
  altKey?: boolean;
  action: () => void;
  description: string;
  preventDefault?: boolean;
}

export interface NavigationItem {
  id: string;
  element: HTMLElement | null;
  group?: string;
  disabled?: boolean;
  onActivate?: () => void;
  onEscape?: () => void;
}

export interface KeyboardNavigationOptions {
  /** Enable automatic tab order management */
  autoTabOrder?: boolean;
  /** Enable arrow key navigation within groups */
  enableArrowKeys?: boolean;
  /** Enable home/end navigation */
  enableHomeEnd?: boolean;
  /** Enable escape handling */
  enableEscape?: boolean;
  /** Focus trap within container */
  trapFocus?: boolean;
  /** Restore focus on unmount */
  restoreFocus?: boolean;
  /** Custom key handlers */
  onKeyDown?: (event: KeyboardEvent) => boolean | void;
}

export interface UseKeyboardNavigationReturn {
  /** Register a keyboard shortcut */
  registerShortcut: (shortcut: KeyboardShortcut) => () => void;
  /** Register a navigation item */
  registerItem: (item: NavigationItem) => () => void;
  /** Set focus to specific item */
  focusItem: (id: string) => void;
  /** Move focus to next item */
  focusNext: () => void;
  /** Move focus to previous item */
  focusPrevious: () => void;
  /** Move focus to first item */
  focusFirst: () => void;
  /** Move focus to last item */
  focusLast: () => void;
  /** Get current focused item */
  focusedItem: string | null;
  /** Whether navigation is active */
  isActive: boolean;
}

// ============================================================================
// Main Hook
// ============================================================================

export function useKeyboardNavigation(
  containerRef: React.RefObject<HTMLElement | null>,
  options: KeyboardNavigationOptions = {}
): UseKeyboardNavigationReturn {
  const {
    autoTabOrder = true,
    enableArrowKeys = true,
    enableHomeEnd = true,
    enableEscape = true,
    trapFocus = false,
    restoreFocus = false,
    onKeyDown
  } = options;

  const [shortcuts, setShortcuts] = useState<Map<string, KeyboardShortcut>>(new Map());
  const [items, setItems] = useState<Map<string, NavigationItem>>(new Map());
  const [focusedItem, setFocusedItem] = useState<string | null>(null);
  const [isActive, setIsActive] = useState(false);
  const previousFocusRef = useRef<HTMLElement | null>(null);

  // ============================================================================
  // Focus Management
  // ============================================================================

  const getFocusableItems = useCallback(() => {
    return Array.from(items.values())
      .filter(item => item.element && !item.disabled)
      .sort((a, b) => {
        if (!a.element || !b.element) return 0;
        
        // Sort by tab index, then by DOM order
        const aTabIndex = a.element.tabIndex || 0;
        const bTabIndex = b.element.tabIndex || 0;
        
        if (aTabIndex !== bTabIndex) {
          return aTabIndex - bTabIndex;
        }
        
        // Use DOM order
        const position = a.element.compareDocumentPosition(b.element);
        return position & Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1;
      });
  }, [items]);

  const focusItem = useCallback((id: string) => {
    const item = items.get(id);
    if (item?.element && !item.disabled) {
      item.element.focus();
      setFocusedItem(id);
    }
  }, [items]);

  const focusNext = useCallback(() => {
    const focusableItems = getFocusableItems();
    if (focusableItems.length === 0) return;

    const currentIndex = focusedItem ? 
      focusableItems.findIndex(item => item.id === focusedItem) : -1;
    
    const nextIndex = (currentIndex + 1) % focusableItems.length;
    focusItem(focusableItems[nextIndex].id);
  }, [focusedItem, getFocusableItems, focusItem]);

  const focusPrevious = useCallback(() => {
    const focusableItems = getFocusableItems();
    if (focusableItems.length === 0) return;

    const currentIndex = focusedItem ? 
      focusableItems.findIndex(item => item.id === focusedItem) : 0;
    
    const prevIndex = currentIndex <= 0 ? focusableItems.length - 1 : currentIndex - 1;
    focusItem(focusableItems[prevIndex].id);
  }, [focusedItem, getFocusableItems, focusItem]);

  const focusFirst = useCallback(() => {
    const focusableItems = getFocusableItems();
    if (focusableItems.length > 0) {
      focusItem(focusableItems[0].id);
    }
  }, [getFocusableItems, focusItem]);

  const focusLast = useCallback(() => {
    const focusableItems = getFocusableItems();
    if (focusableItems.length > 0) {
      focusItem(focusableItems[focusableItems.length - 1].id);
    }
  }, [getFocusableItems, focusItem]);

  // ============================================================================
  // Registration Functions
  // ============================================================================

  const registerShortcut = useCallback((shortcut: KeyboardShortcut) => {
    const key = `${shortcut.ctrlKey ? 'ctrl+' : ''}${shortcut.metaKey ? 'cmd+' : ''}${shortcut.shiftKey ? 'shift+' : ''}${shortcut.altKey ? 'alt+' : ''}${shortcut.key.toLowerCase()}`;
    
    setShortcuts(prev => new Map(prev.set(key, shortcut)));
    
    return () => {
      setShortcuts(prev => {
        const next = new Map(prev);
        next.delete(key);
        return next;
      });
    };
  }, []);

  const registerItem = useCallback((item: NavigationItem) => {
    setItems(prev => new Map(prev.set(item.id, item)));
    
    return () => {
      setItems(prev => {
        const next = new Map(prev);
        next.delete(item.id);
        return next;
      });
    };
  }, []);

  // ============================================================================
  // Keyboard Event Handling
  // ============================================================================

  const handleKeyDown = useCallback((event: KeyboardEvent) => {
    if (!containerRef.current?.contains(event.target as Node)) {
      return;
    }

    // Allow custom handler to override
    if (onKeyDown) {
      const handled = onKeyDown(event);
      if (handled === true) return;
    }

    const target = event.target as HTMLElement;
    const key = event.key.toLowerCase();
    
    // Check for shortcuts first
    const shortcutKey = `${event.ctrlKey ? 'ctrl+' : ''}${event.metaKey ? 'cmd+' : ''}${event.shiftKey ? 'shift+' : ''}${event.altKey ? 'alt+' : ''}${key}`;
    const shortcut = shortcuts.get(shortcutKey);
    
    if (shortcut) {
      if (shortcut.preventDefault !== false) {
        event.preventDefault();
      }
      shortcut.action();
      return;
    }

    // Handle navigation keys
    switch (key) {
      case 'tab':
        if (trapFocus) {
          event.preventDefault();
          if (event.shiftKey) {
            focusPrevious();
          } else {
            focusNext();
          }
        }
        break;

      case 'arrowdown':
      case 'arrowup':
        if (enableArrowKeys) {
          event.preventDefault();
          if (key === 'arrowdown') {
            focusNext();
          } else {
            focusPrevious();
          }
        }
        break;

      case 'arrowleft':
      case 'arrowright':
        if (enableArrowKeys) {
          // Handle horizontal navigation within groups
          const currentItem = focusedItem ? items.get(focusedItem) : null;
          if (currentItem?.group) {
            const groupItems = getFocusableItems().filter(item => item.group === currentItem.group);
            const currentIndex = groupItems.findIndex(item => item.id === focusedItem);
            
            if (currentIndex !== -1) {
              event.preventDefault();
              const nextIndex = key === 'arrowright' 
                ? (currentIndex + 1) % groupItems.length
                : currentIndex <= 0 ? groupItems.length - 1 : currentIndex - 1;
              focusItem(groupItems[nextIndex].id);
            }
          }
        }
        break;

      case 'home':
        if (enableHomeEnd) {
          event.preventDefault();
          focusFirst();
        }
        break;

      case 'end':
        if (enableHomeEnd) {
          event.preventDefault();
          focusLast();
        }
        break;

      case 'enter':
      case ' ':
        const currentItem = focusedItem ? items.get(focusedItem) : null;
        if (currentItem?.onActivate && (target.tagName !== 'INPUT' && target.tagName !== 'TEXTAREA')) {
          event.preventDefault();
          currentItem.onActivate();
        }
        break;

      case 'escape':
        if (enableEscape) {
          const currentItem = focusedItem ? items.get(focusedItem) : null;
          if (currentItem?.onEscape) {
            event.preventDefault();
            currentItem.onEscape();
          }
        }
        break;
    }
  }, [
    containerRef,
    onKeyDown,
    shortcuts,
    focusedItem,
    items,
    trapFocus,
    enableArrowKeys,
    enableHomeEnd,
    enableEscape,
    focusNext,
    focusPrevious,
    focusFirst,
    focusLast,
    getFocusableItems,
    focusItem
  ]);

  // ============================================================================
  // Focus Management
  // ============================================================================

  const handleFocusIn = useCallback((event: FocusEvent) => {
    if (!containerRef.current?.contains(event.target as Node)) {
      return;
    }

    setIsActive(true);
    
    // Find the focused item
    const focusedElement = event.target as HTMLElement;
    const item = Array.from(items.values()).find(item => item.element === focusedElement);
    
    if (item) {
      setFocusedItem(item.id);
    }
  }, [containerRef, items]);

  const handleFocusOut = useCallback((event: FocusEvent) => {
    if (!containerRef.current?.contains(event.relatedTarget as Node)) {
      setIsActive(false);
      setFocusedItem(null);
    }
  }, [containerRef]);

  // ============================================================================
  // Effects
  // ============================================================================

  // Set up event listeners
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    // Store previous focus for restoration
    if (restoreFocus && document.activeElement && document.activeElement !== document.body) {
      previousFocusRef.current = document.activeElement as HTMLElement;
    }

    // Set up tab indices if auto tab order is enabled
    if (autoTabOrder) {
      const focusableItems = getFocusableItems();
      focusableItems.forEach((item, index) => {
        if (item.element) {
          item.element.tabIndex = index;
        }
      });
    }

    document.addEventListener('keydown', handleKeyDown);
    container.addEventListener('focusin', handleFocusIn);
    container.addEventListener('focusout', handleFocusOut);

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      container.removeEventListener('focusin', handleFocusIn);
      container.removeEventListener('focusout', handleFocusOut);

      // Restore focus if needed
      if (restoreFocus && previousFocusRef.current) {
        previousFocusRef.current.focus();
      }
    };
  }, [
    containerRef,
    autoTabOrder,
    restoreFocus,
    getFocusableItems,
    handleKeyDown,
    handleFocusIn,
    handleFocusOut
  ]);

  // ============================================================================
  // Return
  // ============================================================================

  return {
    registerShortcut,
    registerItem,
    focusItem,
    focusNext,
    focusPrevious,
    focusFirst,
    focusLast,
    focusedItem,
    isActive
  };
}

// ============================================================================
// Utility Hooks
// ============================================================================

/**
 * Hook for simple keyboard shortcuts
 */
export function useKeyboardShortcuts(
  shortcuts: KeyboardShortcut[],
  enabled = true
) {
  useEffect(() => {
    if (!enabled) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      const key = event.key.toLowerCase();
      const shortcutKey = `${event.ctrlKey ? 'ctrl+' : ''}${event.metaKey ? 'cmd+' : ''}${event.shiftKey ? 'shift+' : ''}${event.altKey ? 'alt+' : ''}${key}`;
      
      const shortcut = shortcuts.find(s => {
        const sKey = `${s.ctrlKey ? 'ctrl+' : ''}${s.metaKey ? 'cmd+' : ''}${s.shiftKey ? 'shift+' : ''}${s.altKey ? 'alt+' : ''}${s.key.toLowerCase()}`;
        return sKey === shortcutKey;
      });

      if (shortcut) {
        if (shortcut.preventDefault !== false) {
          event.preventDefault();
        }
        shortcut.action();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [shortcuts, enabled]);
}

/**
 * Hook for focus management within a specific element
 */
export function useFocusManagement(
  containerRef: React.RefObject<HTMLElement | null>,
  options: { trapFocus?: boolean; restoreFocus?: boolean } = {}
) {
  const { trapFocus = false, restoreFocus = false } = options;
  const previousFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    if (restoreFocus && document.activeElement && document.activeElement !== document.body) {
      previousFocusRef.current = document.activeElement as HTMLElement;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (!trapFocus || event.key !== 'Tab') return;

      const focusableElements = container.querySelectorAll(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      );

      const firstElement = focusableElements[0] as HTMLElement;
      const lastElement = focusableElements[focusableElements.length - 1] as HTMLElement;

      if (event.shiftKey) {
        if (document.activeElement === firstElement) {
          event.preventDefault();
          lastElement?.focus();
        }
      } else {
        if (document.activeElement === lastElement) {
          event.preventDefault();
          firstElement?.focus();
        }
      }
    };

    if (trapFocus) {
      document.addEventListener('keydown', handleKeyDown);
    }

    return () => {
      if (trapFocus) {
        document.removeEventListener('keydown', handleKeyDown);
      }

      if (restoreFocus && previousFocusRef.current) {
        previousFocusRef.current.focus();
      }
    };
  }, [containerRef, trapFocus, restoreFocus]);
}