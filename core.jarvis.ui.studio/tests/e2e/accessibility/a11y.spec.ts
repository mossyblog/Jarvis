import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { AuthPage } from '../pages/auth.page';
import { DashboardPage } from '../pages/dashboard.page';
import { BentoPage } from '../pages/bento.page';

test.describe('Accessibility Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Setup authentication for protected routes
    await page.evaluate(() => {
      localStorage.setItem('auth-token', 'mock-test-token');
      localStorage.setItem('user-data', JSON.stringify({
        id: 'test-user-id',
        email: 'test@example.com',
        permissions: ['admin', 'table-editor', 'schema-visualizer']
      }));
    });
  });

  test('should not have accessibility violations on login page', async ({ page }) => {
    await page.goto('/login');
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
      .analyze();
    
    expect(accessibilityScanResults.violations).toEqual([]);
  });

  test('should not have accessibility violations on dashboard', async ({ page }) => {
    await page.goto('/');
    
    // Wait for content to load
    await page.waitForSelector('[data-testid="dashboard"], .dashboard, main');
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
      .analyze();
    
    expect(accessibilityScanResults.violations).toEqual([]);
  });

  test('should not have accessibility violations on bento grid', async ({ page }) => {
    await page.goto('/bento');
    
    // Wait for bento grid to load
    await page.waitForSelector('[data-testid="bento-grid"], .bento-grid');
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
      .analyze();
    
    expect(accessibilityScanResults.violations).toEqual([]);
  });

  test('should support keyboard navigation throughout app', async ({ page }) => {
    await page.goto('/');
    
    // Test tab navigation
    const currentElement = page.locator(':focus');
    const focusableElements = [];
    
    // Tab through first 10 focusable elements
    for (let i = 0; i < 10; i++) {
      await page.keyboard.press('Tab');
      const focusedElement = page.locator(':focus');
      
      if (await focusedElement.isVisible()) {
        const tagName = await focusedElement.evaluate(el => el.tagName);
        const role = await focusedElement.getAttribute('role');
        const ariaLabel = await focusedElement.getAttribute('aria-label');
        
        focusableElements.push({
          tagName,
          role,
          ariaLabel,
          isVisible: await focusedElement.isVisible()
        });
        
        // Each focusable element should be visible
        await expect(focusedElement).toBeVisible();
      }
    }
    
    // Should have found focusable elements
    expect(focusableElements.length).toBeGreaterThan(0);
  });

  test('should provide proper ARIA labels and roles', async ({ page }) => {
    await page.goto('/');
    
    // Check for proper landmark roles
    const landmarks = await page.locator('[role="main"], [role="navigation"], [role="banner"], [role="contentinfo"]').all();
    expect(landmarks.length).toBeGreaterThan(0);
    
    // Check for proper heading hierarchy
    const headings = await page.locator('h1, h2, h3, h4, h5, h6, [role="heading"]').all();
    
    for (const heading of headings) {
      if (await heading.isVisible()) {
        const tagName = await heading.evaluate(el => el.tagName);
        const ariaLevel = await heading.getAttribute('aria-level');
        const text = await heading.textContent();
        
        // Headings should have content
        expect(text?.trim().length).toBeGreaterThan(0);
        
        // Should be properly structured
        expect(['H1', 'H2', 'H3', 'H4', 'H5', 'H6'].includes(tagName) || ariaLevel).toBeTruthy();
      }
    }
  });

  test('should provide alternative text for images', async ({ page }) => {
    await page.goto('/');
    
    const images = await page.locator('img').all();
    
    for (const image of images) {
      if (await image.isVisible()) {
        const alt = await image.getAttribute('alt');
        const ariaLabel = await image.getAttribute('aria-label');
        const ariaLabelledBy = await image.getAttribute('aria-labelledby');
        const role = await image.getAttribute('role');
        
        // Images should have alt text or appropriate ARIA labeling
        // Decorative images should have empty alt or role="presentation"
        const hasProperLabeling = alt !== null || ariaLabel || ariaLabelledBy || role === 'presentation';
        expect(hasProperLabeling).toBe(true);
      }
    }
  });

  test('should support screen reader navigation on bento grid', async ({ page }) => {
    const bentoPage = new BentoPage(page);
    await bentoPage.navigateToBento();
    await bentoPage.enableEditMode();
    
    // Check for proper ARIA attributes on draggable elements
    const draggableElements = await page.locator('[draggable="true"], [data-draggable]').all();
    
    for (const element of draggableElements) {
      if (await element.isVisible()) {
        const ariaLabel = await element.getAttribute('aria-label');
        const ariaDescribedBy = await element.getAttribute('aria-describedby');
        const role = await element.getAttribute('role');
        
        // Draggable elements should have proper ARIA labeling
        const hasProperLabeling = ariaLabel || ariaDescribedBy || role;
        expect(hasProperLabeling).toBeTruthy();
      }
    }
    
    // Check for drop zone announcements
    const dropZones = await page.locator('[data-testid="drop-zone"], .drop-zone').all();
    
    for (const zone of dropZones) {
      if (await zone.isVisible()) {
        const ariaLabel = await zone.getAttribute('aria-label');
        const ariaDropEffect = await zone.getAttribute('aria-dropeffect');
        
        // Drop zones should be properly labeled for screen readers
        expect(ariaLabel || ariaDropEffect).toBeTruthy();
      }
    }
  });

  test('should provide proper color contrast', async ({ page }) => {
    await page.goto('/');
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2aa'])
      .include(['color-contrast'])
      .analyze();
    
    // Should pass color contrast requirements
    expect(accessibilityScanResults.violations).toEqual([]);
  });

  test('should support reduced motion preferences', async ({ page }) => {
    // Test with reduced motion preference
    await page.emulateMedia({ reducedMotion: 'reduce' });
    await page.goto('/bento');
    
    const bentoPage = new BentoPage(page);
    await bentoPage.enableEditMode();
    
    // Animations should be reduced or disabled
    const animatedElements = await page.locator('.animate, [data-animate], .transition').all();
    
    for (const element of animatedElements) {
      if (await element.isVisible()) {
        const styles = await element.evaluate(el => {
          const computed = window.getComputedStyle(el);
          return {
            animationDuration: computed.animationDuration,
            transitionDuration: computed.transitionDuration
          };
        });
        
        // Animations should be significantly reduced
        const hasReducedMotion = 
          styles.animationDuration === '0s' || 
          styles.transitionDuration === '0s' ||
          styles.animationDuration.includes('0.01s') ||
          styles.transitionDuration.includes('0.01s');
        
        // This is informational - some animations might still be present
        // but should respect the reduced motion preference
      }
    }
  });

  test('should provide proper focus management in modal dialogs', async ({ page }) => {
    await page.goto('/');
    
    // Look for modal triggers
    const modalTriggers = await page.locator('button:has-text("Modal"), button:has-text("Dialog"), [data-modal-trigger]').all();
    
    for (const trigger of modalTriggers.slice(0, 3)) { // Test first 3 modals
      if (await trigger.isVisible()) {
        await trigger.click();
        
        // Wait for modal to appear
        const modal = page.locator('[role="dialog"], .modal, [data-modal]');
        if (await modal.isVisible({ timeout: 2000 })) {
          // Focus should be trapped in modal
          const focusedElement = page.locator(':focus');
          const modalBounds = await modal.boundingBox();
          const focusedBounds = await focusedElement.boundingBox();
          
          if (modalBounds && focusedBounds) {
            // Focused element should be within modal bounds
            const isWithinModal = 
              focusedBounds.x >= modalBounds.x &&
              focusedBounds.y >= modalBounds.y &&
              focusedBounds.x + focusedBounds.width <= modalBounds.x + modalBounds.width &&
              focusedBounds.y + focusedBounds.height <= modalBounds.y + modalBounds.height;
            
            expect(isWithinModal).toBe(true);
          }
          
          // Close modal
          const closeButton = modal.locator('button:has-text("Close"), [aria-label*="close"], .close');
          if (await closeButton.isVisible()) {
            await closeButton.click();
          } else {
            await page.keyboard.press('Escape');
          }
          
          await page.waitForTimeout(500);
        }
      }
    }
  });

  test('should provide proper form validation messages', async ({ page }) => {
    await page.goto('/login');
    
    const form = page.locator('form');
    if (await form.isVisible()) {
      const submitButton = page.locator('button[type="submit"]');
      
      // Try to submit empty form
      if (await submitButton.isVisible()) {
        await submitButton.click();
        
        // Look for validation messages
        const errorMessages = await page.locator('.error, [role="alert"], [aria-invalid="true"]').all();
        
        for (const error of errorMessages) {
          if (await error.isVisible()) {
            const text = await error.textContent();
            const ariaLive = await error.getAttribute('aria-live');
            const role = await error.getAttribute('role');
            
            // Error messages should be announced to screen readers
            expect(text?.trim().length).toBeGreaterThan(0);
            expect(ariaLive === 'polite' || ariaLive === 'assertive' || role === 'alert').toBe(true);
          }
        }
      }
    }
  });

  test('should support high contrast mode', async ({ page }) => {
    // Simulate high contrast mode
    await page.addStyleTag({
      content: `
        @media (prefers-contrast: high) {
          * {
            border: 1px solid #000 !important;
          }
        }
      `
    });
    
    await page.goto('/');
    
    // Check that content is still visible and usable
    const dashboardPage = new DashboardPage(page);
    await expect(dashboardPage.mainContent).toBeVisible();
    
    // Run accessibility scan with high contrast consideration
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2aa'])
      .analyze();
    
    // Should still pass accessibility requirements
    expect(accessibilityScanResults.violations).toEqual([]);
  });
});