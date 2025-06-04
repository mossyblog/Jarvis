# Test Documentation Guide

## Purpose

This document establishes the documentation standard for all test methods in the Jarvis ECS framework. Comprehensive test documentation is essential for maintainability, onboarding, and understanding original intent when refactoring tests due to signature changes.

## Documentation Template

Every test method must include this documentation structure:

```csharp
/// <summary>
/// INTENT: What this test is trying to validate (the immediate goal)
/// PURPOSE: Why this validation is important (the underlying reason) 
/// BUSINESS CONTEXT: What business scenario or requirement this supports
/// WHY IMPORTANT: The consequences if this behavior fails
/// [ARCHITECTURAL SIGNIFICANCE]: How this relates to the overall system design (when applicable)
/// [FUTURE RESILIENCE]: What to preserve if implementation details change (when applicable)
/// </summary>
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
```

## Documentation Fields Explained

### INTENT (Required)
- **What**: The immediate technical behavior being validated
- **Focus**: Keep this concise and specific to the test
- **Example**: "Validates constructor guard clause for entityId parameter"

### PURPOSE (Required)  
- **What**: The reason this validation exists
- **Focus**: The immediate functional benefit
- **Example**: "Ensures handlers fail fast when initialized with invalid entity identifiers"

### BUSINESS CONTEXT (Required)
- **What**: The real-world business scenario this supports
- **Focus**: Connect technical behavior to business value
- **Example**: "Every handler operates on a specific entity - without a valid entity ID, no database operations or business logic can be performed correctly"

### WHY IMPORTANT (Required)
- **What**: The consequences if this behavior fails or is removed
- **Focus**: Risk mitigation and error prevention
- **Example**: "Empty GUIDs represent uninitialized or invalid entities. Catching this early prevents silent failures or incorrect database queries later in the handler lifecycle"

### ARCHITECTURAL SIGNIFICANCE (Optional)
- **When to use**: For tests that validate key architectural patterns or boundaries
- **What**: How this test relates to the overall system design
- **Example**: "Demonstrates that the Guard pattern is enforced at the base class level, ensuring all derived handlers get input validation automatically"

### FUTURE RESILIENCE (Optional)
- **When to use**: For tests where implementation might change but intent must be preserved
- **What**: What to preserve if the underlying implementation changes
- **Example**: "If EntityQuery implementation changes, this test documents that the method should return a query builder that supports cross-component operations"

## Class-Level Documentation

Every test class must include comprehensive documentation explaining:

```csharp
/// <summary>
/// Tests for [ClassName] - [brief description of the class role]
/// 
/// BUSINESS CONTEXT: [What business capability this class enables]
/// 
/// ARCHITECTURE SIGNIFICANCE: [How this class fits in the overall system]
/// 
/// TEST STRATEGY: [What aspects are tested and what approach is used]
/// </summary>
```

## Examples from Codebase

### Well-Documented Test Class
See `SimpleDataContextTests.cs` for an example of comprehensive class and method documentation following this template.

### Key Patterns Demonstrated

1. **Constructor Validation Tests**
   - Document why each dependency is critical
   - Explain the business impact of missing dependencies
   - Connect to fail-fast principles

2. **Business Logic Tests**
   - Explain the business rule being enforced
   - Describe the real-world scenario
   - Connect to domain modeling principles

3. **Delegation Tests**
   - Explain the architectural pattern (delegation, registry, etc.)
   - Describe why the class should remain focused
   - Connect to separation of concerns

4. **Transaction Tests**
   - Explain the business scenarios requiring transactions
   - Describe the consistency requirements
   - Connect to ACID compliance needs

## Documentation Standards

### Language Guidelines
- Use present tense ("Validates", "Ensures", "Confirms")
- Be specific about conditions ("when entityId is empty", "when business rule is violated")
- Connect technical behavior to business value
- Avoid implementation details in PURPOSE and BUSINESS CONTEXT

### Content Guidelines
- **Be specific**: "Validates constructor guard clause" not "Tests constructor"
- **Explain consequences**: "Prevents silent failures" not just "Validates input"
- **Connect to business**: "Invoice operations need entity context" not just "Needs valid ID"
- **Future-proof**: Document intent that should survive implementation changes

### Consistency Guidelines
- Use the same documentation template across all tests
- Keep field purposes consistent (INTENT = immediate goal, PURPOSE = why it matters)
- Maintain consistent terminology for architectural concepts
- Reference business scenarios consistently

## Maintenance Guidelines

### When Refactoring Tests
1. **Preserve the original INTENT** - this should rarely change
2. **Update BUSINESS CONTEXT** if business requirements change
3. **Keep WHY IMPORTANT** unless the risk profile changes
4. **Update implementation details** in code comments, not in the documentation headers

### When Adding New Tests
1. **Start with documentation** before writing test code
2. **Validate business context** with domain experts if unclear
3. **Review existing tests** for documentation consistency
4. **Consider architectural significance** for framework-level tests

### When Reviewing Test PRs
1. **Verify documentation completeness** - all required fields present
2. **Check business context accuracy** - does it match actual requirements?
3. **Validate consistency** with existing documentation patterns
4. **Ensure future resilience** for architectural boundary tests

## Benefits of This Approach

1. **Onboarding**: New developers understand test purpose immediately
2. **Maintenance**: Refactoring preserves original intent
3. **Architecture**: Tests document architectural decisions and patterns
4. **Business Alignment**: Tests clearly connect to business requirements
5. **Future Resilience**: Implementation can change while preserving test value

## Test Categories and Their Documentation Focus

### Unit Tests
- Focus on immediate behavior and input validation
- Emphasize business rule enforcement
- Document guard clauses and edge cases

### Integration Tests  
- Focus on component interaction and data flow
- Emphasize transaction behavior and consistency
- Document cross-boundary behavior

### Exception Tests
- Focus on error conditions and recovery
- Emphasize user experience and debugging
- Document error classification and handling

This documentation standard ensures our tests remain valuable assets that document both technical behavior and business intent, making the codebase more maintainable and understandable for current and future developers.