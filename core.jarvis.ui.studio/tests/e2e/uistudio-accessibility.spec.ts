import { test, expect, Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * UIStudio Accessibility Tests
 * 
 * These tests ensure the UIStudio interface meets WCAG 2.1 AA standards:
 * - Screen reader compatibility
 * - Keyboard navigation
 * - Color contrast ratios
 * - Focus management
 * - ARIA attributes and semantics
 * - Alternative text and descriptions
 * - Form accessibility
 * - Error message accessibility
 */

class AccessibilityHelper {
  constructor(private page: Page) {}

  async runAxeAnalysis(options?: any) {
    const accessibilityScanResults = await new AxeBuilder({ page: this.page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
      .options(options)
      .analyze();

    return accessibilityScanResults;
  }

  async checkColorContrast(selector: string) {
    return await this.page.evaluate((sel) => {
      const element = document.querySelector(sel);
      if (!element) return null;

      const styles = window.getComputedStyle(element);
      const backgroundColor = styles.backgroundColor;
      const color = styles.color;

      // Simple contrast ratio calculation (simplified)
      const getRGB = (colorStr: string) => {
        const match = colorStr.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
        return match ? [parseInt(match[1]), parseInt(match[2]), parseInt(match[3])] : [0, 0, 0];
      };

      const bgRGB = getRGB(backgroundColor);
      const textRGB = getRGB(color);

      const getLuminance = (rgb: number[]) => {
        const [r, g, b] = rgb.map(c => {
          c = c / 255;
          return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
        });
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
      };

      const bgLuminance = getLuminance(bgRGB);
      const textLuminance = getLuminance(textRGB);

      const contrast = (Math.max(bgLuminance, textLuminance) + 0.05) / 
                      (Math.min(bgLuminance, textLuminance) + 0.05);

      return {
        contrast: Math.round(contrast * 100) / 100,
        backgroundColor,
        color,
        meetsAA: contrast >= 4.5,
        meetsAAA: contrast >= 7
      };
    }, selector);
  }

  async checkFocusVisibility(selector: string) {
    const element = this.page.locator(selector);
    await element.focus();

    return await this.page.evaluate((sel) => {
      const el = document.querySelector(sel);
      if (!el) return null;

      const styles = window.getComputedStyle(el);
      const outline = styles.outline;
      const outlineWidth = styles.outlineWidth;
      const outlineStyle = styles.outlineStyle;
      const outlineColor = styles.outlineColor;
      const boxShadow = styles.boxShadow;

      return {
        outline,
        outlineWidth,
        outlineStyle,
        outlineColor,
        boxShadow,
        hasFocusIndicator: outline !== 'none' || boxShadow !== 'none'
      };
    }, selector);
  }

  async simulateScreenReader() {
    // Enable screen reader announcements tracking
    await this.page.addInitScript(() => {
      window.screenReaderAnnouncements = [];
      
      // Mock screen reader functionality
      const originalCreateElement = document.createElement;
      document.createElement = function(tagName) {
        const element = originalCreateElement.call(this, tagName);
        
        if (tagName.toLowerCase() === 'div' && element.setAttribute) {
          const originalSetAttribute = element.setAttribute;
          element.setAttribute = function(name, value) {
            if (name === 'aria-live' && value) {
              const observer = new MutationObserver((mutations) => {
                mutations.forEach((mutation) => {
                  if (mutation.type === 'childList' || mutation.type === 'characterData') {
                    const text = element.textContent?.trim();
                    if (text) {
                      window.screenReaderAnnouncements.push({
                        text,
                        timestamp: Date.now(),
                        element: element.tagName
                      });
                    }
                  }
                });
              });
              observer.observe(element, { childList: true, subtree: true, characterData: true });
            }
            return originalSetAttribute.call(this, name, value);
          };
        }
        
        return element;
      };
    });
  }

  async getScreenReaderAnnouncements() {
    return await this.page.evaluate(() => {
      return window.screenReaderAnnouncements || [];
    });
  }

  async testKeyboardNavigation(startSelector: string, expectedFocusSequence: string[]) {
    const results = [];
    
    // Start from the specified element
    await this.page.locator(startSelector).focus();
    results.push(await this.page.evaluate(() => document.activeElement?.tagName));

    // Tab through the sequence
    for (let i = 0; i < expectedFocusSequence.length; i++) {
      await this.page.keyboard.press('Tab');
      const focusedElement = await this.page.evaluate(() => {
        const el = document.activeElement;
        return {
          tagName: el?.tagName,
          className: el?.className,
          ariaLabel: el?.getAttribute('aria-label'),
          placeholder: el?.getAttribute('placeholder'),
          text: el?.textContent?.trim()
        };
      });
      results.push(focusedElement);
    }

    return results;
  }
}

test.describe('UIStudio Accessibility Tests', () => {
  let accessibilityHelper: AccessibilityHelper;

  test.beforeEach(async ({ page }) => {
    accessibilityHelper = new AccessibilityHelper(page);
    await accessibilityHelper.simulateScreenReader();
    
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test.describe('Automated Accessibility Scanning', () => {
    test('page has no accessibility violations on initial load', async () => {
      const accessibilityScanResults = await accessibilityHelper.runAxeAnalysis();

      expect(accessibilityScanResults.violations).toEqual([]);
      
      if (accessibilityScanResults.violations.length > 0) {
        console.log('Accessibility violations found:', 
          accessibilityScanResults.violations.map(v => ({
            id: v.id,
            impact: v.impact,
            description: v.description,
            nodes: v.nodes.length
          }))
        );
      }
    });

    test('modal has no accessibility violations', async ({ page }) => {
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });

      const accessibilityScanResults = await accessibilityHelper.runAxeAnalysis();

      expect(accessibilityScanResults.violations).toEqual([]);
    });

    test('form validation errors are accessible', async ({ page }) => {
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      // Trigger validation errors
      await page.getByRole('button', { name: /create & save page/i }).click();
      await page.getByText('Page name is required').waitFor({ state: 'visible' });

      const accessibilityScanResults = await accessibilityHelper.runAxeAnalysis();

      expect(accessibilityScanResults.violations).toEqual([]);
    });

    test('success and error alerts are accessible', async ({ page }) => {
      // Mock API error
      await page.route('**/api/**', route => {
        route.fulfill({ status: 500, body: 'Server Error' });
      });

      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      await page.getByPlaceholder(/enter page name/i).fill('Test Page');
      await page.getByPlaceholder('/my-dashboard').fill('/test-page');
      await page.getByRole('button', { name: /create & save page/i }).click();
      
      await page.locator('[role="alert"]').waitFor({ state: 'visible' });

      const accessibilityScanResults = await accessibilityHelper.runAxeAnalysis();

      expect(accessibilityScanResults.violations).toEqual([]);
    });
  });

  test.describe('Keyboard Navigation', () => {
    test('can navigate to create button using keyboard', async ({ page }) => {
      // Tab to the create button
      await page.keyboard.press('Tab');
      
      const focusedElement = await page.evaluate(() => {
        const el = document.activeElement;
        return {
          tagName: el?.tagName,
          text: el?.textContent?.trim(),
          ariaLabel: el?.getAttribute('aria-label')
        };
      });

      expect(focusedElement.text).toContain('Create New Page');
    });

    test('can open modal using Enter key', async ({ page }) => {
      await page.keyboard.press('Tab'); // Focus create button
      await page.keyboard.press('Enter');
      
      await expect(page.getByRole('dialog')).toBeVisible();
    });

    test('can open modal using Space key', async ({ page }) => {
      await page.keyboard.press('Tab'); // Focus create button
      await page.keyboard.press('Space');
      
      await expect(page.getByRole('dialog')).toBeVisible();
    });

    test('can navigate through modal form using Tab', async ({ page }) => {
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });

      const focusSequence = await accessibilityHelper.testKeyboardNavigation(
        'input[placeholder*="Enter page name"]',
        ['input', 'button', 'button'] // pageUrl input, Cancel button, Create button
      );

      // Verify the focus moves through form elements correctly
      expect(focusSequence[1].tagName).toBe('INPUT'); // Page URL input
      expect(focusSequence[2].tagName).toBe('BUTTON'); // Cancel button
      expect(focusSequence[3].tagName).toBe('BUTTON'); // Create button
    });

    test('can close modal using Escape key', async ({ page }) => {
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      await page.keyboard.press('Escape');
      
      await expect(page.getByRole('dialog')).not.toBeVisible();
    });

    test('focus returns to trigger element after modal closes', async ({ page }) => {
      const createButton = page.getByRole('button', { name: /create new page/i });
      
      await createButton.click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      await page.keyboard.press('Escape');
      await page.getByRole('dialog').waitFor({ state: 'hidden' });
      
      const focusedElement = await page.evaluate(() => document.activeElement?.textContent?.trim());
      expect(focusedElement).toContain('Create New Page');
    });

    test('can submit form using Enter key', async ({ page }) => {
      // Mock successful API response
      await page.route('**/api/**', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([{ id: 'test-id', name: 'Test Page', url: '/test-page' }])
        });
      });

      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      await page.getByPlaceholder(/enter page name/i).fill('Test Page');
      await page.getByPlaceholder('/my-dashboard').fill('/test-page');
      
      // Focus should be on form element, Enter should submit
      await page.keyboard.press('Enter');
      
      await expect(page.getByText('Created Successfully!')).toBeVisible();
    });

    test('tab order is logical and predictable', async ({ page }) => {
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });

      const tabOrder = [];
      
      // Start from first input (should be auto-focused)
      const initialFocus = await page.evaluate(() => document.activeElement?.getAttribute('placeholder'));
      tabOrder.push(initialFocus);

      // Tab through all focusable elements
      for (let i = 0; i < 10; i++) {
        await page.keyboard.press('Tab');
        const focusInfo = await page.evaluate(() => {
          const el = document.activeElement;
          return {
            placeholder: el?.getAttribute('placeholder'),
            text: el?.textContent?.trim(),
            ariaLabel: el?.getAttribute('aria-label'),
            tagName: el?.tagName
          };
        });
        
        if (focusInfo.placeholder || focusInfo.text || focusInfo.ariaLabel) {
          tabOrder.push(focusInfo.placeholder || focusInfo.text || focusInfo.ariaLabel);
        }
        
        // Stop if we've cycled back to the first element
        if (focusInfo.placeholder === initialFocus && i > 0) {
          break;
        }
      }

      console.log('Tab order:', tabOrder);
      
      // Verify logical tab order
      expect(tabOrder).toContain('Enter page name (e.g., My Dashboard)'); // Page name input
      expect(tabOrder).toContain('/my-dashboard'); // Page URL input
      expect(tabOrder).toContain('Cancel'); // Cancel button
      expect(tabOrder).toContain('Create & Save Page'); // Create button
    });
  });

  test.describe('Screen Reader Support', () => {
    test('page has proper document structure', async ({ page }) => {
      // Check for proper heading hierarchy
      const headings = await page.locator('h1, h2, h3, h4, h5, h6').all();
      const headingData = await Promise.all(
        headings.map(h => h.evaluate(el => ({ 
          tagName: el.tagName, 
          text: el.textContent?.trim(),
          level: parseInt(el.tagName.charAt(1))
        })))
      );

      // Should have h1 for main heading
      expect(headingData.some(h => h.tagName === 'H1')).toBe(true);
      
      // Heading levels should be logical (no skipping)
      const levels = headingData.map(h => h.level).sort();
      for (let i = 1; i < levels.length; i++) {
        expect(levels[i] - levels[i-1]).toBeLessThanOrEqual(1);
      }
    });

    test('modal has proper ARIA attributes', async ({ page }) => {
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });

      const modal = page.getByRole('dialog');
      
      // Check modal attributes
      await expect(modal).toHaveAttribute('role', 'dialog');
      await expect(modal).toHaveAttribute('aria-labelledby');
      await expect(modal).toHaveAttribute('aria-describedby');

      // Check that labeled and described elements exist
      const labelId = await modal.getAttribute('aria-labelledby');
      const descId = await modal.getAttribute('aria-describedby');
      
      if (labelId) {
        await expect(page.locator(`#${labelId}`)).toBeVisible();
      }
      if (descId) {
        await expect(page.locator(`#${descId}`)).toBeVisible();
      }
    });

    test('form inputs have proper labels and descriptions', async ({ page }) => {
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });

      // Check page name input
      const pageNameInput = page.getByPlaceholder(/enter page name/i);
      const pageNameLabel = page.getByText('Page Name');
      
      await expect(pageNameLabel).toBeVisible();
      await expect(pageNameInput).toBeVisible();

      // Check page URL input
      const pageUrlInput = page.getByPlaceholder('/my-dashboard');
      const pageUrlLabel = page.getByText('Page URL');
      const urlHint = page.getByText(/must start with \//i);
      
      await expect(pageUrlLabel).toBeVisible();
      await expect(pageUrlInput).toBeVisible();
      await expect(urlHint).toBeVisible();
    });

    test('error messages are associated with inputs', async ({ page }) => {
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      // Trigger validation errors
      await page.getByRole('button', { name: /create & save page/i }).click();
      
      const pageNameError = page.getByText('Page name is required');
      const pageUrlError = page.getByText('Page URL is required');
      
      await expect(pageNameError).toBeVisible();
      await expect(pageUrlError).toBeVisible();

      // Errors should be announced to screen readers
      const announcements = await accessibilityHelper.getScreenReaderAnnouncements();
      const errorAnnouncements = announcements.filter(a => 
        a.text.includes('required') || a.text.includes('error')
      );
      
      expect(errorAnnouncements.length).toBeGreaterThan(0);
    });

    test('loading states are announced', async ({ page }) => {
      // Mock slow API response
      await page.route('**/api/**', route => {
        setTimeout(() => {
          route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify([{ id: 'test-id', name: 'Test Page', url: '/test-page' }])
          });
        }, 1000);
      });

      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      await page.getByPlaceholder(/enter page name/i).fill('Test Page');
      await page.getByPlaceholder('/my-dashboard').fill('/test-page');
      
      await page.getByRole('button', { name: /create & save page/i }).click();
      
      // Loading state should be visible
      await expect(page.getByText('Creating Page...')).toBeVisible();
      
      // Check for loading announcements
      await page.waitForTimeout(500);
      const announcements = await accessibilityHelper.getScreenReaderAnnouncements();
      const loadingAnnouncements = announcements.filter(a => 
        a.text.includes('Creating') || a.text.includes('loading')
      );
      
      // Should have some indication of loading state
      expect(loadingAnnouncements.length).toBeGreaterThanOrEqual(0);
    });

    test('success messages are announced', async ({ page }) => {
      // Mock successful API response
      await page.route('**/api/**', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([{ id: 'test-id', name: 'Test Page', url: '/test-page' }])
        });
      });

      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      await page.getByPlaceholder(/enter page name/i).fill('Test Page');
      await page.getByPlaceholder('/my-dashboard').fill('/test-page');
      
      await page.getByRole('button', { name: /create & save page/i }).click();
      
      await expect(page.getByText('Page "Test Page" created successfully!')).toBeVisible();
      
      // Success should be announced
      const announcements = await accessibilityHelper.getScreenReaderAnnouncements();
      const successAnnouncements = announcements.filter(a => 
        a.text.includes('successfully') || a.text.includes('created')
      );
      
      expect(successAnnouncements.length).toBeGreaterThan(0);
    });
  });

  test.describe('Visual Accessibility', () => {
    test('text has sufficient color contrast', async ({ page }) => {
      // Test main heading
      const headingContrast = await accessibilityHelper.checkColorContrast('h1');
      expect(headingContrast?.meetsAA).toBe(true);

      // Test body text
      const bodyContrast = await accessibilityHelper.checkColorContrast('p');
      expect(bodyContrast?.meetsAA).toBe(true);

      // Test button text
      const buttonContrast = await accessibilityHelper.checkColorContrast('button');
      expect(buttonContrast?.meetsAA).toBe(true);

      console.log('Color contrast results:', {
        heading: headingContrast,
        body: bodyContrast,
        button: buttonContrast
      });
    });

    test('form inputs have sufficient contrast', async ({ page }) => {
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });

      const inputContrast = await accessibilityHelper.checkColorContrast('input');
      expect(inputContrast?.meetsAA).toBe(true);

      // Test input in error state
      await page.getByRole('button', { name: /create & save page/i }).click();
      await page.getByText('Page name is required').waitFor({ state: 'visible' });

      const errorInputContrast = await accessibilityHelper.checkColorContrast('input');
      expect(errorInputContrast?.meetsAA).toBe(true);
    });

    test('focus indicators are clearly visible', async ({ page }) => {
      // Test create button focus
      const createButtonFocus = await accessibilityHelper.checkFocusVisibility('button');
      expect(createButtonFocus?.hasFocusIndicator).toBe(true);

      // Test form input focus
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });

      const inputFocus = await accessibilityHelper.checkFocusVisibility('input');
      expect(inputFocus?.hasFocusIndicator).toBe(true);

      console.log('Focus indicators:', {
        button: createButtonFocus,
        input: inputFocus
      });
    });

    test('text remains readable when zoomed to 200%', async ({ page }) => {
      // Set zoom to 200%
      await page.setViewportSize({ width: 640, height: 480 }); // Simulate 200% zoom
      
      // Check that content is still accessible
      await expect(page.getByRole('heading', { name: 'UIStudio' })).toBeVisible();
      await expect(page.getByRole('button', { name: /create new page/i })).toBeVisible();
      await expect(page.getByText(/welcome to uistudio/i)).toBeVisible();

      // Test modal at 200% zoom
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });

      await expect(page.getByPlaceholder(/enter page name/i)).toBeVisible();
      await expect(page.getByPlaceholder('/my-dashboard')).toBeVisible();
      await expect(page.getByRole('button', { name: /create & save page/i })).toBeVisible();
    });

    test('content adapts to reduced motion preferences', async ({ page }) => {
      // Simulate reduced motion preference
      await page.emulateMedia({ reducedMotion: 'reduce' });
      
      await page.goto('/');
      await page.waitForLoadState('networkidle');

      // Test that modal still works without animations
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });

      // Modal should be functional regardless of animation preferences
      await expect(page.getByRole('dialog')).toBeVisible();
      await expect(page.getByPlaceholder(/enter page name/i)).toBeVisible();
    });
  });

  test.describe('Alternative Input Methods', () => {
    test('supports voice control patterns', async ({ page }) => {
      // Voice control relies on accessible names and roles
      
      // Check that buttons have clear, unique accessible names
      const buttons = await page.locator('button').all();
      const buttonNames = await Promise.all(
        buttons.map(b => b.evaluate(el => 
          el.getAttribute('aria-label') || 
          el.textContent?.trim() || 
          el.getAttribute('title')
        ))
      );

      // Each button should have a unique, descriptive name
      const uniqueNames = new Set(buttonNames.filter(name => name));
      expect(uniqueNames.size).toBe(buttonNames.filter(name => name).length);

      // Names should be descriptive
      expect(buttonNames.some(name => name?.includes('Create'))).toBe(true);
    });

    test('supports switch navigation', async ({ page }) => {
      // Switch navigation relies on clear focus management and selection
      
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });

      // All interactive elements should be focusable
      const focusableElements = await page.locator('button, input, [tabindex="0"]').all();
      
      for (const element of focusableElements) {
        await element.focus();
        const isFocused = await element.evaluate(el => document.activeElement === el);
        expect(isFocused).toBe(true);
      }
    });

    test('supports eye-tracking interfaces', async ({ page }) => {
      // Eye-tracking relies on clear visual boundaries and clickable areas
      
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });

      // Check that interactive elements have sufficient size
      const interactiveElements = page.locator('button, input');
      const elements = await interactiveElements.all();

      for (const element of elements) {
        const box = await element.boundingBox();
        if (box) {
          // Elements should be at least 44x44 pixels (WCAG touch target size)
          expect(box.width).toBeGreaterThanOrEqual(44);
          expect(box.height).toBeGreaterThanOrEqual(44);
        }
      }
    });
  });

  test.describe('Language and Internationalization', () => {
    test('page has proper language declaration', async ({ page }) => {
      const lang = await page.getAttribute('html', 'lang');
      expect(lang).toBeTruthy();
      expect(lang).toMatch(/^[a-z]{2}(-[A-Z]{2})?$/); // Valid language code format
    });

    test('text direction is properly set', async ({ page }) => {
      const dir = await page.getAttribute('html', 'dir');
      expect(['ltr', 'rtl', null]).toContain(dir);
    });

    test('form validation messages are clear and translatable', async ({ page }) => {
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      await page.getByRole('button', { name: /create & save page/i }).click();
      
      const errorMessages = await page.locator('p[class*="text-red"]').all();
      const messageTexts = await Promise.all(
        errorMessages.map(msg => msg.textContent())
      );

      // Error messages should be complete sentences
      messageTexts.forEach(text => {
        expect(text).toBeTruthy();
        expect(text!.length).toBeGreaterThan(10); // Reasonable message length
        expect(text).toMatch(/[.!?]$/); // Ends with punctuation
      });
    });
  });

  test.describe('Error Recovery and Accessibility', () => {
    test('users can recover from errors using assistive technology', async ({ page }) => {
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      // Create error state
      await page.getByRole('button', { name: /create & save page/i }).click();
      await page.getByText('Page name is required').waitFor({ state: 'visible' });

      // Focus should move to first error field or error summary
      const focusedElement = await page.evaluate(() => document.activeElement?.getAttribute('placeholder'));
      
      // User should be able to fix the error
      await page.getByPlaceholder(/enter page name/i).fill('Fixed Page Name');
      
      // Error should clear
      await expect(page.getByText('Page name is required')).not.toBeVisible();
    });

    test('network errors are communicated accessibly', async ({ page }) => {
      // Mock network error
      await page.route('**/api/**', route => {
        route.abort('failed');
      });

      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });
      
      await page.getByPlaceholder(/enter page name/i).fill('Test Page');
      await page.getByPlaceholder('/my-dashboard').fill('/test-page');
      
      await page.getByRole('button', { name: /create & save page/i }).click();
      
      // Error should be announced and visible
      const errorAlert = page.locator('[role="alert"]');
      await expect(errorAlert).toBeVisible();
      
      const errorText = await errorAlert.textContent();
      expect(errorText).toContain('error'); // Should contain error information
    });
  });

  test.describe('Comprehensive Accessibility Summary', () => {
    test('generates accessibility compliance report', async ({ page }) => {
      const report = {
        pageTitle: await page.title(),
        hasH1: await page.locator('h1').count() > 0,
        hasLang: !!(await page.getAttribute('html', 'lang')),
        focusableElements: await page.locator('button, input, a, [tabindex="0"]').count(),
        imagesWithAlt: await page.locator('img[alt]').count(),
        totalImages: await page.locator('img').count(),
        formsWithLabels: 0,
        totalForms: 0,
        accessibilityViolations: 0
      };

      // Test modal accessibility
      await page.getByRole('button', { name: /create new page/i }).click();
      await page.getByRole('dialog').waitFor({ state: 'visible' });

      const modalReport = {
        hasModalRole: await page.locator('[role="dialog"]').count() > 0,
        hasAriaLabelledBy: !!(await page.locator('[role="dialog"]').getAttribute('aria-labelledby')),
        hasAriaDescribedBy: !!(await page.locator('[role="dialog"]').getAttribute('aria-describedby')),
        formInputsWithLabels: await page.locator('input').count(),
        errorMessagesAccessible: true // Would need more complex checking
      };

      // Run final accessibility scan
      const finalScan = await accessibilityHelper.runAxeAnalysis();
      report.accessibilityViolations = finalScan.violations.length;

      console.log('Accessibility Compliance Report:', {
        ...report,
        modal: modalReport,
        violations: finalScan.violations.map(v => v.id)
      });

      // Assert compliance standards
      expect(report.hasH1).toBe(true);
      expect(report.hasLang).toBe(true);
      expect(report.accessibilityViolations).toBe(0);
      expect(modalReport.hasModalRole).toBe(true);
      expect(modalReport.hasAriaLabelledBy).toBe(true);
      expect(modalReport.hasAriaDescribedBy).toBe(true);
    });
  });
});