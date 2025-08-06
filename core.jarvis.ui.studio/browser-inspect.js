// Browser inspection script to run in DevTools Console
// Copy and paste this into your browser's console when viewing the dashboard

console.log('🔍 Starting Dashboard Sizing Inspection...');
console.log('==========================================');

// Helper function to get computed styles
function getElementInfo(element) {
  const styles = window.getComputedStyle(element);
  const rect = element.getBoundingClientRect();
  
  return {
    width: Math.round(rect.width),
    height: Math.round(rect.height),
    computedWidth: styles.width,
    computedHeight: styles.height,
    padding: styles.padding,
    margin: styles.margin,
    fontSize: styles.fontSize,
    classes: element.className
  };
}

// Analyze header
console.log('📏 HEADER ANALYSIS:');
const header = document.querySelector('header');
if (header) {
  const info = getElementInfo(header);
  console.log(`Header: ${info.width}x${info.height}px, computed height: ${info.computedHeight}`);
}

// Analyze all SVG icons
console.log('\n🎯 ICON ANALYSIS:');
const icons = document.querySelectorAll('svg');
const iconSizes = {};

icons.forEach((icon, index) => {
  const info = getElementInfo(icon);
  const parent = icon.closest('button') || icon.parentElement;
  const context = parent?.getAttribute('aria-label') || 
                 parent?.textContent?.trim() || 
                 icon.getAttribute('aria-label') || 
                 'unknown';
  
  const sizeKey = `${info.width}x${info.height}`;
  if (!iconSizes[sizeKey]) {
    iconSizes[sizeKey] = [];
  }
  
  iconSizes[sizeKey].push({
    index,
    context: context.substring(0, 30),
    classes: info.classes
  });
});

// Report icon sizes
Object.entries(iconSizes).forEach(([size, icons]) => {
  console.log(`\n${size}px icons (${icons.length} total):`);
  icons.slice(0, 5).forEach(icon => {
    console.log(`  - ${icon.context} (classes: ${icon.classes})`);
  });
  if (icons.length > 5) {
    console.log(`  ... and ${icons.length - 5} more`);
  }
});

// Analyze buttons
console.log('\n🔘 BUTTON ANALYSIS:');
const buttons = document.querySelectorAll('button');
const buttonSizes = {};

buttons.forEach((button, index) => {
  const info = getElementInfo(button);
  const text = button.textContent?.trim() || 
               button.getAttribute('aria-label') || 
               'unlabeled';
  
  const heightKey = `h${info.height}`;
  if (!buttonSizes[heightKey]) {
    buttonSizes[heightKey] = [];
  }
  
  buttonSizes[heightKey].push({
    index,
    text: text.substring(0, 30),
    width: info.width,
    padding: info.padding,
    classes: info.classes
  });
});

// Report button sizes
Object.entries(buttonSizes).forEach(([height, buttons]) => {
  console.log(`\n${height}px buttons (${buttons.length} total):`);
  buttons.slice(0, 3).forEach(button => {
    console.log(`  - "${button.text}" (${button.width}px wide, padding: ${button.padding})`);
  });
  if (buttons.length > 3) {
    console.log(`  ... and ${buttons.length - 3} more`);
  }
});

// Analyze cards
console.log('\n📋 CARD ANALYSIS:');
const cards = document.querySelectorAll('[class*="card"], [data-testid*="metric"], .bg-card');
cards.forEach((card, index) => {
  const info = getElementInfo(card);
  const title = card.querySelector('h1, h2, h3, h4, h5, h6')?.textContent?.trim() || 
                card.querySelector('[class*="title"]')?.textContent?.trim() ||
                'untitled';
  
  console.log(`Card ${index + 1}: "${title.substring(0, 20)}" - ${info.width}x${info.height}px, padding: ${info.padding}`);
});

// Analyze typography
console.log('\n📝 TYPOGRAPHY ANALYSIS:');
const textElements = document.querySelectorAll('h1, h2, h3, h4, h5, h6, p, span');
const fontSizes = {};

textElements.forEach((element) => {
  const styles = window.getComputedStyle(element);
  const fontSize = styles.fontSize;
  
  if (!fontSizes[fontSize]) {
    fontSizes[fontSize] = [];
  }
  
  fontSizes[fontSize].push({
    tag: element.tagName.toLowerCase(),
    text: element.textContent?.trim().substring(0, 30) || '',
    classes: element.className
  });
});

// Report font sizes
Object.entries(fontSizes).forEach(([fontSize, elements]) => {
  console.log(`\n${fontSize} text (${elements.length} elements):`);
  elements.slice(0, 3).forEach(element => {
    console.log(`  - ${element.tag}: "${element.text}"`);
  });
  if (elements.length > 3) {
    console.log(`  ... and ${elements.length - 3} more`);
  }
});

// T-shirt size class audit
console.log('\n👕 T-SHIRT SIZE CLASS AUDIT:');
const tshirtClasses = ['xs', 'sm', 'md', 'lg', 'xl', '2xl', '3xl'];
const tshirtUsage = {};

tshirtClasses.forEach(size => {
  // Check for various t-shirt size patterns
  const patterns = [
    `h-${size}`, `w-${size}`, `p-${size}`, `m-${size}`, 
    `px-${size}`, `py-${size}`, `gap-${size}`, `text-${size}`,
    `icon-${size}`, `btn-${size}`, `card-${size}`
  ];
  
  patterns.forEach(pattern => {
    const elements = document.querySelectorAll(`[class*="${pattern}"]`);
    if (elements.length > 0) {
      if (!tshirtUsage[pattern]) tshirtUsage[pattern] = 0;
      tshirtUsage[pattern] += elements.length;
    }
  });
});

Object.entries(tshirtUsage)
  .sort(([,a], [,b]) => b - a)
  .forEach(([className, count]) => {
    console.log(`${className}: ${count} uses`);
  });

console.log('\n✅ Inspection complete!');
console.log('Copy the results above and paste them into your wireframe analysis.');

// Save results to global variable for further inspection
window.dashboardSizingAnalysis = {
  iconSizes,
  buttonSizes,
  fontSizes,
  tshirtUsage,
  totalIcons: icons.length,
  totalButtons: buttons.length,
  totalCards: cards.length
};

console.log('\n💾 Results saved to window.dashboardSizingAnalysis for further inspection.');