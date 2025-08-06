const { chromium } = require('playwright');
const path = require('path');

async function captureDashboardScreenshot() {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  try {
    // Set viewport for consistent screenshots
    await page.setViewportSize({ width: 1920, height: 1080 });
    
    // Navigate to the dashboard
    await page.goto('http://localhost:5173', { waitUntil: 'networkidle' });
    
    // Wait for any loading states to complete
    await page.waitForTimeout(2000);
    
    // Take full page screenshot
    const screenshotPath = path.join(__dirname, 'dashboard-screenshot.png');
    await page.screenshot({ 
      path: screenshotPath, 
      fullPage: true,
      type: 'png'
    });
    
    console.log(`Screenshot saved to: ${screenshotPath}`);
    
    // Also capture just the viewport for analysis
    const viewportScreenshotPath = path.join(__dirname, 'dashboard-viewport-screenshot.png');
    await page.screenshot({ 
      path: viewportScreenshotPath, 
      fullPage: false,
      type: 'png'
    });
    
    console.log(`Viewport screenshot saved to: ${viewportScreenshotPath}`);
    
  } catch (error) {
    console.error('Error capturing screenshot:', error);
  } finally {
    await browser.close();
  }
}

captureDashboardScreenshot();