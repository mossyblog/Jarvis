# Keyboard Navigation System

A comprehensive keyboard navigation system for the UI Studio that provides accessible, efficient keyboard interactions throughout the interface.

## Features

- **Comprehensive Navigation**: Tab, Shift+Tab, Arrow keys, Home, End
- **Keyboard Shortcuts**: Customizable shortcuts with modifier key support
- **Focus Management**: Automatic focus trapping and restoration
- **Accessibility**: Full WCAG compliance with ARIA attributes
- **Visual Indicators**: Clear focus states and navigation hints
- **Context Awareness**: Different shortcuts based on current context
- **Modal Management**: Stack-based modal handling for proper escape behavior

## Quick Start

### 1. Wrap your app with the provider

```tsx
import { KeyboardNavigationProvider } from '@/components/keyboard';

function App() {
  return (
    <KeyboardNavigationProvider>
      <YourApp />
    </KeyboardNavigationProvider>
  );
}
```

### 2. Use the enhanced components

```tsx
import { BentoGrid } from '@/components/keyboard';

function MyPage() {
  return (
    <BentoGrid
      grid={myGrid}
      isEditing={true}
      enableKeyboardNavigation={true}
      onComponentMove={handleMove}
      onComponentSelect={handleSelect}
    />
  );
}
```

### 3. Add custom shortcuts

```tsx
import { useKeyboardShortcuts } from '@/components/keyboard';

function MyComponent() {
  useKeyboardShortcuts([
    {
      key: 's',
      ctrlKey: true,
      action: () => save(),
      description: 'Save document'
    }
  ]);

  return <div>My component</div>;
}
```

## Core Components

### KeyboardNavigationProvider

Global context provider that manages keyboard shortcuts and focus state.

```tsx
<KeyboardNavigationProvider
  enabled={true}
  actions={{
    openCommandPalette: () => setCommandPaletteOpen(true),
    toggleEditMode: () => setEditMode(!editMode)
  }}
>
  {children}
</KeyboardNavigationProvider>
```

### BentoGridKeyboard

Enhanced BentoGrid with full keyboard navigation support.

**Key Features:**
- Arrow key navigation between components
- Enter/Space for component activation
- Delete key for component removal
- R key to toggle resize mode
- Shift+Arrow for larger movements (5 units)
- Escape for deselection

```tsx
<BentoGridKeyboard
  grid={grid}
  isEditing={true}
  enableKeyboardNavigation={true}
  selectedComponentId="component-1"
  onKeyboardMove={handleMove}
  onKeyboardResize={handleResize}
/>
```

### ShortcutDisplay

Displays keyboard shortcuts in various formats.

```tsx
// Inline format
<ShortcutDisplay 
  shortcut={{
    key: 's',
    ctrlKey: true,
    action: save,
    description: 'Save document'
  }}
  variant="inline"
/>

// Badge format
<ShortcutDisplay shortcut={shortcut} variant="badge" />

// Tooltip format
<ShortcutDisplay shortcut={shortcut} variant="tooltip" />
```

### ShortcutHelpDialog

Modal dialog showing all available shortcuts.

```tsx
<ShortcutHelpDialog
  shortcuts={customShortcuts}
  title="My App Shortcuts"
  trigger={<Button>Show Shortcuts</Button>}
/>
```

## Hooks

### useKeyboardNavigation

Main hook for implementing keyboard navigation in custom components.

```tsx
import { useKeyboardNavigation } from '@/components/keyboard';

function MyGridComponent() {
  const containerRef = useRef<HTMLDivElement>(null);
  
  const {
    registerShortcut,
    registerItem,
    focusItem,
    focusNext,
    focusPrevious
  } = useKeyboardNavigation(containerRef, {
    enableArrowKeys: true,
    enableHomeEnd: true,
    trapFocus: true
  });

  // Register navigation items
  useEffect(() => {
    const unregister = registerItem({
      id: 'item-1',
      element: itemRef.current,
      onActivate: () => selectItem('item-1')
    });
    
    return unregister;
  }, []);

  return (
    <div ref={containerRef} tabIndex={0}>
      {/* Your navigable content */}
    </div>
  );
}
```

### useKeyboardShortcuts

Simple hook for registering keyboard shortcuts.

```tsx
import { useKeyboardShortcuts } from '@/components/keyboard';

function MyComponent() {
  useKeyboardShortcuts([
    {
      key: 'h',
      action: () => showHelp(),
      description: 'Show help'
    },
    {
      key: 's',
      ctrlKey: true,
      action: () => save(),
      description: 'Save'
    }
  ]);

  return <div>My component</div>;
}
```

### useFocusManagement

Hook for managing focus within a container.

```tsx
import { useFocusManagement } from '@/components/keyboard';

function MyModal({ isOpen }) {
  const modalRef = useRef<HTMLDivElement>(null);
  
  useFocusManagement(modalRef, {
    trapFocus: isOpen,
    restoreFocus: true
  });

  return (
    <div ref={modalRef} role="dialog">
      {/* Modal content */}
    </div>
  );
}
```

## Global Shortcuts

The following shortcuts are available globally:

| Shortcut | Action |
|----------|--------|
| `Ctrl/Cmd + K` | Open command palette |
| `?` | Show help |
| `E` | Toggle edit mode |
| `Ctrl/Cmd + N` | New page |
| `Ctrl/Cmd + S` | Save page |
| `Ctrl/Cmd + B` | Toggle sidebar |
| `Ctrl/Cmd + Shift + L` | Toggle theme |
| `/` | Focus search |
| `Escape` | Close modal/dialog |
| `Alt + Left` | Navigate back |
| `Alt + Right` | Navigate forward |

## Grid Navigation Shortcuts

When focusing on the BentoGrid:

| Shortcut | Action |
|----------|--------|
| `Arrow Keys` | Move component (1 unit) |
| `Shift + Arrow` | Move component (5 units) |
| `R` | Toggle resize mode |
| `Enter/Space` | Open properties |
| `Delete` | Remove component |
| `Escape` | Deselect component |
| `G` | Toggle grid snapping |
| `Ctrl/Cmd + A` | Select first component |
| `H` | Show shortcuts help |

## Accessibility

The keyboard navigation system is designed with accessibility in mind:

- **WCAG 2.1 AA Compliance**: Meets accessibility standards
- **Screen Reader Support**: Proper ARIA attributes and announcements
- **Focus Management**: Visible focus indicators and logical tab order
- **Keyboard Only**: All functionality accessible via keyboard
- **Skip Links**: Built-in skip navigation support

### ARIA Attributes

Components automatically include appropriate ARIA attributes:

```tsx
<div
  role="grid"
  aria-label="Bento grid with keyboard navigation"
  aria-activedescendant="focused-component-id"
>
  <div
    role="gridcell"
    aria-selected="true"
    aria-label="Component metric, 3x2 size"
  >
    {/* Component content */}
  </div>
</div>
```

### Screen Reader Announcements

Live regions provide screen reader feedback:

```tsx
<div aria-live="polite" aria-atomic="true">
  Component component-1 selected. Move mode active. Press H for help.
</div>
```

## Customization

### Custom Shortcuts

```tsx
const customShortcuts = [
  {
    key: 'x',
    action: () => exportData(),
    description: 'Export data'
  },
  {
    key: 'i',
    ctrlKey: true,
    action: () => importData(),
    description: 'Import data'
  }
];

<KeyboardNavigationProvider actions={{
  customAction: () => console.log('Custom action')
}}>
  {children}
</KeyboardNavigationProvider>
```

### Custom Styling

```css
/* Focus indicators */
.keyboard-navigation-enabled .grid-component-wrapper:focus {
  outline: 2px solid #3b82f6;
  outline-offset: 2px;
}

/* Resize mode styling */
.resize-mode .grid-component-wrapper.keyboard-focused {
  box-shadow: 0 0 0 2px #f97316, 0 0 0 4px rgba(249, 115, 22, 0.2);
}
```

## Best Practices

1. **Always provide alternatives**: Every mouse interaction should have a keyboard equivalent
2. **Clear focus indicators**: Make it obvious what element has focus
3. **Logical tab order**: Ensure tab navigation follows a logical flow
4. **Escape hatches**: Always provide a way to exit modal states
5. **Contextual shortcuts**: Different shortcuts for different modes/contexts
6. **Help accessibility**: Make shortcuts discoverable with `?` or help buttons
7. **Test with keyboard only**: Regularly test your interface using only the keyboard

## Testing

Run the keyboard navigation tests:

```bash
npm test src/components/keyboard/__tests__/KeyboardNavigation.test.tsx
```

## Troubleshooting

### Shortcuts not working
- Check if component has focus
- Verify shortcuts are registered properly
- Check for conflicting event handlers

### Focus not visible
- Ensure focus styles are properly defined
- Check if focus is being programmatically moved
- Verify tabindex values are correct

### Navigation not working in modals
- Ensure modal is properly registered in modal stack
- Check focus trap implementation
- Verify escape key handling

## Migration Guide

### From v1 to v2
- Replace `BentoGrid` with `BentoGridKeyboard`
- Wrap app with `KeyboardNavigationProvider`
- Update keyboard shortcut registrations to use new format

### Existing Components
Most existing components will work without changes, but for full keyboard support:

1. Add keyboard event handlers
2. Implement focus management
3. Add ARIA attributes
4. Test keyboard-only navigation