/**
 * BentoGridKeyboard - Enhanced BentoGrid with comprehensive keyboard navigation
 * 
 * Adds keyboard navigation support to the BentoGrid component:
 * - Arrow keys for component navigation
 * - Tab/Shift+Tab for focus management
 * - Enter/Space for component activation
 * - Escape for deselection
 * - Delete key for component removal
 * - Keyboard shortcuts for grid operations
 */

import React, { useRef, useCallback, useEffect, useState } from 'react';
import { useKeyboardNavigation, useKeyboardShortcuts } from '@/hooks/useKeyboardNavigation';
import { useKeyboardNavigationContext } from '@/components/keyboard/KeyboardNavigationProvider';
import type { KeyboardShortcut, NavigationItem } from '@/hooks/useKeyboardNavigation';
import type { BentoGridProps } from './BentoGrid';
import type { GridPosition } from '@/types/bento';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';

// ============================================================================
// Enhanced Props
// ============================================================================

export interface BentoGridKeyboardProps extends BentoGridProps {
  /** Enable keyboard navigation */
  enableKeyboardNavigation?: boolean;
  /** Custom keyboard shortcuts */
  keyboardShortcuts?: KeyboardShortcut[];
  /** Handle keyboard component movement */
  onKeyboardMove?: (componentId: string, direction: 'up' | 'down' | 'left' | 'right', amount?: number) => void;
  /** Handle keyboard component resize */
  onKeyboardResize?: (componentId: string, direction: 'up' | 'down' | 'left' | 'right', amount?: number) => void;
  /** Selected component ID for keyboard navigation */
  selectedComponentId?: string | null;
  /** Optional children for custom rendering */
  children?: React.ReactNode;
}

// ============================================================================
// Component
// ============================================================================

export function BentoGridKeyboard({
  enableKeyboardNavigation = true,
  keyboardShortcuts = [],
  onKeyboardMove,
  onKeyboardResize,
  selectedComponentId,
  onComponentSelect,
  onComponentDelete,
  grid,
  isEditing = false,
  className,
  children,
  ...props
}: BentoGridKeyboardProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const { pushModal, popModal } = useKeyboardNavigationContext();
  const [focusedComponentId, setFocusedComponentId] = useState<string | null>(selectedComponentId || null);
  const [isResizeMode, setIsResizeMode] = useState(false);

  // ============================================================================
  // Keyboard Navigation Setup
  // ============================================================================

  const {
    registerItem,
    isActive
  } = useKeyboardNavigation(containerRef, {
    enableArrowKeys: true,
    enableHomeEnd: true,
    enableEscape: true,
    trapFocus: isEditing,
    onKeyDown: handleCustomKeyDown
  });

  // ============================================================================
  // Component Navigation Helpers
  // ============================================================================

  const getComponentPosition = useCallback((componentId: string): GridPosition | null => {
    const component = grid?.components.find(c => c.id === componentId);
    return component?.position || null;
  }, [grid?.components]);

  const { onComponentMove } = props;
  const moveComponent = useCallback((componentId: string, direction: 'up' | 'down' | 'left' | 'right', amount = 1) => {
    if (!isEditing) return;

    const position = getComponentPosition(componentId);
    if (!position) return;

    const newPosition = { ...position };
    
    switch (direction) {
      case 'up':
        newPosition.y = Math.max(0, position.y - amount);
        break;
      case 'down':
        newPosition.y = position.y + amount;
        break;
      case 'left':
        newPosition.x = Math.max(0, position.x - amount);
        break;
      case 'right':
        newPosition.x = Math.min((grid?.columns || 12) - position.w, position.x + amount);
        break;
    }

    onKeyboardMove?.(componentId, direction, amount);
    onComponentMove?.(componentId, newPosition);
    
    toast.success(`Moved component ${direction} by ${amount} grid unit${amount > 1 ? 's' : ''}`);
  }, [isEditing, getComponentPosition, onKeyboardMove, onComponentMove, grid?.columns]);

  const { onComponentResize } = props;
  const resizeComponent = useCallback((componentId: string, direction: 'up' | 'down' | 'left' | 'right', amount = 1) => {
    if (!isEditing) return;

    const position = getComponentPosition(componentId);
    if (!position) return;

    const newPosition = { ...position };
    
    switch (direction) {
      case 'up':
        newPosition.h = Math.max(1, position.h - amount);
        break;
      case 'down':
        newPosition.h = position.h + amount;
        break;
      case 'left':
        newPosition.w = Math.max(1, position.w - amount);
        break;
      case 'right':
        newPosition.w = Math.min((grid?.columns || 12) - position.x, position.w + amount);
        break;
    }

    onKeyboardResize?.(componentId, direction, amount);
    onComponentResize?.(componentId, { w: newPosition.w, h: newPosition.h });
    
    toast.success(`Resized component ${direction} by ${amount} grid unit${amount > 1 ? 's' : ''}`);
  }, [isEditing, getComponentPosition, onKeyboardResize, onComponentResize, grid?.columns]);

  // ============================================================================
  // Keyboard Event Handler
  // ============================================================================

  function handleCustomKeyDown(event: KeyboardEvent): boolean | void {
    if (!enableKeyboardNavigation || !focusedComponentId) return;

    const isShiftPressed = event.shiftKey;
    const isCtrlPressed = event.ctrlKey || event.metaKey;
    const key = event.key.toLowerCase();

    // Handle movement vs resize mode toggle
    if (key === 'r' && !isCtrlPressed) {
      event.preventDefault();
      setIsResizeMode(!isResizeMode);
      toast.info(`${isResizeMode ? 'Exited' : 'Entered'} resize mode. Use arrow keys to ${isResizeMode ? 'move' : 'resize'} component.`);
      return true;
    }

    // Handle arrow keys for movement or resizing
    if (['arrowup', 'arrowdown', 'arrowleft', 'arrowright'].includes(key)) {
      event.preventDefault();
      
      const direction = key.replace('arrow', '') as 'up' | 'down' | 'left' | 'right';
      const amount = isShiftPressed ? 5 : 1; // Shift for larger movements
      
      if (isResizeMode) {
        resizeComponent(focusedComponentId, direction, amount);
      } else {
        moveComponent(focusedComponentId, direction, amount);
      }
      return true;
    }

    // Handle delete key
    if (key === 'delete' || key === 'backspace') {
      if (isEditing && focusedComponentId) {
        event.preventDefault();
        onComponentDelete?.(focusedComponentId);
        setFocusedComponentId(null);
        toast.success('Component deleted');
        return true;
      }
    }

    // Handle escape key
    if (key === 'escape') {
      if (isResizeMode) {
        setIsResizeMode(false);
        toast.info('Exited resize mode');
      } else {
        setFocusedComponentId(null);
        onComponentSelect?.(null);
        toast.info('Component deselected');
      }
      return true;
    }

    // Handle enter/space for activation
    if ((key === 'enter' || key === ' ') && !isCtrlPressed) {
      event.preventDefault();
      if (focusedComponentId) {
        onComponentSelect?.(focusedComponentId);
        toast.info('Component selected');
      }
      return true;
    }

    return false;
  }

  // ============================================================================
  // Built-in Keyboard Shortcuts
  // ============================================================================

  const builtInShortcuts: KeyboardShortcut[] = [
    {
      key: 'a',
      ctrlKey: true,
      action: () => {
        if (grid?.components) {
          const firstComponent = grid.components[0];
          if (firstComponent) {
            setFocusedComponentId(firstComponent.id);
            onComponentSelect?.(firstComponent.id);
            toast.info('Selected first component');
          }
        }
      },
      description: 'Select first component'
    },
    {
      key: 'a',
      metaKey: true,
      action: () => {
        if (grid?.components) {
          const firstComponent = grid.components[0];
          if (firstComponent) {
            setFocusedComponentId(firstComponent.id);
            onComponentSelect?.(firstComponent.id);
            toast.info('Selected first component');
          }
        }
      },
      description: 'Select first component'
    },
    {
      key: 'g',
      action: () => {
        if (isEditing && grid) {
          toast.info(`Grid snapping toggle - feature coming soon`);
        }
      },
      description: 'Toggle grid snapping'
    },
    {
      key: 'h',
      action: () => {
        const shortcuts = [
          'Arrow Keys: Move/resize component',
          'Shift + Arrow: Move/resize by 5 units',
          'R: Toggle resize mode',
          'Enter/Space: Open properties',
          'Delete: Remove component',
          'Escape: Deselect component',
          'G: Toggle grid snapping',
          'Ctrl/Cmd+A: Select first component',
          'Tab/Shift+Tab: Navigate components'
        ];
        
        toast.info(`Keyboard Shortcuts:\n${shortcuts.join('\n')}`, {
          duration: 8000
        });
      },
      description: 'Show keyboard shortcuts help'
    }
  ];

  // ============================================================================
  // Register Shortcuts
  // ============================================================================

  useKeyboardShortcuts([...builtInShortcuts, ...keyboardShortcuts], enableKeyboardNavigation && isActive);

  // ============================================================================
  // Register Navigation Items
  // ============================================================================

  useEffect(() => {
    if (!grid?.components || !enableKeyboardNavigation) return;

    const unregisterFunctions: (() => void)[] = [];

    grid.components.forEach((component) => {
      const navigationItem: NavigationItem = {
        id: component.id,
        element: containerRef.current?.querySelector(`[data-component-id="${component.id}"]`) as HTMLElement,
        group: 'grid-components',
        onActivate: () => {
          setFocusedComponentId(component.id);
          onComponentSelect?.(component.id);
        },
        onEscape: () => {
          setFocusedComponentId(null);
          onComponentSelect?.(null);
        }
      };

      const unregister = registerItem(navigationItem);
      unregisterFunctions.push(unregister);
    });

    return () => {
      unregisterFunctions.forEach(fn => fn());
    };
  }, [grid?.components, enableKeyboardNavigation, registerItem, onComponentSelect]);

  // ============================================================================
  // Sync Selected Component
  // ============================================================================

  useEffect(() => {
    if (selectedComponentId !== focusedComponentId) {
      setFocusedComponentId(selectedComponentId || null);
    }
  }, [selectedComponentId, focusedComponentId]);

  // ============================================================================
  // Modal Management
  // ============================================================================

  useEffect(() => {
    if (isEditing && isActive) {
      pushModal('bento-grid');
      return () => popModal('bento-grid');
    }
  }, [isEditing, isActive, pushModal, popModal]);

  // ============================================================================
  // Enhanced Component Selection
  // ============================================================================

  const handleComponentClick = useCallback((componentId: string) => {
    setFocusedComponentId(componentId);
    onComponentSelect?.(componentId);
    
    // Focus the component element
    const element = containerRef.current?.querySelector(`[data-component-id="${componentId}"]`) as HTMLElement;
    if (element) {
      element.focus();
    }
  }, [onComponentSelect]);

  // ============================================================================
  // Render
  // ============================================================================

  return (
    <div
      ref={containerRef}
      className={cn(
        'bento-grid-keyboard relative focus-within:outline-none',
        enableKeyboardNavigation && 'keyboard-navigation-enabled',
        isActive && 'keyboard-navigation-active',
        isResizeMode && 'resize-mode',
        className
      )}
      tabIndex={enableKeyboardNavigation ? 0 : -1}
      role="grid"
      aria-label="Bento grid with keyboard navigation"
      data-keyboard-navigation={enableKeyboardNavigation}
      data-focused-component={focusedComponentId}
      data-resize-mode={isResizeMode}
    >
      {/* Accessibility announcement */}
      <div 
        className="sr-only" 
        aria-live="polite" 
        aria-atomic="true"
        id="grid-announcements"
      >
        {focusedComponentId && (
          `Component ${focusedComponentId} selected. ${isResizeMode ? 'Resize mode active.' : 'Move mode active.'} Press H for help.`
        )}
      </div>

      {/* Visual indicators */}
      {enableKeyboardNavigation && isActive && (
        <div className="absolute top-2 right-2 flex items-center gap-xs pointer-events-none z-50">
          <div className="bg-blue-500 text-white text-xs px-2 py-1 rounded font-mono">
            KB Nav
          </div>
          {isResizeMode && (
            <div className="bg-orange-500 text-white text-xs px-2 py-1 rounded font-mono">
              Resize
            </div>
          )}
          {focusedComponentId && (
            <div className="bg-green-500 text-white text-xs px-2 py-1 rounded font-mono">
              {focusedComponentId}
            </div>
          )}
        </div>
      )}

      {/* Enhanced component rendering with keyboard support */}
      {children || (
        <div className="relative w-full h-full">
          {grid?.components.map((component) => (
            <div
              key={component.id}
              data-component-id={component.id}
              className={cn(
                'grid-component-wrapper focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                focusedComponentId === component.id && 'keyboard-focused ring-2 ring-blue-500',
                enableKeyboardNavigation && 'keyboard-navigable cursor-pointer'
              )}
              tabIndex={enableKeyboardNavigation ? 0 : -1}
              role="gridcell"
              aria-label={`Component ${component.id}, type ${component.componentType}`}
              aria-selected={focusedComponentId === component.id}
              onClick={() => enableKeyboardNavigation && handleComponentClick(component.id)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  handleComponentClick(component.id);
                }
              }}
              style={{
                position: 'absolute',
                left: `${(component.position.x / (grid?.columns || 12)) * 100}%`,
                top: `${component.position.y * (grid?.rowHeight || 100)}px`,
                width: `${(component.position.w / (grid?.columns || 12)) * 100}%`,
                height: `${component.position.h * (grid?.rowHeight || 100)}px`,
                zIndex: focusedComponentId === component.id ? 10 : component.display?.zIndex || 1
              }}
            >
              {/* Component content would be rendered here */}
              <div className="w-full h-full bg-white border border-gray-200 rounded-lg p-sm shadow-sm">
                <div className="text-sm font-medium text-gray-900">
                  {component.componentType}
                </div>
                <div className="text-xs text-gray-500 mt-1">
                  {component.id}
                </div>
                <div className="text-xs text-gray-400 mt-2">
                  {component.position.w} × {component.position.h}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Keyboard navigation styles */}
      <style>{`
        .keyboard-navigation-enabled .grid-component-wrapper:focus {
          outline: 2px solid #3b82f6;
          outline-offset: 2px;
        }
        
        .keyboard-navigation-enabled .grid-component-wrapper.keyboard-focused {
          box-shadow: 0 0 0 2px #3b82f6, 0 0 0 4px rgba(59, 130, 246, 0.2);
        }
        
        .resize-mode .grid-component-wrapper.keyboard-focused {
          box-shadow: 0 0 0 2px #f97316, 0 0 0 4px rgba(249, 115, 22, 0.2);
        }
        
        .keyboard-navigable {
          transition: all 0.2s ease-in-out;
        }
        
        .keyboard-navigable:hover {
          transform: translateY(-2px);
          box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
        }
      `}</style>
    </div>
  );
}

// ============================================================================
// Export Enhanced BentoGrid as default
// ============================================================================

export { BentoGridKeyboard as BentoGrid };