/**
 * PageCreationModal Component Tests
 * 
 * Comprehensive test suite for the PageCreationModal component covering
 * form validation, multi-step navigation, template selection, and API integration.
 */

import React from 'react';
import { render, screen, fireEvent, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';

import { PageCreationModal } from '../PageCreationModal';
import { uistudioApiClient } from '../../../services/api/uistudioApiClient';
import type { UIStudioTemplate, UIStudioPage } from '../../../types/uistudio';

// ============================================================================
// Mock Data
// ============================================================================

const mockTemplates: UIStudioTemplate[] = [
  {
    id: 'template-1',
    ownerEntityId: 'user-123',
    lastUpdated: new Date().toISOString(),
    templateName: 'Dashboard Template',
    description: 'A comprehensive dashboard layout',
    templateType: 'page',
    category: 'Analytics',
    templateData: {},
    isPublic: true,
    usageCount: 15,
    createdByEntityId: 'user-123'
  },
  {
    id: 'template-2',
    ownerEntityId: 'user-123',
    lastUpdated: new Date().toISOString(),
    templateName: 'Report Template',
    description: 'Standard report layout with charts',
    templateType: 'page',
    category: 'Reports',
    templateData: {},
    isPublic: false,
    usageCount: 8,
    createdByEntityId: 'user-123'
  }
];

const mockCreatedPage: UIStudioPage = {
  id: 'page-123',
  ownerEntityId: 'user-123',
  lastUpdated: new Date().toISOString(),
  pageName: 'Test Page',
  pageSlug: 'test-page',
  pageType: 'static',
  description: 'A test page',
  isPublished: false,
  createdAt: new Date().toISOString(),
  createdByEntityId: 'user-123'
};

// ============================================================================
// Mocks
// ============================================================================

// Mock the UIStudio API client
vi.mock('../../../services/api/uistudioApiClient', () => ({
  uistudioApiClient: {
    getTemplatesByOwner: vi.fn(),
    createPage: vi.fn(),
    applyTemplate: vi.fn()
  }
}));

// Mock Lucide React icons to avoid rendering issues in tests
vi.mock('lucide-react', () => ({
  Sparkles: () => <div data-testid="sparkles-icon" />,
  FileText: () => <div data-testid="filetext-icon" />,
  Layout: () => <div data-testid="layout-icon" />,
  Settings: () => <div data-testid="settings-icon" />,
  Check: () => <div data-testid="check-icon" />,
  ChevronLeft: () => <div data-testid="chevron-left-icon" />,
  ChevronRight: () => <div data-testid="chevron-right-icon" />,
  ChevronDown: () => <div data-testid="chevron-down-icon" />,
  AlertTriangle: () => <div data-testid="alert-triangle-icon" />,
  Globe: () => <div data-testid="globe-icon" />,
  Database: () => <div data-testid="database-icon" />,
  Eye: () => <div data-testid="eye-icon" />,
  Star: () => <div data-testid="star-icon" />,
  Users: () => <div data-testid="users-icon" />,
  Zap: () => <div data-testid="zap-icon" />,
  X: () => <div data-testid="x-icon" />,
  XIcon: () => <div data-testid="x-icon" />
}));

// ============================================================================
// Test Utilities
// ============================================================================

const createQueryClient = () => {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: Infinity,
        gcTime: Infinity,
      },
      mutations: {
        retry: false,
      },
    },
  });
};

const renderWithQueryClient = (component: React.ReactElement) => {
  const queryClient = createQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      {component}
    </QueryClientProvider>
  );
};

const defaultProps = {
  isOpen: true,
  onClose: vi.fn(),
  onPageCreated: vi.fn(),
  userEntityId: 'user-123',
  onError: vi.fn()
};

// ============================================================================
// Test Suites
// ============================================================================

describe('PageCreationModal', () => {
  let user: ReturnType<typeof userEvent.setup>;

  beforeEach(() => {
    user = userEvent.setup();
    vi.clearAllMocks();
    
    // Setup default API responses
    vi.mocked(uistudioApiClient.getTemplatesByOwner).mockResolvedValue(mockTemplates);
    vi.mocked(uistudioApiClient.createPage).mockResolvedValue([mockCreatedPage]);
    vi.mocked(uistudioApiClient.applyTemplate).mockResolvedValue([mockCreatedPage]);
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  // ==========================================================================
  // Rendering Tests
  // ==========================================================================

  describe('Rendering', () => {
    it('should render the modal when open', () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      expect(screen.getByRole('dialog')).toBeInTheDocument();
      expect(screen.getByText('Create New Page')).toBeInTheDocument();
    });

    it('should not render the modal when closed', () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} isOpen={false} />);
      
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    it('should render step indicators', () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      expect(screen.getByText('Basic Information')).toBeInTheDocument();
      expect(screen.getByText('Template Selection')).toBeInTheDocument();
      expect(screen.getByText('Configuration')).toBeInTheDocument();
    });
  });

  // ==========================================================================
  // Form Validation Tests
  // ==========================================================================

  describe('Form Validation', () => {
    it('should validate required fields on step 1', async () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      const nextButton = screen.getByRole('button', { name: /next/i });
      await user.click(nextButton);
      
      expect(screen.getByText('Page name is required')).toBeInTheDocument();
      expect(screen.getByText('Page slug is required')).toBeInTheDocument();
    });

    it('should auto-generate slug from page name', async () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      const pageNameInput = screen.getByLabelText(/page name/i);
      const pageSlugInput = screen.getByLabelText(/page url/i);
      
      await user.type(pageNameInput, 'My Awesome Page');
      
      await waitFor(() => {
        expect(pageSlugInput).toHaveValue('my-awesome-page');
      });
    });
  });

  // ==========================================================================
  // Multi-Step Navigation Tests
  // ==========================================================================

  describe('Multi-Step Navigation', () => {
    it('should navigate to next step when form is valid', async () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      // Fill out basic info
      await user.type(screen.getByLabelText(/page name/i), 'Test Page');
      await user.type(screen.getByLabelText(/page url/i), 'test-page');
      
      // Navigate to next step
      const nextButton = screen.getByRole('button', { name: /next/i });
      await user.click(nextButton);
      
      await waitFor(() => {
        expect(screen.getByText('Choose a Template')).toBeInTheDocument();
      });
    });

    it('should not navigate to next step when form is invalid', async () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      const nextButton = screen.getByRole('button', { name: /next/i });
      await user.click(nextButton);
      
      // Should stay on first step
      expect(screen.getByLabelText(/page name/i)).toBeInTheDocument();
      expect(screen.queryByText('Choose a Template')).not.toBeInTheDocument();
    });

    it('should disable previous button on first step', () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      const previousButton = screen.getByRole('button', { name: /previous/i });
      expect(previousButton).toBeDisabled();
    });
  });

  // ==========================================================================
  // Template Selection Tests
  // ==========================================================================

  describe('Template Selection', () => {
    it('should load and display templates', async () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      // Navigate to template selection step
      await user.type(screen.getByLabelText(/page name/i), 'Test Page');
      await user.type(screen.getByLabelText(/page url/i), 'test-page');
      await user.click(screen.getByRole('button', { name: /next/i }));
      
      await waitFor(() => {
        expect(screen.getByText('Dashboard Template')).toBeInTheDocument();
        expect(screen.getByText('Report Template')).toBeInTheDocument();
      });
    });

    it('should show "Start from Scratch" option', async () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      // Navigate to template selection
      await user.type(screen.getByLabelText(/page name/i), 'Test Page');
      await user.click(screen.getByRole('button', { name: /next/i }));
      
      await waitFor(() => {
        expect(screen.getByText('Start from Scratch')).toBeInTheDocument();
      });
    });

    it('should select a template', async () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      // Navigate to template selection
      await user.type(screen.getByLabelText(/page name/i), 'Test Page');
      await user.click(screen.getByRole('button', { name: /next/i }));
      
      await waitFor(() => {
        expect(screen.getByText('Dashboard Template')).toBeInTheDocument();
      });
      
      // Select template
      const templateCard = screen.getByText('Dashboard Template').closest('div[class*="cursor-pointer"]');
      if (templateCard) {
        await user.click(templateCard);
        expect(templateCard).toHaveClass('ring-2', 'ring-primary');
      }
    });
  });

  // ==========================================================================
  // API Integration Tests
  // ==========================================================================

  describe('API Integration', () => {
    it('should create page without template', async () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      // Fill form completely
      await user.type(screen.getByLabelText(/page name/i), 'Test Page');
      await user.type(screen.getByLabelText(/page url/i), 'test-page');
      await user.type(screen.getByLabelText(/description/i), 'Test description');
      
      // Navigate through steps
      await user.click(screen.getByRole('button', { name: /next/i }));
      await user.click(screen.getByRole('button', { name: /next/i }));
      
      // Submit form
      await waitFor(() => {
        const createButton = screen.getByRole('button', { name: /create page/i });
        expect(createButton).toBeInTheDocument();
      });
      
      const createButton = screen.getByRole('button', { name: /create page/i });
      await user.click(createButton);
      
      await waitFor(() => {
        expect(uistudioApiClient.createPage).toHaveBeenCalledWith({
          pageName: 'Test Page',
          pageSlug: 'test-page',
          pageType: 'static',
          description: 'Test description',
          createdByEntityId: 'user-123',
          tags: '',
          metadata: {}
        });
      });
    });

    it('should handle API errors', async () => {
      vi.mocked(uistudioApiClient.createPage).mockRejectedValue(new Error('API Error'));
      
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      // Fill form and submit
      await user.type(screen.getByLabelText(/page name/i), 'Test Page');
      await user.click(screen.getByRole('button', { name: /next/i }));
      await user.click(screen.getByRole('button', { name: /next/i }));
      
      const createButton = screen.getByRole('button', { name: /create page/i });
      await user.click(createButton);
      
      await waitFor(() => {
        expect(screen.getByText('Failed to create page')).toBeInTheDocument();
      });
    });
  });

  // ==========================================================================
  // Event Handler Tests
  // ==========================================================================

  describe('Event Handlers', () => {
    it('should call onClose when cancel is clicked', async () => {
      const onClose = vi.fn();
      renderWithQueryClient(<PageCreationModal {...defaultProps} onClose={onClose} />);
      
      const cancelButton = screen.getByRole('button', { name: /cancel/i });
      await user.click(cancelButton);
      
      expect(onClose).toHaveBeenCalled();
    });

    it('should call onPageCreated when page is successfully created', async () => {
      const onPageCreated = vi.fn();
      renderWithQueryClient(<PageCreationModal {...defaultProps} onPageCreated={onPageCreated} />);
      
      // Fill form and submit
      await user.type(screen.getByLabelText(/page name/i), 'Test Page');
      await user.click(screen.getByRole('button', { name: /next/i }));
      await user.click(screen.getByRole('button', { name: /next/i }));
      
      const createButton = screen.getByRole('button', { name: /create page/i });
      await user.click(createButton);
      
      await waitFor(() => {
        expect(onPageCreated).toHaveBeenCalledWith(mockCreatedPage);
      });
    });
  });

  // ==========================================================================
  // Accessibility Tests
  // ==========================================================================

  describe('Accessibility', () => {
    it('should have proper ARIA labels', () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      expect(screen.getByRole('dialog')).toBeInTheDocument();
      expect(screen.getByLabelText(/page name/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/page url/i)).toBeInTheDocument();
    });

    it('should be navigable with keyboard', async () => {
      renderWithQueryClient(<PageCreationModal {...defaultProps} />);
      
      const pageNameInput = screen.getByLabelText(/page name/i);
      
      // Tab to page name input
      await user.tab();
      expect(pageNameInput).toHaveFocus();
      
      // Tab to page slug input
      await user.tab();
      expect(screen.getByLabelText(/page url/i)).toHaveFocus();
    });
  });
});