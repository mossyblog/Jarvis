# Snapshot Integration Tests

These tests require the database schema to be updated with version columns and the component_snapshots table.

## Prerequisites

Before running these tests, ensure your database has been updated:

1. Run the setup script to update the database schema:
   ```sql
   -- Run the contents of /Scripts/setup-test-database.sql
   ```

2. The following tables need a `version INTEGER DEFAULT 1` column:
   - test_component
   - position_component
   - velocity_component
   - All other component tables

3. The `component_snapshots` table must exist

## Known Issues

If you see errors like:
```
Could not find the 'version' column of 'test_component' in the schema cache
```

This means the database schema needs to be updated. Run the setup script or manually add the version columns.