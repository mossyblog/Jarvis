/**
 * Keyboard Navigation Components
 * 
 * Comprehensive keyboard navigation system for the UI Studio.
 * Provides hooks, components, and utilities for accessible keyboard interaction.
 */

// Core hooks
export { 
  useKeyboardNavigation,
  useKeyboardShortcuts,
  useFocusManagement 
} from '@/hooks/useKeyboardNavigation';

// Context provider
export { 
  KeyboardNavigationProvider,
  useKeyboardNavigationContext 
} from './KeyboardNavigationProvider';

// Enhanced components
export { BentoGridKeyboard, BentoGrid } from '../bento/BentoGridKeyboard';

// Display components
export {
  ShortcutDisplay,
  ShortcutBadge,
  ShortcutHelpDialog,
  QuickHelpButton,
  ContextualShortcuts,
  InlineShortcutHint
} from './KeyboardShortcutDisplay';

// Types
export type {
  KeyboardShortcut,
  NavigationItem,
  KeyboardNavigationOptions,
  UseKeyboardNavigationReturn
} from '@/hooks/useKeyboardNavigation';

export type {
  GlobalShortcuts,
  KeyboardNavigationContextType
} from './KeyboardNavigationProvider';

export type {
  BentoGridKeyboardProps
} from '../bento/BentoGridKeyboard';

export type {
  ShortcutDisplayProps,
  ShortcutHelpDialogProps,
  ShortcutBadgeProps
} from './KeyboardShortcutDisplay';