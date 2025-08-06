import { chromium, FullConfig } from '@playwright/test';

async function globalSetup(config: FullConfig) {
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();

  // Wait for the application to be ready
  console.log('🚀 Setting up E2E test environment...');
  
  try {
    // Navigate to base URL and wait for it to load
    await page.goto(config.projects[0].use?.baseURL || 'http://localhost:5173');
    
    // Wait for the main app container to be visible
    await page.waitForSelector('#app-container', { timeout: 30000 });
    
    console.log('✅ Application is ready for testing');
    
    // Set up any global test data or authentication tokens here
    // This is where you'd perform login and store authentication state
    
  } catch (error) {
    console.error('❌ Failed to set up test environment:', error);
    throw error;
  } finally {
    await context.close();
    await browser.close();
  }
}

export default globalSetup;