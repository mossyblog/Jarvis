# CHANGE REQUEST: Move ECS Snapshotting to Component-Based Postgres Table

## Summary
This change proposes migrating the current ECS snapshotting system from in-memory serialization (or document storage) to a **normalized, per-component snapshot table in Supabase**. Each snapshot record will represent a mutation of a single component instance, versioned and linked by the root WorkOrder entity.

## Background
- Jarvis currently supports snapshotting using in-memory serialization of WorkingSet state.
- Previous implementations either stored snapshots inside PartitionDocument or discarded them after commit.
- With Supabase, snapshots can now be stored directly in a SQL-native table with JSON content.

## Goals
- Enable timeline reconstruction for any entity (e.g. WorkOrder) based on component changes.
- Allow filtering, rollback, and historical analysis by component, root, tag, or time.
- Keep snapshotting integrated into ECS orchestration via Commit() but persist independently of PartitionDocument.

## New Table Definition
```sql
create table component_snapshot (
  id uuid primary key default gen_random_uuid(),
  root_entity_id uuid not null,
  entity_id uuid not null,
  component_type text not null,
  snapshot jsonb not null,
  tag text,
  committed_at timestamptz default now()
);
```

## Implementation Plan

### 1. Data Model
- Add `ComponentSnapshotRecord` POCO that maps to `component_snapshot`.

### 2. SnapshotService
- Create `SnapshotService` with method:
```csharp
Task SaveSnapshot(Guid rootEntityId, Guid entityId, IComponent component, string tag);
```
- Serialize component to JSON and insert into Supabase.

### 3. Commit Hook
- In `WorkingSet.Commit()`, iterate all dirty components.
- Call `SnapshotService.SaveSnapshot(...)` for each one with tag = "auto".

### 4. Remove Legacy
- Remove or deprecate `EntitySnapshot.cs` and any snapshotting within PartitionDocument.
- Snapshotting is now owned by Supabase.

### 5. Optional Query Helpers
Add a reusable method to fetch snapshots:
```csharp
Task<IEnumerable<T>> LoadSnapshot<T>(Guid entityId, string componentType);
```

## Benefits
- Queryable history per component or per WorkOrder
- Enables time travel, rollback, or audit tools
- Avoids blob-based snapshotting
- Aligns with normalized Supabase design

## Risks
- Slight increase in Supabase write I/O per commit
- ComponentSnapshotRecord schema must be respected consistently

## Approval Checklist
- [ ] `component_snapshot` table created in Supabase
- [ ] `SnapshotService` implemented and injected into commit workflow
- [ ] Legacy snapshotting logic deprecated and removed
- [ ] Commit() captures snapshot of each mutated component with tag="auto"
- [ ] CI tests added for snapshot lifecycle and timeline queries
