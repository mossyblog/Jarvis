# ThemeContext Test Suite

This directory contains comprehensive tests for the ThemeContext system, covering all aspects of theme management functionality.

## Test Files Overview

### 1. `ThemeContext.test.tsx` - Core Functionality (22 tests)
- **Provider Initialization**: Context setup, error handling for missing provider, default values, readonly arrays
- **Local Storage Integration**: Loading from storage, invalid data handling, saving changes
- **Theme Switching**: Theme/mode changes, independent state management
- **DOM Updates**: CSS class application, data attribute management
- **State Synchronization**: Consumer notifications, function stability
- **Error Handling**: localStorage errors, invalid JSON handling
- **TypeScript Type Safety**: Theme name and mode type enforcement

### 2. `ThemeContext.persistence.test.tsx` - Storage & Validation (16 tests)
- **localStorage Persistence**: Cross-remount persistence, multiple changes, quota errors, disabled storage
- **Theme Validation**: Invalid theme/mode rejection, malformed data handling, available themes validation
- **Edge Cases**: Empty strings, null values, case sensitivity, runtime localStorage changes
- **Performance & Optimization**: Minimal reads, write batching, operation efficiency
- **Cross-tab Synchronization**: Storage event handling (documented for future enhancement)

### 3. `ThemeContext.dom.test.tsx` - DOM Integration (16 tests)
- **CSS Class Management**: Initial application, class removal/addition, rapid changes, order preservation
- **Data Attribute Management**: Theme/mode attributes, consistency, atomic updates
- **CSS Custom Properties Integration**: Data attribute usage, themed component selection
- **Theme Transition Preparation**: DOM structure stability, layout thrashing prevention
- **Error Handling**: DOM manipulation errors (documented for enhancement)
- **Performance Optimizations**: DOM operation minimization, update batching

### 4. `ThemeContext.integration.test.tsx` - Component Integration & Performance (14 tests)
- **ThemeSwitcher Integration**: Component interaction, dropdown functionality, theme changes
- **System Preference Detection**: Dark mode detection (documented for enhancement)
- **Performance Testing**: Rapid changes, memory leak prevention, concurrent operations, stable references
- **Error Boundaries**: React.StrictMode compatibility, provider unmounting, rapid mount/unmount
- **Accessibility Integration**: Screen reader support, ARIA labels, keyboard accessibility

## Test Coverage Areas

### ✅ Core Functionality
- [x] Theme provider functionality and state management
- [x] Theme switching (theme and mode independently)
- [x] Available themes validation
- [x] CSS custom properties application via data attributes
- [x] DOM class and attribute management
- [x] Integration with ThemeSwitcher components

### ✅ Persistence & Storage
- [x] Local storage persistence across sessions
- [x] Theme validation and error handling for invalid themes
- [x] localStorage error scenarios (quota exceeded, disabled storage)
- [x] Performance optimizations (minimal reads, efficient writes)

### ✅ Error Handling & Edge Cases
- [x] Invalid theme/mode handling
- [x] localStorage unavailability
- [x] Malformed data in storage
- [x] DOM manipulation errors (documented)
- [x] Provider unmounting during operations

### ✅ Performance & Optimization
- [x] Rapid theme changes
- [x] Memory leak prevention
- [x] DOM operation batching
- [x] Stable function references
- [x] Concurrent operation handling

### ✅ Integration & Accessibility
- [x] ThemeSwitcher component integration
- [x] React.StrictMode compatibility
- [x] Screen reader support
- [x] Keyboard accessibility
- [x] ARIA attribute management

### 📋 Documented for Future Enhancement
- [ ] System preference detection (prefers-color-scheme)
- [ ] Theme transition animations
- [ ] Cross-tab synchronization via storage events
- [ ] Screen reader announcements for theme changes
- [ ] Graceful localStorage error handling
- [ ] DOM manipulation error recovery

## Test Architecture

### Mock Strategy
- **localStorage**: Comprehensive mock with call tracking and error simulation
- **document.documentElement**: DOM element mock for class/attribute testing
- **Component Mocks**: ThemeSwitcher components with proper themes registry

### Testing Patterns
- **Arrange-Act-Assert**: Clear test structure
- **User-centric Testing**: userEvent for realistic interactions
- **Error Simulation**: Comprehensive error scenario coverage
- **Performance Monitoring**: Render time tracking and optimization verification

### Test Utilities
- **Custom Render Functions**: Provider setup with configurable defaults
- **Mock Factories**: Reusable mock creation functions
- **Performance Monitors**: Render time and DOM operation tracking
- **Error Handlers**: Graceful error testing without console noise

## Running the Tests

```bash
# Run all ThemeContext tests
npm test -- src/contexts/__tests__/ThemeContext

# Run individual test files
npm test -- src/contexts/__tests__/ThemeContext.test.tsx
npm test -- src/contexts/__tests__/ThemeContext.persistence.test.tsx
npm test -- src/contexts/__tests__/ThemeContext.dom.test.tsx
npm test -- src/contexts/__tests__/ThemeContext.integration.test.tsx

# Run with coverage
npm run test:coverage -- src/contexts/__tests__/ThemeContext
```

## Test Statistics
- **Total Tests**: 68 tests across 4 files
- **Coverage Areas**: 10+ major functionality areas
- **Performance Tests**: 6 dedicated performance test cases
- **Error Scenarios**: 12+ error handling test cases
- **Integration Tests**: 8 component integration tests

## Key Testing Principles

1. **Comprehensive Coverage**: Tests cover happy paths, edge cases, and error scenarios
2. **Real-world Usage**: Tests simulate actual user interactions and component integration
3. **Performance Awareness**: Tests verify performance characteristics and optimizations
4. **Future-Ready**: Tests document expected behavior for planned enhancements
5. **Maintainable**: Tests are well-organized, clearly documented, and follow consistent patterns

This test suite provides confidence in the ThemeContext system's reliability, performance, and maintainability while documenting areas for future enhancement.