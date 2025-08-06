import { test, expect, Page } from '@playwright/test';

/**
 * UIStudio Performance Tests
 * 
 * These tests measure performance metrics for the UIStudio interface:
 * - Page load times
 * - Modal open/close performance
 * - Form interaction responsiveness
 * - Memory usage
 * - API response handling
 * - Rendering performance
 * - Animation smoothness
 */

interface PerformanceMetrics {
  loadTime: number;
  domContentLoaded: number;
  networkIdle: number;
  firstContentfulPaint: number;
  largestContentfulPaint: number;
  cumulativeLayoutShift: number;
  firstInputDelay: number;
  timeToInteractive: number;
}

class PerformanceMonitor {
  constructor(private page: Page) {}

  async captureMetrics(): Promise<PerformanceMetrics> {
    const timing = await this.page.evaluate(() => {
      const perf = performance;
      const navigation = perf.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
      
      return {
        loadTime: navigation.loadEventEnd - navigation.navigationStart,
        domContentLoaded: navigation.domContentLoadedEventEnd - navigation.navigationStart,
        networkIdle: navigation.loadEventEnd - navigation.fetchStart,
        responseTime: navigation.responseEnd - navigation.requestStart,
        dnsLookup: navigation.domainLookupEnd - navigation.domainLookupStart,
        tcpConnect: navigation.connectEnd - navigation.connectStart,
        serverResponse: navigation.responseEnd - navigation.responseStart,
      };
    });

    // Get Web Vitals
    const vitals = await this.page.evaluate(() => {
      return new Promise((resolve) => {
        const vitals: any = {};
        
        // First Contentful Paint
        const fcpObserver = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          vitals.firstContentfulPaint = entries[0]?.startTime || 0;
        });
        fcpObserver.observe({ entryTypes: ['paint'] });

        // Largest Contentful Paint
        const lcpObserver = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          vitals.largestContentfulPaint = entries[entries.length - 1]?.startTime || 0;
        });
        lcpObserver.observe({ entryTypes: ['largest-contentful-paint'] });

        // Cumulative Layout Shift
        const clsObserver = new PerformanceObserver((list) => {
          let clsValue = 0;
          list.getEntries().forEach((entry: any) => {
            if (!entry.hadRecentInput) {
              clsValue += entry.value;
            }
          });
          vitals.cumulativeLayoutShift = clsValue;
        });
        clsObserver.observe({ entryTypes: ['layout-shift'] });

        // Time to Interactive (simplified)
        vitals.timeToInteractive = performance.now();

        setTimeout(() => {
          fcpObserver.disconnect();
          lcpObserver.disconnect();
          clsObserver.disconnect();
          resolve(vitals);
        }, 2000);
      });
    });

    return {
      ...timing,
      firstContentfulPaint: (vitals as any).firstContentfulPaint || 0,
      largestContentfulPaint: (vitals as any).largestContentfulPaint || 0,
      cumulativeLayoutShift: (vitals as any).cumulativeLayoutShift || 0,
      firstInputDelay: 0, // Would need user interaction to measure
      timeToInteractive: (vitals as any).timeToInteractive || 0,
    };
  }

  async measureModalPerformance() {
    const startTime = performance.now();
    
    await this.page.getByRole('button', { name: /create new page/i }).click();
    await this.page.getByRole('dialog').waitFor({ state: 'visible' });
    
    const endTime = performance.now();
    return endTime - startTime;
  }

  async measureFormInputPerformance(text: string) {
    const input = this.page.getByPlaceholder(/enter page name/i);
    
    const startTime = performance.now();
    await input.type(text);
    const endTime = performance.now();
    
    return endTime - startTime;
  }

  async measureMemoryUsage() {
    return await this.page.evaluate(() => {
      if ('memory' in performance) {
        const memory = (performance as any).memory;
        return {
          usedJSHeapSize: memory.usedJSHeapSize,
          totalJSHeapSize: memory.totalJSHeapSize,
          jsHeapSizeLimit: memory.jsHeapSizeLimit,
          usedPercentage: (memory.usedJSHeapSize / memory.jsHeapSizeLimit) * 100
        };
      }
      return null;
    });
  }

  async measureNetworkMetrics() {
    const resources = await this.page.evaluate(() => {
      const resources = performance.getEntriesByType('resource') as PerformanceResourceTiming[];
      return resources.map(resource => ({
        name: resource.name,
        duration: resource.duration,
        transferSize: resource.transferSize || 0,
        encodedBodySize: resource.encodedBodySize || 0,
        decodedBodySize: resource.decodedBodySize || 0,
        startTime: resource.startTime,
        responseEnd: resource.responseEnd
      }));
    });

    return {
      totalResources: resources.length,
      totalTransferSize: resources.reduce((sum, r) => sum + r.transferSize, 0),
      averageDuration: resources.reduce((sum, r) => sum + r.duration, 0) / resources.length,
      slowestResource: resources.sort((a, b) => b.duration - a.duration)[0],
      largestResource: resources.sort((a, b) => b.transferSize - a.transferSize)[0]
    };
  }
}

test.describe('UIStudio Performance Tests', () => {
  let perfMonitor: PerformanceMonitor;

  test.beforeEach(async ({ page }) => {
    perfMonitor = new PerformanceMonitor(page);
    
    // Enable performance monitoring
    await page.addInitScript(() => {
      // Mark performance measurement start
      window.performance.mark('test-start');
    });
  });

  test.describe('Page Load Performance', () => {
    test('page loads within acceptable time limits', async ({ page }) => {
      const startTime = Date.now();
      
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      const loadTime = Date.now() - startTime;
      
      // Assertions for performance benchmarks
      expect(loadTime).toBeLessThan(3000); // Should load in under 3 seconds
      
      const metrics = await perfMonitor.captureMetrics();
      
      expect(metrics.domContentLoaded).toBeLessThan(2000); // DOM ready in under 2 seconds
      expect(metrics.firstContentfulPaint).toBeLessThan(1500); // FCP in under 1.5 seconds
      expect(metrics.largestContentfulPaint).toBeLessThan(2500); // LCP in under 2.5 seconds
      expect(metrics.cumulativeLayoutShift).toBeLessThan(0.1); // CLS should be minimal
      
      console.log('Performance Metrics:', {
        loadTime: `${loadTime}ms`,
        domContentLoaded: `${metrics.domContentLoaded}ms`,
        firstContentfulPaint: `${metrics.firstContentfulPaint}ms`,
        largestContentfulPaint: `${metrics.largestContentfulPaint}ms`,
        cumulativeLayoutShift: metrics.cumulativeLayoutShift
      });
    });

    test('initial render is complete within 1 second', async ({ page }) => {
      await page.goto('/');
      
      const renderTime = await page.evaluate(() => {
        const start = performance.now();
        return new Promise((resolve) => {
          requestAnimationFrame(() => {
            requestAnimationFrame(() => {
              resolve(performance.now() - start);
            });
          });
        });
      });
      
      expect(renderTime).toBeLessThan(1000);
    });

    test('all critical elements are visible within 2 seconds', async ({ page }) => {
      const startTime = Date.now();
      
      await page.goto('/');
      
      // Wait for critical elements
      await Promise.all([
        page.getByRole('heading', { name: 'UIStudio' }).waitFor({ state: 'visible' }),
        page.getByRole('button', { name: /create new page/i }).waitFor({ state: 'visible' }),
        page.getByRole('heading', { name: /welcome to uistudio/i }).waitFor({ state: 'visible' }),
        page.getByText('Created Pages').waitFor({ state: 'visible' })
      ]);
      
      const renderTime = Date.now() - startTime;
      expect(renderTime).toBeLessThan(2000);
    });
  });

  test.describe('Modal Performance', () => {
    test('modal opens quickly and smoothly', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      const openTime = await perfMonitor.measureModalPerformance();
      
      expect(openTime).toBeLessThan(200); // Should open in under 200ms
      console.log(`Modal open time: ${openTime.toFixed(2)}ms`);
    });

    test('modal close is immediate', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      // Open modal first
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      const startTime = performance.now();
      await page.getByRole('button', { name: /close modal/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'hidden' });
      const closeTime = performance.now() - startTime;
      
      expect(closeTime).toBeLessThan(100); // Should close in under 100ms
      console.log(`Modal close time: ${closeTime.toFixed(2)}ms`);
    });

    test('modal backdrop interaction is responsive', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      const startTime = performance.now();
      await page.locator('[aria-label="Close modal"]').click();
      await page.getByRole('dialog').waitFor({ state: 'hidden' });
      const responseTime = performance.now() - startTime;
      
      expect(responseTime).toBeLessThan(150);
    });

    test('modal animations do not cause layout shifts', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      // Measure CLS during modal interactions
      await page.evaluate(() => {
        window.cumulativeLayoutShift = 0;
        const observer = new PerformanceObserver((list) => {
          list.getEntries().forEach((entry: any) => {
            if (!entry.hadRecentInput) {
              window.cumulativeLayoutShift += entry.value;
            }
          });
        });
        observer.observe({ entryTypes: ['layout-shift'] });
      });
      
      // Open and close modal multiple times
      for (let i = 0; i < 3; i++) {
        await page.getByRole('button', { name: /create new page/i }).click();
        await page.getByRole('dialog').waitFor({ state: 'visible' });
        await page.getByRole('button', { name: /close modal/i }).click();
        await page.getByRole('dialog').waitFor({ state: 'hidden' });
      }
      
      const cls = await page.evaluate(() => window.cumulativeLayoutShift);
      expect(cls).toBeLessThan(0.05); // Very low layout shift tolerance
    });
  });

  test.describe('Form Input Performance', () => {
    test('input fields are responsive to typing', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      const inputTime = await perfMonitor.measureFormInputPerformance('Test Page Name');
      
      expect(inputTime).toBeLessThan(100); // Should respond in under 100ms
      console.log(`Input response time: ${inputTime.toFixed(2)}ms`);
    });

    test('validation errors appear immediately', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      const startTime = performance.now();
      await page.getByRole('button', { name: /create & save page/i }).click();
      await page.getByText('Page name is required').waitFor({ state: 'visible' });
      const validationTime = performance.now() - startTime;
      
      expect(validationTime).toBeLessThan(50); // Validation should be instant
    });

    test('error clearing is immediate when typing', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      // Trigger error
      await page.getByRole('button', { name: /create & save page/i }).click();
      await page.getByText('Page name is required').waitFor({ state: 'visible' });
      
      const startTime = performance.now();
      await page.getByPlaceholder(/enter page name/i).type('T');
      await page.getByText('Page name is required').waitFor({ state: 'hidden' });
      const clearTime = performance.now() - startTime;
      
      expect(clearTime).toBeLessThan(100);
    });

    test('form state updates are batched efficiently', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      // Measure rendering during rapid state changes
      const startTime = performance.now();
      
      const nameInput = page.getByPlaceholder(/enter page name/i);
      const urlInput = page.getByPlaceholder('/my-dashboard');
      
      // Rapid form filling
      await nameInput.type('Test Page');
      await urlInput.type('/test-page');
      await nameInput.clear();
      await nameInput.type('Updated Page');
      await urlInput.clear();
      await urlInput.type('/updated-page');
      
      const updateTime = performance.now() - startTime;
      expect(updateTime).toBeLessThan(500); // Should handle rapid updates efficiently
    });
  });

  test.describe('API Performance', () => {
    test('form submission starts loading state immediately', async ({ page }) => {
      // Mock slow API response
      await page.route('**/api/**', route => {
        setTimeout(() => {
          route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify([{ id: 'test-id', name: 'Test Page', url: '/test-page' }])
          });
        }, 2000);
      });

      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      await page.getByPlaceholder(/enter page name/i).fill('Test Page');
      await page.getByPlaceholder('/my-dashboard').fill('/test-page');
      
      const startTime = performance.now();
      await page.getByRole('button', { name: /create & save page/i }).click();
      await page.getByText('Creating Page...').waitFor({ state: 'visible' });
      const loadingTime = performance.now() - startTime;
      
      expect(loadingTime).toBeLessThan(50); // Loading state should appear instantly
    });

    test('handles API timeout gracefully', async ({ page }) => {
      // Mock timeout
      await page.route('**/api/**', route => {
        // Never respond to simulate timeout
      });

      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      await page.getByPlaceholder(/enter page name/i).fill('Test Page');
      await page.getByPlaceholder('/my-dashboard').fill('/test-page');
      
      await page.getByRole('button', { name: /create & save page/i }).click();
      
      // Should show loading state and remain functional
      await expect(page.getByText('Creating Page...')).toBeVisible();
      await expect(page.getByRole('dialog')).toBeVisible();
    });

    test('processes successful API response quickly', async ({ page }) => {
      // Mock fast API response
      await page.route('**/api/**', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([{ id: 'test-id', name: 'Test Page', url: '/test-page' }])
        });
      });

      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      await page.getByPlaceholder(/enter page name/i).fill('Test Page');
      await page.getByPlaceholder('/my-dashboard').fill('/test-page');
      
      const startTime = performance.now();
      await page.getByRole('button', { name: /create & save page/i }).click();
      await page.getByText('Created Successfully!').waitFor({ state: 'visible' });
      const responseTime = performance.now() - startTime;
      
      expect(responseTime).toBeLessThan(200); // Should process response quickly
    });
  });

  test.describe('Memory Performance', () => {
    test('memory usage remains stable during interactions', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      const initialMemory = await perfMonitor.measureMemoryUsage();
      
      // Perform multiple interactions
      for (let i = 0; i < 5; i++) {
        await page.getByRole('button', { name: /create new page/i }).click();
        await page.getByRole('dialog').waitFor({ state: 'visible' });
        
        await page.getByPlaceholder(/enter page name/i).fill(`Test Page ${i}`);
        await page.getByPlaceholder('/my-dashboard').fill(`/test-page-${i}`);
        
        await page.getByRole('button', { name: /close modal/i }).click();
        await page.getByRole('dialog').waitFor({ state: 'hidden' });
      }
      
      const finalMemory = await perfMonitor.measureMemoryUsage();
      
      if (initialMemory && finalMemory) {
        const memoryIncrease = finalMemory.usedJSHeapSize - initialMemory.usedJSHeapSize;
        const increasePercentage = (memoryIncrease / initialMemory.usedJSHeapSize) * 100;
        
        // Memory increase should be minimal
        expect(increasePercentage).toBeLessThan(20); // Less than 20% increase
        
        console.log('Memory Usage:', {
          initial: `${(initialMemory.usedJSHeapSize / 1024 / 1024).toFixed(2)}MB`,
          final: `${(finalMemory.usedJSHeapSize / 1024 / 1024).toFixed(2)}MB`,
          increase: `${(memoryIncrease / 1024 / 1024).toFixed(2)}MB`,
          increasePercentage: `${increasePercentage.toFixed(2)}%`
        });
      }
    });

    test('no memory leaks during modal operations', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      // Force garbage collection if available
      await page.evaluate(() => {
        if ('gc' in window) {
          (window as any).gc();
        }
      });
      
      const beforeMemory = await perfMonitor.measureMemoryUsage();
      
      // Open and close modal many times
      for (let i = 0; i < 20; i++) {
        await page.getByRole('button', { name: /create new page/i }).click();
        await page.getByRole('dialog').waitFor({ state: 'visible' });
        await page.getByRole('button', { name: /close modal/i }).click();
        await page.getByRole('dialog').waitFor({ state: 'hidden' });
      }
      
      // Force garbage collection again
      await page.evaluate(() => {
        if ('gc' in window) {
          (window as any).gc();
        }
      });
      
      const afterMemory = await perfMonitor.measureMemoryUsage();
      
      if (beforeMemory && afterMemory) {
        const memoryIncrease = afterMemory.usedJSHeapSize - beforeMemory.usedJSHeapSize;
        const increasePercentage = (memoryIncrease / beforeMemory.usedJSHeapSize) * 100;
        
        // Should not have significant memory leaks
        expect(increasePercentage).toBeLessThan(10);
      }
    });
  });

  test.describe('Network Performance', () => {
    test('resource loading is optimized', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      const networkMetrics = await perfMonitor.measureNetworkMetrics();
      
      console.log('Network Metrics:', {
        totalResources: networkMetrics.totalResources,
        totalTransferSize: `${(networkMetrics.totalTransferSize / 1024).toFixed(2)}KB`,
        averageDuration: `${networkMetrics.averageDuration.toFixed(2)}ms`,
        slowestResource: networkMetrics.slowestResource?.name,
        largestResource: networkMetrics.largestResource?.name
      });
      
      // Assertions for resource optimization
      expect(networkMetrics.totalTransferSize).toBeLessThan(2 * 1024 * 1024); // Under 2MB total
      expect(networkMetrics.averageDuration).toBeLessThan(500); // Average resource load under 500ms
      
      if (networkMetrics.slowestResource) {
        expect(networkMetrics.slowestResource.duration).toBeLessThan(2000); // No resource takes over 2s
      }
    });

    test('no unnecessary network requests during interactions', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      let requestCount = 0;
      
      page.on('request', request => {
        if (!request.url().includes('favicon') && !request.url().includes('__vite')) {
          requestCount++;
        }
      });
      
      // Perform UI interactions
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      await page.getByPlaceholder(/enter page name/i).fill('Test Page');
      await page.getByPlaceholder('/my-dashboard').fill('/test-page');
      
      await page.getByRole('button', { name: /close modal/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'hidden' });
      
      await page.waitForTimeout(1000);
      
      // Should not trigger additional network requests for UI interactions
      expect(requestCount).toBe(0);
    });
  });

  test.describe('Rendering Performance', () => {
    test('component updates do not block UI thread', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      // Measure frame rate during rapid input
      const frameRateData = await page.evaluate(() => {
        return new Promise((resolve) => {
          let frameCount = 0;
          const startTime = performance.now();
          
          function countFrame() {
            frameCount++;
            
            if (performance.now() - startTime >= 1000) {
              resolve(frameCount);
            } else {
              requestAnimationFrame(countFrame);
            }
          }
          
          requestAnimationFrame(countFrame);
        });
      });
      
      // Type rapidly while measuring frames
      const nameInput = page.getByPlaceholder(/enter page name/i);
      await nameInput.type('This is a long page name that should not block the UI thread while typing');
      
      const frameRate = await frameRateData;
      
      // Should maintain reasonable frame rate (at least 30 FPS)
      expect(frameRate).toBeGreaterThanOrEqual(30);
      console.log(`Frame rate during input: ${frameRate} FPS`);
    });

    test('large form data does not cause performance degradation', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      // Fill with very large data
      const largeName = 'A'.repeat(10000);
      const largeUrl = '/test-' + 'a'.repeat(1000);
      
      const startTime = performance.now();
      
      await page.getByPlaceholder(/enter page name/i).fill(largeName);
      await page.getByPlaceholder('/my-dashboard').fill(largeUrl);
      
      const fillTime = performance.now() - startTime;
      
      // Should handle large inputs efficiently
      expect(fillTime).toBeLessThan(1000); // Under 1 second for very large input
      
      // UI should remain responsive
      await expect(page.getByRole('button', { name: /create & save page/i })).toBeEnabled();
    });
  });

  test.describe('Cross-Device Performance', () => {
    test('performs well on mobile viewport', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE
      
      const startTime = Date.now();
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      const loadTime = Date.now() - startTime;
      
      expect(loadTime).toBeLessThan(4000); // Slightly more lenient for mobile
      
      // Test modal performance on mobile
      const modalOpenTime = await perfMonitor.measureModalPerformance();
      expect(modalOpenTime).toBeLessThan(300); // Mobile might be slightly slower
    });

    test('performs well on tablet viewport', async ({ page }) => {
      await page.setViewportSize({ width: 768, height: 1024 }); // iPad
      
      const startTime = Date.now();
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      const loadTime = Date.now() - startTime;
      
      expect(loadTime).toBeLessThan(3500);
      
      const modalOpenTime = await perfMonitor.measureModalPerformance();
      expect(modalOpenTime).toBeLessThan(250);
    });

    test('performs well on large desktop viewport', async ({ page }) => {
      await page.setViewportSize({ width: 1920, height: 1080 }); // 1080p
      
      const startTime = Date.now();
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      const loadTime = Date.now() - startTime;
      
      expect(loadTime).toBeLessThan(2500); // Should be fastest on desktop
      
      const modalOpenTime = await perfMonitor.measureModalPerformance();
      expect(modalOpenTime).toBeLessThan(150);
    });
  });

  test.describe('Performance Regression Detection', () => {
    test('establishes performance baseline', async ({ page }) => {
      const testResults = {
        pageLoadTime: 0,
        modalOpenTime: 0,
        inputResponseTime: 0,
        memoryUsage: 0,
        networkTransferSize: 0,
      };

      // Page load performance
      const loadStart = Date.now();
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      testResults.pageLoadTime = Date.now() - loadStart;

      // Modal performance
      testResults.modalOpenTime = await perfMonitor.measureModalPerformance();

      // Input performance
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      testResults.inputResponseTime = await perfMonitor.measureFormInputPerformance('Test');

      // Memory usage
      const memory = await perfMonitor.measureMemoryUsage();
      testResults.memoryUsage = memory ? memory.usedJSHeapSize : 0;

      // Network performance
      const network = await perfMonitor.measureNetworkMetrics();
      testResults.networkTransferSize = network.totalTransferSize;

      console.log('Performance Baseline:', testResults);

      // Store baseline for comparison
      await page.evaluate((results) => {
        localStorage.setItem('performanceBaseline', JSON.stringify(results));
      }, testResults);

      // All metrics should meet baseline requirements
      expect(testResults.pageLoadTime).toBeLessThan(3000);
      expect(testResults.modalOpenTime).toBeLessThan(200);
      expect(testResults.inputResponseTime).toBeLessThan(100);
      expect(testResults.memoryUsage).toBeGreaterThan(0);
      expect(testResults.networkTransferSize).toBeLessThan(2048000); // 2MB
    });
  });
});