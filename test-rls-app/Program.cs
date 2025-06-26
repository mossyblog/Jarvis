using System;
using System.Collections.Generic;
using core.jarvis.data.RLS;

var registry = new RLSPolicyRegistry();
DefaultRLSPolicies.RegisterDefaultPolicies(registry);

// Test with empty claims
var emptyClaims = new Dictionary<string, string>();
var whereClause = registry.BuildWhereClause("tenant_data", emptyClaims);
Console.WriteLine($"WHERE clause for tenant_data with empty claims: '{whereClause}'");

// Test with tenant_id claim
var claimsWithTenant = new Dictionary<string, string> { ["tenant_id"] = "12345" };
var whereClauseWithTenant = registry.BuildWhereClause("tenant_data", claimsWithTenant);
Console.WriteLine($"WHERE clause for tenant_data with tenant claim: '{whereClauseWithTenant}'");
