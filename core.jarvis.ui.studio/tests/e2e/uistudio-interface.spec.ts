import { test, expect, Page, Locator } from '@playwright/test';

/**
 * Comprehensive UI Studio Interface Tests
 * 
 * This test suite thoroughly covers every aspect of the UIStudio interface:
 * - Modal interactions and states
 * - Input validation and error handling
 * - Loading states and success feedback
 * - Accessibility features
 * - Visual appearance and responsiveness
 * - API error scenarios
 * - User workflows and edge cases
 */

interface UIStudioElements {
  createButton: Locator;
  heroCreateButton: Locator;
  modal: Locator;
  modalTitle: Locator;
  modalDescription: Locator;
  modalCloseButton: Locator;
  pageNameInput: Locator;
  pageUrlInput: Locator;
  pageNameLabel: Locator;
  pageUrlLabel: Locator;
  pageNameError: Locator;
  pageUrlError: Locator;
  pageUrlHint: Locator;
  cancelButton: Locator;
  createSubmitButton: Locator;
  errorAlert: Locator;
  successAlert: Locator;
  backdrop: Locator;
  loadingSpinner: Locator;
  createdPagesGrid: Locator;
}

class UIStudioPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/');
    await this.waitForPageLoad();
  }

  async waitForPageLoad() {
    await this.page.waitForLoadState('networkidle');
    await expect(this.page.locator('h1')).toContainText('UIStudio');
  }

  get elements(): UIStudioElements {
    return {
      createButton: this.page.getByRole('button', { name: 'Create New Page' }).first(),
      heroCreateButton: this.page.getByRole('button', { name: 'Create Your First Page' }),
      modal: this.page.getByRole('dialog'),
      modalTitle: this.page.locator('#modal-title'),
      modalDescription: this.page.locator('#modal-description'),
      modalCloseButton: this.page.getByRole('button', { name: 'Close modal' }),
      pageNameInput: this.page.getByPlaceholder('Enter page name (e.g., My Dashboard)'),
      pageUrlInput: this.page.getByPlaceholder('/my-dashboard'),
      pageNameLabel: this.page.locator('label').filter({ hasText: 'Page Name' }),
      pageUrlLabel: this.page.locator('label').filter({ hasText: 'Page URL' }),
      pageNameError: this.page.locator('p').filter({ hasText: /Page name/ }).filter({ hasText: /required|characters/ }),
      pageUrlError: this.page.locator('p').filter({ hasText: /Page URL/ }),
      pageUrlHint: this.page.locator('p').filter({ hasText: 'Must start with /' }),
      cancelButton: this.page.getByRole('button', { name: 'Cancel' }),
      createSubmitButton: this.page.getByRole('button', { name: 'Create & Save Page' }),
      errorAlert: this.page.locator('[role="alert"]').filter({ hasText: /Failed|Error|network|permission/ }),
      successAlert: this.page.locator('[role="alert"]').filter({ hasText: /successfully|Created/ }),
      backdrop: this.page.locator('.fixed.inset-0.bg-black\\/50'),
      loadingSpinner: this.page.locator('.animate-spin'),
      createdPagesGrid: this.page.locator('.grid').filter({ hasText: /Created Pages|Page/ })
    };
  }

  async openModal() {
    await this.elements.createButton.click();
    await expect(this.elements.modal).toBeVisible();
  }

  async closeModal() {
    await this.elements.modalCloseButton.click();
    await expect(this.elements.modal).not.toBeVisible();
  }

  async closeModalByBackdrop() {
    await this.elements.backdrop.click();
    await expect(this.elements.modal).not.toBeVisible();
  }

  async fillForm(pageName: string, pageUrl: string) {
    await this.elements.pageNameInput.fill(pageName);
    await this.elements.pageUrlInput.fill(pageUrl);
  }

  async submitForm() {
    await this.elements.createSubmitButton.click();
  }

  async createPage(pageName: string, pageUrl: string) {
    await this.openModal();
    await this.fillForm(pageName, pageUrl);
    await this.submitForm();
  }
}

test.describe('UIStudio Interface - Comprehensive Testing', () => {
  let uiStudio: UIStudioPage;

  test.beforeEach(async ({ page }) => {
    uiStudio = new UIStudioPage(page);
    await uiStudio.goto();
  });

  test.describe('Initial Page State', () => {
    test('displays header with title and create button', async () => {
      await expect(uiStudio.page.locator('h1')).toContainText('UIStudio');
      await expect(uiStudio.elements.createButton).toBeVisible();
      await expect(uiStudio.elements.createButton).toContainText('Create New Page');
      await expect(uiStudio.elements.createButton.locator('svg')).toBeVisible(); // Plus icon
    });

    test('displays welcome section with hero content', async () => {
      await expect(uiStudio.page.locator('h2')).toContainText('Welcome to UIStudio');
      await expect(uiStudio.page.getByText('Create beautiful pages with our visual editor')).toBeVisible();
      await expect(uiStudio.elements.heroCreateButton).toBeVisible();
      await expect(uiStudio.elements.heroCreateButton).toContainText('Create Your First Page');
    });

    test('displays created pages section with placeholder content', async () => {
      await expect(uiStudio.page.getByText('Created Pages')).toBeVisible();
      await expect(uiStudio.elements.createdPagesGrid).toBeVisible();
      
      // Should show placeholder cards initially
      const placeholderCards = uiStudio.page.locator('.border.rounded-lg.p-4').filter({ hasText: 'Page' });
      await expect(placeholderCards).toHaveCount(3);
    });
  });

  test.describe('Modal Opening and Closing', () => {
    test('opens modal when header create button is clicked', async () => {
      await uiStudio.elements.createButton.click();
      
      await expect(uiStudio.elements.modal).toBeVisible();
      await expect(uiStudio.elements.modalTitle).toContainText('Create New Page');
      await expect(uiStudio.elements.modalDescription).toContainText('Create a new page for your UIStudio dashboard');
      await expect(uiStudio.elements.backdrop).toBeVisible();
    });

    test('opens modal when hero create button is clicked', async () => {
      await uiStudio.elements.heroCreateButton.click();
      
      await expect(uiStudio.elements.modal).toBeVisible();
      await expect(uiStudio.elements.modalTitle).toBeVisible();
    });

    test('closes modal when X button is clicked', async () => {
      await uiStudio.openModal();
      await uiStudio.elements.modalCloseButton.click();
      
      await expect(uiStudio.elements.modal).not.toBeVisible();
      await expect(uiStudio.elements.backdrop).not.toBeVisible();
    });

    test('closes modal when Cancel button is clicked', async () => {
      await uiStudio.openModal();
      await uiStudio.elements.cancelButton.click();
      
      await expect(uiStudio.elements.modal).not.toBeVisible();
    });

    test('closes modal when backdrop is clicked', async () => {
      await uiStudio.openModal();
      await uiStudio.elements.backdrop.click();
      
      await expect(uiStudio.elements.modal).not.toBeVisible();
    });

    test('does not close modal when backdrop is clicked during creation', async () => {
      // Mock API to delay response
      await uiStudio.page.route('**/api/**', route => {
        setTimeout(() => route.fulfill({ status: 200, body: '[]' }), 2000);
      });

      await uiStudio.openModal();
      await uiStudio.fillForm('Test Page', '/test-page');
      await uiStudio.elements.createSubmitButton.click();
      
      // Try to click backdrop while creating
      await uiStudio.elements.backdrop.click();
      
      // Modal should still be visible
      await expect(uiStudio.elements.modal).toBeVisible();
    });

    test('clears form data when modal is reopened', async () => {
      await uiStudio.openModal();
      await uiStudio.fillForm('Test Page', '/test-page');
      await uiStudio.elements.cancelButton.click();
      
      // Reopen modal
      await uiStudio.openModal();
      
      await expect(uiStudio.elements.pageNameInput).toHaveValue('');
      await expect(uiStudio.elements.pageUrlInput).toHaveValue('');
    });
  });

  test.describe('Modal Structure and Accessibility', () => {
    test('has proper modal structure and ARIA attributes', async () => {
      await uiStudio.openModal();
      
      await expect(uiStudio.elements.modal).toHaveAttribute('role', 'dialog');
      await expect(uiStudio.elements.modal).toHaveAttribute('aria-labelledby', 'modal-title');
      await expect(uiStudio.elements.modal).toHaveAttribute('aria-describedby', 'modal-description');
      await expect(uiStudio.elements.backdrop).toHaveAttribute('aria-label', 'Close modal');
      await expect(uiStudio.elements.modalCloseButton).toHaveAttribute('aria-label', 'Close modal');
    });

    test('focuses first input when modal opens', async () => {
      await uiStudio.openModal();
      
      await expect(uiStudio.elements.pageNameInput).toBeFocused();
    });

    test('has proper form labels with required indicators', async () => {
      await uiStudio.openModal();
      
      await expect(uiStudio.elements.pageNameLabel).toContainText('Page Name');
      await expect(uiStudio.elements.pageNameLabel.locator('span.text-red-500')).toContainText('*');
      
      await expect(uiStudio.elements.pageUrlLabel).toContainText('Page URL');
      await expect(uiStudio.elements.pageUrlLabel.locator('span.text-red-500')).toContainText('*');
    });

    test('includes helpful hint text for URL field', async () => {
      await uiStudio.openModal();
      
      await expect(uiStudio.elements.pageUrlHint).toContainText('Must start with / and contain only letters, numbers, hyphens, underscores, and slashes');
    });
  });

  test.describe('Input Behavior and Validation', () => {
    test('page name input is clearly visible and responsive', async () => {
      await uiStudio.openModal();
      
      const input = uiStudio.elements.pageNameInput;
      
      // Test visibility
      await expect(input).toBeVisible();
      await expect(input).toBeEnabled();
      
      // Test typing visibility
      await input.type('My Test Page');
      await expect(input).toHaveValue('My Test Page');
      
      // Test styling
      await expect(input).toHaveClass(/border-gray-300|dark:border-gray-600/);
      
      // Test focus state
      await input.focus();
      await expect(input).toHaveClass(/focus:ring-blue-500/);
    });

    test('page URL input auto-formats and validates', async () => {
      await uiStudio.openModal();
      
      const input = uiStudio.elements.pageUrlInput;
      
      // Test auto-formatting (adding leading slash)
      await input.fill('test-page');
      await expect(input).toHaveValue('/test-page');
      
      // Test manual formatting
      await input.fill('/manual-page');
      await expect(input).toHaveValue('/manual-page');
      
      // Test visibility during typing
      await input.clear();
      await input.type('dashboard');
      await expect(input).toHaveValue('/dashboard');
    });

    test('clears validation errors as user types', async () => {
      await uiStudio.openModal();
      
      // Trigger validation error
      await uiStudio.elements.createSubmitButton.click();
      await expect(uiStudio.elements.pageNameError).toBeVisible();
      
      // Start typing - error should clear
      await uiStudio.elements.pageNameInput.type('Test');
      await expect(uiStudio.elements.pageNameError).not.toBeVisible();
    });

    test('validates page name requirements', async () => {
      await uiStudio.openModal();
      
      // Test empty name
      await uiStudio.elements.createSubmitButton.click();
      await expect(uiStudio.elements.pageNameError).toContainText('Page name is required');
      
      // Test too short name
      await uiStudio.elements.pageNameInput.fill('A');
      await uiStudio.elements.createSubmitButton.click();
      await expect(uiStudio.elements.pageNameError).toContainText('Page name must be at least 2 characters');
      
      // Test valid name
      await uiStudio.elements.pageNameInput.fill('Valid Page');
      await uiStudio.elements.pageUrlInput.fill('/valid-page');
      await expect(uiStudio.elements.createSubmitButton).toBeEnabled();
    });

    test('validates page URL requirements', async () => {
      await uiStudio.openModal();
      
      // Test empty URL
      await uiStudio.elements.pageNameInput.fill('Test Page');
      await uiStudio.elements.createSubmitButton.click();
      await expect(uiStudio.elements.pageUrlError).toContainText('Page URL is required');
      
      // Test invalid characters
      await uiStudio.elements.pageUrlInput.fill('/test page!');
      await uiStudio.elements.createSubmitButton.click();
      await expect(uiStudio.elements.pageUrlError).toContainText('Page URL can only contain');
      
      // Test valid URL
      await uiStudio.elements.pageUrlInput.fill('/test-page_123');
      await expect(uiStudio.elements.createSubmitButton).toBeEnabled();
    });

    test('disables form during submission', async () => {
      // Mock slow API response
      await uiStudio.page.route('**/api/**', route => {
        setTimeout(() => route.fulfill({ status: 200, body: '[]' }), 1000);
      });

      await uiStudio.openModal();
      await uiStudio.fillForm('Test Page', '/test-page');
      await uiStudio.elements.createSubmitButton.click();
      
      // Inputs should be disabled
      await expect(uiStudio.elements.pageNameInput).toBeDisabled();
      await expect(uiStudio.elements.pageUrlInput).toBeDisabled();
      await expect(uiStudio.elements.createSubmitButton).toBeDisabled();
    });
  });

  test.describe('Loading States and Visual Feedback', () => {
    test('shows loading state during page creation', async () => {
      // Mock slow API response
      await uiStudio.page.route('**/api/**', route => {
        setTimeout(() => route.fulfill({ status: 200, body: '[]' }), 1000);
      });

      await uiStudio.openModal();
      await uiStudio.fillForm('Test Page', '/test-page');
      await uiStudio.elements.createSubmitButton.click();
      
      // Check loading state
      await expect(uiStudio.elements.createSubmitButton).toContainText('Creating Page...');
      await expect(uiStudio.elements.loadingSpinner).toBeVisible();
      await expect(uiStudio.elements.cancelButton).toContainText('Creating...');
      await expect(uiStudio.elements.cancelButton).toBeDisabled();
    });

    test('shows success state after page creation', async () => {
      // Mock successful API response
      await uiStudio.page.route('**/api/**', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([{ id: 'test-id', name: 'Test Page', url: '/test-page' }])
        });
      });

      await uiStudio.openModal();
      await uiStudio.fillForm('Test Page', '/test-page');
      await uiStudio.elements.createSubmitButton.click();
      
      // Check success state
      await expect(uiStudio.elements.successAlert).toBeVisible();
      await expect(uiStudio.elements.successAlert).toContainText('Page "Test Page" created successfully!');
      await expect(uiStudio.elements.createSubmitButton).toContainText('Created Successfully!');
      await expect(uiStudio.elements.createSubmitButton.locator('svg')).toBeVisible(); // CheckCircle icon
      
      // Modal should close after delay
      await expect(uiStudio.elements.modal).not.toBeVisible({ timeout: 2000 });
    });

    test('button shows correct states during workflow', async () => {
      await uiStudio.openModal();
      
      // Initial state
      await expect(uiStudio.elements.createSubmitButton).toContainText('Create & Save Page');
      await expect(uiStudio.elements.createSubmitButton.locator('.lucide-save')).toBeVisible();
      
      // Disabled when form is invalid
      await expect(uiStudio.elements.createSubmitButton).toBeDisabled();
      
      // Enabled when form is valid
      await uiStudio.fillForm('Test Page', '/test-page');
      await expect(uiStudio.elements.createSubmitButton).toBeEnabled();
    });
  });

  test.describe('Error Handling and User Feedback', () => {
    test('displays general validation error', async () => {
      await uiStudio.openModal();
      await uiStudio.elements.createSubmitButton.click();
      
      await expect(uiStudio.elements.errorAlert).toBeVisible();
      await expect(uiStudio.elements.errorAlert).toContainText('Please fix the validation errors above');
      await expect(uiStudio.elements.errorAlert.locator('.lucide-alert-circle')).toBeVisible();
    });

    test('handles API error with user-friendly message', async () => {
      // Mock API error
      await uiStudio.page.route('**/api/**', route => {
        route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Internal server error' })
        });
      });

      await uiStudio.openModal();
      await uiStudio.fillForm('Test Page', '/test-page');
      await uiStudio.elements.createSubmitButton.click();
      
      await expect(uiStudio.elements.errorAlert).toBeVisible();
      await expect(uiStudio.elements.errorAlert).toContainText('Failed to create page');
    });

    test('handles network error with specific message', async () => {
      // Mock network error
      await uiStudio.page.route('**/api/**', route => {
        route.abort('failed');
      });

      await uiStudio.openModal();
      await uiStudio.fillForm('Test Page', '/test-page');
      await uiStudio.elements.createSubmitButton.click();
      
      await expect(uiStudio.elements.errorAlert).toBeVisible();
      await expect(uiStudio.elements.errorAlert).toContainText(/Network error|connection/);
    });

    test('handles duplicate page error', async () => {
      // Mock duplicate error
      await uiStudio.page.route('**/api/**', route => {
        route.fulfill({
          status: 409,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Page already exists' })
        });
      });

      await uiStudio.openModal();
      await uiStudio.fillForm('Test Page', '/test-page');
      await uiStudio.elements.createSubmitButton.click();
      
      await expect(uiStudio.elements.errorAlert).toBeVisible();
      await expect(uiStudio.elements.errorAlert).toContainText('A page with this name or URL already exists');
    });

    test('handles permission error', async () => {
      // Mock permission error
      await uiStudio.page.route('**/api/**', route => {
        route.fulfill({
          status: 403,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Unauthorized' })
        });
      });

      await uiStudio.openModal();
      await uiStudio.fillForm('Test Page', '/test-page');
      await uiStudio.elements.createSubmitButton.click();
      
      await expect(uiStudio.elements.errorAlert).toBeVisible();
      await expect(uiStudio.elements.errorAlert).toContainText('You do not have permission to create pages');
    });

    test('clears errors when user makes changes', async () => {
      await uiStudio.openModal();
      await uiStudio.elements.createSubmitButton.click();
      
      // Error should be visible
      await expect(uiStudio.elements.errorAlert).toBeVisible();
      
      // Make changes - error should clear
      await uiStudio.elements.pageNameInput.type('Test');
      await expect(uiStudio.elements.errorAlert).not.toBeVisible();
    });
  });

  test.describe('Created Pages Display', () => {
    test('shows newly created page in grid', async () => {
      // Mock successful API response
      await uiStudio.page.route('**/api/**', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([{ id: 'test-id', name: 'My Dashboard', url: '/my-dashboard' }])
        });
      });

      await uiStudio.createPage('My Dashboard', '/my-dashboard');
      
      // Wait for modal to close and page to update
      await uiStudio.page.waitForTimeout(1600);
      
      // Check that new page appears in grid
      const newPageCard = uiStudio.page.locator('.border.rounded-lg.p-4').filter({ hasText: 'My Dashboard' });
      await expect(newPageCard).toBeVisible();
      await expect(newPageCard).toContainText('/my-dashboard');
      await expect(newPageCard).toContainText('Created');
      await expect(newPageCard.locator('.lucide-check')).toBeVisible(); // Success icon
      
      // Should have green styling for success
      await expect(newPageCard).toHaveClass(/bg-green-50|border-green-200/);
    });

    test('displays creation timestamp', async () => {
      // Mock successful API response
      await uiStudio.page.route('**/api/**', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([{ id: 'test-id', name: 'Test Page', url: '/test-page' }])
        });
      });

      await uiStudio.createPage('Test Page', '/test-page');
      await uiStudio.page.waitForTimeout(1600);
      
      const newPageCard = uiStudio.page.locator('.border.rounded-lg.p-4').filter({ hasText: 'Test Page' });
      await expect(newPageCard).toContainText(/Created \d+:\d+:\d+/); // Matches time format
    });
  });

  test.describe('Responsive Design and Visual Testing', () => {
    test('modal is responsive on mobile viewport', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE
      
      await uiStudio.openModal();
      
      // Modal should still be visible and usable
      await expect(uiStudio.elements.modal).toBeVisible();
      await expect(uiStudio.elements.pageNameInput).toBeVisible();
      await expect(uiStudio.elements.pageUrlInput).toBeVisible();
      
      // Should have proper spacing on mobile
      const modal = uiStudio.elements.modal;
      await expect(modal).toHaveClass(/max-w-md/);
    });

    test('grid layout is responsive', async ({ page }) => {
      await page.setViewportSize({ width: 768, height: 1024 }); // Tablet
      
      const grid = uiStudio.elements.createdPagesGrid;
      await expect(grid).toHaveClass(/md:grid-cols-2/);
      
      await page.setViewportSize({ width: 1024, height: 768 }); // Desktop
      await expect(grid).toHaveClass(/lg:grid-cols-3/);
    });

    test('dark mode styling works correctly', async ({ page }) => {
      // Toggle dark mode (assuming there's a way to do this)
      await page.addStyleTag({
        content: `
          html { color-scheme: dark; }
          * { 
            --background: 222.2% 84% 4.9%;
            --foreground: 210% 40% 98%;
          }
        `
      });
      
      await uiStudio.openModal();
      
      // Check dark mode input styling
      await expect(uiStudio.elements.pageNameInput).toHaveClass(/dark:bg-gray-700|dark:text-white/);
      await expect(uiStudio.elements.pageUrlInput).toHaveClass(/dark:bg-gray-700|dark:text-white/);
    });
  });

  test.describe('Keyboard Navigation and Accessibility', () => {
    test('supports keyboard navigation', async ({ page }) => {
      await uiStudio.openModal();
      
      // Tab through elements
      await page.keyboard.press('Tab'); // Should be on pageNameInput (already focused)
      await expect(uiStudio.elements.pageNameInput).toBeFocused();
      
      await page.keyboard.press('Tab'); // pageUrlInput
      await expect(uiStudio.elements.pageUrlInput).toBeFocused();
      
      await page.keyboard.press('Tab'); // Cancel button
      await expect(uiStudio.elements.cancelButton).toBeFocused();
      
      await page.keyboard.press('Tab'); // Create button
      await expect(uiStudio.elements.createSubmitButton).toBeFocused();
    });

    test('supports Escape key to close modal', async ({ page }) => {
      await uiStudio.openModal();
      
      await page.keyboard.press('Escape');
      await expect(uiStudio.elements.modal).not.toBeVisible();
    });

    test('supports Enter key to submit form', async ({ page }) => {
      // Mock successful API response
      await page.route('**/api/**', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([{ id: 'test-id', name: 'Test Page', url: '/test-page' }])
        });
      });

      await uiStudio.openModal();
      await uiStudio.fillForm('Test Page', '/test-page');
      
      await page.keyboard.press('Enter');
      await expect(uiStudio.elements.successAlert).toBeVisible();
    });

    test('has proper focus management', async ({ page }) => {
      await uiStudio.openModal();
      
      // Focus should be trapped in modal
      await expect(uiStudio.elements.pageNameInput).toBeFocused();
      
      // Close modal and reopen
      await page.keyboard.press('Escape');
      await uiStudio.openModal();
      
      // Focus should return to first input
      await expect(uiStudio.elements.pageNameInput).toBeFocused();
    });
  });

  test.describe('Edge Cases and Error Recovery', () => {
    test('handles extremely long page names gracefully', async () => {
      await uiStudio.openModal();
      
      const longName = 'A'.repeat(1000);
      await uiStudio.elements.pageNameInput.fill(longName);
      await uiStudio.elements.pageUrlInput.fill('/test');
      
      // Should still be functional
      await expect(uiStudio.elements.pageNameInput).toHaveValue(longName);
      await expect(uiStudio.elements.createSubmitButton).toBeEnabled();
    });

    test('handles special characters in page name', async () => {
      await uiStudio.openModal();
      
      const specialName = 'Test Page @ 2024 #1 & More!';
      await uiStudio.fillForm(specialName, '/test-page');
      
      await expect(uiStudio.elements.pageNameInput).toHaveValue(specialName);
      await expect(uiStudio.elements.createSubmitButton).toBeEnabled();
    });

    test('recovers from network interruption', async ({ page }) => {
      // First, fail the request
      await page.route('**/api/**', route => route.abort('failed'));
      
      await uiStudio.openModal();
      await uiStudio.fillForm('Test Page', '/test-page');
      await uiStudio.elements.createSubmitButton.click();
      
      await expect(uiStudio.elements.errorAlert).toBeVisible();
      
      // Now allow the request to succeed
      await page.unroute('**/api/**');
      await page.route('**/api/**', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([{ id: 'test-id', name: 'Test Page', url: '/test-page' }])
        });
      });
      
      // User can retry
      await uiStudio.elements.createSubmitButton.click();
      await expect(uiStudio.elements.successAlert).toBeVisible();
    });

    test('maintains form state during temporary errors', async () => {
      // Mock error response
      await uiStudio.page.route('**/api/**', route => {
        route.fulfill({ status: 500, body: 'Server Error' });
      });

      await uiStudio.openModal();
      await uiStudio.fillForm('Important Page', '/important-page');
      await uiStudio.elements.createSubmitButton.click();
      
      // Error should be shown but form data preserved
      await expect(uiStudio.elements.errorAlert).toBeVisible();
      await expect(uiStudio.elements.pageNameInput).toHaveValue('Important Page');
      await expect(uiStudio.elements.pageUrlInput).toHaveValue('/important-page');
      await expect(uiStudio.elements.modal).toBeVisible();
    });
  });

  test.describe('Performance and Animation', () => {
    test('modal animations are smooth', async ({ page }) => {
      // Open modal and check it appears quickly
      const startTime = Date.now();
      await uiStudio.openModal();
      const openTime = Date.now() - startTime;
      
      expect(openTime).toBeLessThan(500); // Should open in under 500ms
      
      // Close modal and check it disappears quickly
      const closeStartTime = Date.now();
      await uiStudio.closeModal();
      const closeTime = Date.now() - closeStartTime;
      
      expect(closeTime).toBeLessThan(500); // Should close in under 500ms
    });

    test('form submission is responsive', async () => {
      await uiStudio.openModal();
      await uiStudio.fillForm('Test Page', '/test-page');
      
      const submitStartTime = Date.now();
      await uiStudio.elements.createSubmitButton.click();
      
      // Loading state should appear immediately
      await expect(uiStudio.elements.loadingSpinner).toBeVisible();
      const loadingTime = Date.now() - submitStartTime;
      
      expect(loadingTime).toBeLessThan(100); // Loading state should appear in under 100ms
    });

    test('input responses are immediate', async () => {
      await uiStudio.openModal();
      
      const typingStartTime = Date.now();
      await uiStudio.elements.pageNameInput.type('Test');
      const typingTime = Date.now() - typingStartTime;
      
      expect(typingTime).toBeLessThan(200); // Typing should be responsive
      await expect(uiStudio.elements.pageNameInput).toHaveValue('Test');
    });
  });

  test.describe('Cross-Browser Compatibility', () => {
    test('works correctly in different browsers', async ({ browserName }) => {
      // Test basic functionality across browsers
      await uiStudio.openModal();
      await uiStudio.fillForm('Cross Browser Test', '/cross-browser');
      
      await expect(uiStudio.elements.modal).toBeVisible();
      await expect(uiStudio.elements.pageNameInput).toHaveValue('Cross Browser Test');
      await expect(uiStudio.elements.pageUrlInput).toHaveValue('/cross-browser');
      await expect(uiStudio.elements.createSubmitButton).toBeEnabled();
      
      console.log(`✅ Test passed in ${browserName}`);
    });
  });
});

/**
 * Visual Regression Tests
 * 
 * These tests capture screenshots and compare them against baselines
 * to ensure visual consistency across changes.
 */
test.describe('Visual Regression Tests', () => {
  let uiStudio: UIStudioPage;

  test.beforeEach(async ({ page }) => {
    uiStudio = new UIStudioPage(page);
    await uiStudio.goto();
  });

  test('initial page appearance', async ({ page }) => {
    await expect(page).toHaveScreenshot('uistudio-initial-page.png');
  });

  test('modal appearance', async ({ page }) => {
    await uiStudio.openModal();
    await expect(page).toHaveScreenshot('uistudio-create-modal.png');
  });

  test('modal with validation errors', async ({ page }) => {
    await uiStudio.openModal();
    await uiStudio.elements.createSubmitButton.click();
    await expect(page).toHaveScreenshot('uistudio-modal-validation-errors.png');
  });

  test('modal with filled form', async ({ page }) => {
    await uiStudio.openModal();
    await uiStudio.fillForm('Test Dashboard', '/test-dashboard');
    await expect(page).toHaveScreenshot('uistudio-modal-filled-form.png');
  });

  test('modal loading state', async ({ page }) => {
    // Mock slow API response
    await page.route('**/api/**', route => {
      setTimeout(() => route.fulfill({ status: 200, body: '[]' }), 5000);
    });

    await uiStudio.openModal();
    await uiStudio.fillForm('Test Page', '/test-page');
    await uiStudio.elements.createSubmitButton.click();
    
    await expect(page).toHaveScreenshot('uistudio-modal-loading.png');
  });

  test('error state appearance', async ({ page }) => {
    await page.route('**/api/**', route => {
      route.fulfill({ status: 500, body: 'Server Error' });
    });

    await uiStudio.openModal();
    await uiStudio.fillForm('Test Page', '/test-page');
    await uiStudio.elements.createSubmitButton.click();
    
    await expect(page).toHaveScreenshot('uistudio-modal-error.png');
  });

  test('success state appearance', async ({ page }) => {
    await page.route('**/api/**', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([{ id: 'test-id', name: 'Test Page', url: '/test-page' }])
      });
    });

    await uiStudio.openModal();
    await uiStudio.fillForm('Test Page', '/test-page');
    await uiStudio.elements.createSubmitButton.click();
    
    await expect(uiStudio.elements.successAlert).toBeVisible();
    await expect(page).toHaveScreenshot('uistudio-modal-success.png');
  });

  test('created page in grid', async ({ page }) => {
    await page.route('**/api/**', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([{ id: 'test-id', name: 'My Dashboard', url: '/my-dashboard' }])
      });
    });

    await uiStudio.createPage('My Dashboard', '/my-dashboard');
    await page.waitForTimeout(1600); // Wait for modal to close
    
    await expect(page).toHaveScreenshot('uistudio-with-created-page.png');
  });
});