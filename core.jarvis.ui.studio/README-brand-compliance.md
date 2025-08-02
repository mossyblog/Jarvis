# Brand Compliance Checker

🎨 **Maintain consistency with Supabase brand guidelines automatically**

## Quick Start

```bash
# Check your code for brand violations
npm run brand-check

# Install Git hook for automatic checking
npm run install-hooks
```

## What it does

✅ **Prevents brand drift** by catching violations early  
✅ **Enforces semantic color tokens** instead of hardcoded grays  
✅ **Validates typography** with approved font weights and families  
✅ **Ensures icon consistency** by requiring Lucide React only  
✅ **Maintains animation standards** with approved timing and easing  

## Results on current codebase

The checker found **10 violations** in the current codebase:
- All violations are `font-light` usage (should be `font-normal`)
- 96% of files (133/138) are fully compliant
- No hardcoded colors or unauthorized icons found

## Commands

| Command | Description |
|---------|-------------|
| `npm run brand-check` | Check all files with detailed report |
| `npm run brand-check:json` | Get machine-readable JSON output |
| `npm run brand-check:strict` | Exit with error if violations found (for CI) |
| `npm run install-hooks` | Set up automatic Git pre-commit checking |

## Common Fixes

### Font Weight Violations
```tsx
// ❌ Before  
<span className="font-light">Text</span>

// ✅ After
<span className="font-normal">Text</span>
```

### Color Violations  
```tsx
// ❌ Before
<div className="bg-gray-50 text-gray-900">

// ✅ After  
<div className="bg-background text-foreground">
```

### Icon Violations
```tsx
// ❌ Before
import { FaUser } from 'react-icons/fa';

// ✅ After
import { User } from 'lucide-react';
```

## Files

- **[brandCompliance.ts](/mnt/c/code/risksec/jarvis/core.jarvis.ui.studio/src/utils/brandCompliance.ts)** - Full TypeScript implementation with extensive rules
- **[brand-check.js](/mnt/c/code/risksec/jarvis/core.jarvis.ui.studio/scripts/brand-check.js)** - Lightweight CLI tool
- **[brand-compliance.md](/mnt/c/code/risksec/jarvis/core.jarvis.ui.studio/docs/brand-compliance.md)** - Complete documentation

## Integration

The tool is designed to integrate seamlessly into your workflow:

- **Development**: Run manually to check changes
- **Pre-commit**: Automatic checking with Git hooks  
- **CI/CD**: Use strict mode to prevent non-compliant merges
- **IDE**: JSON output can be consumed by editor extensions

---

**Ready to maintain brand consistency?** Run `npm run brand-check` to get started!