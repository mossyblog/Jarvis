import { Page, expect } from '@playwright/test';

export class TestHelpers {
  
  /**
   * Wait for network idle state
   */
  static async waitForNetworkIdle(page: Page, timeout = 30000) {
    await page.waitForLoadState('networkidle', { timeout });
  }

  /**
   * Wait for element to be visible and stable
   */
  static async waitForStableElement(page: Page, selector: string, timeout = 10000) {
    const element = page.locator(selector);
    await element.waitFor({ state: 'visible', timeout });
    await element.waitFor({ state: 'stable', timeout });
    return element;
  }

  /**
   * Simulate drag and drop with precise coordinates
   */
  static async dragAndDrop(
    page: Page, 
    sourceSelector: string, 
    targetSelector: string,
    options?: { sourcePosition?: { x: number; y: number }, targetPosition?: { x: number; y: number } }
  ) {
    const source = await this.waitForStableElement(page, sourceSelector);
    const target = await this.waitForStableElement(page, targetSelector);
    
    const sourceBox = await source.boundingBox();
    const targetBox = await target.boundingBox();
    
    if (!sourceBox || !targetBox) {
      throw new Error('Could not get bounding boxes for drag and drop elements');
    }
    
    const sourceX = sourceBox.x + (options?.sourcePosition?.x || sourceBox.width / 2);
    const sourceY = sourceBox.y + (options?.sourcePosition?.y || sourceBox.height / 2);
    const targetX = targetBox.x + (options?.targetPosition?.x || targetBox.width / 2);
    const targetY = targetBox.y + (options?.targetPosition?.y || targetBox.height / 2);
    
    await page.mouse.move(sourceX, sourceY);
    await page.mouse.down();
    await page.mouse.move(targetX, targetY, { steps: 10 });
    await page.mouse.up();
    
    // Wait for any animations to complete
    await page.waitForTimeout(500);
  }

  /**
   * Mock API responses for testing
   */
  static async mockApiResponse(page: Page, endpoint: string, response: any, status = 200) {
    await page.route(`**${endpoint}`, route => {
      route.fulfill({
        status,
        contentType: 'application/json',
        body: JSON.stringify(response)
      });
    });
  }

  /**
   * Simulate mobile device interactions
   */
  static async simulateTouchGesture(
    page: Page, 
    selector: string, 
    gesture: 'tap' | 'swipe-left' | 'swipe-right' | 'pinch'
  ) {
    const element = await this.waitForStableElement(page, selector);
    const box = await element.boundingBox();
    
    if (!box) throw new Error('Element not found for touch gesture');
    
    const centerX = box.x + box.width / 2;
    const centerY = box.y + box.height / 2;
    
    switch (gesture) {
      case 'tap':
        await page.touchscreen.tap(centerX, centerY);
        break;
      case 'swipe-left':
        await page.touchscreen.tap(centerX + 50, centerY);
        await page.mouse.move(centerX + 50, centerY);
        await page.mouse.down();
        await page.mouse.move(centerX - 50, centerY, { steps: 10 });
        await page.mouse.up();
        break;
      case 'swipe-right':
        await page.touchscreen.tap(centerX - 50, centerY);
        await page.mouse.move(centerX - 50, centerY);
        await page.mouse.down();
        await page.mouse.move(centerX + 50, centerY, { steps: 10 });
        await page.mouse.up();
        break;
      case 'pinch':
        // Simulate pinch gesture for zoom
        await page.touchscreen.tap(centerX - 20, centerY - 20);
        await page.touchscreen.tap(centerX + 20, centerY + 20);
        break;
    }
    
    await page.waitForTimeout(300);
  }

  /**
   * Take performance measurements
   */
  static async measurePerformance(page: Page) {
    const performanceMetrics = await page.evaluate(() => {
      const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
      const paint = performance.getEntriesByType('paint');
      
      return {
        // Navigation timing
        domContentLoaded: navigation.domContentLoadedEventEnd - navigation.domContentLoadedEventStart,
        loadComplete: navigation.loadEventEnd - navigation.loadEventStart,
        
        // Paint timing
        firstPaint: paint.find(p => p.name === 'first-paint')?.startTime || 0,
        firstContentfulPaint: paint.find(p => p.name === 'first-contentful-paint')?.startTime || 0,
        
        // Core Web Vitals (approximated)
        timeToFirstByte: navigation.responseStart - navigation.requestStart,
        domInteractive: navigation.domInteractive - navigation.navigationStart,
      };
    });
    
    return performanceMetrics;
  }

  /**
   * Check for console errors and warnings
   */
  static async checkConsoleErrors(page: Page): Promise<string[]> {
    const consoleMessages: string[] = [];
    
    page.on('console', msg => {
      if (msg.type() === 'error' || msg.type() === 'warning') {
        consoleMessages.push(`${msg.type()}: ${msg.text()}`);
      }
    });
    
    return consoleMessages;
  }

  /**
   * Wait for specific number of network requests to complete
   */
  static async waitForRequests(page: Page, count: number, timeout = 30000) {
    let requestCount = 0;
    
    return new Promise<void>((resolve, reject) => {
      const timer = setTimeout(() => {
        reject(new Error(`Timeout waiting for ${count} requests. Got ${requestCount}`));
      }, timeout);
      
      page.on('response', () => {
        requestCount++;
        if (requestCount >= count) {
          clearTimeout(timer);
          resolve();
        }
      });
    });
  }

  /**
   * Verify element is responsive across different viewport sizes
   */
  static async testResponsiveElement(page: Page, selector: string) {
    const viewports = [
      { width: 320, height: 568 },   // Mobile
      { width: 768, height: 1024 },  // Tablet
      { width: 1024, height: 768 },  // Desktop small
      { width: 1920, height: 1080 }  // Desktop large
    ];
    
    const results = [];
    
    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      await page.waitForTimeout(500); // Allow for responsive changes
      
      const element = page.locator(selector);
      const isVisible = await element.isVisible();
      const boundingBox = await element.boundingBox();
      
      results.push({
        viewport,
        isVisible,
        boundingBox
      });
    }
    
    return results;
  }

  /**
   * Generate test data for forms
   */
  static generateTestData() {
    const timestamp = Date.now();
    return {
      email: `test-${timestamp}@example.com`,
      username: `testuser${timestamp}`,
      password: 'TestPassword123!',
      text: `Test content ${timestamp}`,
      number: Math.floor(Math.random() * 1000),
      date: new Date().toISOString().split('T')[0]
    };
  }
}