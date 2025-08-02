import { test, expect } from '../fixtures/auth.fixture';
import { TestHelpers } from '../utils/test-helpers';

test.describe('Visual Regression Tests', () => {
  test.beforeEach(async ({ page, authPage }) => {
    await authPage.loginWithTestUser();
  });

  test('should match dashboard screenshot', async ({ page }) => {
    await page.goto('/');
    await TestHelpers.waitForNetworkIdle(page);
    
    // Wait for all content to stabilize
    await page.waitForTimeout(1000);
    
    // Take full page screenshot
    await expect(page).toHaveScreenshot('dashboard-full-page.png', {
      fullPage: true,
      animations: 'disabled'
    });
    
    // Take screenshot of main content area only
    const mainContent = page.locator('[data-testid="main-content"], main');
    if (await mainContent.isVisible()) {
      await expect(mainContent).toHaveScreenshot('dashboard-main-content.png');
    }
  });

  test('should match sidebar navigation screenshots', async ({ page }) => {
    await page.goto('/');
    await TestHelpers.waitForNetworkIdle(page);
    
    const sidebar = page.locator('[data-testid="sidebar"], .sidebar, nav');
    if (await sidebar.isVisible()) {
      await expect(sidebar).toHaveScreenshot('sidebar-navigation.png');
    }
  });

  test('should match bento grid screenshots in different states', async ({ page }) => {
    await page.goto('/bento');
    await TestHelpers.waitForNetworkIdle(page);
    
    // View mode screenshot
    const bentoGrid = page.locator('[data-testid="bento-grid"], .bento-grid');
    await expect(bentoGrid).toHaveScreenshot('bento-grid-view-mode.png');
    
    // Edit mode screenshot
    const editModeToggle = page.locator('[data-testid="edit-mode-toggle"]');
    if (await editModeToggle.isVisible()) {
      await editModeToggle.click();
      await page.waitForTimeout(500);
      
      await expect(bentoGrid).toHaveScreenshot('bento-grid-edit-mode.png');
      
      // Component palette screenshot
      const componentPalette = page.locator('[data-testid="component-palette"], .component-palette');
      if (await componentPalette.isVisible()) {
        await expect(componentPalette).toHaveScreenshot('component-palette.png');
      }
    }
  });

  test('should match login form screenshot', async ({ page }) => {
    await page.goto('/login');
    await TestHelpers.waitForNetworkIdle(page);
    
    await expect(page).toHaveScreenshot('login-form.png');
    
    // Test error state if possible
    const emailInput = page.locator('input[type="email"]');
    const passwordInput = page.locator('input[type="password"]');
    const submitButton = page.locator('button[type="submit"]');
    
    if (await emailInput.isVisible() && await passwordInput.isVisible() && await submitButton.isVisible()) {
      await emailInput.fill('invalid-email');
      await passwordInput.fill('wrong-password');
      await submitButton.click();
      
      await page.waitForTimeout(1000);
      await expect(page).toHaveScreenshot('login-form-error-state.png');
    }
  });

  test('should match component screenshots across different themes', async ({ page }) => {
    await page.goto('/');
    await TestHelpers.waitForNetworkIdle(page);
    
    // Test different theme modes if available
    const themeToggle = page.locator('[data-testid="theme-toggle"], .theme-toggle');
    
    // Default theme
    await expect(page).toHaveScreenshot('app-default-theme.png', {
      fullPage: true,
      animations: 'disabled'
    });
    
    // Try to toggle theme
    if (await themeToggle.isVisible()) {
      await themeToggle.click();
      await page.waitForTimeout(500);
      
      await expect(page).toHaveScreenshot('app-toggled-theme.png', {
        fullPage: true,
        animations: 'disabled'
      });
    }
  });

  test('should match mobile responsive screenshots', async ({ page }) => {
    const mobileViewports = [
      { name: 'mobile-portrait', width: 375, height: 667 },
      { name: 'mobile-landscape', width: 667, height: 375 },
      { name: 'tablet', width: 768, height: 1024 }
    ];
    
    for (const viewport of mobileViewports) {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.goto('/');
      await TestHelpers.waitForNetworkIdle(page);
      await page.waitForTimeout(500);
      
      await expect(page).toHaveScreenshot(`dashboard-${viewport.name}.png`, {
        fullPage: true,
        animations: 'disabled'
      });
    }
  });

  test('should match component states during interactions', async ({ page }) => {
    await page.goto('/bento');
    await TestHelpers.waitForNetworkIdle(page);
    
    const editModeToggle = page.locator('[data-testid="edit-mode-toggle"]');
    if (await editModeToggle.isVisible()) {
      await editModeToggle.click();
      await page.waitForTimeout(500);
    }
    
    // Test hover states
    const componentTiles = page.locator('[data-testid="component-tile"]');
    if (await componentTiles.count() > 0) {
      const firstTile = componentTiles.first();
      
      // Normal state
      await expect(firstTile).toHaveScreenshot('component-tile-normal.png');
      
      // Hover state
      await firstTile.hover();
      await page.waitForTimeout(200);
      await expect(firstTile).toHaveScreenshot('component-tile-hover.png');
    }
    
    // Test focus states
    const focusableButtons = page.locator('button:visible');
    if (await focusableButtons.count() > 0) {
      const firstButton = focusableButtons.first();
      await firstButton.focus();
      await expect(firstButton).toHaveScreenshot('button-focused.png');
    }
  });

  test('should match grid layout variations', async ({ page }) => {
    await page.goto('/bento');
    await TestHelpers.waitForNetworkIdle(page);
    
    const editModeToggle = page.locator('[data-testid="edit-mode-toggle"]');
    if (await editModeToggle.isVisible()) {
      await editModeToggle.click();
      await page.waitForTimeout(500);
    }
    
    // Test empty grid
    const bentoGrid = page.locator('[data-testid="bento-grid"]');
    await expect(bentoGrid).toHaveScreenshot('bento-grid-empty.png');
    
    // Add components and test different layouts
    const componentTiles = page.locator('[data-testid="component-tile"]');
    const dropZones = page.locator('[data-testid="drop-zone"]');
    
    if (await componentTiles.count() > 0 && await dropZones.count() > 0) {
      // Add first component
      await TestHelpers.dragAndDrop(
        page,
        componentTiles.first().locator('.').first(),
        dropZones.first().locator('.').first()
      );
      await page.waitForTimeout(500);
      
      await expect(bentoGrid).toHaveScreenshot('bento-grid-single-component.png');
      
      // Add second component if available
      if (await componentTiles.count() > 1 && await dropZones.count() > 1) {
        await TestHelpers.dragAndDrop(
          page,
          componentTiles.nth(1).locator('.').first(),
          dropZones.nth(1).locator('.').first()
        );
        await page.waitForTimeout(500);
        
        await expect(bentoGrid).toHaveScreenshot('bento-grid-multiple-components.png');
      }
    }
  });

  test('should match form validation states', async ({ page }) => {
    await page.goto('/login');
    
    const form = page.locator('form');
    const emailInput = page.locator('input[type="email"]');
    const passwordInput = page.locator('input[type="password"]');
    const submitButton = page.locator('button[type="submit"]');
    
    if (await form.isVisible()) {
      // Empty form state
      await expect(form).toHaveScreenshot('form-empty.png');
      
      // Filled form state
      await emailInput.fill('test@example.com');
      await passwordInput.fill('password123');
      await expect(form).toHaveScreenshot('form-filled.png');
      
      // Try invalid email
      await emailInput.fill('invalid-email');
      await submitButton.click();
      await page.waitForTimeout(500);
      await expect(form).toHaveScreenshot('form-validation-error.png');
    }
  });

  test('should match loading states', async ({ page }) => {
    // Mock slow API response to capture loading state
    await page.route('**/api/**', route => {
      setTimeout(() => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ success: true })
        });
      }, 2000);
    });
    
    await page.goto('/accounts');
    
    // Capture loading state
    const loadingIndicators = page.locator('.loading, .spinner, [data-testid="loading"]');
    if (await loadingIndicators.first().isVisible({ timeout: 1000 })) {
      await expect(page).toHaveScreenshot('page-loading-state.png');
    }
    
    // Wait for content to load
    await TestHelpers.waitForNetworkIdle(page);
    await expect(page).toHaveScreenshot('page-loaded-state.png');
  });

  test('should match modal and dialog screenshots', async ({ page }) => {
    await page.goto('/');
    await TestHelpers.waitForNetworkIdle(page);
    
    // Look for modal triggers
    const modalTriggers = page.locator('button:has-text("Modal"), button:has-text("Dialog"), [data-modal-trigger]');
    
    if (await modalTriggers.count() > 0) {
      await modalTriggers.first().click();
      
      const modal = page.locator('[role="dialog"], .modal');
      if (await modal.isVisible({ timeout: 2000 })) {
        await expect(modal).toHaveScreenshot('modal-dialog.png');
        
        // Test modal with backdrop
        await expect(page).toHaveScreenshot('modal-with-backdrop.png');
      }
    }
  });

  test('should match error state screenshots', async ({ page }) => {
    // Mock 404 error
    await page.route('**/api/**', route => {
      route.fulfill({
        status: 404,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Not found' })
      });
    });
    
    await page.goto('/nonexistent-page');
    await page.waitForTimeout(1000);
    
    // Should show 404 page
    await expect(page).toHaveScreenshot('404-error-page.png');
    
    // Test network error state
    await page.route('**/api/**', route => route.abort());
    await page.goto('/accounts');
    await page.waitForTimeout(2000);
    
    // Should show error state
    await expect(page).toHaveScreenshot('network-error-state.png');
  });
});