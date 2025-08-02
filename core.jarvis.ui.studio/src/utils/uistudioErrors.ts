/**
 * UIStudio Error Handling Utilities
 * 
 * Comprehensive error handling for UIStudio API operations with
 * user-friendly messages and retry mechanisms.
 * 
 * @module UIStudioErrors
 */

// ============================================================================
// Error Types
// ============================================================================

/** Base UIStudio error class */
export class UIStudioError extends Error {
  constructor(
    message: string,
    public code?: string,
    public status?: number,
    public details?: Record<string, unknown>
  ) {
    super(message);
    this.name = 'UIStudioError';
  }
}

/** Network-related errors */
export class UIStudioNetworkError extends UIStudioError {
  constructor(message: string, public originalError?: Error) {
    super(message, 'NETWORK_ERROR', 0);
    this.name = 'UIStudioNetworkError';
  }
}

/** Authentication errors */
export class UIStudioAuthError extends UIStudioError {
  constructor(message: string = 'Authentication required') {
    super(message, 'AUTH_ERROR', 401);
    this.name = 'UIStudioAuthError';
  }
}

/** Permission errors */
export class UIStudioPermissionError extends UIStudioError {
  constructor(message: string = 'Insufficient permissions') {
    super(message, 'PERMISSION_ERROR', 403);
    this.name = 'UIStudioPermissionError';
  }
}

/** Resource not found errors */
export class UIStudioNotFoundError extends UIStudioError {
  constructor(resource: string, id?: string) {
    const message = id ? `${resource} with ID ${id} not found` : `${resource} not found`;
    super(message, 'NOT_FOUND', 404);
    this.name = 'UIStudioNotFoundError';
  }
}

/** Validation errors */
export class UIStudioValidationError extends UIStudioError {
  constructor(
    message: string,
    public field?: string,
    public validationErrors?: Array<{ field: string; message: string }>
  ) {
    super(message, 'VALIDATION_ERROR', 400);
    this.name = 'UIStudioValidationError';
  }
}

/** Conflict errors (optimistic concurrency) */
export class UIStudioConflictError extends UIStudioError {
  constructor(message: string = 'Resource has been modified by another user') {
    super(message, 'CONFLICT_ERROR', 409);
    this.name = 'UIStudioConflictError';
  }
}

/** Rate limiting errors */
export class UIStudioRateLimitError extends UIStudioError {
  constructor(retryAfter?: number) {
    const message = retryAfter 
      ? `Rate limit exceeded. Retry after ${retryAfter} seconds.`
      : 'Rate limit exceeded';
    super(message, 'RATE_LIMIT', 429);
    this.name = 'UIStudioRateLimitError';
  }
}

/** Server errors */
export class UIStudioServerError extends UIStudioError {
  constructor(message: string = 'Internal server error') {
    super(message, 'SERVER_ERROR', 500);
    this.name = 'UIStudioServerError';
  }
}

// ============================================================================
// Error Factory
// ============================================================================

/** Create appropriate error from HTTP response */
export function createErrorFromResponse(
  status: number,
  data?: { error?: string; message?: string; details?: Record<string, unknown> }
): UIStudioError {
  const message = data?.message || data?.error || 'Unknown error';
  const details = data?.details;

  switch (status) {
    case 400:
      return new UIStudioValidationError(message, undefined, undefined);
    case 401:
      return new UIStudioAuthError(message);
    case 403:
      return new UIStudioPermissionError(message);
    case 404:
      return new UIStudioNotFoundError(message);
    case 409:
      return new UIStudioConflictError(message);
    case 429:
      return new UIStudioRateLimitError();
    case 500:
    case 502:
    case 503:
    case 504:
      return new UIStudioServerError(message);
    default:
      return new UIStudioError(message, 'UNKNOWN_ERROR', status, details);
  }
}

/** Create network error */
export function createNetworkError(error: Error): UIStudioNetworkError {
  return new UIStudioNetworkError(
    'Network request failed. Please check your connection and try again.',
    error
  );
}

// ============================================================================
// User-Friendly Error Messages
// ============================================================================

/** Error message mappings for user display */
export const ERROR_MESSAGES = {
  // Network errors
  NETWORK_ERROR: 'Connection problem. Please check your internet and try again.',
  TIMEOUT_ERROR: 'Request timed out. Please try again.',
  
  // Authentication errors
  AUTH_ERROR: 'Please log in to continue.',
  INVALID_TOKEN: 'Your session has expired. Please log in again.',
  
  // Permission errors
  PERMISSION_ERROR: 'You don\'t have permission to perform this action.',
  INSUFFICIENT_PERMISSIONS: 'Insufficient permissions for this operation.',
  
  // Resource errors
  NOT_FOUND: 'The requested item could not be found.',
  PAGE_NOT_FOUND: 'Page not found. It may have been deleted or moved.',
  LAYOUT_NOT_FOUND: 'Layout not found. It may have been deleted.',
  BINDING_NOT_FOUND: 'Component binding not found.',
  TEMPLATE_NOT_FOUND: 'Template not found. It may have been deleted.',
  
  // Validation errors
  VALIDATION_ERROR: 'Please check your input and try again.',
  INVALID_PAGE_NAME: 'Page name must be between 1 and 100 characters.',
  INVALID_PAGE_SLUG: 'Page slug must contain only letters, numbers, and dashes.',
  DUPLICATE_PAGE_SLUG: 'A page with this URL already exists.',
  INVALID_COMPONENT_TYPE: 'Invalid component type specified.',
  
  // Conflict errors
  CONFLICT_ERROR: 'This item was modified by someone else. Please refresh and try again.',
  OPTIMISTIC_LOCK_ERROR: 'The item has been updated. Please reload and try again.',
  
  // Server errors
  SERVER_ERROR: 'Something went wrong on our end. Please try again later.',
  MAINTENANCE_MODE: 'System is temporarily under maintenance. Please try again later.',
  
  // Rate limiting
  RATE_LIMIT: 'Too many requests. Please wait a moment and try again.',
  
  // Generic fallback
  UNKNOWN_ERROR: 'An unexpected error occurred. Please try again.'
} as const;

/** Get user-friendly error message */
export function getUserFriendlyMessage(error: UIStudioError): string {
  // Check for specific error codes first
  if (error.code && error.code in ERROR_MESSAGES) {
    return ERROR_MESSAGES[error.code as keyof typeof ERROR_MESSAGES];
  }
  
  // Check for error type patterns
  if (error instanceof UIStudioNetworkError) {
    return ERROR_MESSAGES.NETWORK_ERROR;
  }
  
  if (error instanceof UIStudioAuthError) {
    return ERROR_MESSAGES.AUTH_ERROR;
  }
  
  if (error instanceof UIStudioPermissionError) {
    return ERROR_MESSAGES.PERMISSION_ERROR;
  }
  
  if (error instanceof UIStudioNotFoundError) {
    return ERROR_MESSAGES.NOT_FOUND;
  }
  
  if (error instanceof UIStudioValidationError) {
    return ERROR_MESSAGES.VALIDATION_ERROR;
  }
  
  if (error instanceof UIStudioConflictError) {
    return ERROR_MESSAGES.CONFLICT_ERROR;
  }
  
  if (error instanceof UIStudioRateLimitError) {
    return ERROR_MESSAGES.RATE_LIMIT;
  }
  
  if (error instanceof UIStudioServerError) {
    return ERROR_MESSAGES.SERVER_ERROR;
  }
  
  // Fallback to original message or generic error
  return error.message || ERROR_MESSAGES.UNKNOWN_ERROR;
}

// ============================================================================
// Retry Logic
// ============================================================================

/** Retry configuration */
export interface RetryConfig {
  maxAttempts: number;
  baseDelay: number;
  maxDelay: number;
  exponentialBackoff: boolean;
  retryableErrors: string[];
}

/** Default retry configuration */
export const DEFAULT_RETRY_CONFIG: RetryConfig = {
  maxAttempts: 3,
  baseDelay: 1000, // 1 second
  maxDelay: 10000, // 10 seconds
  exponentialBackoff: true,
  retryableErrors: [
    'NETWORK_ERROR',
    'TIMEOUT_ERROR',
    'SERVER_ERROR',
    'RATE_LIMIT'
  ]
};

/** Check if error is retryable */
export function isRetryableError(error: UIStudioError, config: RetryConfig = DEFAULT_RETRY_CONFIG): boolean {
  if (!error.code) return false;
  return config.retryableErrors.includes(error.code);
}

/** Calculate retry delay with exponential backoff */
export function calculateRetryDelay(
  attempt: number,
  config: RetryConfig = DEFAULT_RETRY_CONFIG
): number {
  if (!config.exponentialBackoff) {
    return config.baseDelay;
  }
  
  const delay = config.baseDelay * Math.pow(2, attempt - 1);
  return Math.min(delay, config.maxDelay);
}

/** Sleep utility for retry delays */
export function sleep(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}

/** Retry wrapper for async operations */
export async function withRetry<T>(
  operation: () => Promise<T>,
  config: RetryConfig = DEFAULT_RETRY_CONFIG
): Promise<T> {
  let lastError: UIStudioError;
  
  for (let attempt = 1; attempt <= config.maxAttempts; attempt++) {
    try {
      return await operation();
    } catch (error) {
      lastError = error instanceof UIStudioError ? error : createNetworkError(error as Error);
      
      // Don't retry if error is not retryable or this is the last attempt
      if (!isRetryableError(lastError, config) || attempt === config.maxAttempts) {
        throw lastError;
      }
      
      // Wait before retrying
      const delay = calculateRetryDelay(attempt, config);
      await sleep(delay);
    }
  }
  
  throw lastError!;
}

// ============================================================================
// Error Reporting
// ============================================================================

/** Error context for debugging */
export interface ErrorContext {
  operation: string;
  resource?: string;
  resourceId?: string;
  userId?: string;
  timestamp: string;
  userAgent?: string;
  url?: string;
  additionalData?: Record<string, unknown>;
}

/** Create error context */
export function createErrorContext(
  operation: string,
  additionalData?: Partial<ErrorContext>
): ErrorContext {
  return {
    operation,
    timestamp: new Date().toISOString(),
    userAgent: typeof navigator !== 'undefined' ? navigator.userAgent : undefined,
    url: typeof window !== 'undefined' ? window.location.href : undefined,
    ...additionalData
  };
}

/** Log error for debugging */
export function logError(error: UIStudioError, context: ErrorContext): void {
  const errorData = {
    error: {
      name: error.name,
      message: error.message,
      code: error.code,
      status: error.status,
      stack: error.stack
    },
    context
  };
  
  console.error('[UIStudio Error]', errorData);
  
  // In production, you might want to send this to an error reporting service
  // Example: errorReportingService.report(errorData);
}

// ============================================================================
// Error Recovery
// ============================================================================

/** Error recovery strategies */
export type ErrorRecoveryStrategy = 
  | 'retry'
  | 'fallback'
  | 'ignore'
  | 'refresh'
  | 'redirect';

/** Error recovery configuration */
export interface ErrorRecoveryConfig {
  strategy: ErrorRecoveryStrategy;
  fallbackData?: unknown;
  redirectUrl?: string;
  showToast?: boolean;
  toastMessage?: string;
}

/** Default recovery configurations by error type */
export const DEFAULT_RECOVERY_CONFIGS: Record<string, ErrorRecoveryConfig> = {
  NETWORK_ERROR: {
    strategy: 'retry',
    showToast: true,
    toastMessage: 'Connection issue. Retrying...'
  },
  
  AUTH_ERROR: {
    strategy: 'redirect',
    redirectUrl: '/login',
    showToast: true,
    toastMessage: 'Please log in to continue'
  },
  
  PERMISSION_ERROR: {
    strategy: 'redirect',
    redirectUrl: '/',
    showToast: true,
    toastMessage: 'Access denied'
  },
  
  NOT_FOUND: {
    strategy: 'fallback',
    fallbackData: null,
    showToast: true,
    toastMessage: 'Item not found'
  },
  
  VALIDATION_ERROR: {
    strategy: 'ignore',
    showToast: true,
    toastMessage: 'Please check your input'
  },
  
  CONFLICT_ERROR: {
    strategy: 'refresh',
    showToast: true,
    toastMessage: 'Data has been updated. Refreshing...'
  },
  
  SERVER_ERROR: {
    strategy: 'retry',
    showToast: true,
    toastMessage: 'Server error. Retrying...'
  }
};

/** Get recovery configuration for error */
export function getRecoveryConfig(error: UIStudioError): ErrorRecoveryConfig {
  if (error.code && error.code in DEFAULT_RECOVERY_CONFIGS) {
    return DEFAULT_RECOVERY_CONFIGS[error.code];
  }
  
  return {
    strategy: 'ignore',
    showToast: true,
    toastMessage: getUserFriendlyMessage(error)
  };
}

// ============================================================================
// Type Guards
// ============================================================================

/** Check if error is a UIStudio error */
export function isUIStudioError(error: unknown): error is UIStudioError {
  return error instanceof UIStudioError;
}

/** Check if error is a network error */
export function isNetworkError(error: unknown): error is UIStudioNetworkError {
  return error instanceof UIStudioNetworkError;
}

/** Check if error is an auth error */
export function isAuthError(error: unknown): error is UIStudioAuthError {
  return error instanceof UIStudioAuthError;
}

/** Check if error is a permission error */
export function isPermissionError(error: unknown): error is UIStudioPermissionError {
  return error instanceof UIStudioPermissionError;
}

/** Check if error is a validation error */
export function isValidationError(error: unknown): error is UIStudioValidationError {
  return error instanceof UIStudioValidationError;
}

/** Check if error is a conflict error */
export function isConflictError(error: unknown): error is UIStudioConflictError {
  return error instanceof UIStudioConflictError;
}