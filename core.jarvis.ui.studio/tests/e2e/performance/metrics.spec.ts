import { test, expect } from '../fixtures/auth.fixture';
import { TestHelpers } from '../utils/test-helpers';

test.describe('Performance Metrics', () => {
  test.beforeEach(async ({ page, authPage }) => {
    await authPage.loginWithTestUser();
  });

  test('should meet Core Web Vitals benchmarks on dashboard', async ({ page }) => {
    await page.goto('/');
    await TestHelpers.waitForNetworkIdle(page);
    
    const metrics = await TestHelpers.measurePerformance(page);
    
    // Core Web Vitals thresholds (in milliseconds)
    const thresholds = {
      firstContentfulPaint: 2500,     // Good: < 1.8s, Needs Improvement: < 3s
      timeToFirstByte: 800,           // Good: < 0.8s
      domInteractive: 3000,           // Good: < 3s
      loadComplete: 5000              // Good: < 5s
    };
    
    console.log('Performance Metrics:', metrics);
    
    // Assert performance thresholds
    expect(metrics.firstContentfulPaint).toBeLessThan(thresholds.firstContentfulPaint);
    expect(metrics.timeToFirstByte).toBeLessThan(thresholds.timeToFirstByte);
    expect(metrics.domInteractive).toBeLessThan(thresholds.domInteractive);
    expect(metrics.loadComplete).toBeLessThan(thresholds.loadComplete);
  });

  test('should handle concurrent users efficiently', async ({ page, context }) => {
    const startTime = performance.now();
    
    // Simulate multiple concurrent page loads
    const promises = [];
    for (let i = 0; i < 5; i++) {
      const newPage = await context.newPage();
      promises.push(
        newPage.goto('/').then(async () => {
          await TestHelpers.waitForNetworkIdle(newPage);
          await newPage.close();
        })
      );
    }
    
    await Promise.all(promises);
    const endTime = performance.now();
    const totalTime = endTime - startTime;
    
    // All concurrent loads should complete within reasonable time
    expect(totalTime).toBeLessThan(15000); // 15 seconds max for 5 concurrent loads
  });

  test('should maintain performance during intensive drag operations', async ({ page }) => {
    await page.goto('/bento');
    await TestHelpers.waitForNetworkIdle(page);
    
    const editModeToggle = page.locator('[data-testid="edit-mode-toggle"]');
    if (await editModeToggle.isVisible()) {
      await editModeToggle.click();
    }
    
    // Measure performance during multiple drag operations
    const startTime = performance.now();
    
    const componentTiles = page.locator('[data-testid="component-tile"]');
    const dropZones = page.locator('[data-testid="drop-zone"]');
    
    if (await componentTiles.count() > 0 && await dropZones.count() > 0) {
      // Perform multiple drag operations
      for (let i = 0; i < 3; i++) {
        await TestHelpers.dragAndDrop(
          page,
          componentTiles.nth(i % await componentTiles.count()).locator('.').first(),
          dropZones.nth(i % await dropZones.count()).locator('.').first()
        );
        await page.waitForTimeout(100);
      }
    }
    
    const endTime = performance.now();
    const operationTime = endTime - startTime;
    
    // Drag operations should be responsive
    expect(operationTime).toBeLessThan(5000); // 5 seconds max for 3 operations
    
    // Check for performance degradation
    const finalMetrics = await TestHelpers.measurePerformance(page);
    expect(finalMetrics.domInteractive).toBeLessThan(3000);
  });

  test('should efficiently handle large datasets', async ({ page }) => {
    // Mock large dataset response
    await TestHelpers.mockApiResponse(page, '/api/data', {
      items: Array.from({ length: 1000 }, (_, i) => ({
        id: i,
        name: `Item ${i}`,
        description: `Description for item ${i}`,
        data: Array.from({ length: 10 }, (_, j) => `value_${j}`)
      }))
    });
    
    await page.goto('/accounts'); // Assuming this loads a large dataset
    
    const startTime = performance.now();
    await TestHelpers.waitForNetworkIdle(page);
    const endTime = performance.now();
    
    const loadTime = endTime - startTime;
    
    // Large dataset should load within reasonable time
    expect(loadTime).toBeLessThan(8000); // 8 seconds max
    
    // Check memory usage doesn't spike excessively
    const memoryUsage = await page.evaluate(() => {
      return (performance as any).memory ? {
        usedJSHeapSize: (performance as any).memory.usedJSHeapSize,
        totalJSHeapSize: (performance as any).memory.totalJSHeapSize,
        jsHeapSizeLimit: (performance as any).memory.jsHeapSizeLimit
      } : null;
    });
    
    if (memoryUsage) {
      // Memory usage should be reasonable (less than 100MB)
      expect(memoryUsage.usedJSHeapSize).toBeLessThan(100 * 1024 * 1024);
    }
  });

  test('should optimize bundle size and loading', async ({ page }) => {
    // Track network requests and bundle sizes
    const requests: Array<{ url: string; size: number; type: string }> = [];
    
    page.on('response', async response => {
      const url = response.url();
      const type = response.request().resourceType();
      
      try {
        const headers = response.headers();
        const contentLength = headers['content-length'];
        const size = contentLength ? parseInt(contentLength, 10) : 0;
        
        requests.push({ url, size, type });
      } catch (error) {
        // Ignore errors for responses we can't measure
      }
    });
    
    await page.goto('/');
    await TestHelpers.waitForNetworkIdle(page);
    
    // Analyze bundle sizes
    const jsFiles = requests.filter(req => req.type === 'script' && req.url.includes('.js'));
    const cssFiles = requests.filter(req => req.type === 'stylesheet' && req.url.includes('.css'));
    
    const totalJSSize = jsFiles.reduce((sum, file) => sum + file.size, 0);
    const totalCSSSize = cssFiles.reduce((sum, file) => sum + file.size, 0);
    
    console.log(`Total JS bundle size: ${(totalJSSize / 1024).toFixed(2)} KB`);
    console.log(`Total CSS bundle size: ${(totalCSSSize / 1024).toFixed(2)} KB`);
    
    // Bundle size thresholds
    expect(totalJSSize).toBeLessThan(2 * 1024 * 1024); // 2MB max for JS
    expect(totalCSSSize).toBeLessThan(500 * 1024);     // 500KB max for CSS
    
    // Check for efficient loading patterns
    const mainBundle = jsFiles.find(file => file.url.includes('index') || file.url.includes('main'));
    if (mainBundle) {
      expect(mainBundle.size).toBeLessThan(1024 * 1024); // 1MB max for main bundle
    }
  });

  test('should handle rapid user interactions without blocking', async ({ page }) => {
    await page.goto('/bento');
    await TestHelpers.waitForNetworkIdle(page);
    
    const editModeToggle = page.locator('[data-testid="edit-mode-toggle"]');
    if (await editModeToggle.isVisible()) {
      await editModeToggle.click();
    }
    
    // Measure responsiveness during rapid interactions
    const startTime = performance.now();
    
    // Rapid clicking/toggling
    for (let i = 0; i < 10; i++) {
      if (await editModeToggle.isVisible()) {
        await editModeToggle.click();
        await page.waitForTimeout(50); // Small delay between clicks
      }
    }
    
    const endTime = performance.now();
    const interactionTime = endTime - startTime;
    
    // Rapid interactions should not cause significant delays
    expect(interactionTime).toBeLessThan(3000); // 3 seconds max for 10 rapid interactions
    
    // UI should remain responsive
    const finalClickTime = performance.now();
    if (await editModeToggle.isVisible()) {
      await editModeToggle.click();
    }
    const responsiveClickTime = performance.now() - finalClickTime;
    
    expect(responsiveClickTime).toBeLessThan(500); // Individual click should be < 500ms
  });

  test('should efficiently handle component mounting/unmounting', async ({ page }) => {
    await page.goto('/');
    
    const sections = ['accounts', 'bento', 'schema'] as const;
    const navigationTimes: number[] = [];
    
    for (let i = 0; i < 3; i++) { // Test multiple cycles
      for (const section of sections) {
        const startTime = performance.now();
        
        await page.goto(`/${section}`);
        await TestHelpers.waitForNetworkIdle(page);
        
        const endTime = performance.now();
        navigationTimes.push(endTime - startTime);
      }
    }
    
    // Navigation times should be consistent (no memory leaks causing slowdowns)
    const averageTime = navigationTimes.reduce((sum, time) => sum + time, 0) / navigationTimes.length;
    const maxTime = Math.max(...navigationTimes);
    
    console.log(`Average navigation time: ${averageTime.toFixed(2)}ms`);
    console.log(`Maximum navigation time: ${maxTime.toFixed(2)}ms`);
    
    expect(averageTime).toBeLessThan(3000); // 3 seconds average
    expect(maxTime).toBeLessThan(5000);     // 5 seconds max
    
    // Times should not degrade significantly over cycles
    const firstCycleAvg = navigationTimes.slice(0, 3).reduce((sum, time) => sum + time, 0) / 3;
    const lastCycleAvg = navigationTimes.slice(-3).reduce((sum, time) => sum + time, 0) / 3;
    
    // Last cycle should not be more than 50% slower than first cycle
    expect(lastCycleAvg).toBeLessThan(firstCycleAvg * 1.5);
  });

  test('should minimize layout thrashing during animations', async ({ page }) => {
    await page.goto('/bento');
    await TestHelpers.waitForNetworkIdle(page);
    
    // Enable performance monitoring
    await page.evaluate(() => {
      (window as any).layoutShiftEntries = [];
      new PerformanceObserver((list) => {
        for (const entry of list.getEntries()) {
          (window as any).layoutShiftEntries.push(entry);
        }
      }).observe({ entryTypes: ['layout-shift'] });
    });
    
    const editModeToggle = page.locator('[data-testid="edit-mode-toggle"]');
    if (await editModeToggle.isVisible()) {
      await editModeToggle.click();
      await page.waitForTimeout(1000); // Wait for transitions
    }
    
    // Perform animations that might cause layout shifts
    const componentTiles = page.locator('[data-testid="component-tile"]');
    if (await componentTiles.count() > 0) {
      await componentTiles.first().hover();
      await page.waitForTimeout(500);
    }
    
    // Check for layout shifts
    const layoutShifts = await page.evaluate(() => (window as any).layoutShiftEntries);
    
    // Calculate Cumulative Layout Shift (CLS)
    const totalLayoutShift = layoutShifts.reduce((sum: number, entry: any) => sum + entry.value, 0);
    
    console.log(`Cumulative Layout Shift: ${totalLayoutShift}`);
    
    // CLS should be minimal (< 0.1 is good)
    expect(totalLayoutShift).toBeLessThan(0.25); // Allow some layout shift for dynamic content
  });
});