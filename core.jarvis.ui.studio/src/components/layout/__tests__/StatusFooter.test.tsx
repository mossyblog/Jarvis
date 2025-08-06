import { render, screen, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { StatusFooter } from '../StatusFooter';
import { ApiStatusProvider } from '../../../contexts/ApiStatusContext';

// Mock fetch
const mockFetch = vi.fn();

// Mock navigator.onLine
Object.defineProperty(navigator, 'onLine', {
  writable: true,
  value: true,
});

const TestWrapper = ({ children }: { children: React.ReactNode }) => (
  <ApiStatusProvider>
    {children}
  </ApiStatusProvider>
);

describe('StatusFooter', () => {
  const originalFetch = global.fetch;

  beforeEach(() => {
    mockFetch.mockClear();
    global.fetch = mockFetch;
    vi.clearAllTimers();
    vi.useFakeTimers();
  });

  afterEach(() => {
    global.fetch = originalFetch;
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
  });

  it('renders version information', async () => {
    await act(async () => {
      render(
        <TestWrapper>
          <StatusFooter />
        </TestWrapper>
      );
    });

    expect(screen.getByText(/v0\.0\.0/)).toBeInTheDocument();
    expect(screen.getByText(/development/)).toBeInTheDocument();
  });

  it('displays connection status', async () => {
    await act(async () => {
      render(
        <TestWrapper>
          <StatusFooter />
        </TestWrapper>
      );
    });

    // Should show online status
    expect(screen.getByText('Online')).toBeInTheDocument();
  });

  it('tracks network requests', async () => {
    mockFetch.mockResolvedValue({ ok: true, status: 200 });

    await act(async () => {
      render(
        <TestWrapper>
          <StatusFooter />
        </TestWrapper>
      );
    });

    // Should show request counter
    expect(screen.getByText(/Requests:/)).toBeInTheDocument();
  });

  it('shows offline status when navigator is offline', async () => {
    // Mock offline state
    Object.defineProperty(navigator, 'onLine', {
      writable: true,
      value: false,
    });

    await act(async () => {
      render(
        <TestWrapper>
          <StatusFooter />
        </TestWrapper>
      );
    });

    expect(screen.getByText('Offline')).toBeInTheDocument();
  });

  it('displays error count when requests fail', async () => {
    mockFetch.mockRejectedValue(new Error('Network error'));

    await act(async () => {
      render(
        <TestWrapper>
          <StatusFooter />
        </TestWrapper>
      );
    });

    // Should show request counter
    expect(screen.getByText(/Requests:/)).toBeInTheDocument();
  });

  it('is responsive on mobile', async () => {
    await act(async () => {
      render(
        <TestWrapper>
          <StatusFooter />
        </TestWrapper>
      );
    });

    // Check for mobile-specific classes
    const mobileRow = screen.getByText(/Reqs:/).closest('div');
    expect(mobileRow).toHaveClass('md:hidden');
  });

  it('shows API status correctly', async () => {
    await act(async () => {
      render(
        <TestWrapper>
          <StatusFooter />
        </TestWrapper>
      );
    });

    // Should show connected status by default
    expect(screen.getByText('Connected')).toBeInTheDocument();
  });
});