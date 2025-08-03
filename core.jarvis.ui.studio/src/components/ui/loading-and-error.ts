/**
 * Loading and Error State Components
 * 
 * Centralized exports for all loading and error handling components
 * and hooks for consistent imports across the application.
 * 
 * @module LoadingAndError
 */

// Error Boundary Components
export {
  ErrorBoundary,
  withErrorBoundary,
  useErrorHandler,
  PageErrorBoundary,
  SectionErrorBoundary,
  ComponentErrorBoundary,
  QueryErrorBoundary
} from './error-boundary';

// Error Display Components
export {
  ErrorAlert,
  InlineError,
  ErrorPage,
  ErrorToast,
  FieldError,
  EmptyState,
  errorAlertVariants,
  getErrorIcon,
  getErrorVariant
} from './error-display';

// Loading Components
export {
  LoadingSpinner,
  LoadingState,
  LoadingOverlay,
  LoadingButton,
  Pulse,
  LoadingDots,
  ProgressIndicator,
  InlineLoading,
  LoadingText
} from './loading-spinner';

// Skeleton Components
export {
  Skeleton,
  SkeletonText,
  SkeletonCard,
  SkeletonTable,
  SkeletonList,
  SkeletonForm,
  SkeletonGrid,
  SkeletonChart,
  SkeletonNavigation,
  SkeletonPage,
  SkeletonAvatar,
  LoadingSkeleton
} from './skeleton';

// Retry Components
export {
  RetryButton,
  RetryStatus,
  RetryProgress,
  RetryPanel,
  RetryWrapper,
  AutoRetry
} from './retry';

// Retry Hooks
export {
  useRetry,
  useSimpleRetry,
  useManualRetry,
  useAsyncOperation
} from '../../hooks/useRetry';

// Types
export type {
  UseRetryOptions,
  UseRetryState,
  UseSimpleRetryOptions
} from '../../hooks/useRetry';

export type {
  RetryButtonProps,
  RetryStatusProps,
  RetryProgressProps,
  RetryPanelProps,
  RetryWrapperProps,
  AutoRetryProps
} from './retry';