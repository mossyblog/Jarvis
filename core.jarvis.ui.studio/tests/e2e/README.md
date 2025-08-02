# E2E Testing Suite

Comprehensive end-to-end testing suite for core.jarvis.ui.studio using Playwright.

## Overview

This testing suite provides complete coverage for:
- ✅ **Authentication flows** - Login, logout, session management
- ✅ **Cross-browser testing** - Chrome, Firefox, Safari
- ✅ **Mobile responsiveness** - Touch interactions, responsive layouts
- ✅ **Accessibility (a11y)** - WCAG compliance, screen reader support
- ✅ **Performance testing** - Core Web Vitals, load times
- ✅ **Visual regression** - Screenshot comparisons
- ✅ **Drag & drop workflows** - Bento grid interactions
- ✅ **API integration** - Mocked and real API testing

## Quick Start

```bash
# Install dependencies
npm install

# Install Playwright browsers
npm run test:e2e:install

# Run all E2E tests
npm run test:e2e

# Run with UI mode (interactive)
npm run test:e2e:ui

# Run specific browser
npm run test:e2e:chromium
npm run test:e2e:firefox
npm run test:e2e:webkit
```

## Test Categories

### 🔐 Authentication Tests
**Location**: `tests/e2e/auth/`
- Login form validation
- Session persistence
- Logout functionality
- Protected route access
- Error handling

### 🧭 Navigation Tests
**Location**: `tests/e2e/dashboard/`
- Sidebar navigation
- Route transitions
- Responsive navigation
- Keyboard navigation
- Deep linking

### 🎯 Bento Grid Tests
**Location**: `tests/e2e/bento/`
- Drag and drop functionality
- Component placement
- Grid layout validation
- Edit mode interactions
- Touch gestures (mobile)

### 📱 Mobile Tests
**Location**: `tests/e2e/mobile/`
- Responsive design
- Touch interactions
- Mobile navigation
- Orientation changes
- Mobile-specific features

### ♿ Accessibility Tests
**Location**: `tests/e2e/accessibility/`
- WCAG compliance
- Keyboard navigation
- Screen reader support
- Color contrast
- Focus management

### ⚡ Performance Tests
**Location**: `tests/e2e/performance/`
- Core Web Vitals
- Load time measurements
- Bundle size analysis
- Memory usage
- Network efficiency

### 👁️ Visual Regression Tests
**Location**: `tests/e2e/visual/`
- Screenshot comparisons
- Cross-browser consistency
- Theme variations
- Component states
- Responsive layouts

## Test Structure

```
tests/e2e/
├── fixtures/           # Test fixtures and shared setup
│   ├── auth.fixture.ts  # Authentication helpers
│   └── test-data.ts     # Mock data factory
├── pages/              # Page Object Models
│   ├── auth.page.ts     # Login/logout page actions
│   ├── dashboard.page.ts # Dashboard interactions
│   └── bento.page.ts    # Bento grid operations
├── utils/              # Test utilities
│   └── test-helpers.ts  # Common test functions
├── auth/               # Authentication tests
├── dashboard/          # Navigation tests
├── bento/             # Drag & drop tests
├── mobile/            # Mobile-specific tests
├── accessibility/     # A11y tests
├── performance/       # Performance tests
├── visual/            # Visual regression tests
├── global-setup.ts    # Global test setup
└── global-teardown.ts # Global test cleanup
```

## Configuration

### Playwright Config
**File**: `playwright.config.ts`

Key settings:
- **Base URL**: `http://localhost:5173`
- **Browsers**: Chrome, Firefox, Safari, Mobile Chrome, Mobile Safari
- **Retries**: 2 on CI, 0 locally
- **Timeout**: 30 seconds per test
- **Parallelization**: Enabled (configurable)

### Test Projects

| Project | Purpose | Browser | Notes |
|---------|---------|---------|-------|
| `chromium` | Desktop Chrome | Chromium | Primary testing browser |
| `firefox` | Desktop Firefox | Firefox | Cross-browser compatibility |
| `webkit` | Desktop Safari | WebKit | macOS/Safari compatibility |
| `Mobile Chrome` | Mobile testing | Chromium | Touch interactions |
| `Mobile Safari` | iOS testing | WebKit | iOS-specific features |
| `accessibility` | A11y testing | Chromium | WCAG compliance |
| `performance-desktop` | Performance | Chromium | Core Web Vitals |
| `visual-regression` | Visual tests | Chromium | Screenshot comparison |

## Writing Tests

### Page Object Model

Use page objects for reusable interactions:

```typescript
import { BentoPage } from '../pages/bento.page';

test('should drag component to grid', async ({ page, authPage }) => {
  const bentoPage = new BentoPage(page);
  await authPage.loginWithTestUser();
  await bentoPage.navigateToBento();
  await bentoPage.enableEditMode();
  await bentoPage.dragComponentToGrid('Card Component');
});
```

### Test Data

Use the data factory for consistent test data:

```typescript
import { TestDataFactory } from '../fixtures/test-data';

const testUser = TestDataFactory.createTestUser();
const mockComponents = TestDataFactory.createTestComponents(5);
```

### API Mocking

Mock API responses for isolated testing:

```typescript
await TestHelpers.mockApiResponse(page, '/api/components', {
  components: mockComponents
});
```

## Best Practices

### ✅ Do's
- Use data-testid attributes for reliable selectors
- Test user journeys, not just individual features
- Include both happy path and error scenarios
- Test responsive behavior across viewports
- Verify accessibility compliance
- Monitor performance metrics
- Use page objects for code reuse

### ❌ Don'ts
- Don't rely on CSS selectors that may change
- Don't test implementation details
- Don't ignore flaky tests
- Don't skip mobile testing
- Don't forget error states
- Don't hardcode delays (use waitFor methods)

## Debugging Tests

### Local Debugging
```bash
# Run with browser visible
npm run test:e2e:headed

# Debug mode (interactive)
npm run test:e2e:debug

# Run specific test file
npx playwright test auth/login.spec.ts --debug
```

### CI Debugging
- Test artifacts are uploaded automatically
- HTML reports available in workflow artifacts
- Screenshots captured on failure
- Videos recorded for failed tests

## Performance Benchmarks

### Target Metrics
- **First Contentful Paint**: < 2.5s
- **Time to First Byte**: < 800ms
- **DOM Interactive**: < 3s
- **Load Complete**: < 5s
- **Bundle Size**: < 2MB (JS), < 500KB (CSS)

### Mobile Performance
- Touch response: < 100ms
- Scroll performance: 60 FPS
- Touch target size: ≥ 44px

## Accessibility Standards

### WCAG Compliance
- **Level AA**: Required for all public interfaces
- **Keyboard Navigation**: Full app navigable via keyboard
- **Color Contrast**: 4.5:1 minimum ratio
- **Screen Reader**: Compatible with NVDA, JAWS, VoiceOver

### Testing Tools
- **@axe-core/playwright**: Automated a11y testing
- **Manual Testing**: Keyboard-only navigation
- **Screen Reader Testing**: VoiceOver/NVDA verification

## CI/CD Integration

### GitHub Actions
**File**: `.github/workflows/e2e-tests.yml`

The workflow runs:
1. **Cross-browser tests** - Chrome, Firefox, Safari
2. **Mobile tests** - Touch interactions
3. **Accessibility tests** - WCAG compliance
4. **Performance tests** - Core Web Vitals
5. **Visual regression tests** - Screenshot comparison

### Test Reports
- HTML report with screenshots
- JUnit XML for CI integration
- JSON results for custom processing
- Allure reports for detailed analysis

## Troubleshooting

### Common Issues

**Flaky tests**:
- Use `waitFor` instead of `setTimeout`
- Ensure proper element stability
- Handle network timing properly

**Browser differences**:
- Test in all target browsers
- Use feature detection, not browser detection
- Account for WebKit quirks

**Mobile testing issues**:
- Use proper viewport sizes
- Test touch gestures thoroughly
- Consider device-specific behaviors

### Getting Help

1. Check test logs and screenshots
2. Run tests locally with `--debug`
3. Review page object implementations
4. Check network requests in browser dev tools
5. Validate API responses match expectations

## Maintenance

### Regular Tasks
- Update browser versions monthly
- Review and update visual regression baselines
- Monitor performance regression trends
- Update test data as features evolve
- Review and fix flaky tests

### Test Data Management
- Keep mock data minimal but realistic
- Update test fixtures when API changes
- Clean up test artifacts regularly
- Rotate test credentials periodically