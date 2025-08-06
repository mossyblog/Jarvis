import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { UIStudioHeader } from '../UIStudioHeader';
import { AuthProvider } from '../../../contexts/AuthContext';
import { EditModeProvider } from '../../../contexts/EditModeContext';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';

// Mock the contexts
vi.mock('../../../contexts/AuthContext', () => ({
  AuthProvider: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  useAuth: () => ({
    user: {
      id: 'user-123',
      name: 'John Doe',
      email: 'john.doe@example.com',
      avatar: null,
      roles: [{ id: 'role-1', name: 'Admin' }],
    },
    isAuthenticated: true,
    logout: vi.fn(),
  }),
}));

vi.mock('../../../contexts/EditModeContext', () => ({
  EditModeProvider: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  useEditMode: () => ({
    isEditMode: false,
    toggleEditMode: vi.fn(),
    hasUnsavedChanges: false,
  }),
}));

// Test wrapper component
const TestWrapper = ({ children }: { children: React.ReactNode }) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: Infinity,
      },
    },
  });

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <EditModeProvider>
            {children}
          </EditModeProvider>
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
};

describe('UIStudioHeader', () => {
  const defaultProps = {
    userEntityId: 'user-123',
    onOpenMobileSidebar: vi.fn(),
    onCreatePage: vi.fn(),
    onOpenTemplates: vi.fn(),
    onOpenSearch: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the header with default title', () => {
    render(
      <TestWrapper>
        <UIStudioHeader {...defaultProps} />
      </TestWrapper>
    );

    expect(screen.getByRole('banner')).toBeInTheDocument();
    expect(screen.getByText('UIStudio')).toBeInTheDocument();
  });

  it('renders custom title and subtitle', () => {
    render(
      <TestWrapper>
        <UIStudioHeader 
          {...defaultProps} 
          title="Custom Title" 
          subtitle="Custom Subtitle" 
        />
      </TestWrapper>
    );

    expect(screen.getByText('Custom Title')).toBeInTheDocument();
    expect(screen.getByText('Custom Subtitle')).toBeInTheDocument();
  });

  it('renders user information when authenticated', () => {
    render(
      <TestWrapper>
        <UIStudioHeader {...defaultProps} />
      </TestWrapper>
    );

    // User name should be visible in the header
    expect(screen.getByText('John Doe')).toBeInTheDocument();
    // Role should be visible
    expect(screen.getByText('Admin')).toBeInTheDocument();
  });

  it('calls onCreatePage when create button is clicked', () => {
    render(
      <TestWrapper>
        <UIStudioHeader {...defaultProps} />
      </TestWrapper>
    );

    const createButton = screen.getByLabelText('Create new page');
    fireEvent.click(createButton);
    
    expect(defaultProps.onCreatePage).toHaveBeenCalledTimes(1);
  });

  it('calls onOpenTemplates when templates button is clicked', () => {
    render(
      <TestWrapper>
        <UIStudioHeader {...defaultProps} />
      </TestWrapper>
    );

    const templatesButton = screen.getByLabelText('Browse templates');
    fireEvent.click(templatesButton);
    
    expect(defaultProps.onOpenTemplates).toHaveBeenCalledTimes(1);
  });

  it('calls onOpenMobileSidebar when mobile menu button is clicked', () => {
    render(
      <TestWrapper>
        <UIStudioHeader {...defaultProps} />
      </TestWrapper>
    );

    const mobileMenuButton = screen.getByLabelText('Open navigation menu');
    fireEvent.click(mobileMenuButton);
    
    expect(defaultProps.onOpenMobileSidebar).toHaveBeenCalledTimes(1);
  });

  it('shows notifications badge when there are unread notifications', () => {
    render(
      <TestWrapper>
        <UIStudioHeader {...defaultProps} />
      </TestWrapper>
    );

    // The component has mock notifications, so we should see a badge
    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('renders with project selector when enabled', () => {
    render(
      <TestWrapper>
        <UIStudioHeader {...defaultProps} showProjectSelector={true} />
      </TestWrapper>
    );

    expect(screen.getByText('Current Project')).toBeInTheDocument();
  });

  it('hides action buttons when configured', () => {
    render(
      <TestWrapper>
        <UIStudioHeader 
          {...defaultProps} 
          showActions={{
            createPage: false,
            templates: false,
            search: false,
            notifications: false,
            settings: false,
            help: false,
          }}
        />
      </TestWrapper>
    );

    expect(screen.queryByLabelText('Create new page')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Browse templates')).not.toBeInTheDocument();
  });

  it('has proper ARIA landmarks and labels', () => {
    render(
      <TestWrapper>
        <UIStudioHeader {...defaultProps} />
      </TestWrapper>
    );

    expect(screen.getByRole('banner')).toHaveAttribute('aria-label', 'UIStudio navigation header');
    expect(screen.getByLabelText('Open navigation menu')).toBeInTheDocument();
    expect(screen.getByLabelText('User menu')).toBeInTheDocument();
  });

  it('handles keyboard shortcuts', () => {
    render(
      <TestWrapper>
        <UIStudioHeader {...defaultProps} />
      </TestWrapper>
    );

    // Simulate Ctrl+K for search
    fireEvent.keyDown(document, { key: 'k', ctrlKey: true });
    expect(defaultProps.onOpenSearch).toHaveBeenCalledTimes(1);

    // Simulate Ctrl+N for create page
    fireEvent.keyDown(document, { key: 'n', ctrlKey: true });
    expect(defaultProps.onCreatePage).toHaveBeenCalledTimes(1);
  });

  it('renders correctly with minimal configuration', () => {
    render(
      <TestWrapper>
        <UIStudioHeader 
          userEntityId="user-123"
          onOpenMobileSidebar={defaultProps.onOpenMobileSidebar}
        />
      </TestWrapper>
    );

    expect(screen.getByRole('banner')).toBeInTheDocument();
    expect(screen.getByText('UIStudio')).toBeInTheDocument();
  });
});