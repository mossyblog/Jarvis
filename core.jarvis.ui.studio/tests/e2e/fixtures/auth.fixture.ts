import { test as base, expect } from '@playwright/test';
import { AuthPage } from '../pages/auth.page';

// Extend basic test by providing authentication fixtures
export const test = base.extend<{
  authPage: AuthPage;
  authenticatedContext: any;
}>({
  authPage: async ({ page }, use) => {
    const authPage = new AuthPage(page);
    await use(authPage);
  },

  authenticatedContext: async ({ browser }, use) => {
    // Create a new browser context with authentication
    const context = await browser.newContext();
    const page = await context.newPage();
    
    // Perform login
    await page.goto('/login');
    
    // Mock successful authentication for testing
    await page.evaluate(() => {
      localStorage.setItem('auth-token', 'mock-test-token');
      localStorage.setItem('user-data', JSON.stringify({
        id: 'test-user-id',
        email: 'test@example.com',
        permissions: ['admin', 'table-editor', 'schema-visualizer']
      }));
    });
    
    await page.goto('/');
    
    // Wait for dashboard to load
    await page.waitForSelector('[data-testid="dashboard"]', { timeout: 10000 });
    
    await use(context);
    await context.close();
  }
});

export { expect };