import { test, expect } from '../fixtures/auth.fixture';
import { DashboardPage } from '../pages/dashboard.page';
import { TestHelpers } from '../utils/test-helpers';

test.describe('Dashboard Navigation', () => {
  let dashboardPage: DashboardPage;

  test.beforeEach(async ({ page, authPage }) => {
    dashboardPage = new DashboardPage(page);
    await authPage.loginWithTestUser();
    await dashboardPage.navigateToDashboard();
  });

  test('should display main dashboard elements', async ({ page }) => {
    await expect(dashboardPage.sidebar).toBeVisible();
    await expect(dashboardPage.mainContent).toBeVisible();
    
    // Check for basic dashboard content
    const pageTitle = page.locator('h1, .page-title');
    if (await pageTitle.isVisible()) {
      await expect(pageTitle).toContainText(/dashboard|home/i);
    }
  });

  test('should navigate to all main sections', async ({ page }) => {
    const sections = ['accounts', 'editor', 'schema', 'bento'] as const;
    
    for (const section of sections) {
      await dashboardPage.navigateToSection(section);
      
      // Verify URL change
      await expect(page).toHaveURL(`/${section}`);
      
      // Wait for page to load
      await TestHelpers.waitForNetworkIdle(page);
      
      // Verify page content loads
      const mainContent = page.locator('main, .main-content, [data-testid="main-content"]');
      await expect(mainContent).toBeVisible();
    }
  });

  test('should display network monitor', async ({ page }) => {
    if (await dashboardPage.networkMonitor.isVisible()) {
      await expect(dashboardPage.networkMonitor).toBeVisible();
      
      const status = await dashboardPage.getNetworkStatus();
      expect(['online', 'offline', 'slow']).toContain(status);
    }
  });

  test('should handle edit mode toggle', async ({ page }) => {
    if (await dashboardPage.editModeToggle.isVisible()) {
      const initialState = await dashboardPage.isEditModeEnabled();
      
      await dashboardPage.toggleEditMode();
      
      const newState = await dashboardPage.isEditModeEnabled();
      expect(newState).toBe(!initialState);
      
      // Toggle back
      await dashboardPage.toggleEditMode();
      const finalState = await dashboardPage.isEditModeEnabled();
      expect(finalState).toBe(initialState);
    }
  });

  test('should display sidebar navigation correctly', async ({ page }) => {
    const navItems = await dashboardPage.checkSidebarNavigation();
    
    // Should have at least some navigation items
    expect(navItems.length).toBeGreaterThan(0);
    
    // Check that navigation items have proper structure
    for (const item of navItems) {
      expect(item.href).toBeDefined();
      expect(item.text).toBeDefined();
      expect(item.isVisible).toBe(true);
    }
  });

  test('should handle API status correctly', async ({ page }) => {
    const apiStatus = await dashboardPage.getApiStatus();
    expect(['connected', 'disconnected', 'error']).toContain(apiStatus);
    
    // If there's an API status banner, it should be informative
    if (await dashboardPage.apiStatusBanner.isVisible()) {
      const bannerText = await dashboardPage.apiStatusBanner.textContent();
      expect(bannerText).toBeDefined();
      expect(bannerText!.length).toBeGreaterThan(0);
    }
  });

  test('should be responsive on different screen sizes', async ({ page }) => {
    const viewports = [
      { width: 320, height: 568 },   // Mobile
      { width: 768, height: 1024 },  // Tablet
      { width: 1920, height: 1080 }  // Desktop
    ];
    
    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      await page.waitForTimeout(500);
      
      // Main elements should still be visible
      await expect(dashboardPage.mainContent).toBeVisible();
      
      // Sidebar might be hidden on mobile
      if (viewport.width >= 768) {
        await expect(dashboardPage.sidebar).toBeVisible();
      }
    }
  });

  test('should handle keyboard navigation', async ({ page }) => {
    // Test tab navigation through main elements
    await page.keyboard.press('Tab');
    
    // Should be able to navigate to interactive elements
    const focusedElement = page.locator(':focus');
    await expect(focusedElement).toBeVisible();
    
    // Test navigation with arrow keys if applicable
    const navLinks = dashboardPage.navigationLinks;
    if (await navLinks.first().isVisible()) {
      await navLinks.first().focus();
      await page.keyboard.press('ArrowDown');
      // Should move focus to next navigation item
    }
  });

  test('should persist navigation state', async ({ page }) => {
    // Navigate to a specific section
    await dashboardPage.navigateToSection('bento');
    await expect(page).toHaveURL('/bento');
    
    // Refresh the page
    await page.reload();
    await TestHelpers.waitForNetworkIdle(page);
    
    // Should remain on the same page
    await expect(page).toHaveURL('/bento');
  });

  test('should handle deep linking correctly', async ({ page }) => {
    // Direct navigation to a deep route
    await page.goto('/accounts');
    await TestHelpers.waitForNetworkIdle(page);
    
    // Should load the correct page
    await expect(page).toHaveURL('/accounts');
    
    // Main navigation should still be functional
    await expect(dashboardPage.sidebar).toBeVisible();
    await expect(dashboardPage.mainContent).toBeVisible();
  });

  test('should display loading states appropriately', async ({ page }) => {
    // Navigate to a page and check for loading indicators
    await dashboardPage.navigateToSection('schema');
    
    // Look for loading spinners or skeleton screens
    const loadingIndicators = page.locator('.loading, .spinner, .skeleton, [data-testid="loading"]');
    
    // If loading indicators exist, they should disappear after content loads
    if (await loadingIndicators.first().isVisible()) {
      await expect(loadingIndicators.first()).not.toBeVisible({ timeout: 10000 });
    }
    
    // Content should be visible after loading
    await expect(dashboardPage.mainContent).toBeVisible();
  });
});