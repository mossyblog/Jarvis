/**
 * UIStudio Interface Components
 * 
 * Barrel export for all interface components used in the UIStudio application.
 * These components provide complete user interface workflows for page creation,
 * editing, and management.
 * 
 * @module InterfaceComponents
 */

export { UIStudioInterface, type UIStudioInterfaceProps } from './UIStudioInterface';
export { UIStudioFooter, type UIStudioFooterProps } from './UIStudioFooter';

// Export all interface-related types for convenience
export type {
  UIStudioViewState,
  UIStudioFilters,
  UIStudioSelectionState,
  UIStudioModalState,
  UIStudioCache,
  UIStudioApiResult,
  UIStudioMutationResult,
  UIStudioPaginatedResult,
  UIStudioSearchResult,
  UIStudioInterfaceState
} from './UIStudioInterface';

// Export UIStudioFooter types
export type {
  ConnectionStatus,
  BuildStatus,
  HealthStatus,
  StatusIndicator,
  SystemMetrics,
  UseSystemStatusReturn
} from './UIStudioFooter';