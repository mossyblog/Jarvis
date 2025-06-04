# Test Guidelines

## 1. Test Base Class and Setup
- **All integration tests must inherit from `IntegrationTestBase`** (in `Helpers/IntegrationTestBase.cs`).
- This base class centralizes DI, test environment, and handler registration for all test types.
- **Access the test data context via `TestDataContext()`** (not `DataContext` or `DataContext()`).
- Do **not** create or resolve your own `DataContext` or `SupabaseClient` in test classes—always use the base class.

## 2. Data Access and Orchestration
- **Always use `TestDataContext()`** for all data access, handler resolution, setup, and cleanup in tests.
- **Never** use direct `Supabase.Client`, `Postgrest`, or raw database calls in test code.
- All handler operations and data manipulations must go through `TestDataContext()` and registered handlers.

## 3. Assertions
- **Use [Shouldly](https://shouldly.readthedocs.io/en/latest/)** for all assertions in tests.
- Do **not** use `Assert` from xUnit or other frameworks.
- Example: `x.ShouldBe(y)` instead of `Assert.Equal(y, x)`.

## 4. Test Setup and Cleanup
- All test setup (inserts, updates) and cleanup (removals) **must** use `TestDataContext().Commit` and `TestDataContext().Remove`.
- Never manipulate the database directly in test code.
- All entities need to be tracked for cleanup!

## 5. Test Documentation
- Every test method must be documented with the following template as an XML doc comment:
  - **INTENT:** What is the test verifying?
  - **PURPOSE:** Why does this test exist?
  - **BUSINESS CONTEXT:** What business scenario does this test support?
  - **WHY IMPORTANT:** Why is this test critical for correctness?
  - **ARCHITECTURAL SIGNIFICANCE:** What architectural contract does this test enforce?
  - **FUTURE RESILIENCE:** How does this test protect against future regressions?
- Tests should use AAA structure (Act, Arrange, Assert )


## 6. Scope of Testing
- **Never test the Supabase SDK or Postgrest client directly.**
- All tests must focus on the behavior of the Jarvis SDK, `TestDataContext()`, and handler orchestration.

## 7. Handler-Based Orchestration
- Prefer handler-based retrieval and orchestration for all business operations in tests.
- Use `TestDataContext().For<THandler>(entityId)` to resolve handlers and perform operations.

## 8. Evolving Rules
- As the codebase evolves, add new rules and best practices to this document.
- Review and update guidelines after major architectural or testing changes.

## 9. Run Tests to validate.
- Always run a specific test you make change sor additions to to validate it passes and compiles.
- Do not move onto other tests until it passes.
- A task is never complete until the build and tests pass.

## 10. Component Usage and Maintenance
- **Avoid creating additional components unless the current ones available do not meet the test requirements.**
- At all times, use the current components to reduce additional maintenance complexity.

## 11. Test Classes should not have inline classes or components inside their file. 
---

**Following these guidelines ensures tests are maintainable, reliable, and aligned with the architectural intent of the Jarvis platform.** 