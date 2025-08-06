# Current React 19 + Radix UI Infinite Loop Issue

## Problem
The UIStudioInterface component at `/studio` route is experiencing a "Maximum update depth exceeded" error, causing an infinite render loop.

## Error Details
- Error occurs in button component with refs
- Stack trace mentions chunk-P23B2OQX.js (likely Radix UI compiled code)
- Error: "An error occurred in the <button> component"
- Multiple setState calls in useEffect causing circular dependencies

## Environment
- React 19.1.0
- Radix UI components (@radix-ui/react-* version 1.x)
- Vite build system
- TypeScript

## Attempted Fixes
1. Fixed circular dependencies in filteredPagesData useMemo
2. Removed currentPage from dependencies
3. Memoized recentPagesManager 
4. Fixed keyboard shortcuts useEffect dependencies
5. Made userEntityId optional and get from auth context

## Potential Causes
- React 19 compatibility issue with Radix UI ref handling
- Circular dependencies in state management
- useEffect hooks with incorrect dependencies

## Next Steps
- Check Radix UI compatibility with React 19
- Review all button components and ref usage
- Consider downgrading React or updating Radix UI
- Add error boundaries to isolate the issue