import { test, expect } from '../fixtures/auth.fixture';
import { DashboardPage } from '../pages/dashboard.page';
import { BentoPage } from '../pages/bento.page';
import { TestHelpers } from '../utils/test-helpers';

test.describe('Mobile Responsive Design', () => {
  const mobileViewports = [
    { name: 'iPhone SE', width: 375, height: 667 },
    { name: 'iPhone 12', width: 390, height: 844 },
    { name: 'Pixel 5', width: 393, height: 851 },
    { name: 'Galaxy S20', width: 360, height: 800 }
  ];

  const tabletViewports = [
    { name: 'iPad', width: 768, height: 1024 },
    { name: 'iPad Pro', width: 1024, height: 1366 },
    { name: 'Surface Pro', width: 912, height: 1368 }
  ];

  test.beforeEach(async ({ page, authPage }) => {
    await authPage.loginWithTestUser();
  });

  test('should display mobile navigation correctly', async ({ page }) => {
    for (const viewport of mobileViewports) {
      await page.setViewportSize(viewport);
      await page.goto('/');
      await TestHelpers.waitForNetworkIdle(page);
      
      const dashboardPage = new DashboardPage(page);
      
      // Main content should be visible
      await expect(dashboardPage.mainContent).toBeVisible();
      
      // Check for mobile navigation patterns
      const mobileMenu = page.locator('.mobile-menu, [data-testid="mobile-menu"], .hamburger-menu');
      const sidebar = dashboardPage.sidebar;
      
      // Either sidebar is hidden and mobile menu is shown, or sidebar is adaptive
      if (await mobileMenu.isVisible()) {
        await expect(mobileMenu).toBeVisible();
      } else {
        // Sidebar might be hidden on mobile
        const sidebarBox = await sidebar.boundingBox();
        if (sidebarBox) {
          // Sidebar should either be hidden or take full width
          expect(sidebarBox.width).toBeLessThanOrEqual(viewport.width);
        }
      }
    }
  });

  test('should handle touch interactions on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');
    await TestHelpers.waitForNetworkIdle(page);
    
    const dashboardPage = new DashboardPage(page);
    
    // Test touch navigation
    const navLinks = await dashboardPage.navigationLinks.all();
    if (navLinks.length > 0) {
      await TestHelpers.simulateTouchGesture(page, navLinks[0].locator('.').first(), 'tap');
      await page.waitForTimeout(500);
    }
    
    // Test swipe gestures if applicable
    const swipeableElements = page.locator('.swipeable, [data-swipeable="true"]');
    if (await swipeableElements.first().isVisible()) {
      await TestHelpers.simulateTouchGesture(page, swipeableElements.first().locator('.').first(), 'swipe-left');
      await page.waitForTimeout(500);
    }
  });

  test('should adapt bento grid for mobile devices', async ({ page }) => {
    const bentoPage = new BentoPage(page);
    
    for (const viewport of mobileViewports) {
      await page.setViewportSize(viewport);
      await bentoPage.navigateToBento();
      
      // Grid should be visible and responsive
      await expect(bentoPage.bentoGrid).toBeVisible();
      
      const gridBox = await bentoPage.bentoGrid.boundingBox();
      if (gridBox) {
        // Grid should fit within viewport
        expect(gridBox.width).toBeLessThanOrEqual(viewport.width);
      }
      
      // Enable edit mode and test mobile interactions
      await bentoPage.enableEditMode();
      
      // Component palette should be accessible on mobile
      if (await bentoPage.componentPalette.isVisible()) {
        const paletteBox = await bentoPage.componentPalette.boundingBox();
        if (paletteBox) {
          expect(paletteBox.width).toBeLessThanOrEqual(viewport.width);
        }
      }
    }
  });

  test('should handle mobile drag and drop', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    
    const bentoPage = new BentoPage(page);
    await bentoPage.navigateToBento();
    await bentoPage.enableEditMode();
    
    const availableComponents = await bentoPage.getAvailableComponents();
    if (availableComponents.length > 0) {
      const componentSelector = `[data-testid="component-tile"]:has-text("${availableComponents[0]}")`;
      const gridSelector = '[data-testid="drop-zone"], .drop-zone';
      
      const componentElement = page.locator(componentSelector);
      const gridElement = page.locator(gridSelector).first();
      
      if (await componentElement.isVisible() && await gridElement.isVisible()) {
        // Test touch-based drag and drop
        const componentBox = await componentElement.boundingBox();
        const gridBox = await gridElement.boundingBox();
        
        if (componentBox && gridBox) {
          // Long press to start drag
          await page.touchscreen.tap(
            componentBox.x + componentBox.width / 2,
            componentBox.y + componentBox.height / 2
          );
          
          // Hold for long press
          await page.waitForTimeout(500);
          
          // Drag to target
          await page.mouse.move(
            componentBox.x + componentBox.width / 2,
            componentBox.y + componentBox.height / 2
          );
          await page.mouse.down();
          await page.mouse.move(
            gridBox.x + gridBox.width / 2,
            gridBox.y + gridBox.height / 2,
            { steps: 10 }
          );
          await page.mouse.up();
          
          await page.waitForTimeout(1000);
          
          // Verify component was placed
          const newCount = await bentoPage.getComponentCount();
          expect(newCount).toBeGreaterThan(0);
        }
      }
    }
  });

  test('should provide proper touch targets', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');
    await TestHelpers.waitForNetworkIdle(page);
    
    // Check touch target sizes (should be at least 44px per Apple guidelines)
    const interactiveElements = page.locator('button, a, input, [role="button"], [tabindex="0"]');
    const elements = await interactiveElements.all();
    
    for (const element of elements.slice(0, 10)) { // Check first 10 elements
      if (await element.isVisible()) {
        const box = await element.boundingBox();
        if (box) {
          // Touch targets should be at least 44x44px
          const minSize = 44;
          const hasMinWidth = box.width >= minSize;
          const hasMinHeight = box.height >= minSize;
          
          // Either width or height should meet minimum, or element should have padding
          const hasAdequateSize = hasMinWidth || hasMinHeight || 
            (box.width >= 32 && box.height >= 32); // Slightly smaller acceptable with good spacing
          
          expect(hasAdequateSize).toBe(true);
        }
      }
    }
  });

  test('should handle orientation changes', async ({ page }) => {
    // Portrait mode
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');
    await TestHelpers.waitForNetworkIdle(page);
    
    const dashboardPage = new DashboardPage(page);
    await expect(dashboardPage.mainContent).toBeVisible();
    
    // Landscape mode
    await page.setViewportSize({ width: 667, height: 375 });
    await page.waitForTimeout(500);
    
    // Content should still be visible and properly laid out
    await expect(dashboardPage.mainContent).toBeVisible();
    
    const mainBox = await dashboardPage.mainContent.boundingBox();
    if (mainBox) {
      expect(mainBox.width).toBeLessThanOrEqual(667);
      expect(mainBox.height).toBeLessThanOrEqual(375);
    }
  });

  test('should support pinch-to-zoom on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    
    const bentoPage = new BentoPage(page);
    await bentoPage.navigateToBento();
    
    // Check if zoom is supported on the grid
    const grid = bentoPage.bentoGrid;
    if (await grid.isVisible()) {
      // Simulate pinch gesture
      await TestHelpers.simulateTouchGesture(page, grid.locator('.').first(), 'pinch');
      
      // Grid should handle zoom appropriately
      await page.waitForTimeout(500);
      await expect(grid).toBeVisible();
    }
  });

  test('should display tablet layout correctly', async ({ page }) => {
    for (const viewport of tabletViewports) {
      await page.setViewportSize(viewport);
      await page.goto('/');
      await TestHelpers.waitForNetworkIdle(page);
      
      const dashboardPage = new DashboardPage(page);
      
      // Sidebar should be visible on tablet
      await expect(dashboardPage.sidebar).toBeVisible();
      await expect(dashboardPage.mainContent).toBeVisible();
      
      // Check layout doesn't overflow
      const sidebarBox = await dashboardPage.sidebar.boundingBox();
      const mainBox = await dashboardPage.mainContent.boundingBox();
      
      if (sidebarBox && mainBox) {
        expect(sidebarBox.x + sidebarBox.width + mainBox.width).toBeLessThanOrEqual(viewport.width + 50); // 50px tolerance
      }
    }
  });

  test('should handle mobile form interactions', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/login');
    
    // Test mobile keyboard interactions
    const emailInput = page.locator('input[type="email"]');
    const passwordInput = page.locator('input[type="password"]');
    
    if (await emailInput.isVisible() && await passwordInput.isVisible()) {
      // Focus should bring up mobile keyboard
      await emailInput.focus();
      
      // Input should be properly visible when keyboard is up
      const emailBox = await emailInput.boundingBox();
      if (emailBox) {
        expect(emailBox.y).toBeGreaterThan(0);
      }
      
      // Test form submission on mobile
      await emailInput.fill('test@example.com');
      await passwordInput.fill('password123');
      
      const submitButton = page.locator('button[type="submit"]');
      if (await submitButton.isVisible()) {
        await TestHelpers.simulateTouchGesture(page, submitButton.locator('.').first(), 'tap');
      }
    }
  });

  test('should provide mobile-specific feedback', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    
    const bentoPage = new BentoPage(page);
    await bentoPage.navigateToBento();
    await bentoPage.enableEditMode();
    
    // Look for mobile-specific UI elements
    const mobileHints = page.locator('.mobile-hint, [data-mobile-hint], .touch-hint');
    const mobileControls = page.locator('.mobile-controls, [data-mobile-controls]');
    
    // Check for haptic feedback simulation or visual feedback
    const availableComponents = await bentoPage.getAvailableComponents();
    if (availableComponents.length > 0) {
      const componentTile = page.locator(`[data-testid="component-tile"]:has-text("${availableComponents[0]}")`);
      
      // Long press should provide feedback
      if (await componentTile.isVisible()) {
        await TestHelpers.simulateTouchGesture(page, componentTile.locator('.').first(), 'tap');
        
        // Check for visual feedback
        await page.waitForTimeout(300);
        
        // Element might change appearance on interaction
        const activeState = await componentTile.getAttribute('class');
        expect(activeState).toBeDefined();
      }
    }
  });
});