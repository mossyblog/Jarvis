// Quick dashboard sizing analysis
const { chromium } = require('playwright');

async function quickAnalysis() {
  let browser;
  try {
    console.log('🚀 Starting quick dashboard analysis...');
    browser = await chromium.launch({ headless: false });
    const page = await browser.newPage();
    
    // Set viewport
    await page.setViewportSize({ width: 1920, height: 1080 });
    
    console.log('🌐 Navigating to dashboard...');
    await page.goto('http://localhost:5173', { 
      waitUntil: 'networkidle',
      timeout: 10000 
    });
    
    // Wait for content
    await page.waitForTimeout(2000);
    
    console.log('📸 Taking screenshot...');
    await page.screenshot({ 
      path: 'dashboard-live-screenshot.png', 
      fullPage: true 
    });
    
    console.log('🔍 Analyzing element sizes...');
    const sizes = await page.evaluate(() => {
      const results = {
        icons: [],
        buttons: [],
        headers: [],
        spacing: []
      };
      
      // Get all SVG icons
      document.querySelectorAll('svg').forEach((svg, i) => {
        const rect = svg.getBoundingClientRect();
        const style = getComputedStyle(svg);
        const parent = svg.closest('button') || svg.parentElement;
        
        results.icons.push({
          index: i,
          width: Math.round(rect.width),
          height: Math.round(rect.height),
          computedWidth: style.width,
          computedHeight: style.height,
          context: parent?.getAttribute('aria-label') || parent?.textContent?.trim() || 'unknown',
          classes: svg.className.baseVal || svg.className
        });
      });
      
      // Get all buttons
      document.querySelectorAll('button').forEach((btn, i) => {
        const rect = btn.getBoundingClientRect();
        const style = getComputedStyle(btn);
        
        results.buttons.push({
          index: i,
          width: Math.round(rect.width),
          height: Math.round(rect.height),
          padding: style.padding,
          text: btn.textContent?.trim() || btn.getAttribute('aria-label') || '',
          classes: btn.className
        });
      });
      
      // Get header
      const header = document.querySelector('header');
      if (header) {
        const rect = header.getBoundingClientRect();
        const style = getComputedStyle(header);
        
        results.headers.push({
          tag: 'header',
          width: Math.round(rect.width),
          height: Math.round(rect.height),
          padding: style.padding,
          classes: header.className
        });
      }
      
      return results;
    });
    
    console.log('\n📊 ANALYSIS RESULTS:');
    console.log('==================');
    
    console.log('\n🔤 HEADER ANALYSIS:');
    sizes.headers.forEach(h => {
      console.log(`  Header: ${h.width}x${h.height}px, padding: ${h.padding}`);
    });
    
    console.log('\n🎯 ICON SIZE BREAKDOWN:');
    const iconSizeGroups = {};
    sizes.icons.forEach(icon => {
      const key = `${icon.width}x${icon.height}`;
      if (!iconSizeGroups[key]) iconSizeGroups[key] = [];
      iconSizeGroups[key].push(icon);
    });
    
    Object.entries(iconSizeGroups).forEach(([size, icons]) => {
      console.log(`  ${size}px: ${icons.length} icons`);
      icons.slice(0, 3).forEach(icon => {
        console.log(`    - ${icon.context} (classes: ${icon.classes})`);
      });
    });
    
    console.log('\n🔘 BUTTON SIZE BREAKDOWN:');
    const buttonSizeGroups = {};
    sizes.buttons.forEach(btn => {
      const key = `h${btn.height}`;
      if (!buttonSizeGroups[key]) buttonSizeGroups[key] = [];
      buttonSizeGroups[key].push(btn);
    });
    
    Object.entries(buttonSizeGroups).forEach(([height, buttons]) => {
      console.log(`  ${height}px: ${buttons.length} buttons`);
      buttons.slice(0, 3).forEach(btn => {
        console.log(`    - "${btn.text}" (w:${btn.width}px, padding:${btn.padding})`);
      });
    });
    
    // Save detailed results
    require('fs').writeFileSync('dashboard-analysis-results.json', JSON.stringify(sizes, null, 2));
    console.log('\n💾 Detailed results saved to dashboard-analysis-results.json');
    console.log('📸 Screenshot saved to dashboard-live-screenshot.png');
    
  } catch (error) {
    console.error('❌ Error:', error.message);
  } finally {
    if (browser) await browser.close();
  }
}

quickAnalysis();