===============================================================================
CHANGE REQUEST: REFACTOR ECS HANDLER & ENTITYQUERY TO PLUGGABLE, TYPE-SAFE, 
NO-REFLECTION, DOMAIN-EXTENSIBLE PATTERN FOR JARVIS SUPABASE
===============================================================================

!! FOLLOW EVERY STEP -- DO NOT DEVIATE, DO NOT "IMPROVE" OR "ENHANCE" !!

-------------------------------------------------------------------------------
PART 1: REMOVE LEGACY ECS BOILERPLATE & REFLECTION
-------------------------------------------------------------------------------
[ ] 1. Delete all manual handler registrations:
        - Remove all .Register<TComponent, THandler>() calls from test bases, constructors, and setup code.
[ ] 2. Delete all transient handler factories (Func<Guid, ...>) from DI setup.
[ ] 3. Remove all code using MethodInfo.Invoke, MakeGenericMethod, dynamic, or reflection to resolve/invoke handlers anywhere (EntityQuery, test cleanup, DataContext, etc).
[ ] 4. Remove all reflection from test cleanup and teardown.

-------------------------------------------------------------------------------
PART 2: ENFORCE TYPE-SAFE HANDLER AND QUERY REGISTRY
-------------------------------------------------------------------------------
[ ] 5. Register each IComponentHandler<T> and IComponentQueryHandler<T> ONCE globally via DI in the module/plugin where T is defined:
        Example:
            services.AddSingleton<IComponentHandler<Invoice>, InvoiceHandler>();
            services.AddSingleton<IComponentQueryHandler<Invoice>, InvoiceQueryHandler>();
[ ] 6. The handler registries (IComponentHandlerRegistry, IComponentQueryHandlerRegistry) must use DI to resolve handlers, keyed by type T, **with zero reflection**.
        - If handler is missing, throw clear exception.
[ ] 7. EntityQuery and DataContext MUST ONLY interact with handlers and queries via their interface types, never by reflection.

-------------------------------------------------------------------------------
PART 3: ENTITYQUERY IMPLEMENTATION & USAGE
-------------------------------------------------------------------------------
[ ] 8. EntityQuery must support:
        - .WithAll<T>(Expression<Func<T, bool>>)  => intersection (AND)
        - .WithAny<T>(...)                        => union (OR)
        - .WithNone<T>(...)                       => exclusion (NOT)
    The query evaluation must:
        - For WithAll: intersect all entity ID sets
        - For WithAny: union all entity ID sets
        - For WithNone: exclude all entity IDs from result set

    Example EntityQuery implementation:
        class EntityQuery : IEntityQuery
        {
            private readonly IComponentQueryHandlerRegistry _handlerRegistry;
            private readonly Dictionary<Type, LambdaExpression> _withAll = new();
            private readonly Dictionary<Type, LambdaExpression> _withAny = new();
            private readonly Dictionary<Type, LambdaExpression> _withNone = new();

            // ...ctor...

            public IEntityQuery WithAll<T>(Expression<Func<T, bool>> filter) { ... }
            public IEntityQuery WithAny<T>(Expression<Func<T, bool>> filter) { ... }
            public IEntityQuery WithNone<T>(Expression<Func<T, bool>> filter) { ... }

            public async Task<List<Guid>> ToEntityIds() { ... }
            public async Task<Dictionary<Guid, EntityComponents>> ToEntityComponents() { ... }
        }

    All handler calls in EntityQuery must go through IComponentQueryHandler (see below).
    NO reflection, NO dynamic, NO generic method tricks.

-------------------------------------------------------------------------------
PART 4: INTERFACE BRIDGE REQUIREMENT (NO GENERIC METHOD REFLECTION)
-------------------------------------------------------------------------------
[ ] 9. IComponentQueryHandler interface MUST look like:
        interface IComponentQueryHandler {
            Type ComponentType { get; }
            Task<IEnumerable<Guid>> QueryEntityIds(LambdaExpression filter);
            Task<IEnumerable<IComponent>> LoadComponents(IEnumerable<Guid> entityIds);
        }
    And the generic form:
        interface IComponentQueryHandler<T> : IComponentQueryHandler where T : BaseModel, IComponent, new() {
            Task<IEnumerable<Guid>> QueryEntityIds(Expression<Func<T, bool>> filter);
            Task<IEnumerable<T>> LoadComponents(IEnumerable<Guid> entityIds);
        }
    The generic handler must implement the bridge interface explicitly.

-------------------------------------------------------------------------------
PART 5: DOMAIN/PLUGIN USAGE & EXTENSION METHODS
-------------------------------------------------------------------------------
[ ] 10. In each domain/module (e.g., Finance), provide extension methods for business access:
        public static InvoiceHandler InvoiceHandler(this IDataContext ctx, Guid invoiceId)
            => ctx.ComponentHandlerRegistry.Resolve<InvoiceHandler>(invoiceId);

    Never add domain-specific accessors to Jarvis core.
    Always use extension/facade pattern for domain entrypoints.

-------------------------------------------------------------------------------
PART 6: TESTS
-------------------------------------------------------------------------------
[ ] 11. Tests must use DI to inject IDataContext and registry. Do not create, register, or map handlers in tests.
[ ] 12. For test cleanup, call IDataContext.Remove<T>(id) with concrete types. Do not use reflection for test cleanup or entity deletion.

-------------------------------------------------------------------------------
PART 7: USAGE EXAMPLES (DO NOT CHANGE THIS PATTERN)
-------------------------------------------------------------------------------
[ ] 13. Example query:
        var entityIds = await _entityQuery
            .WithAll<Invoice>(inv => !inv.IsPaid)
            .WithAny<WorkOrder>(wo => wo.Status == "Pending")
            .WithNone<Foo>(f => f.IsRead)
            .ToEntityIds();

    Example usage for component data:
        var entityComponents = await _entityQuery
            .Include<Invoice>()
            .Include<WorkOrder>()
            .ToEntityComponents();

    Example business access:
        var handler = _dataContext.InvoiceHandler(invoiceId);
        handler.MarkPaid();

-------------------------------------------------------------------------------
PART 8: DO NOT DO ANY OF THE FOLLOWING
-------------------------------------------------------------------------------
[ ] 14. Do NOT:
        - Register handlers manually per test/class
        - Use handler factories in DI (Func<Guid, ...>)
        - Use MethodInfo, MakeGenericMethod, or dynamic for handler calls
        - Add business extensions to Jarvis core
        - "Enhance", "improve", or "simplify" these instructions with reflection or dynamic logic

===============================================================================
!! HANDLER/QUERY PATTERN MUST REMAIN STRICTLY TYPE-SAFE AND PLUGGABLE !!
===============================================================================

NEW INTERFACES
--------------

IComponentQueryHandler
----------------------
    interface IComponentQueryHandler {
        Type ComponentType { get; }
        Task<IEnumerable<Guid>> QueryEntityIds(LambdaExpression filter);
        Task<IEnumerable<IComponent>> LoadComponents(IEnumerable<Guid> entityIds);
    }

IComponentQueryHandler<T>
-------------------------
    interface IComponentQueryHandler<T> : IComponentQueryHandler where T : BaseModel, IComponent, new() {
        Task<IEnumerable<Guid>> QueryEntityIds(Expression<Func<T, bool>> filter);
        Task<IEnumerable<T>> LoadComponents(IEnumerable<Guid> entityIds);
    }

GENERIC HANDLER IMPLEMENTATION (PER COMPONENT TYPE)
---------------------------------------------------
    class ComponentQueryHandler<T> : IComponentQueryHandler<T> where T : BaseModel, IComponent, new() {
        public Type ComponentType => typeof(T);

        public async Task<IEnumerable<Guid>> QueryEntityIds(Expression<Func<T, bool>> filter) {
            // Use Supabase query logic here
        }

        public async Task<IEnumerable<T>> LoadComponents(IEnumerable<Guid> entityIds) {
            // Use Supabase filter("owner_entity_id", IN, entityIds)
        }

        async Task<IEnumerable<Guid>> IComponentQueryHandler.QueryEntityIds(LambdaExpression filter) =>
            await QueryEntityIds((Expression<Func<T, bool>>)filter);

        async Task<IEnumerable<IComponent>> IComponentQueryHandler.LoadComponents(IEnumerable<Guid> entityIds) =>
            (await LoadComponents(entityIds)).Cast<IComponent>();
    }

HANDLER REGISTRY
----------------
    interface IComponentQueryHandlerRegistry {
        IComponentQueryHandler GetHandler(Type componentType);
    }

ENTITYQUERY USAGE & LOGIC
-------------------------
- .WithAll<T>()    => intersection (AND)
- .WithAny<T>()    => union (OR)
- .WithNone<T>()   => exclusion (NOT)

The handler registry allows dynamic resolution without reflection on methods.

    private async Task<IEnumerable<Guid>> ExecuteQueryEntityIds(Type type, LambdaExpression filter) {
        var handler = _handlerRegistry.GetHandler(type);
        return await handler.QueryEntityIds(filter);
    }

    private async Task ExecuteLoadAndAssign(Type type, List<Guid> ids, Dictionary<Guid, EntityComponents> map) {
        var handler = _handlerRegistry.GetHandler(type);
        var components = await handler.LoadComponents(ids);
        foreach (var c in components)
            map[c.OwnerEntityId].Add(type, c);
    }

BENEFITS
--------
- Compile-time safety for all generic handler logic
- No reliance on reflection
- Enables plugin-based component systems with composable logic
- Simplifies debugging and improves runtime performance

================================================================================
================================================================================
ENTITYQUERY REPLACEMENT (TYPE-SAFE, NO REFLECTION, WITHALL/WITHANY/WITHNONE)
================================================================================

EntityQuery - Key Fields
------------------------
    class EntityQuery : IEntityQuery
    {
        private readonly IComponentQueryHandlerRegistry _handlerRegistry;

        // Store filters for each query type
        private readonly Dictionary<Type, LambdaExpression> _withAll = new();
        private readonly Dictionary<Type, LambdaExpression> _withAny = new();
        private readonly Dictionary<Type, LambdaExpression> _withNone = new();

        public EntityQuery(IComponentQueryHandlerRegistry handlerRegistry)
        {
            _handlerRegistry = handlerRegistry;
        }

        // Add intersection (AND) filter
        public IEntityQuery WithAll<T>(Expression<Func<T, bool>> filter) where T : BaseModel, IComponent, new()
        {
            _withAll[typeof(T)] = filter;
            return this;
        }

        // Add union (OR) filter
        public IEntityQuery WithAny<T>(Expression<Func<T, bool>> filter) where T : BaseModel, IComponent, new()
        {
            _withAny[typeof(T)] = filter;
            return this;
        }

        // Add exclusion (NOT) filter
        public IEntityQuery WithNone<T>(Expression<Func<T, bool>> filter) where T : BaseModel, IComponent, new()
        {
            _withNone[typeof(T)] = filter;
            return this;
        }

        // Resolve entity IDs according to all three filter categories
        public async Task<List<Guid>> ToEntityIds()
        {
            var allSets = new List<HashSet<Guid>>();
            var anySet = new HashSet<Guid>();
            var noneSet = new HashSet<Guid>();

            // ALL - intersection (AND)
            foreach (var (type, filter) in _withAll)
            {
                var ids = await ExecuteQueryEntityIds(type, filter);
                allSets.Add(ids.ToHashSet());
            }

            // ANY - union (OR)
            foreach (var (type, filter) in _withAny)
            {
                var ids = await ExecuteQueryEntityIds(type, filter);
                anySet.UnionWith(ids);
            }

            // NONE - exclusion (NOT)
            foreach (var (type, filter) in _withNone)
            {
                var ids = await ExecuteQueryEntityIds(type, filter);
                noneSet.UnionWith(ids);
            }

            // Combine results
            HashSet<Guid> result;
            if (allSets.Any())
            {
                result = allSets.Aggregate((a, b) => { a.IntersectWith(b); return a; });
            }
            else if (anySet.Any())
            {
                result = new HashSet<Guid>(anySet);
            }
            else
            {
                result = new HashSet<Guid>();
            }

            // Exclude NONE matches
            result.ExceptWith(noneSet);
            return result.ToList();
        }

        // Load full component sets for all resolved entity IDs
        public async Task<Dictionary<Guid, EntityComponents>> ToEntityComponents()
        {
            var entityIds = await ToEntityIds();
            var allTypes = _withAll.Keys
                .Concat(_withAny.Keys)
                .Concat(_withNone.Keys)
                .Distinct()
                .ToList();

            var result = entityIds.ToDictionary(id => id, _ => new EntityComponents());

            foreach (var type in allTypes)
            {
                await ExecuteLoadAndAssign(type, entityIds, result);
            }

            return result;
        }

        // Handler lookup for entity ID queries
        private async Task<IEnumerable<Guid>> ExecuteQueryEntityIds(Type componentType, LambdaExpression filter)
        {
            var handler = _handlerRegistry.GetHandler(componentType);
            return await handler.QueryEntityIds(filter);
        }

        // Handler lookup for loading actual components
        private async Task ExecuteLoadAndAssign(Type componentType, List<Guid> entityIds, Dictionary<Guid, EntityComponents> result)
        {
            var handler = _handlerRegistry.GetHandler(componentType);
            var components = await handler.LoadComponents(entityIds);

            foreach (var component in components)
            {
                if (!result.TryGetValue(component.OwnerEntityId, out var ec))
                    continue;
                ec.Add(componentType, component);
            }
        }
    }

================================================================================

USAGE EXAMPLES
--------------
    var entityIds = await _entityQuery
        .WithAll<Invoice>(inv => !inv.IsPaid)
        .WithAny<WorkOrder>(wo => wo.Status == \"Pending\")
        .WithNone<Foo>(f => f.IsRead)
        .ToEntityIds();

    var entityComponents = await _entityQuery
        .Include<Invoice>()
        .Include<WorkOrder>()
        .ToEntityComponents();

================================================================================

KEY POINTS
----------
- All handler calls are via IComponentQueryHandler interface (no reflection).
- .WithAll<T>(), .WithAny<T>(), .WithNone<T>() map to AND/OR/NOT logic.
- Registry allows you to add new component types without any core changes.
