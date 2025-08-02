import { Page, Locator } from '@playwright/test';
import { TestHelpers } from '../utils/test-helpers';

export class AuthPage {
  readonly page: Page;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly loginButton: Locator;
  readonly errorMessage: Locator;
  readonly forgotPasswordLink: Locator;

  constructor(page: Page) {
    this.page = page;
    this.emailInput = page.locator('input[type="email"], input[name="email"]');
    this.passwordInput = page.locator('input[type="password"], input[name="password"]');
    this.loginButton = page.locator('button[type="submit"], button:has-text("Login"), button:has-text("Sign In")');
    this.errorMessage = page.locator('[data-testid="error-message"], .error, .alert-error');
    this.forgotPasswordLink = page.locator('a:has-text("Forgot"), a:has-text("Reset")');
  }

  async navigateToLogin() {
    await this.page.goto('/login');
    await TestHelpers.waitForNetworkIdle(this.page);
  }

  async login(email: string, password: string) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.loginButton.click();
    
    // Wait for navigation or error
    await Promise.race([
      this.page.waitForURL('/', { timeout: 10000 }),
      this.errorMessage.waitFor({ state: 'visible', timeout: 5000 })
    ]);
  }

  async loginWithTestUser() {
    // Use mock authentication for testing
    await this.page.evaluate(() => {
      localStorage.setItem('auth-token', 'mock-test-token');
      localStorage.setItem('user-data', JSON.stringify({
        id: 'test-user-id',
        email: 'test@example.com',
        permissions: ['admin', 'table-editor', 'schema-visualizer']
      }));
    });
    
    await this.page.goto('/');
    await this.page.waitForSelector('[data-testid="dashboard"], .dashboard, main', { timeout: 10000 });
  }

  async logout() {
    // Look for logout button or menu
    const logoutButton = this.page.locator('button:has-text("Logout"), button:has-text("Sign Out"), [data-testid="logout"]');
    
    if (await logoutButton.isVisible()) {
      await logoutButton.click();
    } else {
      // Try user menu first
      const userMenu = this.page.locator('[data-testid="user-menu"], .user-menu, .profile-menu');
      if (await userMenu.isVisible()) {
        await userMenu.click();
        await logoutButton.click();
      }
    }
    
    await this.page.waitForURL('/login', { timeout: 10000 });
  }

  async isLoggedIn(): Promise<boolean> {
    try {
      await this.page.waitForSelector('[data-testid="dashboard"], .dashboard, main', { timeout: 5000 });
      return true;
    } catch {
      return false;
    }
  }

  async getErrorMessage(): Promise<string | null> {
    if (await this.errorMessage.isVisible()) {
      return await this.errorMessage.textContent();
    }
    return null;
  }
}