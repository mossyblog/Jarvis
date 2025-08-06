/**
 * KeyboardShortcutDisplay - Component for displaying keyboard shortcuts
 * 
 * Provides accessible displays for keyboard shortcuts throughout the interface.
 * Includes visual indicators, tooltips, and help dialogs.
 */

import React, { useState } from 'react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { Keyboard, HelpCircle } from 'lucide-react';
import type { KeyboardShortcut } from '@/hooks/useKeyboardNavigation';
import { useKeyboardNavigationContext } from './KeyboardNavigationProvider';

// ============================================================================
// Types
// ============================================================================

export interface ShortcutDisplayProps {
  shortcut: KeyboardShortcut;
  variant?: 'inline' | 'badge' | 'tooltip';
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

export interface ShortcutHelpDialogProps {
  shortcuts?: KeyboardShortcut[];
  trigger?: React.ReactNode;
  title?: string;
  description?: string;
}

export interface ShortcutBadgeProps {
  keys: string[];
  variant?: 'default' | 'outline' | 'secondary';
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

// ============================================================================
// Keyboard Key Display
// ============================================================================

function formatShortcutKey(shortcut: KeyboardShortcut): string[] {
  const keys: string[] = [];
  
  if (shortcut.ctrlKey) keys.push('Ctrl');
  if (shortcut.metaKey) keys.push('Cmd');
  if (shortcut.altKey) keys.push('Alt');
  if (shortcut.shiftKey) keys.push('Shift');
  
  // Format special keys
  const keyMap: Record<string, string> = {
    ' ': 'Space',
    'arrowup': '↑',
    'arrowdown': '↓',
    'arrowleft': '←',
    'arrowright': '→',
    'enter': 'Enter',
    'escape': 'Esc',
    'tab': 'Tab',
    'backspace': 'Backspace',
    'delete': 'Del',
    'home': 'Home',
    'end': 'End',
    'pageup': 'PgUp',
    'pagedown': 'PgDn'
  };
  
  const key = shortcut.key.toLowerCase();
  keys.push(keyMap[key] || shortcut.key.toUpperCase());
  
  return keys;
}

// ============================================================================
// Shortcut Badge Component
// ============================================================================

export function ShortcutBadge({ 
  keys, 
  variant = 'outline', 
  size = 'sm',
  className 
}: ShortcutBadgeProps) {
  const sizeClasses = {
    sm: 'text-xs px-1.5 py-0.5',
    md: 'text-sm px-2 py-1',
    lg: 'text-base px-3 py-1.5'
  };

  return (
    <div className={cn('flex items-center gap-1', className)}>
      {keys.map((key, index) => (
        <React.Fragment key={key}>
          {index > 0 && <span className="text-muted-foreground text-xs">+</span>}
          <Badge 
            variant={variant} 
            className={cn(
              'font-mono font-medium border border-border bg-background text-foreground',
              sizeClasses[size]
            )}
          >
            {key}
          </Badge>
        </React.Fragment>
      ))}
    </div>
  );
}

// ============================================================================
// Shortcut Display Component
// ============================================================================

export function ShortcutDisplay({ 
  shortcut, 
  variant = 'inline',
  size = 'md',
  className 
}: ShortcutDisplayProps) {
  const keys = formatShortcutKey(shortcut);

  switch (variant) {
    case 'badge':
      return (
        <ShortcutBadge 
          keys={keys} 
          size={size}
          className={className}
        />
      );

    case 'tooltip':
      return (
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <div className={cn('inline-flex items-center gap-2', className)}>
                <span>{shortcut.description}</span>
                <ShortcutBadge keys={keys} size={size} />
              </div>
            </TooltipTrigger>
            <TooltipContent>
              <p>Keyboard shortcut: {keys.join(' + ')}</p>
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      );

    case 'inline':
    default:
      return (
        <div className={cn('flex items-center justify-between gap-4', className)}>
          <span className="text-sm text-foreground">{shortcut.description}</span>
          <ShortcutBadge keys={keys} size={size} />
        </div>
      );
  }
}

// ============================================================================
// Shortcut Help Dialog
// ============================================================================

export function ShortcutHelpDialog({ 
  shortcuts: customShortcuts,
  trigger,
  title = 'Keyboard Shortcuts',
  description = 'Speed up your workflow with these keyboard shortcuts'
}: ShortcutHelpDialogProps) {
  const { getShortcuts } = useKeyboardNavigationContext();
  const [isOpen, setIsOpen] = useState(false);
  
  const shortcuts = customShortcuts || getShortcuts();
  
  // Group shortcuts by category
  const groupedShortcuts = shortcuts.reduce((groups, shortcut) => {
    // Extract category from description or use default
    const category = shortcut.description.includes(':') 
      ? shortcut.description.split(':')[0].trim()
      : 'General';
    
    if (!groups[category]) {
      groups[category] = [];
    }
    groups[category].push(shortcut);
    return groups;
  }, {} as Record<string, KeyboardShortcut[]>);

  const defaultTrigger = (
    <Button variant="ghost" size="sm">
      <Keyboard className="h-xs w-xs mr-2" />
      Shortcuts
    </Button>
  );

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        {trigger || defaultTrigger}
      </DialogTrigger>
      <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Keyboard className="h-sm w-sm" />
            {title}
          </DialogTitle>
          {description && (
            <p className="text-sm text-muted-foreground mt-2">
              {description}
            </p>
          )}
        </DialogHeader>
        
        <div className="space-y-6 mt-4">
          {Object.entries(groupedShortcuts).map(([category, categoryShortcuts]) => (
            <div key={category}>
              <h3 className="text-lg font-semibold mb-3 text-foreground">
                {category}
              </h3>
              <div className="space-y-2">
                {categoryShortcuts.map((shortcut, index) => (
                  <ShortcutDisplay 
                    key={`${category}-${index}`}
                    shortcut={shortcut}
                    variant="inline"
                    className="py-2"
                  />
                ))}
              </div>
              {Object.keys(groupedShortcuts).length > 1 && 
               category !== Object.keys(groupedShortcuts)[Object.keys(groupedShortcuts).length - 1] && (
                <Separator className="mt-4" />
              )}
            </div>
          ))}
        </div>

        {/* Footer */}
        <div className="mt-6 p-4 bg-muted/50 rounded-lg">
          <p className="text-xs text-muted-foreground">
            <strong>Tip:</strong> These shortcuts work when the interface has focus. 
            Some shortcuts may be disabled when typing in input fields.
          </p>
        </div>
      </DialogContent>
    </Dialog>
  );
}

// ============================================================================
// Quick Help Button
// ============================================================================

export function QuickHelpButton({ 
  shortcuts,
  className 
}: { 
  shortcuts?: KeyboardShortcut[];
  className?: string;
}) {
  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <ShortcutHelpDialog 
            shortcuts={shortcuts}
            trigger={
              <Button 
                variant="ghost" 
                size="sm"
                className={cn('h-lg w-lg p-0', className)}
              >
                <HelpCircle className="h-xs w-xs" />
                <span className="sr-only">Show keyboard shortcuts</span>
              </Button>
            }
          />
        </TooltipTrigger>
        <TooltipContent>
          <p>Keyboard shortcuts (Press ? or click)</p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

// ============================================================================
// Context Shortcut Display
// ============================================================================

export function ContextualShortcuts({ 
  shortcuts,
  title = 'Available shortcuts',
  className 
}: {
  shortcuts: KeyboardShortcut[];
  title?: string;
  className?: string;
}) {
  if (shortcuts.length === 0) return null;

  return (
    <div className={cn('space-y-2', className)}>
      <h4 className="text-sm font-medium text-muted-foreground">{title}</h4>
      <div className="space-y-1">
        {shortcuts.slice(0, 5).map((shortcut, index) => (
          <ShortcutDisplay 
            key={index}
            shortcut={shortcut}
            variant="inline"
            size="sm"
            className="text-xs"
          />
        ))}
        {shortcuts.length > 5 && (
          <ShortcutHelpDialog 
            shortcuts={shortcuts}
            trigger={
              <Button variant="ghost" size="sm" className="h-md text-xs">
                +{shortcuts.length - 5} more shortcuts
              </Button>
            }
          />
        )}
      </div>
    </div>
  );
}

// ============================================================================
// Inline Shortcut Hint
// ============================================================================

export function InlineShortcutHint({ 
  shortcut,
  children,
  className 
}: {
  shortcut: KeyboardShortcut;
  children: React.ReactNode;
  className?: string;
}) {
  const keys = formatShortcutKey(shortcut);

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <div className={cn('inline-flex items-center gap-2', className)}>
            {children}
            <ShortcutBadge keys={keys} size="sm" variant="secondary" />
          </div>
        </TooltipTrigger>
        <TooltipContent>
          <p>{shortcut.description}</p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}