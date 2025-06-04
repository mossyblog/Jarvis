# Performance Issues

If you experience slow performance in Jarvis ECS:

- Check for N+1 query problems; use batching and Include<T>()
- Profile database queries for slow operations
- Minimize round-trips to the database
- Use efficient filters in queries
- Review handler logic for unnecessary work

Use logs and profiling tools to identify bottlenecks. 