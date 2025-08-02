# Brand Compliance Checker

A tool to help maintain consistency with Supabase brand guidelines in the Jarvis UI Studio.

## Overview

The Brand Compliance Checker automatically scans your TypeScript/React code to ensure adherence to design system standards. It helps prevent brand drift and maintains consistency across the codebase.

## Quick Start

```bash
# Check all files in src/
npm run brand-check

# Check specific directory
npm run brand-check -- --path ./src/components

# Get JSON output
npm run brand-check:json

# Strict mode (exit with error if violations found)
npm run brand-check:strict
```

## What It Checks

### 🎨 Colors
- **No hardcoded gray colors**: Use semantic tokens (`gray-100` to `gray-900`) instead of Tailwind's hardcoded grays
- **No hardcoded neutral colors**: Avoid `neutral-*`, `slate-*`, `stone-*` colors
- **Prefer semantic colors**: Use tokens like `background`, `foreground`, `primary`, `muted`

#### ❌ Bad
```tsx
<div className="bg-gray-50 text-gray-900 border-slate-200">
```

#### ✅ Good
```tsx
<div className="bg-background text-foreground border-border">
```

### 🔤 Typography
- **Approved font families**: Only `font-sans`, `font-custom`, `font-mono`
- **Approved font weights**: Only `font-normal`, `font-medium`, `font-semibold`, `font-bold`
- **No hardcoded font sizes**: Use Tailwind `text-*` classes

#### ❌ Bad
```tsx
<h1 className="font-thin text-gray-800" style={{ fontSize: '24px' }}>
```

#### ✅ Good
```tsx
<h1 className="font-semibold text-foreground text-2xl">
```

### 🎯 Icons
- **Lucide React only**: All icons must come from `lucide-react`

#### ❌ Bad
```tsx
import { FaUser } from 'react-icons/fa';
import Icon from '@heroicons/react/solid';
```

#### ✅ Good
```tsx
import { User, Settings, Home } from 'lucide-react';
```

### ✨ Animations
- **Standard timing**: Use approved durations (100ms, 150ms, 200ms, 300ms, 500ms, etc.)
- **Approved easing**: Use standard easing functions

## Semantic Color Tokens

Our design system uses semantic color tokens that automatically adapt to light/dark themes:

### Primary Colors
- `background` / `foreground`
- `primary` / `primary-foreground`
- `secondary` / `secondary-foreground`
- `destructive` / `destructive-foreground`
- `muted` / `muted-foreground`
- `accent` / `accent-foreground`

### UI Elements
- `border` / `border-stronger`
- `input` / `ring`
- `card` / `card-foreground`
- `popover` / `popover-foreground`

### Gray Scale (Semantic)
- `gray-100` through `gray-900` (maps to CSS custom properties)

## Installation

### Automatic Git Hook
```bash
# Install pre-commit hook (recommended)
node scripts/install-hooks.js
```

This will automatically run brand compliance checks before each commit.

### Manual Integration
Add to your CI/CD pipeline:
```yaml
- name: Brand Compliance Check
  run: npm run brand-check:strict
```

## Configuration

### Custom Rules
You can extend the checker with custom rules:

```typescript
import BrandComplianceChecker, { BrandRule } from './utils/brandCompliance';

const customRules: BrandRule[] = [
  {
    name: 'custom-rule',
    description: 'Custom brand rule',
    severity: 'warning',
    category: 'color'
  }
];

const checker = new BrandComplianceChecker(customRules);
```

### Ignoring Violations
Add comments to ignore specific violations:
```tsx
// brand-compliance-ignore-next-line
<div className="bg-red-500">Emergency styling</div>
```

## CLI Options

```bash
npm run brand-check [options]

Options:
  --path <path>     Target directory to scan (default: ./src)
  --format <format> Output format: text|json (default: text)
  --strict          Exit with code 1 if errors found
  --help, -h        Show help message
```

## Integration with VS Code

For real-time feedback, consider adding ESLint rules that integrate with the brand checker:

```json
{
  "eslint.validate": ["typescript", "typescriptreact"],
  "eslint.codeActionsOnSave.source.fixAll.eslint": true
}
```

## Fixing Common Violations

### Hard-coded Gray Colors
Replace Tailwind grays with semantic tokens:
```tsx
// Before
bg-gray-50 → bg-background
text-gray-900 → text-foreground  
border-gray-200 → border-border
```

### Font Weight Issues
Use approved font weights:
```tsx
// Before
font-light → font-normal
font-extrabold → font-bold
font-black → font-bold
```

### Icon Library Issues
Switch to Lucide React:
```tsx
// Before
import { FaHome } from 'react-icons/fa';
// After  
import { Home } from 'lucide-react';
```

## Troubleshooting

### False Positives
If the checker flags valid code, you can:
1. Add an ignore comment
2. Update the approved tokens list
3. Create a custom rule

### Performance
For large codebases, scan specific directories:
```bash
npm run brand-check -- --path ./src/components
```

### CI/CD Integration
Use strict mode in CI to prevent non-compliant code:
```bash
npm run brand-check:strict
```

## Contributing

To add new brand rules:

1. Edit `/src/utils/brandCompliance.ts`
2. Add your rule to `BRAND_RULES`
3. Implement the check method
4. Add tests and documentation

## Examples

### Full Example Component
```tsx
import React from 'react';
import { User, Settings } from 'lucide-react';

export function UserCard() {
  return (
    <div className="bg-card border-border rounded-lg p-md">
      <div className="flex items-center gap-sm">
        <User className="text-muted-foreground" size={20} />
        <h3 className="font-semibold text-foreground text-lg">
          John Doe
        </h3>
      </div>
      <p className="text-muted-foreground text-sm mt-xs">
        Senior Developer
      </p>
      <button className="bg-primary text-primary-foreground font-medium px-md py-sm rounded-md mt-md hover:bg-primary/90 transition-colors duration-200">
        <Settings size={16} className="mr-xs" />
        Edit Profile  
      </button>
    </div>
  );
}
```

This component follows all brand compliance rules:
- ✅ Uses semantic color tokens
- ✅ Uses approved font weights
- ✅ Uses Lucide React icons
- ✅ Uses standard animation timing
- ✅ Uses 8px grid spacing system