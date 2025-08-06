import { Page, Locator } from '@playwright/test';
import { TestHelpers } from '../utils/test-helpers';

export class DashboardPage {
  readonly page: Page;
  readonly sidebar: Locator;
  readonly mainContent: Locator;
  readonly networkMonitor: Locator;
  readonly editModeToggle: Locator;
  readonly navigationLinks: Locator;
  readonly userMenu: Locator;
  readonly apiStatusBanner: Locator;

  constructor(page: Page) {
    this.page = page;
    this.sidebar = page.locator('[data-testid="sidebar"], .sidebar, nav');
    this.mainContent = page.locator('[data-testid="main-content"], main, .main-content');
    this.networkMonitor = page.locator('[data-testid="network-monitor"], .network-monitor');
    this.editModeToggle = page.locator('[data-testid="edit-mode-toggle"], .edit-mode-toggle');
    this.navigationLinks = page.locator('nav a, .sidebar a, [data-testid="nav-link"]');
    this.userMenu = page.locator('[data-testid="user-menu"], .user-menu');
    this.apiStatusBanner = page.locator('[data-testid="api-status-banner"], .api-status-banner');
  }

  async navigateToDashboard() {
    await this.page.goto('/');
    await TestHelpers.waitForNetworkIdle(this.page);
    await this.page.waitForSelector('[data-testid="dashboard"], .dashboard, main');
  }

  async navigateToSection(section: 'accounts' | 'editor' | 'schema' | 'roles' | 'bento' | 'settings') {
    const linkText = {
      accounts: 'User Management',
      editor: 'Table Editor', 
      schema: 'Schema',
      roles: 'Roles',
      bento: 'Bento',
      settings: 'Settings'
    };
    
    const link = this.page.locator(`a:has-text("${linkText[section]}"), [href="/${section}"]`);
    await link.click();
    await TestHelpers.waitForNetworkIdle(this.page);
  }

  async toggleEditMode() {
    if (await this.editModeToggle.isVisible()) {
      await this.editModeToggle.click();
      await this.page.waitForTimeout(500); // Allow for mode transition
    }
  }

  async isEditModeEnabled(): Promise<boolean> {
    // Check for edit mode indicators in the UI
    const editModeIndicators = [
      '[data-testid="edit-mode-active"]',
      '.edit-mode-active',
      '[data-edit-mode="true"]'
    ];
    
    for (const selector of editModeIndicators) {
      if (await this.page.locator(selector).isVisible()) {
        return true;
      }
    }
    
    return false;
  }

  async getNetworkStatus(): Promise<'online' | 'offline' | 'slow'> {
    if (await this.networkMonitor.isVisible()) {
      const statusText = await this.networkMonitor.textContent();
      if (statusText?.toLowerCase().includes('offline')) return 'offline';
      if (statusText?.toLowerCase().includes('slow')) return 'slow';
    }
    return 'online';
  }

  async getApiStatus(): Promise<'connected' | 'disconnected' | 'error'> {
    if (await this.apiStatusBanner.isVisible()) {
      const statusText = await this.apiStatusBanner.textContent();
      if (statusText?.toLowerCase().includes('disconnected')) return 'disconnected';
      if (statusText?.toLowerCase().includes('error')) return 'error';
    }
    return 'connected';
  }

  async waitForPageLoad() {
    await TestHelpers.waitForNetworkIdle(this.page);
    await this.page.waitForSelector('[data-testid="dashboard"], .dashboard, main');
  }

  async checkSidebarNavigation() {
    const links = await this.navigationLinks.all();
    const results = [];
    
    for (const link of links) {
      const href = await link.getAttribute('href');
      const text = await link.textContent();
      const isVisible = await link.isVisible();
      
      results.push({
        href,
        text: text?.trim(),
        isVisible
      });
    }
    
    return results;
  }

  async openUserMenu() {
    if (await this.userMenu.isVisible()) {
      await this.userMenu.click();
      await this.page.waitForTimeout(300);
    }
  }
}