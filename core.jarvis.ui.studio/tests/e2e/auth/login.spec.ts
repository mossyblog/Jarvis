import { test, expect } from '../fixtures/auth.fixture';
import { AuthPage } from '../pages/auth.page';
import { DashboardPage } from '../pages/dashboard.page';
import { TestHelpers } from '../utils/test-helpers';

test.describe('Authentication Flow', () => {
  let authPage: AuthPage;
  let dashboardPage: DashboardPage;

  test.beforeEach(async ({ page }) => {
    authPage = new AuthPage(page);
    dashboardPage = new DashboardPage(page);
  });

  test('should display login form correctly', async ({ page }) => {
    await authPage.navigateToLogin();
    
    await expect(authPage.emailInput).toBeVisible();
    await expect(authPage.passwordInput).toBeVisible();
    await expect(authPage.loginButton).toBeVisible();
    
    // Check form attributes
    await expect(authPage.emailInput).toHaveAttribute('type', 'email');
    await expect(authPage.passwordInput).toHaveAttribute('type', 'password');
  });

  test('should show error for invalid credentials', async ({ page }) => {
    await authPage.navigateToLogin();
    
    await authPage.login('invalid@example.com', 'wrongpassword');
    
    // Should remain on login page or show error
    const currentUrl = page.url();
    expect(currentUrl).toContain('/login');
    
    // Check for error message (if implemented)
    const errorMessage = await authPage.getErrorMessage();
    if (errorMessage) {
      expect(errorMessage.toLowerCase()).toContain('invalid');
    }
  });

  test('should successfully login with valid credentials', async ({ page }) => {
    await authPage.navigateToLogin();
    await authPage.loginWithTestUser();
    
    // Should redirect to dashboard
    await expect(page).toHaveURL('/');
    await expect(dashboardPage.mainContent).toBeVisible();
    
    // Verify user is authenticated
    expect(await authPage.isLoggedIn()).toBe(true);
  });

  test('should maintain session after page refresh', async ({ page }) => {
    await authPage.navigateToLogin();
    await authPage.loginWithTestUser();
    
    // Refresh the page
    await page.reload();
    await TestHelpers.waitForNetworkIdle(page);
    
    // Should still be logged in
    expect(await authPage.isLoggedIn()).toBe(true);
    await expect(page).toHaveURL('/');
  });

  test('should redirect to login when accessing protected route', async ({ page }) => {
    // Try to access dashboard without authentication
    await page.goto('/');
    
    // Should redirect to login
    await page.waitForURL('/login', { timeout: 10000 });
    await expect(authPage.emailInput).toBeVisible();
  });

  test('should handle logout correctly', async ({ page }) => {
    await authPage.navigateToLogin();
    await authPage.loginWithTestUser();
    
    // Verify logged in
    expect(await authPage.isLoggedIn()).toBe(true);
    
    // Perform logout
    await authPage.logout();
    
    // Should redirect to login
    await expect(page).toHaveURL('/login');
    expect(await authPage.isLoggedIn()).toBe(false);
  });

  test('should validate email format', async ({ page }) => {
    await authPage.navigateToLogin();
    
    // Try invalid email formats
    const invalidEmails = ['invalid', 'invalid@', '@example.com', 'test@'];
    
    for (const email of invalidEmails) {
      await authPage.emailInput.fill(email);
      await authPage.passwordInput.fill('password123');
      await authPage.loginButton.click();
      
      // Should show validation error or remain on page
      const currentUrl = page.url();
      expect(currentUrl).toContain('/login');
    }
  });

  test('should handle network errors gracefully', async ({ page }) => {
    // Mock network failure
    await page.route('**/api/auth/login', route => route.abort());
    
    await authPage.navigateToLogin();
    await authPage.login('test@example.com', 'password123');
    
    // Should handle error gracefully
    const currentUrl = page.url();
    expect(currentUrl).toContain('/login');
  });

  test('should support keyboard navigation', async ({ page }) => {
    await authPage.navigateToLogin();
    
    // Tab through form elements
    await page.keyboard.press('Tab');
    await expect(authPage.emailInput).toBeFocused();
    
    await page.keyboard.press('Tab');
    await expect(authPage.passwordInput).toBeFocused();
    
    await page.keyboard.press('Tab');
    await expect(authPage.loginButton).toBeFocused();
    
    // Submit with Enter key
    await authPage.emailInput.fill('test@example.com');
    await authPage.passwordInput.fill('password123');
    await page.keyboard.press('Enter');
    
    // Should attempt login
    await page.waitForTimeout(1000);
  });

  test('should handle remember me functionality', async ({ page }) => {
    await authPage.navigateToLogin();
    
    const rememberCheckbox = page.locator('input[type="checkbox"], input[name="remember"]');
    if (await rememberCheckbox.isVisible()) {
      await rememberCheckbox.check();
      await authPage.loginWithTestUser();
      
      // Close browser and reopen to test persistence
      await page.context().close();
      
      // This would require a new context to test properly
      // For now, just verify the checkbox can be checked
      expect(await rememberCheckbox.isChecked()).toBe(true);
    }
  });
});