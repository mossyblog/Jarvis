# Performance Optimization

Jarvis ECS provides features to optimize performance:

- Use `EntityQuery` to batch load components and avoid N+1 queries.
- Use `Include<T>()` to eager load related components.
- Filter with `With<T>(filter)` to reduce result set size.
- Minimize round-trips to the database by combining operations.

Efficient queries and batching are key to scalable performance. 