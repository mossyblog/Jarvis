import { test, expect } from '@playwright/test';

test.describe('Basic Smoke Tests', () => {
  test('should load the application', async ({ page }) => {
    await page.goto('/');
    
    // Should have a title
    await expect(page).toHaveTitle(/Jarvis/);
    
    // Should have the app container
    const appContainer = page.locator('#app-container, body');
    await expect(appContainer).toBeVisible();
  });

  test('should have proper meta tags', async ({ page }) => {
    await page.goto('/');
    
    // Check for viewport meta tag
    const viewport = page.locator('meta[name="viewport"]');
    await expect(viewport).toHaveAttribute('content', /width=device-width/);
  });

  test('should respond within performance budget', async ({ page }) => {
    const startTime = Date.now();
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    const loadTime = Date.now() - startTime;
    
    // Should load within 10 seconds (generous for initial setup)
    expect(loadTime).toBeLessThan(10000);
  });
});