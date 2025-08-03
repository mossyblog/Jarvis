/**
 * KeyboardNavigationProvider - Global keyboard navigation context
 * 
 * Provides application-wide keyboard navigation and shortcuts.
 * Manages global shortcuts, focus state, and navigation patterns.
 */

import React, { createContext, useContext, useCallback, useState, useRef, useEffect } from 'react';
import { useKeyboardShortcuts } from '@/hooks/useKeyboardNavigation';
import type { KeyboardShortcut } from '@/hooks/useKeyboardNavigation';

// ============================================================================
// Types
// ============================================================================

export interface GlobalShortcuts {
  openCommandPalette: () => void;
  openHelp: () => void;
  toggleEditMode: () => void;
  newPage: () => void;
  savePage: () => void;
  toggleSidebar: () => void;
  toggleTheme: () => void;
  focusSearch: () => void;
  closeModal: () => void;
  navigateBack: () => void;
  navigateForward: () => void;
}

export interface KeyboardNavigationContextType {
  /** Register a global shortcut */
  registerGlobalShortcut: (shortcut: KeyboardShortcut) => () => void;
  /** Get all available shortcuts */
  getShortcuts: () => KeyboardShortcut[];
  /** Whether keyboard navigation is enabled */
  enabled: boolean;
  /** Set keyboard navigation enabled state */
  setEnabled: (enabled: boolean) => void;
  /** Current modal stack for escape handling */
  modalStack: string[];
  /** Push modal to stack */
  pushModal: (id: string) => void;
  /** Pop modal from stack */
  popModal: (id?: string) => void;
  /** Global action handlers */
  actions: GlobalShortcuts;
}

// ============================================================================
// Context
// ============================================================================

const KeyboardNavigationContext = createContext<KeyboardNavigationContextType | null>(null);

export function useKeyboardNavigationContext() {
  const context = useContext(KeyboardNavigationContext);
  if (!context) {
    throw new Error('useKeyboardNavigationContext must be used within KeyboardNavigationProvider');
  }
  return context;
}

// ============================================================================
// Provider Props
// ============================================================================

export interface KeyboardNavigationProviderProps {
  children: React.ReactNode;
  /** Custom global shortcuts */
  actions?: Partial<GlobalShortcuts>;
  /** Whether to enable keyboard navigation by default */
  enabled?: boolean;
}

// ============================================================================
// Provider Component
// ============================================================================

export function KeyboardNavigationProvider({
  children,
  actions: customActions = {},
  enabled: initialEnabled = true
}: KeyboardNavigationProviderProps) {
  const [enabled, setEnabled] = useState(initialEnabled);
  const [shortcuts, setShortcuts] = useState<Map<string, KeyboardShortcut>>(new Map());
  const [modalStack, setModalStack] = useState<string[]>([]);
  const actionsRef = useRef<GlobalShortcuts>({} as GlobalShortcuts);

  // ============================================================================
  // Modal Stack Management
  // ============================================================================

  const pushModal = useCallback((id: string) => {
    setModalStack(prev => [...prev, id]);
  }, []);

  const popModal = useCallback((id?: string) => {
    setModalStack(prev => {
      if (id) {
        return prev.filter(modalId => modalId !== id);
      }
      return prev.slice(0, -1);
    });
  }, []);

  // ============================================================================
  // Default Actions
  // ============================================================================

  const defaultActions: GlobalShortcuts = {
    openCommandPalette: () => {
      console.log('🎹 Open command palette (Ctrl/Cmd+K)');
      // TODO: Implement command palette
    },
    openHelp: () => {
      console.log('🎹 Open help (?)');
      // TODO: Implement help modal
    },
    toggleEditMode: () => {
      console.log('🎹 Toggle edit mode (E)');
      // TODO: Implement edit mode toggle
    },
    newPage: () => {
      console.log('🎹 New page (Ctrl/Cmd+N)');
      // TODO: Implement new page
    },
    savePage: () => {
      console.log('🎹 Save page (Ctrl/Cmd+S)');
      // TODO: Implement save page
    },
    toggleSidebar: () => {
      console.log('🎹 Toggle sidebar (Ctrl/Cmd+B)');
      // TODO: Implement sidebar toggle
    },
    toggleTheme: () => {
      console.log('🎹 Toggle theme (Ctrl/Cmd+Shift+L)');
      // TODO: Implement theme toggle
    },
    focusSearch: () => {
      console.log('🎹 Focus search (/)');
      // TODO: Implement focus search
    },
    closeModal: () => {
      console.log('🎹 Close modal (Escape)');
      if (modalStack.length > 0) {
        popModal();
      }
    },
    navigateBack: () => {
      console.log('🎹 Navigate back (Alt+Left)');
      window.history.back();
    },
    navigateForward: () => {
      console.log('🎹 Navigate forward (Alt+Right)');
      window.history.forward();
    }
  };

  // Merge custom actions with defaults
  actionsRef.current = { ...defaultActions, ...customActions };

  // ============================================================================
  // Built-in Shortcuts
  // ============================================================================

  const builtInShortcuts: KeyboardShortcut[] = [
    {
      key: 'k',
      ctrlKey: true,
      action: actionsRef.current.openCommandPalette,
      description: 'Open command palette'
    },
    {
      key: 'k',
      metaKey: true,
      action: actionsRef.current.openCommandPalette,
      description: 'Open command palette'
    },
    {
      key: '?',
      action: actionsRef.current.openHelp,
      description: 'Show help'
    },
    {
      key: 'e',
      action: actionsRef.current.toggleEditMode,
      description: 'Toggle edit mode'
    },
    {
      key: 'n',
      ctrlKey: true,
      action: actionsRef.current.newPage,
      description: 'New page'
    },
    {
      key: 'n',
      metaKey: true,
      action: actionsRef.current.newPage,
      description: 'New page'
    },
    {
      key: 's',
      ctrlKey: true,
      action: actionsRef.current.savePage,
      description: 'Save page'
    },
    {
      key: 's',
      metaKey: true,
      action: actionsRef.current.savePage,
      description: 'Save page'
    },
    {
      key: 'b',
      ctrlKey: true,
      action: actionsRef.current.toggleSidebar,
      description: 'Toggle sidebar'
    },
    {
      key: 'b',
      metaKey: true,
      action: actionsRef.current.toggleSidebar,
      description: 'Toggle sidebar'
    },
    {
      key: 'l',
      ctrlKey: true,
      shiftKey: true,
      action: actionsRef.current.toggleTheme,
      description: 'Toggle theme'
    },
    {
      key: 'l',
      metaKey: true,
      shiftKey: true,
      action: actionsRef.current.toggleTheme,
      description: 'Toggle theme'
    },
    {
      key: '/',
      action: actionsRef.current.focusSearch,
      description: 'Focus search'
    },
    {
      key: 'escape',
      action: actionsRef.current.closeModal,
      description: 'Close modal/dialog'
    },
    {
      key: 'arrowleft',
      altKey: true,
      action: actionsRef.current.navigateBack,
      description: 'Navigate back'
    },
    {
      key: 'arrowright',
      altKey: true,
      action: actionsRef.current.navigateForward,
      description: 'Navigate forward'
    }
  ];

  // ============================================================================
  // Shortcut Management
  // ============================================================================

  const registerGlobalShortcut = useCallback((shortcut: KeyboardShortcut) => {
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

  const getShortcuts = useCallback(() => {
    return [...builtInShortcuts, ...Array.from(shortcuts.values())];
  }, [builtInShortcuts, shortcuts]);

  // ============================================================================
  // Keyboard Shortcuts Setup
  // ============================================================================

  useKeyboardShortcuts(getShortcuts(), enabled);

  // ============================================================================
  // Handle Input Focus State
  // ============================================================================

  useEffect(() => {
    const handleFocusIn = (event: FocusEvent) => {
      const target = event.target as HTMLElement;
      const isInput = target.tagName === 'INPUT' || 
                     target.tagName === 'TEXTAREA' || 
                     target.contentEditable === 'true';
      
      // Disable certain shortcuts when focused on inputs
      if (isInput) {
        setEnabled(false);
      } else {
        setEnabled(initialEnabled);
      }
    };

    const handleFocusOut = (event: FocusEvent) => {
      const target = event.target as HTMLElement;
      const isInput = target.tagName === 'INPUT' || 
                     target.tagName === 'TEXTAREA' || 
                     target.contentEditable === 'true';
      
      if (isInput) {
        setEnabled(initialEnabled);
      }
    };

    document.addEventListener('focusin', handleFocusIn);
    document.addEventListener('focusout', handleFocusOut);

    return () => {
      document.removeEventListener('focusin', handleFocusIn);
      document.removeEventListener('focusout', handleFocusOut);
    };
  }, [initialEnabled]);

  // ============================================================================
  // Context Value
  // ============================================================================

  const contextValue: KeyboardNavigationContextType = {
    registerGlobalShortcut,
    getShortcuts,
    enabled,
    setEnabled,
    modalStack,
    pushModal,
    popModal,
    actions: actionsRef.current
  };

  // ============================================================================
  // Render
  // ============================================================================

  return (
    <KeyboardNavigationContext.Provider value={contextValue}>
      {children}
      {/* Debug info in development */}
      {import.meta.env.DEV && (
        <div
          style={{
            position: 'fixed',
            bottom: '10px',
            right: '10px',
            background: 'rgba(0, 0, 0, 0.8)',
            color: 'white',
            padding: '8px',
            borderRadius: '4px',
            fontSize: '12px',
            zIndex: 9999,
            fontFamily: 'monospace',
            display: enabled ? 'block' : 'none'
          }}
        >
          🎹 Keyboard navigation: {enabled ? 'ON' : 'OFF'}
          {modalStack.length > 0 && (
            <div>Modals: {modalStack.join(' → ')}</div>
          )}
        </div>
      )}
    </KeyboardNavigationContext.Provider>
  );
}