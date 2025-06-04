# 🔧 C# Project Change Request Template

## 🧾 Metadata

- **Change Request Title**: Implement Concurrency Control with TryCommit Pattern
- **Author**: Claude
- **Date Created**: 2025-05-26
- **Status**: Draft
- **Target Branch/Environment**: main
- **Related Tickets/References**:
  - CR-datacontext.md
  - [Issue: Add proper concurrency handling]

---

## 🎯 Objective

**Implement mandatory optimistic concurrency control in the Jarvis framework to prevent data loss from concurrent updates.**  
> This change replaces the existing Commit() method with TryCommit() as the only way to persist changes, ensuring all operations handle concurrency explicitly. Version management is fully encapsulated in C# code with no database-side logic.

> **Clarification:** This concurrency control mechanism does **not** perform any automatic merging, conflict resolution, or version history tracking. It only checks the version (e.g., eTag) before commit. If the version does not match, the commit is rejected and the caller must decide how to handle the conflict (e.g., retry, reload, or abort). No merge, no partial commit, and no history table is involved.

---

## 📦 Scope of Change

### 1. **Affected Components/Namespaces**

- `core.jarvis.Data.IDataContext`
- `core.jarvis.Data.DataContext`
- `core.jarvis.Data.IComponent`
- `core.jarvis.Data.Entity`
- `core.jarvis.Data.ComponentHandler<T>`
- `core.jarvis.Data.DataContextComponentHandler`
- `core.jarvis.Exceptions.ConcurrencyException`

### 2. **New Classes / Interfaces (if any)**

- `IVersionedComponent`: Interface for components with version tracking
- `CommitResult`: Result object for TryCommit operations
- `ConcurrencyConflict`: Details about version conflicts

### 3. **Modified Classes / Methods**

<pre>
<b>// BEFORE - IDataContext</b>
public interface IDataContext {
    Task Commit();
    Task SaveComponent<T>(T component) where T : class, IComponent;
}

<b>// AFTER - IDataContext</b>
public interface IDataContext {
    Task<CommitResult> TryCommit(); // ONLY commit method - always returns result
    // Removed: Commit(), SaveComponent() - force explicit concurrency handling
}
</pre>

<pre>
<b>// BEFORE - IComponent</b>
public interface IComponent {
    Guid EntityId { get; set; }
}

<b>// AFTER - IComponent</b>
public interface IComponent {
    Guid EntityId { get; set; }
}

public interface IVersionedComponent : IComponent {
    int Version { get; set; }
    DateTime UpdatedAt { get; set; }
}
</pre>

<pre>
<b>// BEFORE - DataContext</b>
public async Task Commit() {
    var components = _workingSet.GetAllComponents();
    foreach (var component in components) {
        await _postgrestClient.Table<T>()
            .Upsert(component)
            .Execute();
    }
}

<b>// AFTER - DataContext (TryCommit is the ONLY commit method)</b>
public async Task<CommitResult> TryCommit() {
    var conflicts = new List<ConcurrencyConflict>();
    var components = _workingSet.GetAllComponents();
    var successfulUpdates = new List<IComponent>();
    
    foreach (var component in components) {
        if (component is IVersionedComponent versioned) {
            // Fetch current version from database
            var currentInDb = await _supabaseClient
                .From<T>()
                .Where(x => x.EntityId == component.EntityId)
                .Single();
            
            if (currentInDb != null) {
                var dbVersioned = currentInDb as IVersionedComponent;
                if (dbVersioned != null && dbVersioned.Version != versioned.Version) {
                    conflicts.Add(new ConcurrencyConflict {
                        Component = component,
                        ExpectedVersion = versioned.Version,
                        ActualVersion = dbVersioned.Version
                    });
                    continue;
                }
            }
            
            // Increment version and update timestamp in C#
            versioned.Version++;
            versioned.UpdatedAt = DateTime.UtcNow;
        }
        
        // Only update if no conflicts
        if (!conflicts.Any()) {
            await _supabaseClient
                .From<T>()
                .Upsert(component)
                .Execute();
            successfulUpdates.Add(component);
        }
    }
    
    // If any conflicts, rollback is automatic (nothing was saved)
    return new CommitResult {
        Success = !conflicts.Any(),
        Conflicts = conflicts,
        UpdatedComponents = successfulUpdates
    };
}
</pre>

<pre>
<b>// BEFORE - ComponentHandler</b>
public abstract class ComponentHandler<T> {
    protected async Task SaveComponent(T component) {
        _dataContext.AddOrUpdate(component);
        await _dataContext.Commit();
    }
}

<b>// AFTER - ComponentHandler</b>
public abstract class ComponentHandler<T> {
    // All saves must use TryCommit and handle the result
    protected async Task<bool> SaveChanges() {
        var result = await _dataContext.TryCommit();
        if (!result.Success) {
            // Handler must decide: retry, merge, or fail
            throw new ConcurrencyException(
                "Update conflicts detected", 
                result.Conflicts);
        }
        return result.Success;
    }
}
</pre>

---

## ✅ Acceptance Criteria

- [ ] TryCommit() is the ONLY way to persist changes (no Commit() method)
- [ ] Version field automatically incremented in C# code on successful commits
- [ ] TryCommit() always returns CommitResult with success/failure details
- [ ] All SaveComponent and Commit methods removed
- [ ] Version management entirely in C# (no database triggers or stored procedures)
- [ ] Existing non-versioned components continue to work (backward compatible)
- [ ] Integration tests verify concurrent update scenarios
- [ ] All database operations through DataContext.TryCommit()
- [ ] No dynamic typing or reflection used
- [ ] Handlers must explicitly handle CommitResult
- [ ] **No automatic merging, conflict resolution, or version history is performed. If a version conflict is detected, the update is rejected and the caller must decide how to proceed.**
- [ ] **No partial commits: if any component in a batch fails the version check, nothing is committed.**

---

## 🧪 Testing Plan

### Unit Tests
- `TestTryCommit_ReturnsFailure_WhenVersionMismatch()`
- `TestTryCommit_ReturnsSuccess_WhenVersionMatches()`
- `TestTryCommit_IncrementsVersion_OnSuccess()`
- `TestTryCommit_UpdatesTimestamp_InCSharp()`
- `TestTryCommit_WorksWithNonVersionedComponents()`

### Integration Tests
- `TestConcurrentUpdates_SecondUpdateFails_WithVersionedComponents()`
- `TestConcurrentUpdates_BothSucceed_WithNonVersionedComponents()`
- `TestTryCommit_AllowsRetryLogic()`
- `TestTryCommit_DoesNotPartiallyCommit_OnConflict()`
- `TestHandlers_MustHandleCommitResult()`

### Manual Tests (If Required)
- Simulate two users updating same entity simultaneously
- Verify version numbers increment correctly in database
- Confirm audit logs capture version changes
- **Verify that no merge or auto-resolution occurs: if a version conflict is detected, the update is rejected and the caller must handle the conflict.**

---

## 📚 Data Changes

> Describe any schema changes, migrations, or seed data modifications.

- **Altered Tables**: All component tables need:
  - `version INTEGER DEFAULT 1`
  - `updated_at TIMESTAMPTZ DEFAULT NOW()`
- **NO database triggers or functions** - all logic in C#
- **Indexes**: Add index on entity_id for faster lookups

Example migration (simple DDL only):
```sql
-- For each component table
ALTER TABLE invoice ADD COLUMN version INTEGER DEFAULT 1;
ALTER TABLE invoice ADD COLUMN updated_at TIMESTAMPTZ DEFAULT NOW();
CREATE INDEX idx_invoice_entity ON invoice(entity_id);

-- NO triggers, NO stored procedures, NO database functions
-- All version incrementing and timestamp updates handled in C# code
```

---

## 🔐 Security & AuthZ Impact

> Describe any new permission boundaries, roles, encryption logic, or token behavior.

- No security impact - concurrency control is transparent
- Version numbers are not sensitive data
- Audit logs will capture version changes for compliance

---

## ⏱️ Performance Considerations

> Note any impact on response time, memory usage, or concurrency behavior.

- Additional SELECT before UPDATE adds ~5-10ms per component
- Version comparison is O(1) operation in C# memory
- Index on entity_id ensures fast lookups
- All-or-nothing commit pattern prevents partial updates
- Retry logic in handler code may add latency on conflicts

---

## 🔄 Backward Compatibility

> Is anything deprecated? Does old code break?

- **BREAKING**: Commit() method removed - must use TryCommit()
- **BREAKING**: SaveComponent() removed - use TryCommit() directly
- Components without IVersionedComponent continue to work (no version checking)
- Database changes are additive only - no data loss

Migration path:
1. Add version columns with defaults (non-breaking)
2. Replace all Commit() calls with TryCommit() and handle result
3. Update components to implement IVersionedComponent (opt-in)
4. All handlers must handle CommitResult explicitly

---

## 🧠 AI Notes for Code Generation

### 1. **Constraints**
- Must use Supabase SDK operations only (no direct PostgreSQL)
- TryCommit() is the ONLY persistence method allowed
- All version/timestamp logic in C# code (no database triggers)
- No dynamic typing or reflection
- No bypassing DataContext to access Supabase directly
- All handlers must explicitly handle CommitResult

### 2. **Naming Conventions**
- Version property is `Version` (int)
- Timestamp is `UpdatedAt` (DateTime)
- Single method: `TryCommit()` (no Commit())
- Result types: `CommitResult`, `ConcurrencyConflict`

### 3. **Architecture Pattern**
- All concurrency logic encapsulated in DataContext.TryCommit()
- Handlers must handle CommitResult and decide on conflicts
- Components opt-in via IVersionedComponent
- No database-side logic (all in C#)
- Maintain existing audit trail integration

---

## 🔄 Rollback Strategy

> If something fails, how do we recover?

- Remove version columns from tables (data preserved)
- Revert to previous DataContext implementation
- Re-add SaveComponent methods if needed
- Version fields default to 1, so no data corruption

---

## 📎 Related Files to Update

- [ ] `/core.jarvis/Data/IDataContext.cs`
- [ ] `/core.jarvis/Data/DataContext.cs`
- [ ] `/core.jarvis/Data/IComponent.cs`
- [ ] `/core.jarvis/Exceptions/ConcurrencyException.cs`
- [ ] All component handlers using SaveComponent
- [ ] Integration tests for concurrency scenarios
- [ ] Migration scripts for database changes

---

## 🧠 Final Checklist for AI Execution

- [ ] Generate IVersionedComponent interface with Version and UpdatedAt
- [ ] Implement TryCommit as the ONLY commit method (remove Commit())
- [ ] Version increment and timestamp update in C# code only
- [ ] Update ConcurrencyException to include conflict details
- [ ] Remove ALL SaveComponent and Commit methods
- [ ] Write integration tests simulating concurrent updates
- [ ] Ensure all operations use DataContext.TryCommit() only
- [ ] Create simple SQL migration (no triggers/functions)
- [ ] Update all handlers to use TryCommit and handle result
- [ ] No partial commits - all or nothing on conflicts

---

> **NOTE**: This change request focuses on adding essential concurrency control while maintaining the simplicity and testability of the Jarvis framework. The implementation uses Supabase's capabilities appropriately while keeping all business logic in the DataContext layer.