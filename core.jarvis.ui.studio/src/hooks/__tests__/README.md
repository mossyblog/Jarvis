# Mobile Hooks Test Suite

This directory contains comprehensive tests for mobile-related React hooks in the core.jarvis.ui.studio project.

## Test Files

### `useTouchGestures.test.ts`
**Core gesture recognition hook testing**

**Test Categories:**
- **Hook Initialization**: Default state and configuration merging
- **Long Press Detection**: Timing, cancellation, and state management
- **Pinch Gesture Detection**: Two-finger gestures, scale calculation
- **Swipe Gesture Detection**: Direction, velocity, and threshold handling
- **Tap and Double Tap**: Single and multi-tap recognition
- **Touch Target Validation**: WCAG compliance checking
- **Multi-touch Coordination**: Gesture conflict resolution
- **Performance**: High-frequency event handling

### `useTouchGestures.performance.test.ts`
**Performance and stress testing for touch gestures**

**Test Categories:**
- **High-Frequency Event Handling**: 120fps touch event processing
- **Memory Usage Optimization**: Memory leak detection and prevention
- **Concurrent Gesture Processing**: Multiple simultaneous recognizers
- **Extreme Conditions**: Stress testing under adverse conditions
- **Performance Regression Detection**: Benchmarking and monitoring

**Key Features:**
- Memory usage monitoring with `performance.memory` API
- Frame rate tracking for smooth 60fps performance
- Event throttling validation to prevent performance degradation
- Stress testing with rapid gesture sequences
- Concurrent processing with multiple gesture recognizers

### `useBottomSheet.test.ts`
**Bottom sheet state management hook testing**

**Test Categories:**
- **Basic State Management**: Open/close/toggle functionality
- **Position Calculations**: Height calculations from drag deltas
- **Snap Point Logic**: Nearest point and velocity-based snapping
- **Animation State Management**: Smooth transitions and easing
- **Closing Logic**: Determining when to close based on position/velocity
- **Drag State Management**: Tracking drag interactions
- **Memory Management**: Cleanup and lifecycle handling
- **Integration Scenarios**: Multi-instance and concurrent usage

**Key Features:**
- Extended hook implementation for comprehensive testing
- Real-time position and velocity calculations
- Animation state tracking and validation
- Memory leak detection during repeated operations
- Error handling for edge cases and malformed input

## Testing Utilities

### Performance Monitoring
```typescript
const performanceMonitor = createPerformanceMonitor()
performanceMonitor.recordExecution(duration)
performanceMonitor.recordMemory()
performanceMonitor.recordFrame()
```

### Gesture Simulation
```typescript
// High-frequency touch events
const events = generateHighFrequencyTouchSequence(element, 1000, 120)

// Memory pressure simulation
const cleanup = simulateMemoryPressure(1000)
```

### Test Environment Setup
```typescript
const testEnvironment = setupBentoTestEnvironment()
testEnvironment.enableTouchDevice()
```

## Performance Benchmarks

### Target Metrics
- **Average execution time**: < 5ms per gesture handler
- **Frame rate**: > 30fps during gesture processing
- **Memory growth**: < 100KB during extended use
- **Event processing**: Handle 120fps input without lag

### Stress Test Scenarios
- **Rapid gesture switching**: 100+ gestures in 2 seconds
- **Memory pressure**: 1000+ temporary objects during processing
- **Concurrent recognizers**: 20+ simultaneous gesture handlers
- **High-frequency events**: 240fps touch input simulation

## Test Coverage

### Functional Coverage
- ✅ All hook return values and methods
- ✅ State transitions and updates
- ✅ Event listener lifecycle
- ✅ Configuration option handling
- ✅ Error boundaries and edge cases

### Performance Coverage
- ✅ Memory usage patterns
- ✅ Execution time benchmarks
- ✅ Frame rate stability
- ✅ Event throttling effectiveness
- ✅ Concurrent processing efficiency

### Integration Coverage
- ✅ Multiple hook instances
- ✅ Cross-hook interactions
- ✅ Component lifecycle integration
- ✅ Real-world usage patterns
- ✅ Device-specific behaviors

## Running Tests

```bash
# Run all hook tests
npm test src/hooks/__tests__/

# Run specific hook tests
npm test src/hooks/__tests__/useTouchGestures.test.ts
npm test src/hooks/__tests__/useBottomSheet.test.ts

# Run performance tests
npm test src/hooks/__tests__/useTouchGestures.performance.test.ts

# Run with coverage
npm test -- --coverage src/hooks/

# Run with verbose output
npm test -- --reporter=verbose src/hooks/__tests__/
```

## Test Environment

### Browser API Mocks
- `ResizeObserver`: Window resize monitoring
- `IntersectionObserver`: Element visibility tracking
- `requestAnimationFrame`: Animation frame scheduling
- `performance.memory`: Memory usage monitoring
- `TouchEvent`: Touch input simulation

### Device Simulation
- Touch device detection
- High DPI display support
- Mobile browser user agents
- Viewport size variations
- Orientation changes

## Common Issues and Solutions

### Act Warnings
Some tests may show React `act()` warnings for state updates. These are typically expected for gesture-driven state changes and can be wrapped in `act()` if needed.

### Memory Pressure
Performance tests intentionally create memory pressure to validate optimization. This may cause temporary slowdowns during test execution.

### Timing Sensitivity
Gesture timing tests may occasionally fail on slower systems. Consider adjusting timeout values or test expectations for CI environments.

### Touch Event Support
Tests require proper touch event simulation. The test environment automatically sets up touch device mocking.

## Best Practices

### Test Structure
- Use descriptive test names that explain the specific behavior
- Group related tests in describe blocks
- Set up and tear down test environment consistently
- Use appropriate timeouts for async operations

### Performance Testing
- Always clean up resources after performance tests
- Use realistic data sizes and event frequencies
- Monitor memory usage during extended operations
- Validate frame rate stability during animations

### Mock Management
- Restore all mocks after each test
- Use consistent mock implementations across tests
- Validate mock interactions where appropriate
- Clean up global state modifications

## Future Enhancements

- **Real device testing** with browser automation
- **Performance regression tracking** in CI/CD
- **Memory profiling** with heap snapshots
- **Cross-browser compatibility** validation
- **Accessibility automation** with assistive technology simulation