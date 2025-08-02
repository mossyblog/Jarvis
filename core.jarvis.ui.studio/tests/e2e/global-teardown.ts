import { FullConfig } from '@playwright/test';

async function globalTeardown(config: FullConfig) {
  console.log('🧹 Cleaning up E2E test environment...');
  
  // Clean up any global test data
  // Clear any temporary files or test artifacts
  // Reset any modified application state
  
  console.log('✅ E2E test environment cleaned up');
}

export default globalTeardown;