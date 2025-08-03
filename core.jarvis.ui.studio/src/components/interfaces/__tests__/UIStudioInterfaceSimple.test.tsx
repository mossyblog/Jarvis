import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, test, expect, beforeEach, afterEach } from 'vitest';
import { UIStudioInterfaceSimple } from '../UIStudioInterfaceSimple';

// Mock the useCreateUIStudioPage hook
const mockMutateAsync = vi.fn();
const mockCreatePage = {
  mutateAsync: mockMutateAsync,
  isLoading: false,
  error: null,
  data: null,
  isError: false,
  isSuccess: false
};

vi.mock('../../../hooks/useUIStudio', () => ({
  useCreateUIStudioPage: () => mockCreatePage
}));

// Mock Lucide React icons
vi.mock('lucide-react', () => ({
  Plus: () => <div data-testid="plus-icon">Plus</div>,
  Save: () => <div data-testid="save-icon">Save</div>,
  Check: () => <div data-testid="check-icon">Check</div>,
  AlertCircle: () => <div data-testid="alert-circle-icon">AlertCircle</div>,
  CheckCircle2: () => <div data-testid="check-circle-icon">CheckCircle2</div>,
  X: () => <div data-testid="x-icon">X</div>,
  Loader2: () => <div data-testid="loader-icon">Loader2</div>
}));

/**
 * Comprehensive Unit Tests for UIStudioInterfaceSimple
 * 
 * These tests cover:
 * - Component rendering and initial state
 * - User interactions and event handling
 * - Form validation logic
 * - API integration and error handling
 * - State management and side effects
 * - Accessibility features
 * - Edge cases and error scenarios
 */

describe('UIStudioInterfaceSimple', () => {
  let queryClient: QueryClient;
  let user: ReturnType<typeof userEvent.setup>;

  const renderComponent = (props = {}) => {
    return render(
      <QueryClientProvider client={queryClient}>
        <UIStudioInterfaceSimple userEntityId="test-user-id" {...props} />
      </QueryClientProvider>
    );
  };

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
          cacheTime: 0,
        },
        mutations: {
          retry: false,
        },
      },
    });
    user = userEvent.setup();
    vi.clearAllMocks();
  });

  afterEach(() => {
    queryClient.clear();
  });

  describe('Initial Rendering', () => {
    test('renders the main interface with header and welcome section', () => {
      renderComponent();
      
      // Header elements
      expect(screen.getByRole('heading', { name: 'UIStudio' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /create new page/i })).toBeInTheDocument();
      expect(screen.getByTestId('plus-icon')).toBeInTheDocument();
      
      // Welcome section
      expect(screen.getByRole('heading', { name: /welcome to uistudio/i })).toBeInTheDocument();
      expect(screen.getByText(/create beautiful pages with our visual editor/i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /create your first page/i })).toBeInTheDocument();
      
      // Created pages section
      expect(screen.getByText('Created Pages')).toBeInTheDocument();
      
      // Placeholder cards should be visible
      const placeholderCards = screen.getAllByText(/page \d+/i);
      expect(placeholderCards).toHaveLength(3);
    });

    test('modal is not visible initially', () => {
      renderComponent();
      
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
      expect(screen.queryByText('Create New Page')).not.toBeInTheDocument();
    });

    test('renders without userEntityId prop', () => {
      renderComponent({ userEntityId: undefined });
      
      expect(screen.getByRole('heading', { name: 'UIStudio' })).toBeInTheDocument();
    });
  });

  describe('Modal Opening and Closing', () => {
    test('opens modal when header create button is clicked', async () => {
      renderComponent();
      
      const createButton = screen.getByRole('button', { name: /create new page/i });
      await user.click(createButton);
      
      expect(screen.getByRole('dialog')).toBeInTheDocument();
      expect(screen.getByText('Create New Page')).toBeInTheDocument();
      expect(screen.getByText(/create a new page for your uistudio dashboard/i)).toBeInTheDocument();
    });

    test('opens modal when hero create button is clicked', async () => {
      renderComponent();
      
      const heroButton = screen.getByRole('button', { name: /create your first page/i });
      await user.click(heroButton);
      
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });

    test('closes modal when X button is clicked', async () => {
      renderComponent();
      
      // Open modal
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      expect(screen.getByRole('dialog')).toBeInTheDocument();
      
      // Close modal
      const closeButton = screen.getByRole('button', { name: /close modal/i });
      await user.click(closeButton);
      
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    test('closes modal when Cancel button is clicked', async () => {
      renderComponent();
      
      // Open modal
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      expect(screen.getByRole('dialog')).toBeInTheDocument();
      
      // Close modal
      const cancelButton = screen.getByRole('button', { name: 'Cancel' });
      await user.click(cancelButton);
      
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    test('closes modal when backdrop is clicked', async () => {
      renderComponent();
      
      // Open modal
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      expect(screen.getByRole('dialog')).toBeInTheDocument();
      
      // Click backdrop
      const backdrop = screen.getByLabelText('Close modal');
      await user.click(backdrop);
      
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    test('clears form data when modal is reopened', async () => {
      renderComponent();
      
      // Open modal and fill form
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const pageNameInput = screen.getByPlaceholderText(/enter page name/i);
      const pageUrlInput = screen.getByPlaceholderText('/my-dashboard');
      
      await user.type(pageNameInput, 'Test Page');
      await user.type(pageUrlInput, '/test-page');
      
      // Close modal
      await user.click(screen.getByRole('button', { name: 'Cancel' }));
      
      // Reopen modal
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      // Form should be cleared
      expect(screen.getByPlaceholderText(/enter page name/i)).toHaveValue('');
      expect(screen.getByPlaceholderText('/my-dashboard')).toHaveValue('');
    });
  });

  describe('Modal Structure and Accessibility', () => {
    test('modal has proper ARIA attributes', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const modal = screen.getByRole('dialog');
      expect(modal).toHaveAttribute('aria-labelledby', 'modal-title');
      expect(modal).toHaveAttribute('aria-describedby', 'modal-description');
      
      expect(screen.getByLabelText('Close modal')).toBeInTheDocument();
    });

    test('form has proper labels and required indicators', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      // Check labels
      expect(screen.getByText('Page Name')).toBeInTheDocument();
      expect(screen.getByText('Page URL')).toBeInTheDocument();
      
      // Check required indicators
      const requiredIndicators = screen.getAllByText('*');
      expect(requiredIndicators).toHaveLength(2);
    });

    test('includes helpful hint text', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      expect(screen.getByText(/must start with \/ and contain only/i)).toBeInTheDocument();
    });
  });

  describe('Input Behavior and Validation', () => {
    test('auto-formats page URL with leading slash', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const pageUrlInput = screen.getByPlaceholderText('/my-dashboard');
      
      await user.type(pageUrlInput, 'test-page');
      expect(pageUrlInput).toHaveValue('/test-page');
    });

    test('preserves manually entered leading slash', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const pageUrlInput = screen.getByPlaceholderText('/my-dashboard');
      
      await user.type(pageUrlInput, '/manual-page');
      expect(pageUrlInput).toHaveValue('/manual-page');
    });

    test('clears validation errors as user types', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      // Trigger validation error
      const createButton = screen.getByRole('button', { name: /create & save page/i });
      await user.click(createButton);
      
      expect(screen.getByText('Page name is required')).toBeInTheDocument();
      
      // Start typing - error should clear
      const pageNameInput = screen.getByPlaceholderText(/enter page name/i);
      await user.type(pageNameInput, 'Test');
      
      expect(screen.queryByText('Page name is required')).not.toBeInTheDocument();
    });

    test('validates page name requirements', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const createButton = screen.getByRole('button', { name: /create & save page/i });
      
      // Test empty name
      await user.click(createButton);
      expect(screen.getByText('Page name is required')).toBeInTheDocument();
      
      // Test too short name
      const pageNameInput = screen.getByPlaceholderText(/enter page name/i);
      await user.clear(pageNameInput);
      await user.type(pageNameInput, 'A');
      await user.click(createButton);
      expect(screen.getByText('Page name must be at least 2 characters')).toBeInTheDocument();
      
      // Test valid name
      await user.clear(pageNameInput);
      await user.type(pageNameInput, 'Valid Page');
      
      const pageUrlInput = screen.getByPlaceholderText('/my-dashboard');
      await user.type(pageUrlInput, '/valid-page');
      
      expect(createButton).toBeEnabled();
    });

    test('validates page URL requirements', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const pageNameInput = screen.getByPlaceholderText(/enter page name/i);
      const pageUrlInput = screen.getByPlaceholderText('/my-dashboard');
      const createButton = screen.getByRole('button', { name: /create & save page/i });
      
      // Fill page name first
      await user.type(pageNameInput, 'Test Page');
      
      // Test empty URL
      await user.click(createButton);
      expect(screen.getByText('Page URL is required')).toBeInTheDocument();
      
      // Test invalid characters
      await user.type(pageUrlInput, '/test page!');
      await user.click(createButton);
      expect(screen.getByText(/page url can only contain/i)).toBeInTheDocument();
      
      // Test valid URL
      await user.clear(pageUrlInput);
      await user.type(pageUrlInput, '/test-page_123');
      
      expect(createButton).toBeEnabled();
    });

    test('disables inputs during form submission', async () => {
      mockMutateAsync.mockImplementation(() => new Promise(resolve => setTimeout(resolve, 1000)));
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const pageNameInput = screen.getByPlaceholderText(/enter page name/i);
      const pageUrlInput = screen.getByPlaceholderText('/my-dashboard');
      const createButton = screen.getByRole('button', { name: /create & save page/i });
      
      await user.type(pageNameInput, 'Test Page');
      await user.type(pageUrlInput, '/test-page');
      
      await user.click(createButton);
      
      // Inputs should be disabled
      expect(pageNameInput).toBeDisabled();
      expect(pageUrlInput).toBeDisabled();
      expect(createButton).toBeDisabled();
    });
  });

  describe('Form Submission and API Integration', () => {
    test('submits form with correct data', async () => {
      mockMutateAsync.mockResolvedValue([{ id: 'test-id', name: 'Test Page', url: '/test-page' }]);
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      expect(mockMutateAsync).toHaveBeenCalledWith({
        pageName: 'Test Page',
        pageSlug: '/test-page',
        pageType: 'static',
        description: 'Page created via UIStudio: Test Page',
        createdByEntityId: 'test-user-id',
        metadata: {
          createdVia: 'UIStudio',
          version: '1.0.0'
        },
        tags: 'uistudio,dashboard'
      });
    });

    test('trims whitespace from inputs', async () => {
      mockMutateAsync.mockResolvedValue([{ id: 'test-id', name: 'Test Page', url: '/test-page' }]);
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), '  Test Page  ');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '  /test-page  ');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      expect(mockMutateAsync).toHaveBeenCalledWith(
        expect.objectContaining({
          pageName: 'Test Page',
          pageSlug: '/test-page'
        })
      );
    });

    test('uses fallback user ID when not provided', async () => {
      mockMutateAsync.mockResolvedValue([{ id: 'test-id', name: 'Test Page', url: '/test-page' }]);
      
      renderComponent({ userEntityId: undefined });
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      expect(mockMutateAsync).toHaveBeenCalledWith(
        expect.objectContaining({
          createdByEntityId: 'temp-user-id'
        })
      );
    });
  });

  describe('Loading States and Visual Feedback', () => {
    test('shows loading state during form submission', async () => {
      mockMutateAsync.mockImplementation(() => new Promise(resolve => setTimeout(() => resolve([]), 1000)));
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      // Check loading indicators
      expect(screen.getByText('Creating Page...')).toBeInTheDocument();
      expect(screen.getByTestId('loader-icon')).toBeInTheDocument();
      expect(screen.getByText('Creating...')).toBeInTheDocument(); // Cancel button text
    });

    test('shows success state after page creation', async () => {
      mockMutateAsync.mockResolvedValue([{ id: 'test-id', name: 'Test Page', url: '/test-page' }]);
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      await waitFor(() => {
        expect(screen.getByText('Page \"Test Page\" created successfully!')).toBeInTheDocument();
        expect(screen.getByText('Created Successfully!')).toBeInTheDocument();
        expect(screen.getByTestId('check-circle-icon')).toBeInTheDocument();
      });
    });

    test('button is disabled when form is invalid', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const createButton = screen.getByRole('button', { name: /create & save page/i });
      
      // Initially disabled
      expect(createButton).toBeDisabled();
      
      // Still disabled with only page name
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      expect(createButton).toBeDisabled();
      
      // Enabled when both fields are filled
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      expect(createButton).toBeEnabled();
    });

    test('modal closes automatically after successful creation', async () => {
      mockMutateAsync.mockResolvedValue([{ id: 'test-id', name: 'Test Page', url: '/test-page' }]);
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      // Wait for success message and modal to close
      await waitFor(
        () => {
          expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
        },
        { timeout: 2000 }
      );
    });
  });

  describe('Error Handling', () => {
    test('displays validation error message', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      expect(screen.getByText('Please fix the validation errors above')).toBeInTheDocument();
      expect(screen.getByTestId('alert-circle-icon')).toBeInTheDocument();
    });

    test('handles API error with custom message', async () => {
      const errorMessage = 'Internal server error';
      mockMutateAsync.mockRejectedValue({ 
        response: { data: { message: errorMessage } } 
      });
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      await waitFor(() => {
        expect(screen.getByText(errorMessage)).toBeInTheDocument();
      });
    });

    test('handles network error', async () => {
      mockMutateAsync.mockRejectedValue(new Error('Network error'));
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      await waitFor(() => {
        expect(screen.getByText(/network error/i)).toBeInTheDocument();
      });
    });

    test('handles duplicate page error', async () => {
      mockMutateAsync.mockRejectedValue({ 
        message: 'Page already exists' 
      });
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      await waitFor(() => {
        expect(screen.getByText(/a page with this name or url already exists/i)).toBeInTheDocument();
      });
    });

    test('handles permission error', async () => {
      mockMutateAsync.mockRejectedValue({ 
        message: 'Unauthorized' 
      });
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      await waitFor(() => {
        expect(screen.getByText(/you do not have permission to create pages/i)).toBeInTheDocument();
      });
    });

    test('maintains form state during errors', async () => {
      mockMutateAsync.mockRejectedValue(new Error('Server error'));
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Important Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/important-page');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      await waitFor(() => {
        expect(screen.getByText(/failed to create page/i)).toBeInTheDocument();
      });
      
      // Form data should be preserved
      expect(screen.getByDisplayValue('Important Page')).toBeInTheDocument();
      expect(screen.getByDisplayValue('/important-page')).toBeInTheDocument();
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });

    test('clears errors when user makes changes', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      // Error should be visible
      expect(screen.getByText('Please fix the validation errors above')).toBeInTheDocument();
      
      // Make changes - error should clear
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test');
      
      expect(screen.queryByText('Please fix the validation errors above')).not.toBeInTheDocument();
    });
  });

  describe('Created Pages Display', () => {
    test('shows newly created page in grid', async () => {
      mockMutateAsync.mockResolvedValue([{ 
        id: 'test-id', 
        name: 'My Dashboard', 
        url: '/my-dashboard' 
      }]);
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'My Dashboard');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/my-dashboard');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
      }, { timeout: 2000 });
      
      // Check that new page appears in grid
      expect(screen.getByText('My Dashboard')).toBeInTheDocument();
      expect(screen.getByText('/my-dashboard')).toBeInTheDocument();
      expect(screen.getByText(/created/i)).toBeInTheDocument();
      expect(screen.getByTestId('check-icon')).toBeInTheDocument();
    });

    test('displays creation timestamp', async () => {
      mockMutateAsync.mockResolvedValue([{ 
        id: 'test-id', 
        name: 'Test Page', 
        url: '/test-page' 
      }]);
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
      }, { timeout: 2000 });
      
      // Should display timestamp
      expect(screen.getByText(/created \d+:\d+:\d+/i)).toBeInTheDocument();
    });

    test('adds multiple pages to grid', async () => {
      renderComponent();
      
      // Create first page
      mockMutateAsync.mockResolvedValueOnce([{ 
        id: 'test-id-1', 
        name: 'Page One', 
        url: '/page-one' 
      }]);
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Page One');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/page-one');
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
      }, { timeout: 2000 });
      
      // Create second page
      mockMutateAsync.mockResolvedValueOnce([{ 
        id: 'test-id-2', 
        name: 'Page Two', 
        url: '/page-two' 
      }]);
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Page Two');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/page-two');
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
      }, { timeout: 2000 });
      
      // Both pages should be visible
      expect(screen.getByText('Page One')).toBeInTheDocument();
      expect(screen.getByText('Page Two')).toBeInTheDocument();
    });
  });

  describe('Edge Cases and Error Recovery', () => {
    test('handles extremely long page names', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const longName = 'A'.repeat(1000);
      const pageNameInput = screen.getByPlaceholderText(/enter page name/i);
      
      await user.type(pageNameInput, longName);
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test');
      
      expect(pageNameInput).toHaveValue(longName);
      expect(screen.getByRole('button', { name: /create & save page/i })).toBeEnabled();
    });

    test('handles special characters in page name', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const specialName = 'Test Page @ 2024 #1 & More!';
      const pageNameInput = screen.getByPlaceholderText(/enter page name/i);
      
      await user.type(pageNameInput, specialName);
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      expect(pageNameInput).toHaveValue(specialName);
      expect(screen.getByRole('button', { name: /create & save page/i })).toBeEnabled();
    });

    test('prevents modal close during creation via backdrop click', async () => {
      // Mock a slow API call
      mockMutateAsync.mockImplementation(() => new Promise(resolve => setTimeout(resolve, 1000)));
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      // Try to close via backdrop - should not close
      const backdrop = screen.getByLabelText('Close modal');
      await user.click(backdrop);
      
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });

    test('prevents modal close during creation via close button', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      // Close button should be visible initially
      expect(screen.getByRole('button', { name: /close modal/i })).toBeInTheDocument();
      
      // Start creation
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      // Mock slow API call
      mockMutateAsync.mockImplementation(() => new Promise(resolve => setTimeout(resolve, 1000)));
      
      await user.click(screen.getByRole('button', { name: /create & save page/i }));
      
      // Close button should be hidden during creation
      expect(screen.queryByRole('button', { name: /close modal/i })).not.toBeInTheDocument();
    });
  });

  describe('Keyboard Navigation', () => {
    test('focuses first input when modal opens', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const pageNameInput = screen.getByPlaceholderText(/enter page name/i);
      expect(pageNameInput).toHaveFocus();
    });

    test('supports tab navigation between form elements', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const pageNameInput = screen.getByPlaceholderText(/enter page name/i);
      const pageUrlInput = screen.getByPlaceholderText('/my-dashboard');
      const cancelButton = screen.getByRole('button', { name: 'Cancel' });
      const createButton = screen.getByRole('button', { name: /create & save page/i });
      
      // Initial focus
      expect(pageNameInput).toHaveFocus();
      
      // Tab to next element
      await user.tab();
      expect(pageUrlInput).toHaveFocus();
      
      await user.tab();
      expect(cancelButton).toHaveFocus();
      
      await user.tab();
      expect(createButton).toHaveFocus();
    });

    test('supports Enter key to submit form', async () => {
      mockMutateAsync.mockResolvedValue([{ id: 'test-id', name: 'Test Page', url: '/test-page' }]);
      
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      await user.type(screen.getByPlaceholderText('/my-dashboard'), '/test-page');
      
      // Press Enter to submit
      await user.keyboard('{Enter}');
      
      expect(mockMutateAsync).toHaveBeenCalled();
    });
  });

  describe('Performance and Memory', () => {
    test('cleans up state properly', async () => {
      const { unmount } = renderComponent();
      
      // Open modal and fill form
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      await user.type(screen.getByPlaceholderText(/enter page name/i), 'Test Page');
      
      // Unmount component
      unmount();
      
      // Should not throw any errors during cleanup
      expect(true).toBe(true);
    });

    test('handles rapid form interactions', async () => {
      renderComponent();
      
      await user.click(screen.getByRole('button', { name: /create new page/i }));
      
      const pageNameInput = screen.getByPlaceholderText(/enter page name/i);
      
      // Rapid typing
      await user.type(pageNameInput, 'A');
      await user.type(pageNameInput, 'B');
      await user.type(pageNameInput, 'C');
      
      expect(pageNameInput).toHaveValue('ABC');
    });
  });
});