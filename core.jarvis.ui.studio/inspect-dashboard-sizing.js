const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

async function inspectDashboardSizing() {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  try {
    // Set viewport for consistent analysis
    await page.setViewportSize({ width: 1920, height: 1080 });
    
    // Navigate to the dashboard
    console.log('🌐 Navigating to dashboard...');
    await page.goto('http://localhost:5173', { waitUntil: 'networkidle' });
    
    // Wait for content to load
    await page.waitForTimeout(3000);
    
    // Take screenshot first
    console.log('📸 Capturing screenshots...');
    const screenshotPath = path.join(__dirname, 'dashboard-analysis-screenshot.png');
    await page.screenshot({ 
      path: screenshotPath, 
      fullPage: true,
      type: 'png'
    });
    
    console.log(`Screenshot saved to: ${screenshotPath}`);
    
    // Analyze various UI elements and their computed styles
    console.log('🔍 Analyzing UI element sizes...');
    
    const analysis = await page.evaluate(() => {
      const results = {
        header: {},
        toolbar: {},
        icons: {},
        buttons: {},
        cards: {},
        typography: {},
        spacing: {},
        elements: []
      };
      
      // Helper function to get computed styles
      const getComputedStyles = (element) => {
        const styles = window.getComputedStyle(element);
        return {
          width: styles.width,
          height: styles.height,
          padding: styles.padding,
          margin: styles.margin,
          fontSize: styles.fontSize,
          lineHeight: styles.lineHeight,
          borderRadius: styles.borderRadius,
          boxShadow: styles.boxShadow
        };
      };
      
      // Analyze header elements
      const header = document.querySelector('header');
      if (header) {
        results.header = {
          tag: 'header',
          computed: getComputedStyles(header),
          classes: header.className
        };
      }
      
      // Analyze all icons (SVG elements)
      const icons = document.querySelectorAll('svg');
      icons.forEach((icon, index) => {
        const computed = getComputedStyles(icon);
        const parent = icon.closest('button') || icon.parentElement;
        const context = parent ? parent.getAttribute('aria-label') || parent.textContent?.trim() || 'unknown' : 'unknown';
        
        results.icons[`icon_${index}`] = {
          width: computed.width,
          height: computed.height,
          context: context,
          classes: icon.className,
          parentClasses: parent?.className || ''
        };
      });
      
      // Analyze buttons
      const buttons = document.querySelectorAll('button');
      buttons.forEach((button, index) => {
        const computed = getComputedStyles(button);
        const text = button.textContent?.trim() || button.getAttribute('aria-label') || '';
        
        results.buttons[`button_${index}`] = {
          text: text,
          computed: computed,
          classes: button.className
        };
      });
      
      // Analyze cards/metric cards
      const cards = document.querySelectorAll('[class*="card"], [data-testid*="metric"]');
      cards.forEach((card, index) => {
        const computed = getComputedStyles(card);
        const title = card.querySelector('h2, h3, [class*="title"]')?.textContent || '';
        
        results.cards[`card_${index}`] = {
          title: title,
          computed: computed,
          classes: card.className
        };
      });
      
      // Analyze typography elements
      const typographyElements = document.querySelectorAll('h1, h2, h3, h4, h5, h6, p, span[class*="text"], [class*="typography"]');
      typographyElements.forEach((element, index) => {
        const computed = getComputedStyles(element);
        const text = element.textContent?.trim().substring(0, 50) || '';
        
        results.typography[`text_${index}`] = {
          tag: element.tagName.toLowerCase(),
          text: text,
          fontSize: computed.fontSize,
          lineHeight: computed.lineHeight,
          classes: element.className
        };
      });
      
      // Find elements with t-shirt size classes
      const tshirtSizeElements = document.querySelectorAll('[class*="-xs"], [class*="-sm"], [class*="-md"], [class*="-lg"], [class*="-xl"]');
      tshirtSizeElements.forEach((element, index) => {
        const computed = getComputedStyles(element);
        const classes = element.className;
        const tshirtClasses = classes.split(' ').filter(cls => 
          cls.includes('-xs') || cls.includes('-sm') || cls.includes('-md') || 
          cls.includes('-lg') || cls.includes('-xl') || cls.includes('-2xl') || cls.includes('-3xl')
        );
        
        results.elements.push({
          index,
          tag: element.tagName.toLowerCase(),
          tshirtClasses: tshirtClasses,
          allClasses: classes,
          computed: computed,
          text: element.textContent?.trim().substring(0, 30) || ''
        });
      });
      
      return results;
    });
    
    // Save analysis to JSON file
    const analysisPath = path.join(__dirname, 'dashboard-sizing-analysis.json');
    fs.writeFileSync(analysisPath, JSON.stringify(analysis, null, 2));
    console.log(`Analysis saved to: ${analysisPath}`);
    
    // Create a detailed wireframe document
    const wireframe = generateWireframeDocument(analysis);
    const wireframePath = path.join(__dirname, 'DASHBOARD_SIZING_WIREFRAME.md');
    fs.writeFileSync(wireframePath, wireframe);
    console.log(`Wireframe document saved to: ${wireframePath}`);
    
    // Print summary to console
    console.log('\n🎯 SIZING ANALYSIS SUMMARY:');
    console.log('=====================================');
    
    // Analyze icon sizes
    const iconSizes = Object.values(analysis.icons).map(icon => ({
      width: parseInt(icon.width),
      height: parseInt(icon.height),
      context: icon.context
    }));
    
    console.log('\n📐 ICON SIZES FOUND:');
    const uniqueIconSizes = [...new Set(iconSizes.map(i => `${i.width}x${i.height}`))];
    uniqueIconSizes.forEach(size => {
      const examples = iconSizes.filter(i => `${i.width}x${i.height}` === size);
      console.log(`  ${size}px: ${examples.length} icons (contexts: ${examples.slice(0, 3).map(e => e.context).join(', ')})`);
    });
    
    // Analyze button heights
    const buttonHeights = Object.values(analysis.buttons)
      .map(btn => parseInt(btn.computed.height))
      .filter(h => !isNaN(h));
    const uniqueButtonHeights = [...new Set(buttonHeights)];
    
    console.log('\n🔘 BUTTON HEIGHTS FOUND:');
    uniqueButtonHeights.forEach(height => {
      const count = buttonHeights.filter(h => h === height).length;
      console.log(`  ${height}px: ${count} buttons`);
    });
    
    // Analyze t-shirt size usage
    const tshirtUsage = {};
    analysis.elements.forEach(element => {
      element.tshirtClasses.forEach(cls => {
        if (!tshirtUsage[cls]) tshirtUsage[cls] = 0;
        tshirtUsage[cls]++;
      });
    });
    
    console.log('\n👕 T-SHIRT SIZE CLASS USAGE:');
    Object.entries(tshirtUsage)
      .sort(([,a], [,b]) => b - a)
      .forEach(([cls, count]) => {
        console.log(`  ${cls}: ${count} uses`);
      });
    
  } catch (error) {
    console.error('❌ Error during analysis:', error);
  } finally {
    await browser.close();
  }
}

function generateWireframeDocument(analysis) {
  return `# Dashboard T-Shirt Sizing Wireframe Analysis

## Executive Summary

This document provides a comprehensive analysis of the current UI element sizing in the dashboard, based on computed CSS values from the live application.

## Icon Size Analysis

### Current Icon Sizes in Use:
${Object.entries(groupBy(Object.values(analysis.icons), icon => \`\${icon.width}x\${icon.height}\`))
  .map(([size, icons]) => \`
**\${size}px**
- Count: \${icons.length} icons
- Contexts: \${icons.map(i => i.context).join(', ')}
- Current T-shirt classification: \${classifyIconSize(size)}
\`)
  .join('')}

## Button Size Analysis

### Current Button Heights:
${Object.entries(groupBy(Object.values(analysis.buttons), btn => parseInt(btn.computed.height)))
  .map(([height, buttons]) => \`
**\${height}px height**
- Count: \${buttons.length} buttons
- Current T-shirt classification: \${classifyButtonHeight(height)}
- Examples: \${buttons.slice(0, 3).map(b => b.text || 'unlabeled').join(', ')}
\`)
  .join('')}

## Typography Analysis

### Font Sizes in Use:
${Object.entries(groupBy(Object.values(analysis.typography), text => text.fontSize))
  .map(([fontSize, elements]) => \`
**\${fontSize}**
- Count: \${elements.length} elements
- Elements: \${elements.slice(0, 3).map(e => \`\${e.tag}(\${e.text.substring(0, 20)}...)\`).join(', ')}
\`)
  .join('')}

## T-Shirt Size Recommendations

### Icons
Based on context analysis:
- **Toolbar/Header icons**: Should be **sm (20px)** - Currently: Mixed sizes
- **Content icons**: Should be **md (24px)** - Currently: Mixed sizes
- **Large feature icons**: Should be **lg (32px)** - Currently: Mixed sizes

### Buttons
Based on usage context:
- **Toolbar buttons**: Should be **sm (40px height)** - Currently: Mixed heights
- **Primary action buttons**: Should be **md (48px height)** - Currently: Mixed heights
- **Secondary buttons**: Should be **sm (40px height)** - Currently: Mixed heights

### Cards
- **Metric cards**: Should use **md** padding and spacing
- **Content cards**: Should use **lg** padding for better readability

## Inconsistencies Found

${generateInconsistencyReport(analysis)}

## Recommended Changes

### Immediate Actions:
1. **Standardize toolbar icons to 20px (sm)**
2. **Standardize content icons to 24px (md)**
3. **Implement consistent button heights based on context**
4. **Apply consistent card padding using t-shirt sizes**

### Implementation Strategy:
1. Update component default props to use t-shirt sizes
2. Create specific size variants for different contexts
3. Remove hardcoded pixel values in favor of CSS custom properties
4. Add design system documentation for size guidelines

---
*Generated on: \${new Date().toISOString()}*
`;
}

function groupBy(array, keyFunction) {
  return array.reduce((result, item) => {
    const key = keyFunction(item);
    if (!result[key]) result[key] = [];
    result[key].push(item);
    return result;
  }, {});
}

function classifyIconSize(sizeString) {
  const [width] = sizeString.split('x').map(Number);
  if (width <= 16) return 'xs (should be 16px)';
  if (width <= 20) return 'sm (correct at 20px)';
  if (width <= 24) return 'md (correct at 24px)';
  if (width <= 32) return 'lg (correct at 32px)';
  return 'xl+ (consider if appropriate)';
}

function classifyButtonHeight(height) {
  const h = parseInt(height);
  if (h <= 32) return 'xs (32px)';
  if (h <= 40) return 'sm (40px)';
  if (h <= 48) return 'md (48px)';
  if (h <= 56) return 'lg (56px)';
  return 'xl+ (consider if appropriate)';
}

function generateInconsistencyReport(analysis) {
  const issues = [];
  
  // Check for icon size inconsistencies
  const iconSizes = Object.values(analysis.icons).map(i => \`\${i.width}x\${i.height}\`);
  const uniqueIconSizes = [...new Set(iconSizes)];
  if (uniqueIconSizes.length > 4) {
    issues.push(\`**Too many icon sizes**: Found \${uniqueIconSizes.length} different icon sizes, recommend max 4 (xs, sm, md, lg)\`);
  }
  
  // Check for button height inconsistencies
  const buttonHeights = Object.values(analysis.buttons)
    .map(btn => parseInt(btn.computed.height))
    .filter(h => !isNaN(h));
  const uniqueButtonHeights = [...new Set(buttonHeights)];
  if (uniqueButtonHeights.length > 3) {
    issues.push(\`**Too many button heights**: Found \${uniqueButtonHeights.length} different button heights, recommend max 3-4 standard sizes\`);
  }
  
  if (issues.length === 0) {
    return '✅ No major inconsistencies detected in current sizing system.';
  }
  
  return issues.map(issue => \`- \${issue}\`).join('\n');
}

// Run the analysis
inspectDashboardSizing();