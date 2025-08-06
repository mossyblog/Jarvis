/**
 * QuickActionsPanel Component Tests
 * 
 * Unit tests for the QuickActionsPanel component covering:
 * - Rendering and basic functionality
 * - Keyboard shortcuts
 * - Import/export functionality
 * - Accessibility features
 */

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import userEvent from '@testing-library/user-event';
import { QuickActionsPanel } from './QuickActionsPanel';
import { KeyboardNavigationProvider } from '../keyboard/KeyboardNavigationProvider';

// Mock the navigation hook
const mockNavigate = vi.fn();
vi.mock('react-router-dom', () => ({
  useNavigate: () => mockNavigate,
}));

// Test wrapper with required providers
const TestWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <KeyboardNavigationProvider>
    {children}
  </KeyboardNavigationProvider>
);

describe('QuickActionsPanel', () => {
  const defaultProps = {
    userEntityId: 'test-user-123',
    onCreatePage: vi.fn(),
    onOpenTemplates: vi.fn(),
    onImport: vi.fn(),
    onExport: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    it('renders the quick actions panel with title', () => {
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} />
        </TestWrapper>
      );

      expect(screen.getByText('Quick Actions')).toBeInTheDocument();
      expect(screen.getByText('Get started with common tasks and shortcuts')).toBeInTheDocument();
    });

    it('renders all primary action buttons', () => {
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} />
        </TestWrapper>
      );

      expect(screen.getByText('Create New Page')).toBeInTheDocument();
      expect(screen.getByText('Browse Templates')).toBeInTheDocument();
      expect(screen.getByText('Import Pages')).toBeInTheDocument();
      expect(screen.getByText('Export Pages')).toBeInTheDocument();
    });

    it('renders additional actions section', () => {
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} />
        </TestWrapper>
      );

      expect(screen.getByText('Explore & Learn')).toBeInTheDocument();
      expect(screen.getByText('Quick Start Guide')).toBeInTheDocument();
      expect(screen.getByText('View Examples')).toBeInTheDocument();
      expect(screen.getByText('Component Library')).toBeInTheDocument();
      expect(screen.getByText('Design System')).toBeInTheDocument();
    });
  });

  describe('Keyboard Shortcuts', () => {
    it('shows keyboard shortcut hints when enabled', () => {
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} showShortcuts={true} />
        </TestWrapper>
      );

      expect(screen.getByText('Shortcuts enabled')).toBeInTheDocument();
      expect(screen.getByText('⌘N')).toBeInTheDocument();
      expect(screen.getByText('⌘T')).toBeInTheDocument();
    });

    it('hides keyboard shortcut hints when disabled', () => {
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} showShortcuts={false} />
        </TestWrapper>
      );

      expect(screen.queryByText('Shortcuts enabled')).not.toBeInTheDocument();
      expect(screen.queryByText('⌘N')).not.toBeInTheDocument();
    });

    it('triggers create page action with Ctrl+N', async () => {
      const user = userEvent.setup();
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} />
        </TestWrapper>
      );

      await user.keyboard('{Control>}n{/Control}');
      expect(defaultProps.onCreatePage).toHaveBeenCalled();
    });

    it('triggers templates action with Ctrl+T', async () => {
      const user = userEvent.setup();
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} />
        </TestWrapper>
      );

      await user.keyboard('{Control>}t{/Control}');
      expect(defaultProps.onOpenTemplates).toHaveBeenCalled();
    });
  });

  describe('Action Interactions', () => {
    it('calls onCreatePage when create button is clicked', async () => {
      const user = userEvent.setup();
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} />
        </TestWrapper>
      );

      await user.click(screen.getByText('Create New Page'));
      expect(defaultProps.onCreatePage).toHaveBeenCalled();
    });

    it('calls onOpenTemplates when templates button is clicked', async () => {
      const user = userEvent.setup();
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} />
        </TestWrapper>
      );

      await user.click(screen.getByText('Browse Templates'));
      expect(defaultProps.onOpenTemplates).toHaveBeenCalled();
    });

    it('opens import dialog when import button is clicked', async () => {
      const user = userEvent.setup();
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} />
        </TestWrapper>
      );

      await user.click(screen.getByText('Import Pages'));
      expect(screen.getByText('Select a file to import pages. Supported formats: JSON, ZIP')).toBeInTheDocument();
    });

    it('opens export dialog when export button is clicked', async () => {
      const user = userEvent.setup();
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} />
        </TestWrapper>
      );

      await user.click(screen.getByText('Export Pages'));
      expect(screen.getByText('Choose the export format for your pages and data')).toBeInTheDocument();
    });
  });

  describe('Import/Export Functionality', () => {
    it('handles file import correctly', async () => {
      const user = userEvent.setup();
      const mockFile = new File(['test content'], 'test.json', { type: 'application/json' });
      const onImport = vi.fn().mockResolvedValue(undefined);

      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} onImport={onImport} />
        </TestWrapper>
      );

      // Open import dialog
      await user.click(screen.getByText('Import Pages'));
      
      // Click choose file button
      await user.click(screen.getByText('Choose File'));
      
      // Simulate file selection
      const fileInput = screen.getByRole('button', { name: /choose file/i }).parentElement?.querySelector('input[type="file"]');
      if (fileInput) {
        await user.upload(fileInput, mockFile);
        await waitFor(() => {
          expect(onImport).toHaveBeenCalledWith(mockFile);
        });
      }
    });

    it('handles export format selection', async () => {
      const user = userEvent.setup();
      const onExport = vi.fn().mockResolvedValue(undefined);

      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} onExport={onExport} />
        </TestWrapper>
      );

      // Open export dialog
      await user.click(screen.getByText('Export Pages'));
      
      // Click JSON export
      await user.click(screen.getByText('JSON'));
      
      await waitFor(() => {
        expect(onExport).toHaveBeenCalledWith('json');
      });
    });
  });

  describe('Loading States', () => {
    it('shows loading state for creating page', () => {
      render(
        <TestWrapper>
          <QuickActionsPanel 
            {...defaultProps} 
            loading={{ creating: true }} 
          />
        </TestWrapper>
      );

      const createCard = screen.getByText('Create New Page').closest('[role="button"]');
      expect(createCard).toHaveClass('opacity-50');
    });

    it('shows loading state for importing', () => {
      render(
        <TestWrapper>
          <QuickActionsPanel 
            {...defaultProps} 
            loading={{ importing: true }} 
          />
        </TestWrapper>
      );

      expect(screen.getByText('Importing...')).toBeInTheDocument();
    });

    it('shows loading state for exporting', () => {
      render(
        <TestWrapper>
          <QuickActionsPanel 
            {...defaultProps} 
            loading={{ exporting: true }} 
          />
        </TestWrapper>
      );

      expect(screen.getByText('Exporting...')).toBeInTheDocument();
    });
  });

  describe('Error States', () => {
    it('displays import error message', async () => {
      const user = userEvent.setup();
      render(
        <TestWrapper>
          <QuickActionsPanel 
            {...defaultProps} 
            errors={{ import: 'Import failed' }} 
          />
        </TestWrapper>
      );

      await user.click(screen.getByText('Import Pages'));
      expect(screen.getByText('Import failed')).toBeInTheDocument();
    });

    it('displays export error message', async () => {
      const user = userEvent.setup();
      render(
        <TestWrapper>
          <QuickActionsPanel 
            {...defaultProps} 
            errors={{ export: 'Export failed' }} 
          />
        </TestWrapper>
      );

      await user.click(screen.getByText('Export Pages'));
      expect(screen.getByText('Export failed')).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('has proper ARIA labels and roles', () => {
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} />
        </TestWrapper>
      );

      expect(screen.getByRole('region', { name: 'Quick Actions Panel' })).toBeInTheDocument();
      expect(screen.getByRole('group', { name: 'Primary quick actions' })).toBeInTheDocument();
    });

    it('supports keyboard navigation on action cards', async () => {
      const user = userEvent.setup();
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} />
        </TestWrapper>
      );

      const createCard = screen.getByText('Create New Page').closest('[role="button"]');
      if (createCard) {
        createCard.focus();
        await user.keyboard('{Enter}');
        expect(defaultProps.onCreatePage).toHaveBeenCalled();
      }
    });

    it('has proper focus management', async () => {
      const user = userEvent.setup();
      render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} />
        </TestWrapper>
      );

      const createCard = screen.getByText('Create New Page').closest('[role="button"]');
      if (createCard) {
        await user.tab();
        expect(createCard).toHaveFocus();
      }
    });
  });

  describe('Layout Variants', () => {
    it('applies correct classes for grid variant', () => {
      const { container } = render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} variant="grid" />
        </TestWrapper>
      );

      expect(container.querySelector('.grid')).toBeInTheDocument();
    });

    it('applies correct classes for horizontal variant', () => {
      const { container } = render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} variant="horizontal" />
        </TestWrapper>
      );

      expect(container.querySelector('.flex-row')).toBeInTheDocument();
    });

    it('applies correct classes for vertical variant', () => {
      const { container } = render(
        <TestWrapper>
          <QuickActionsPanel {...defaultProps} variant="vertical" />
        </TestWrapper>
      );

      expect(container.querySelector('.flex-col')).toBeInTheDocument();
    });
  });
});