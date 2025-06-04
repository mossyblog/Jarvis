namespace core.jarvis.data.RLS
{
    /// <summary>
    /// Defines a Row Level Security policy that can be applied at the SDK level.
    /// </summary>
    public class RLSPolicy
    {
        public string TableName { get; set; } = "";
        public PolicyType Type { get; set; }
        public Func<Dictionary<string, string>, string>? WhereClause { get; set; }
        public Func<Dictionary<string, string>, Dictionary<string, object>, bool>? CheckFunction { get; set; }
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
                WhereClause = claims =>
                {
                    if (claims.TryGetValue("tenant_id", out var tenantId))
                        return $"tenant_id = '{tenantId}'::uuid";
                    return "1=0"; // No access without tenant_id
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
                }
            });

            // User data policies
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "user_data",
                Type = PolicyType.Select,
                WhereClause = claims =>
                {
                    if (!claims.TryGetValue("tenant_id", out var tenantId))
                        return "1=0";
                    if (!claims.TryGetValue("sub", out var userId))
                        return "1=0";
                    
                    // Users can see their own records OR public records from their tenant
                    return $"tenant_id = '{tenantId}'::uuid AND (user_id = '{userId}'::uuid OR is_public = TRUE)";
                }
            });

            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "user_data",
                Type = PolicyType.Insert,
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
                }
            });

            // Sensitive data role-based policies
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "sensitive_data",
                Type = PolicyType.Select,
                WhereClause = claims =>
                {
                    if (!claims.TryGetValue("tenant_id", out var tenantId))
                        return "1=0";
                    if (!claims.TryGetValue("role", out var role))
                        return "1=0";

                    var conditions = new List<string> { $"tenant_id = '{tenantId}'::uuid" };
                    
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
                }
            });

            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "sensitive_data",
                Type = PolicyType.Insert,
                CheckFunction = (claims, data) =>
                {
                    if (!claims.TryGetValue("role", out var role))
                        return false;
                    
                    // Only managers and admins can insert sensitive data
                    return role.ToLower() == "manager" || role.ToLower() == "admin";
                }
            });
        }
    }
}