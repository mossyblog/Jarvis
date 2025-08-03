# Task Completion Checklist

When completing any coding task in the Jarvis project, follow these steps:

## Backend (C#) Tasks
1. **Run build** to ensure no compilation errors:
   ```bash
   dotnet build
   ```

2. **Run tests** to ensure nothing is broken:
   ```bash
   dotnet test
   ```

3. **Run specific tests** if you modified a particular area:
   ```bash
   dotnet test --filter "NameSpace.ClassName"
   ```

## Frontend (React/TypeScript) Tasks
1. **Check TypeScript types**:
   ```bash
   cd core.jarvis.ui.studio
   npm run typecheck
   ```

2. **Run linting**:
   ```bash
   npm run lint
   ```

3. **Run unit tests**:
   ```bash
   npm run test:run
   ```

4. **Build to verify**:
   ```bash
   npm run build
   ```

5. **Run E2E tests** if UI changes were made:
   ```bash
   npm run test:e2e
   ```

## General Checklist
- [ ] Code compiles without errors
- [ ] All tests pass
- [ ] TypeScript types are correct (frontend)
- [ ] Linting passes (frontend)
- [ ] No console errors in browser (frontend)
- [ ] Changes follow ECS architecture patterns
- [ ] No direct component references (use LinkRelationship)
- [ ] Handlers registered in DI correctly
- [ ] Components are immutable records
- [ ] Error handling is appropriate

## Important Notes
- Always run tests after code changes
- Check for infinite loops in React useEffect hooks
- Verify circular dependencies are avoided
- Ensure proper cleanup in integration tests (TrackEntity)
- Check that JWT/auth context is properly handled