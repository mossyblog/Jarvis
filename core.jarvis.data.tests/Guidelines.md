# Test Guidelines
## Directory & Information Architecture

- Organize tests by feature or domain, using subfolders for logical separation (e.g., `Tables/`, `Integration/`, `Unit/`).
- Place shared test utilities, fixtures, and base classes in a `Helpers/` or `Common/` directory.
- Each test class should target a single table, feature, or business scenario.
- Use clear, descriptive file and class names (e.g., `UserTableTests.cs`, `OrderIntegrationTests.cs`).
- Store SQL scripts or seed data in a dedicated `Scripts/` or `Seed/` directory if needed.
- Keep documentation (like this guidelines file) at the root of the test project for easy discovery.
- Avoid deeply nested directories; prefer flat, discoverable structures.
- Example structure for a PgSQL-style project:
  - `Tables/` — tests for each table or entity
  - `Integration/` — end-to-end or multi-table tests
  - `Helpers/` — base classes, test utilities
  - `Scripts/` — SQL setup/teardown scripts
  - `Guidelines.md` — test standards and documentation

## 1. Test Base Class and Setup
- All integration tests should inherit from a shared test base class if available.
- Centralize DI, test environment, and resource registration in the base class.
- Do not create or resolve your own database connections or clients in test classes—always use the base class or provided setup.

## 2. Data Access and Orchestration
- Use the PgClient for all data access, setup, and cleanup in tests.
- Never use direct raw SQL or other database clients in test code.
- All operations and data manipulations must go through PgClient and its API.

## 3. Assertions
- Use [Shouldly](https://shouldly.readthedocs.io/en/latest/) for all assertions in tests.
- Do not use `Assert` from xUnit or other frameworks.
- Example: `x.ShouldBe(y)` instead of `Assert.Equal(y, x)`.

## 4. Test Setup and Cleanup
- All test setup (inserts, updates) and cleanup (removals) must use PgClient methods.
- Never manipulate the database directly in test code.
- All entities need to be tracked for cleanup.

## 5. Test Documentation
- Every test method must be documented with the following template as an XML doc comment:
  - INTENT: What is the test verifying?
  - PURPOSE: Why does this test exist?
  - BUSINESS CONTEXT: What business scenario does this test support?
  - WHY IMPORTANT: Why is this test critical for correctness?
  - ARCHITECTURAL SIGNIFICANCE: What architectural contract does this test enforce?
  - FUTURE RESILIENCE: How does this test protect against future regressions?
- Tests should use AAA structure (Arrange, Act, Assert).

## 6. Scope of Testing
- Never test the PgClient or ORM directly.
- All tests must focus on the behavior of the data access layer, orchestration, and business logic.

## 7. Handler-Based Orchestration
- Prefer handler-based retrieval and orchestration for all business operations in tests, if applicable.
- Use context-based handler resolution to perform operations if/when introduced.

## 8. Evolving Rules
- As the codebase evolves, add new rules and best practices to this document.
- Review and update guidelines after major architectural or testing changes.

## 9. Run Tests to Validate
- Always run a specific test you make changes or additions to, to validate it passes and compiles.
- Do not move onto other tests until it passes.
- A task is never complete until the build and tests pass.

## 10. Component Usage and Maintenance
- Avoid creating additional components unless the current ones available do not meet the test requirements.
- At all times, use the current components to reduce additional maintenance complexity.

## 11. Test Classes Should Not Have Inline Classes or Components
---

Following these guidelines ensures tests are maintainable, reliable, and aligned with the architectural intent of the platform.