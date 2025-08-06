# Mobile Components Test Suite

This directory contains comprehensive tests for mobile components in the core.jarvis.ui.studio project, specifically focusing on:

1. **BottomSheet component tests** - drag gestures, snap points, state transitions
2. **useTouchGestures hook tests** - swipe detection, pinch zoom, drag handling  
3. **useBottomSheet hook tests** - state management, position calculations
4. **Mobile touch interaction tests** - gesture recognition, multi-touch
5. **Responsive breakpoint tests** - cross-device compatibility

## Test Files Overview

### Core Component Tests

#### `BottomSheet.test.tsx`
- Basic component rendering and props
- Open/close state management
- Drag handle functionality
- Content rendering and accessibility
- Portal mounting and cleanup

#### `BottomSheet.dragSystem.test.tsx`
- **Drag State Transitions**: Complete drag lifecycle testing
- **Snap Point Behavior**: Velocity-based snapping and animation
- **Multi-touch Conflicts**: Handling simultaneous touch/mouse events
- **Performance Under Load**: High-frequency event handling
- **Accessibility During Drag**: Focus management and screen reader support

### Hook Tests

#### `useTouchGestures.test.ts` (Existing)
- Long press detection and timing
- Pinch-to-zoom gesture recognition
- Swipe gesture detection and direction
- Tap and double-tap handling
- Touch target validation
- Multi-touch coordination

#### `useTouchGestures.performance.test.ts`
- **High-Frequency Event Handling**: 120fps touch event processing
- **Memory Usage Optimization**: Leak detection and cleanup
- **Concurrent Gesture Processing**: Multiple simultaneous recognizers
- **Extreme Conditions**: Stress testing and error recovery
- **Performance Regression Detection**: Benchmarking and monitoring

#### `useBottomSheet.test.ts`
- **State Management**: Open/close/toggle functionality
- **Position Calculations**: Delta-based height calculations
- **Snap Point Logic**: Nearest point and velocity-based snapping
- **Animation State**: Smooth transitions and easing
- **Memory Management**: Cleanup and lifecycle handling

### Integration Tests

#### `MobileGestureIntegration.test.tsx`
- **Cross-Component Coordination**: Gesture handling between components
- **Real-World Usage Scenarios**: Typical mobile app navigation flows
- **Performance Under Mixed Load**: Simultaneous gesture processing
- **Device-Specific Edge Cases**: iOS Safari, Android Chrome quirks
- **Error Recovery**: Graceful handling of conflicts and interruptions

#### `ResponsiveBreakpoints.test.tsx`
- **Breakpoint Detection**: Accurate size-based switching
- **Component Adaptation**: Touch targets, layouts, and gestures
- **CSS Custom Properties**: Dynamic property updates
- **Performance During Changes**: Smooth breakpoint transitions
- **Accessibility Compliance**: WCAG standards across screen sizes

### Behavior Tests

#### `ResponsiveBehavior.test.tsx` (Existing)
- Viewport size adaptation
- Breakpoint-specific optimizations
- Component layout responsiveness
- Orientation change handling

#### `TouchTargetCompliance.test.tsx` (Existing)
- **WCAG 2.1 AA Compliance**: 44x44px minimum touch targets
- **Accessibility Features**: ARIA labels and keyboard navigation
- **High Contrast Support**: Visibility in accessibility modes
- **Performance**: Efficient touch target validation

#### `OrientationHandling.test.tsx` (Existing)
- Portrait/landscape transitions
- Gesture adaptation during rotation
- Layout reflow optimization
- Content preservation

## Testing Approach

### Performance Testing
- **Memory monitoring** using `performance.memory` API
- **Frame rate tracking** with `requestAnimationFrame`
- **Event throttling** validation
- **Stress testing** with rapid gesture sequences

### Accessibility Testing
- **Touch target size validation** (44px minimum on mobile)
- **Keyboard navigation** support verification
- **Screen reader** compatibility testing
- **High contrast mode** support
- **Reduced motion** preference handling

### Cross-Device Testing
- **Mobile devices**: iOS Safari, Android Chrome
- **Tablets**: iPadOS, Android tablets
- **Desktop**: Mouse and keyboard interactions
- **High DPI displays**: Pixel ratio handling
- **Foldable devices**: Dynamic screen changes

### Edge Case Handling
- **Malformed events** and error recovery
- **Memory pressure** scenarios
- **Rapid state changes** and race conditions
- **Component unmounting** during gestures
- **Network interruptions** and focus loss

## Test Utilities

### `bento-test-utils.tsx`
Comprehensive testing utilities including:
- Mock touch event creation
- Gesture simulation functions
- Performance monitoring tools
- Test environment setup
- Mock component renderers

### Common Patterns
- **Setup/Teardown**: Consistent environment preparation
- **Event Simulation**: Realistic touch and gesture events
- **Performance Monitoring**: Memory and timing measurements
- **Accessibility Validation**: Automated compliance checking

## Running Tests

```bash
# Run all mobile component tests
npm test src/components/mobile/__tests__/

# Run specific test suites
npm test src/hooks/__tests__/useTouchGestures.test.ts
npm test src/components/mobile/__tests__/BottomSheet.dragSystem.test.tsx

# Run with coverage
npm test -- --coverage src/components/mobile/

# Run performance tests
npm test src/hooks/__tests__/useTouchGestures.performance.test.ts
```

## Test Coverage Areas

### Functional Testing
- ✅ Component rendering and props
- ✅ State management and transitions
- ✅ Event handling and gestures
- ✅ Accessibility compliance
- ✅ Responsive behavior

### Performance Testing
- ✅ High-frequency event handling
- ✅ Memory usage optimization
- ✅ Animation performance
- ✅ Concurrent gesture processing
- ✅ Stress testing

### Integration Testing
- ✅ Cross-component interaction
- ✅ Real-world usage scenarios
- ✅ Device-specific behaviors
- ✅ Error recovery mechanisms
- ✅ Breakpoint transitions

### Edge Case Testing
- ✅ Malformed input handling
- ✅ Extreme viewport sizes
- ✅ Rapid state changes
- ✅ Component lifecycle issues
- ✅ Memory pressure scenarios

## Notes

- Tests use **Vitest** as the test runner with **@testing-library/react**
- Mock implementations for browser APIs (ResizeObserver, IntersectionObserver)
- Performance tests include memory leak detection
- Integration tests cover real-world mobile usage patterns
- All tests include accessibility validation where applicable

## Future Enhancements

- **Visual regression testing** for UI consistency
- **End-to-end testing** with Playwright
- **Cross-browser automation** testing
- **Performance benchmarking** with CI/CD integration
- **Accessibility automation** with axe-core