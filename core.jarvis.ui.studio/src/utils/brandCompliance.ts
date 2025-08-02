/**
 * Brand Compliance Checker for Supabase Design System
 * 
 * This tool helps maintain consistency with Supabase brand guidelines by:
 * 1. Checking for hard-coded colors instead of semantic tokens
 * 2. Validating font usage against approved classes
 * 3. Ensuring icon library restrictions (Lucide React only)
 * 4. Validating animation timing standards
 */

import * as fs from 'fs';
import * as path from 'path';

// ============================================================================
// BRAND RULES CONFIGURATION
// ============================================================================

export interface BrandRule {
  name: string;
  description: string;
  severity: 'error' | 'warning' | 'info';
  category: 'color' | 'typography' | 'icons' | 'animation' | 'spacing';
}

export interface ComplianceViolation {
  rule: BrandRule;
  file: string;
  line: number;
  column: number;
  match: string;
  suggestion: string;
  context?: string;
}

export interface ComplianceReport {
  violations: ComplianceViolation[];
  summary: {
    total: number;
    errors: number;
    warnings: number;
    info: number;
  };
  filesCanned: number;
  compliantFiles: string[];
  nonCompliantFiles: string[];
}

// ============================================================================
// SEMANTIC COLOR TOKENS (Supabase Design System)
// ============================================================================

const APPROVED_SEMANTIC_COLORS = [
  // Primary semantic colors
  'background', 'foreground', 'foreground-light', 'foreground-lighter',
  'primary', 'primary-foreground',
  'secondary', 'secondary-foreground', 
  'destructive', 'destructive-foreground',
  'muted', 'muted-foreground',
  'accent', 'accent-foreground',
  'popover', 'popover-foreground',
  'card', 'card-foreground',
  
  // UI element colors
  'border', 'border-stronger', 'input', 'ring',
  
  // Dashboard specific
  'dash-sidebar', 'default', 'brand', 'brand-600',
  
  // Sidebar semantic colors
  'sidebar-background', 'sidebar-foreground', 'sidebar-primary',
  'sidebar-primary-foreground', 'sidebar-accent', 'sidebar-accent-foreground',
  'sidebar-border', 'sidebar-ring',
  
  // Approved semantic gray scale (maps to CSS custom properties)
  'gray-100', 'gray-200', 'gray-300', 'gray-400', 'gray-500',
  'gray-600', 'gray-700', 'gray-800', 'gray-900'
];

const APPROVED_TAILWIND_COLORS = [
  // Standard Tailwind colors that are allowed
  'transparent', 'current', 'inherit',
  'black', 'white',
  // State colors
  'red-500', 'red-600', 'green-500', 'green-600', 'amber-500', 'amber-600',
  'blue-500', 'blue-600', 'indigo-500', 'indigo-600',
  // These should be used sparingly and ideally replaced with semantic tokens
];

// ============================================================================
// APPROVED FONT CLASSES AND WEIGHTS
// ============================================================================

const APPROVED_FONT_FAMILIES = [
  'font-sans',      // Inter (default)
  'font-custom',    // Custom brand font
  'font-mono'       // Source Code Pro
];

const APPROVED_FONT_SIZES = [
  'text-xs', 'text-sm', 'text-base', 'text-lg', 'text-xl',
  'text-2xl', 'text-3xl', 'text-4xl', 'text-5xl', 'text-6xl',
  'text-7xl', 'text-8xl', 'text-9xl'
];

const APPROVED_FONT_WEIGHTS = [
  'font-normal',    // 400
  'font-medium',    // 500  
  'font-semibold',  // 600
  'font-bold'       // 700
];

// ============================================================================
// ANIMATION TIMING STANDARDS
// ============================================================================

const APPROVED_ANIMATION_TIMINGS = [
  // Micro interactions (0-300ms)
  '100ms', '150ms', '200ms', '250ms', '300ms',
  // Standard transitions (300-500ms)  
  '300ms', '400ms', '500ms',
  // Page transitions (500ms+)
  '600ms', '700ms', '800ms', '1000ms',
  
  // Tailwind duration classes
  'duration-75', 'duration-100', 'duration-150', 'duration-200',
  'duration-300', 'duration-500', 'duration-700', 'duration-1000'
];

const APPROVED_EASING_FUNCTIONS = [
  'ease', 'ease-in', 'ease-out', 'ease-in-out',
  'cubic-bezier(0.4, 0, 0.6, 1)',      // Tailwind default
  'cubic-bezier(0.4, 0, 0.2, 1)',      // Material ease-out
  'cubic-bezier(0.4, 0.0, 0.2, 1)'     // Alternative format
];

// ============================================================================
// BRAND RULES DEFINITIONS
// ============================================================================

const BRAND_RULES: BrandRule[] = [
  // Color Rules
  {
    name: 'no-hardcoded-gray-colors',
    description: 'Use semantic color tokens (gray-100 to gray-900) instead of hardcoded Tailwind gray colors',
    severity: 'error',
    category: 'color'
  },
  {
    name: 'no-hardcoded-neutral-colors', 
    description: 'Use semantic color tokens instead of hardcoded neutral, slate, stone colors',
    severity: 'error',
    category: 'color'
  },
  {
    name: 'prefer-semantic-colors',
    description: 'Use semantic color tokens (background, foreground, etc.) instead of hardcoded colors',
    severity: 'warning',
    category: 'color'
  },
  
  // Typography Rules
  {
    name: 'approved-font-families',
    description: 'Only use approved font families: font-sans, font-custom, font-mono',
    severity: 'error',
    category: 'typography'
  },
  {
    name: 'approved-font-weights',
    description: 'Only use approved font weights: font-normal, font-medium, font-semibold, font-bold',
    severity: 'warning',
    category: 'typography'
  },
  {
    name: 'no-hardcoded-font-sizes',
    description: 'Use Tailwind text- classes instead of hardcoded font sizes',
    severity: 'warning',
    category: 'typography'
  },
  
  // Icon Rules
  {
    name: 'lucide-icons-only',
    description: 'Only use Lucide React icons for consistency',
    severity: 'error',
    category: 'icons'
  },
  
  // Animation Rules
  {
    name: 'standard-animation-timing',
    description: 'Use standard animation timings for consistent motion design',
    severity: 'info',
    category: 'animation'
  },
  {
    name: 'approved-easing-functions',
    description: 'Use approved easing functions for smooth animations',
    severity: 'info', 
    category: 'animation'
  }
];

// ============================================================================
// COMPLIANCE CHECKER IMPLEMENTATION
// ============================================================================

export class BrandComplianceChecker {
  private rules: BrandRule[];
  
  constructor(customRules?: BrandRule[]) {
    this.rules = customRules || BRAND_RULES;
  }

  /**
   * Scan a single file for brand compliance violations
   */
  async scanFile(filePath: string): Promise<ComplianceViolation[]> {
    const violations: ComplianceViolation[] = [];
    
    if (!fs.existsSync(filePath)) {
      return violations;
    }
    
    const content = fs.readFileSync(filePath, 'utf-8');
    const lines = content.split('\n');
    
    for (let lineIndex = 0; lineIndex < lines.length; lineIndex++) {
      const line = lines[lineIndex];
      const lineNumber = lineIndex + 1;
      
      // Check each rule against the current line
      for (const rule of this.rules) {
        const ruleViolations = this.checkRule(rule, line, filePath, lineNumber);
        violations.push(...ruleViolations);
      }
    }
    
    return violations;
  }

  /**
   * Scan multiple files or directories
   */
  async scanPath(targetPath: string, extensions: string[] = ['.tsx', '.ts', '.jsx', '.js']): Promise<ComplianceReport> {
    const violations: ComplianceViolation[] = [];
    const scannedFiles: string[] = [];
    
    const scanDirectory = (dirPath: string) => {
      const entries = fs.readdirSync(dirPath, { withFileTypes: true });
      
      for (const entry of entries) {
        const fullPath = path.join(dirPath, entry.name);
        
        if (entry.isDirectory() && !this.shouldSkipDirectory(entry.name)) {
          scanDirectory(fullPath);
        } else if (entry.isFile() && extensions.some(ext => entry.name.endsWith(ext))) {
          scannedFiles.push(fullPath);
        }
      }
    };
    
    if (fs.statSync(targetPath).isDirectory()) {
      scanDirectory(targetPath);
    } else {
      scannedFiles.push(targetPath);
    }
    
    // Scan all collected files
    for (const filePath of scannedFiles) {
      const fileViolations = await this.scanFile(filePath);
      violations.push(...fileViolations);
    }
    
    return this.generateReport(violations, scannedFiles);
  }

  /**
   * Check a specific rule against a line of code
   */
  private checkRule(rule: BrandRule, line: string, file: string, lineNumber: number): ComplianceViolation[] {
    const violations: ComplianceViolation[] = [];
    
    switch (rule.name) {
      case 'no-hardcoded-gray-colors':
        violations.push(...this.checkHardcodedGrayColors(rule, line, file, lineNumber));
        break;
      case 'no-hardcoded-neutral-colors':
        violations.push(...this.checkHardcodedNeutralColors(rule, line, file, lineNumber));
        break;
      case 'prefer-semantic-colors':
        violations.push(...this.checkSemanticColors(rule, line, file, lineNumber));
        break;
      case 'approved-font-families':
        violations.push(...this.checkFontFamilies(rule, line, file, lineNumber));
        break;
      case 'approved-font-weights':
        violations.push(...this.checkFontWeights(rule, line, file, lineNumber));
        break;
      case 'no-hardcoded-font-sizes':
        violations.push(...this.checkHardcodedFontSizes(rule, line, file, lineNumber));
        break;
      case 'lucide-icons-only':
        violations.push(...this.checkIconLibrary(rule, line, file, lineNumber));
        break;
      case 'standard-animation-timing':
        violations.push(...this.checkAnimationTiming(rule, line, file, lineNumber));
        break;
      case 'approved-easing-functions':
        violations.push(...this.checkEasingFunctions(rule, line, file, lineNumber));
        break;
    }
    
    return violations;
  }

  // ============================================================================
  // RULE CHECK IMPLEMENTATIONS
  // ============================================================================

  private checkHardcodedGrayColors(rule: BrandRule, line: string, file: string, lineNumber: number): ComplianceViolation[] {
    const violations: ComplianceViolation[] = [];
    
    // Pattern to match Tailwind gray colors (but not our semantic ones)
    const grayPattern = /\b(bg-gray-|text-gray-|border-gray-|ring-gray-|from-gray-|to-gray-|via-gray-)(\d{2,3})\b/g;
    let match;
    
    while ((match = grayPattern.exec(line)) !== null) {
      const [fullMatch, prefix, number] = match;
      
      // Skip if it's one of our approved semantic gray tokens
      if (APPROVED_SEMANTIC_COLORS.includes(`gray-${number}`)) {
        continue;
      }
      
      violations.push({
        rule,
        file,
        line: lineNumber,
        column: match.index + 1,
        match: fullMatch,
        suggestion: `Use semantic token: ${prefix.replace('-gray-', '-gray-')}${this.mapToSemanticGray(number)}`,
        context: line.trim()
      });
    }
    
    return violations;
  }

  private checkHardcodedNeutralColors(rule: BrandRule, line: string, file: string, lineNumber: number): ComplianceViolation[] {
    const violations: ComplianceViolation[] = [];
    
    const neutralPattern = /\b(bg-|text-|border-|ring-|from-|to-|via-)(neutral|slate|stone|zinc)-(\d{2,3})\b/g;
    let match;
    
    while ((match = neutralPattern.exec(line)) !== null) {
      const [fullMatch] = match;
      
      violations.push({
        rule,
        file,
        line: lineNumber,
        column: match.index + 1,
        match: fullMatch,
        suggestion: 'Use semantic color tokens like background, foreground, muted, etc.',
        context: line.trim()
      });
    }
    
    return violations;
  }

  private checkSemanticColors(rule: BrandRule, line: string, file: string, lineNumber: number): ComplianceViolation[] {
    const violations: ComplianceViolation[] = [];
    
    // Look for hardcoded Tailwind colors that should be semantic
    const colorPattern = /\b(bg-|text-|border-|ring-|from-|to-|via-)(red|blue|green|yellow|purple|pink|indigo|cyan|teal|orange|amber|lime|emerald|sky|violet|fuchsia|rose)-(\d{2,3})\b/g;
    let match;
    
    while ((match = colorPattern.exec(line)) !== null) {
      const [fullMatch, , color, number] = match;
      
      // Skip state colors that are commonly needed
      if (APPROVED_TAILWIND_COLORS.includes(`${color}-${number}`)) {
        continue;
      }
      
      violations.push({
        rule,
        file,
        line: lineNumber,
        column: match.index + 1,
        match: fullMatch,
        suggestion: 'Consider using semantic tokens like primary, destructive, accent, etc.',
        context: line.trim()
      });
    }
    
    return violations;
  }

  private checkFontFamilies(rule: BrandRule, line: string, file: string, lineNumber: number): ComplianceViolation[] {
    const violations: ComplianceViolation[] = [];
    
    const fontFamilyPattern = /\bfont-(serif|cursive|fantasy|system-ui|\w+)\b/g;
    let match;
    
    while ((match = fontFamilyPattern.exec(line)) !== null) {
      const [fullMatch] = match;
      
      if (!APPROVED_FONT_FAMILIES.includes(fullMatch)) {
        violations.push({
          rule,
          file,
          line: lineNumber,
          column: match.index + 1,
          match: fullMatch,
          suggestion: `Use approved font families: ${APPROVED_FONT_FAMILIES.join(', ')}`,
          context: line.trim()
        });
      }
    }
    
    return violations;
  }

  private checkFontWeights(rule: BrandRule, line: string, file: string, lineNumber: number): ComplianceViolation[] {
    const violations: ComplianceViolation[] = [];
    
    const fontWeightPattern = /\bfont-(thin|extralight|light|regular|black|extrabold|\d{3})\b/g;
    let match;
    
    while ((match = fontWeightPattern.exec(line)) !== null) {
      const [fullMatch] = match;
      
      if (!APPROVED_FONT_WEIGHTS.includes(fullMatch) && !APPROVED_FONT_WEIGHTS.includes(`font-${fullMatch.split('-')[1]}`)) {
        violations.push({
          rule,
          file,
          line: lineNumber,
          column: match.index + 1,
          match: fullMatch,
          suggestion: `Use approved font weights: ${APPROVED_FONT_WEIGHTS.join(', ')}`,
          context: line.trim()
        });
      }
    }
    
    return violations;
  }

  private checkHardcodedFontSizes(rule: BrandRule, line: string, file: string, lineNumber: number): ComplianceViolation[] {
    const violations: ComplianceViolation[] = [];
    
    // Look for hardcoded font sizes in CSS or style attributes
    const fontSizePattern = /font-size:\s*(\d+(?:\.\d+)?(?:px|em|rem|pt))/g;
    let match;
    
    while ((match = fontSizePattern.exec(line)) !== null) {
      const [fullMatch] = match;
      
      violations.push({
        rule,
        file,
        line: lineNumber,
        column: match.index + 1,
        match: fullMatch,
        suggestion: `Use Tailwind text- classes: ${APPROVED_FONT_SIZES.join(', ')}`,
        context: line.trim()
      });
    }
    
    return violations;
  }

  private checkIconLibrary(rule: BrandRule, line: string, file: string, lineNumber: number): ComplianceViolation[] {
    const violations: ComplianceViolation[] = [];
    
    // Check for non-Lucide icon imports
    const iconImportPattern = /import.*from\s+['"](?!lucide-react)([^'"]*icon[^'"]*|.*icons?[^'"]*)['"];?/gi;
    let match;
    
    while ((match = iconImportPattern.exec(line)) !== null) {
      const [fullMatch] = match;
      
      violations.push({
        rule,
        file,
        line: lineNumber,
        column: match.index + 1,
        match: fullMatch,
        suggestion: 'Use Lucide React icons: import { IconName } from "lucide-react"',
        context: line.trim()
      });
    }
    
    return violations;
  }

  private checkAnimationTiming(rule: BrandRule, line: string, file: string, lineNumber: number): ComplianceViolation[] {
    const violations: ComplianceViolation[] = [];
    
    // Look for animation durations
    const durationPattern = /(?:duration|transition-duration):\s*(\d+(?:\.\d+)?(?:ms|s))/g;
    let match;
    
    while ((match = durationPattern.exec(line)) !== null) {
      const [fullMatch, duration] = match;
      
      if (!APPROVED_ANIMATION_TIMINGS.includes(duration)) {
        violations.push({
          rule,
          file,
          line: lineNumber,
          column: match.index + 1,
          match: fullMatch,
          suggestion: `Use standard timings: ${APPROVED_ANIMATION_TIMINGS.slice(0, 8).join(', ')}...`,
          context: line.trim()
        });
      }
    }
    
    return violations;
  }

  private checkEasingFunctions(rule: BrandRule, line: string, file: string, lineNumber: number): ComplianceViolation[] {
    const violations: ComplianceViolation[] = [];
    
    const easingPattern = /transition-timing-function:\s*([^;]+)/g;
    let match;
    
    while ((match = easingPattern.exec(line)) !== null) {
      const [fullMatch, easing] = match;
      
      if (!APPROVED_EASING_FUNCTIONS.some(approved => easing.includes(approved))) {
        violations.push({
          rule,
          file,
          line: lineNumber,
          column: match.index + 1,
          match: fullMatch,
          suggestion: `Use approved easing: ${APPROVED_EASING_FUNCTIONS.slice(0, 4).join(', ')}`,
          context: line.trim()
        });
      }
    }
    
    return violations;
  }

  // ============================================================================
  // UTILITY METHODS
  // ============================================================================

  private mapToSemanticGray(number: string): string {
    const num = parseInt(number);
    if (num <= 200) return '100';
    if (num <= 300) return '200';
    if (num <= 400) return '300';
    if (num <= 500) return '400';
    if (num <= 600) return '500';
    if (num <= 700) return '600';
    if (num <= 800) return '700';
    if (num <= 900) return '800';
    return '900';
  }

  private shouldSkipDirectory(dirName: string): boolean {
    const skipDirs = ['node_modules', '.git', 'dist', 'build', '.next', 'coverage', '.storybook'];
    return skipDirs.includes(dirName) || dirName.startsWith('.');
  }

  private generateReport(violations: ComplianceViolation[], scannedFiles: string[]): ComplianceReport {
    const summary = {
      total: violations.length,
      errors: violations.filter(v => v.rule.severity === 'error').length,
      warnings: violations.filter(v => v.rule.severity === 'warning').length,
      info: violations.filter(v => v.rule.severity === 'info').length
    };

    const violationFiles = new Set(violations.map(v => v.file));
    const compliantFiles = scannedFiles.filter(f => !violationFiles.has(f));
    const nonCompliantFiles = Array.from(violationFiles);

    return {
      violations,
      summary,
      filesCanned: scannedFiles.length,
      compliantFiles,
      nonCompliantFiles
    };
  }

  /**
   * Format violations as a human-readable report
   */
  formatReport(report: ComplianceReport): string {
    let output = '\n🎨 Brand Compliance Report\n';
    output += '================================\n\n';
    
    output += `📊 Summary:\n`;
    output += `   Files scanned: ${report.filesCanned}\n`;
    output += `   Total violations: ${report.summary.total}\n`;
    output += `   • Errors: ${report.summary.errors}\n`;
    output += `   • Warnings: ${report.summary.warnings}\n`;
    output += `   • Info: ${report.summary.info}\n\n`;

    if (report.violations.length === 0) {
      output += '✅ All files are brand compliant!\n';
      return output;
    }

    // Group violations by file
    const violationsByFile = report.violations.reduce((acc, violation) => {
      if (!acc[violation.file]) {
        acc[violation.file] = [];
      }
      acc[violation.file].push(violation);
      return acc;
    }, {} as Record<string, ComplianceViolation[]>);

    for (const [file, violations] of Object.entries(violationsByFile)) {
      output += `📄 ${file}\n`;
      
      for (const violation of violations) {
        const icon = violation.rule.severity === 'error' ? '❌' : 
                    violation.rule.severity === 'warning' ? '⚠️' : 'ℹ️';
        
        output += `   ${icon} Line ${violation.line}:${violation.column} - ${violation.rule.name}\n`;
        output += `      Found: ${violation.match}\n`;
        output += `      Fix: ${violation.suggestion}\n`;
        if (violation.context) {
          output += `      Context: ${violation.context}\n`;
        }
        output += '\n';
      }
    }

    return output;
  }
}

// ============================================================================
// CLI INTERFACE AND UTILITIES
// ============================================================================

/**
 * Run brand compliance check on a directory
 */
export async function checkBrandCompliance(
  targetPath: string = './src',
  options: {
    extensions?: string[];
    rules?: BrandRule[];
    outputFormat?: 'text' | 'json';
  } = {}
): Promise<ComplianceReport> {
  const { 
    extensions = ['.tsx', '.ts', '.jsx', '.js'],
    rules,
    outputFormat = 'text'
  } = options;

  const checker = new BrandComplianceChecker(rules);
  const report = await checker.scanPath(targetPath, extensions);
  
  if (outputFormat === 'text') {
    console.log(checker.formatReport(report));
  } else {
    console.log(JSON.stringify(report, null, 2));
  }
  
  return report;
}

/**
 * Create a pre-commit hook configuration
 */
export function generatePreCommitHook(): string {
  return `#!/bin/sh
# Brand Compliance Pre-commit Hook
# Add this to .git/hooks/pre-commit and make it executable

echo "🎨 Running brand compliance check..."

# Run the brand compliance checker
npm run brand-check

# Check exit code
if [ $? -ne 0 ]; then
  echo "❌ Brand compliance check failed. Please fix violations before committing."
  exit 1
fi

echo "✅ Brand compliance check passed!"
exit 0
`;
}

/**
 * Generate package.json script
 */
export function generatePackageScript(): string {
  return `{
  "scripts": {
    "brand-check": "node -e \\"require('./src/utils/brandCompliance.ts').checkBrandCompliance('./src')\\"",
    "brand-check:json": "node -e \\"require('./src/utils/brandCompliance.ts').checkBrandCompliance('./src', { outputFormat: 'json' })\\"",
    "brand-check:strict": "node -e \\"const report = require('./src/utils/brandCompliance.ts').checkBrandCompliance('./src'); process.exit(report.summary.errors > 0 ? 1 : 0)\\"" 
  }
}`;
}

// Export the main functionality
export { BRAND_RULES, APPROVED_SEMANTIC_COLORS, APPROVED_FONT_FAMILIES };
export default BrandComplianceChecker;