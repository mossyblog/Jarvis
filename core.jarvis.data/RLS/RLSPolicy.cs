namespace core.jarvis.data.RLS
{
    /// <summary>
    /// Defines a Row Level Security policy that can be applied at the SDK level
    /// and/or enforced at the PostgreSQL database level.
    /// </summary>
    public class RLSPolicy
    {
        // Table and operation type
        public string TableName { get; set; } = "";
        public PolicyType Type { get; set; }

        // SDK-level enforcement (existing - for backwards compatibility)
        public Func<Dictionary<string, string>, string>? WhereClause { get; set; }
        public Func<Dictionary<string, string>, Dictionary<string, object>, bool>? CheckFunction { get; set; }

        // PostgreSQL-native enforcement (NEW)
        /// <summary>
        /// The name of the PostgreSQL policy (e.g., "tenant_isolation").
        /// </summary>
        public string? PolicyName { get; set; }

        /// <summary>
        /// The USING expression for SELECT/UPDATE/DELETE operations.
        /// Example: "tenant_id = current_setting('app.tenant_id', true)::uuid"
        /// </summary>
        public string? UsingExpression { get; set; }

        /// <summary>
        /// The WITH CHECK expression for INSERT/UPDATE operations.
        /// If null, uses UsingExpression for WITH CHECK.
        /// </summary>
        public string? WithCheckExpression { get; set; }

        /// <summary>
        /// Whether this policy has PostgreSQL-native expressions defined.
        /// </summary>
        public bool HasPostgresPolicy => !string.IsNullOrEmpty(PolicyName) && !string.IsNullOrEmpty(UsingExpression);
    }

    public enum PolicyType
    {
        Select,
        Insert,
        Update,
        Delete,
        All
    }

    /// <summary>
    /// Registry for RLS policies that are enforced at the SDK level.
    /// </summary>
    public class RLSPolicyRegistry
    {
        private readonly Dictionary<string, List<RLSPolicy>> _policies = new();

        /// <summary>
        /// Quotes a PostgreSQL identifier to prevent SQL injection.
        /// Doubles any embedded quotes and wraps in double quotes.
        /// </summary>
        private static string QuoteIdentifier(string identifier)
        {
            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }

        /// <summary>
        /// Registers a policy for a table.
        /// </summary>
        public void RegisterPolicy(RLSPolicy policy)
        {
            if (!_policies.ContainsKey(policy.TableName))
            {
                _policies[policy.TableName] = new List<RLSPolicy>();
            }
            _policies[policy.TableName].Add(policy);
        }

        /// <summary>
        /// Gets all policies for a table and operation type.
        /// </summary>
        public IEnumerable<RLSPolicy> GetPolicies(string tableName, PolicyType type)
        {
            if (!_policies.ContainsKey(tableName))
                return Enumerable.Empty<RLSPolicy>();

            return _policies[tableName]
                .Where(p => p.Type == type || p.Type == PolicyType.All);
        }

        /// <summary>
        /// Gets all registered policies across all tables.
        /// Used by RLSMigrationHelper to generate migration scripts.
        /// </summary>
        public IEnumerable<RLSPolicy> GetAllPolicies()
        {
            return _policies.Values.SelectMany(p => p);
        }

        /// <summary>
        /// Gets all policies that have PostgreSQL policy definitions (PolicyName is not null).
        /// </summary>
        public IEnumerable<RLSPolicy> GetAllPostgresPolicies()
        {
            return _policies.Values
                .SelectMany(p => p)
                .Where(p => p.PolicyName != null);
        }

        /// <summary>
        /// Builds WHERE clause additions for SELECT operations.
        /// </summary>
        public string BuildWhereClause(string tableName, Dictionary<string, string> claims)
        {
            var policies = GetPolicies(tableName, PolicyType.Select);
            var clauses = policies
                .Where(p => p.WhereClause != null)
                .Select(p => p.WhereClause!(claims))
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            if (!clauses.Any())
                return "";

            // Combine all policies with AND (all must pass)
            return "(" + string.Join(") AND (", clauses) + ")";
        }

        /// <summary>
        /// Checks if an operation is allowed based on policies.
        /// </summary>
        public bool CheckOperation(string tableName, PolicyType type, Dictionary<string, string> claims, Dictionary<string, object> data)
        {
            var policies = GetPolicies(tableName, type);

            // If no policies exist for this table, allow the operation (no RLS configured)
            if (!_policies.ContainsKey(tableName))
                return true;

            // If table has policies but none for this operation type, deny by default
            if (!policies.Any())
                return false;

            // All applicable policies must pass
            foreach (var policy in policies)
            {
                if (policy.CheckFunction != null && !policy.CheckFunction(claims, data))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Generates SQL to enable Row Level Security on a table.
        /// </summary>
        public string GenerateEnableRLSSql(string tableName)
        {
            return $"ALTER TABLE {QuoteIdentifier(tableName)} ENABLE ROW LEVEL SECURITY;";
        }

        /// <summary>
        /// Generates SQL to force Row Level Security on a table (applies to table owner as well).
        /// </summary>
        public string GenerateForceRLSSql(string tableName)
        {
            return $"ALTER TABLE {QuoteIdentifier(tableName)} FORCE ROW LEVEL SECURITY;";
        }

        /// <summary>
        /// Generates CREATE POLICY SQL statement for a given RLS policy.
        /// </summary>
        public string GenerateCreatePolicySql(RLSPolicy policy)
        {
            var policyType = policy.Type == PolicyType.All ? "ALL" : policy.Type.ToString().ToUpper();

            var sql = $"CREATE POLICY {QuoteIdentifier(policy.PolicyName!)} ON {QuoteIdentifier(policy.TableName)} FOR {policyType} USING ({policy.UsingExpression})";

            if (policy.WithCheckExpression != null)
            {
                sql += $" WITH CHECK ({policy.WithCheckExpression})";
            }

            sql += ";";

            return sql;
        }

        /// <summary>
        /// Generates DROP POLICY SQL statement.
        /// </summary>
        public string GenerateDropPolicySql(string tableName, string policyName)
        {
            return $"DROP POLICY IF EXISTS {QuoteIdentifier(policyName)} ON {QuoteIdentifier(tableName)};";
        }
    }

    /// <summary>
    /// Default RLS policies that match the test scenarios.
    /// </summary>
    public static class DefaultRLSPolicies
    {
        public static void RegisterDefaultPolicies(RLSPolicyRegistry registry)
        {
            // Tenant isolation policy for tenant_data table
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.All,
                // SDK-level enforcement
                WhereClause = claims =>
                {
                    if (claims.TryGetValue("tenant_id", out var tenantId) && Guid.TryParse(tenantId, out var validGuid))
                        return $"tenant_id = '{validGuid}'::uuid";
                    return "1=0"; // No access without valid tenant_id GUID
                },
                CheckFunction = (claims, data) =>
                {
                    if (!claims.TryGetValue("tenant_id", out var claimTenantId))
                        return false;

                    if (data.TryGetValue("tenant_id", out var dataTenantId))
                    {
                        return claimTenantId == dataTenantId?.ToString();
                    }
                    return false;
                },
                // PostgreSQL-level enforcement
                PolicyName = "tenant_isolation",
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid",
                WithCheckExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid"
            });

            // User data policies
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "user_data",
                Type = PolicyType.Select,
                // SDK-level enforcement
                WhereClause = claims =>
                {
                    if (!claims.TryGetValue("tenant_id", out var tenantId) || !Guid.TryParse(tenantId, out var validTenantGuid))
                        return "1=0";
                    if (!claims.TryGetValue("sub", out var userId) || !Guid.TryParse(userId, out var validUserGuid))
                        return "1=0";

                    // Users can see their own records OR public records from their tenant
                    return $"tenant_id = '{validTenantGuid}'::uuid AND (user_id = '{validUserGuid}'::uuid OR is_public = TRUE)";
                },
                // PostgreSQL-level enforcement
                PolicyName = "user_data_select",
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid AND (user_id = current_setting('app.user_id', true)::uuid OR is_public = TRUE)"
            });

            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "user_data",
                Type = PolicyType.Insert,
                // SDK-level enforcement
                CheckFunction = (claims, data) =>
                {
                    if (!claims.TryGetValue("tenant_id", out var claimTenantId))
                        return false;
                    if (!claims.TryGetValue("sub", out var claimUserId))
                        return false;

                    // Check tenant_id matches
                    if (data.TryGetValue("tenant_id", out var dataTenantId) &&
                        claimTenantId != dataTenantId?.ToString())
                        return false;

                    // Check user_id matches
                    if (data.TryGetValue("user_id", out var dataUserId) &&
                        claimUserId != dataUserId?.ToString())
                        return false;

                    return true;
                },
                // PostgreSQL-level enforcement
                PolicyName = "user_data_insert",
                UsingExpression = "true", // INSERT doesn't use USING
                WithCheckExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid AND user_id = current_setting('app.user_id', true)::uuid"
            });

            // Sensitive data role-based policies
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "sensitive_data",
                Type = PolicyType.Select,
                // SDK-level enforcement
                WhereClause = claims =>
                {
                    if (!claims.TryGetValue("tenant_id", out var tenantId) || !Guid.TryParse(tenantId, out var validTenantGuid))
                        return "1=0";
                    if (!claims.TryGetValue("role", out var role))
                        return "1=0";

                    var conditions = new List<string> { $"tenant_id = '{validTenantGuid}'::uuid" };

                    switch (role.ToLower())
                    {
                        case "admin":
                            // Admin sees all classifications
                            break;
                        case "manager":
                            conditions.Add("classification IN ('public', 'internal', 'confidential')");
                            break;
                        case "user":
                            conditions.Add("classification IN ('public', 'internal')");
                            break;
                        default:
                            conditions.Add("classification = 'public'");
                            break;
                    }

                    return string.Join(" AND ", conditions);
                },
                // PostgreSQL-level enforcement (simplified - full role logic would need a function)
                PolicyName = "sensitive_data_select",
                UsingExpression = @"tenant_id = current_setting('app.tenant_id', true)::uuid AND (
                    current_setting('app.role', true) IN ('admin', 'Admin') OR
                    (current_setting('app.role', true) IN ('manager', 'Manager') AND classification IN ('public', 'internal', 'confidential')) OR
                    (current_setting('app.role', true) IN ('user', 'User') AND classification IN ('public', 'internal')) OR
                    classification = 'public'
                )"
            });

            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "sensitive_data",
                Type = PolicyType.Insert,
                // SDK-level enforcement
                CheckFunction = (claims, data) =>
                {
                    if (!claims.TryGetValue("role", out var role))
                        return false;

                    // Only managers and admins can insert sensitive data
                    return role.ToLower() == "manager" || role.ToLower() == "admin";
                },
                // PostgreSQL-level enforcement
                PolicyName = "sensitive_data_insert",
                UsingExpression = "true",
                WithCheckExpression = "current_setting('app.role', true) IN ('manager', 'admin', 'Manager', 'Admin')"
            });

            // Component tables pattern - for all *_component tables
            // This is a template that gets resolved for specific component tables
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "*_component",  // Wildcard pattern
                Type = PolicyType.All,
                PolicyName = "component_owner_isolation",
                UsingExpression = "owner_entity_id = current_setting('app.user_id', true)::uuid",
                WithCheckExpression = "owner_entity_id = current_setting('app.user_id', true)::uuid"
            });
        }
    }
}
