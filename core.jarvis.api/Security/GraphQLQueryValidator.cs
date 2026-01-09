using System.Text.RegularExpressions;

namespace core.jarvis.api.Security;

/// <summary>
/// Validates GraphQL queries to prevent DoS attacks through query complexity.
/// </summary>
public interface IGraphQLQueryValidator
{
    /// <summary>
    /// Validates a GraphQL query against security limits.
    /// </summary>
    /// <param name="query">The GraphQL query string</param>
    /// <returns>Validation result with success status and error message if failed</returns>
    GraphQLValidationResult Validate(string query);
}

/// <summary>
/// Result of GraphQL query validation.
/// </summary>
public class GraphQLValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }

    public static GraphQLValidationResult Success() => new() { IsValid = true };

    public static GraphQLValidationResult Failure(string errorCode, string errorMessage) => new()
    {
        IsValid = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Configuration options for GraphQL query validation.
/// </summary>
public class GraphQLValidationOptions
{
    /// <summary>
    /// Maximum allowed query depth (nesting level of selections).
    /// Default: 10 levels.
    /// </summary>
    public int MaxQueryDepth { get; set; } = 10;

    /// <summary>
    /// Maximum number of fields allowed in a single query.
    /// Default: 100 fields.
    /// </summary>
    public int MaxFieldCount { get; set; } = 100;

    /// <summary>
    /// Whether to block introspection queries in production.
    /// Default: true (blocked in production).
    /// </summary>
    public bool BlockIntrospection { get; set; } = true;

    /// <summary>
    /// Maximum query string length in characters.
    /// Default: 10000 characters.
    /// </summary>
    public int MaxQueryLength { get; set; } = 10000;

    /// <summary>
    /// Maximum number of field aliases allowed in a single query.
    /// This prevents alias multiplication attacks where attackers use aliases
    /// to execute the same expensive operation multiple times.
    /// Example attack: { user1: user { id } user2: user { id } ... user100: user { id } }
    /// Each alias results in a separate database query.
    /// Default: 10 aliases.
    /// </summary>
    public int MaxAliases { get; set; } = 10;
}

/// <summary>
/// Validates GraphQL queries against configurable security limits.
/// </summary>
public class GraphQLQueryValidator : IGraphQLQueryValidator
{
    private readonly GraphQLValidationOptions _options;
    private readonly bool _isProduction;

    // Regex patterns for validation
    private static readonly Regex FieldPattern = new(@"\b\w+\s*(?:@[^{(]+)?(?:\([^)]*\))?\s*(?={|$)", RegexOptions.Compiled);
    private static readonly Regex IntrospectionPattern = new(@"__schema|__type", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // Pattern to detect field aliases: "aliasName: fieldName" pattern
    // Matches: word, optional whitespace, colon, optional whitespace, word (not followed by opening paren which would be a directive)
    private static readonly Regex AliasPattern = new(@"\b(\w+)\s*:\s*(\w+)\s*(?:\(|{|\s)", RegexOptions.Compiled);

    public GraphQLQueryValidator(GraphQLValidationOptions? options = null, bool? isProduction = null)
    {
        _options = options ?? new GraphQLValidationOptions();
        _isProduction = isProduction ?? (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development"
                                         && Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Test");
    }

    public GraphQLValidationResult Validate(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GraphQLValidationResult.Failure("EMPTY_QUERY", "Query cannot be empty");
        }

        // Check query length
        if (query.Length > _options.MaxQueryLength)
        {
            return GraphQLValidationResult.Failure(
                "QUERY_TOO_LONG",
                $"Query exceeds maximum length of {_options.MaxQueryLength} characters");
        }

        // Check for introspection queries in production
        if (_isProduction && _options.BlockIntrospection && ContainsIntrospection(query))
        {
            return GraphQLValidationResult.Failure(
                "INTROSPECTION_BLOCKED",
                "Introspection queries are not allowed in production");
        }

        // Validate braces are balanced and check query depth
        var (isBalanced, depth) = ValidateAndCalculateDepth(query);
        if (!isBalanced)
        {
            return GraphQLValidationResult.Failure(
                "UNBALANCED_BRACES",
                "Query contains unbalanced braces");
        }
        if (depth > _options.MaxQueryDepth)
        {
            return GraphQLValidationResult.Failure(
                "QUERY_TOO_DEEP",
                $"Query depth {depth} exceeds maximum allowed depth of {_options.MaxQueryDepth}");
        }

        // Check alias count to prevent alias multiplication attacks
        var aliasCount = CountAliases(query);
        if (aliasCount > _options.MaxAliases)
        {
            return GraphQLValidationResult.Failure(
                "TOO_MANY_ALIASES",
                $"Query contains {aliasCount} aliases, exceeding maximum of {_options.MaxAliases}");
        }

        // Check field count (including aliases - each alias is a separate field execution)
        var fieldCount = CountFields(query);
        if (fieldCount > _options.MaxFieldCount)
        {
            return GraphQLValidationResult.Failure(
                "TOO_MANY_FIELDS",
                $"Query contains {fieldCount} fields, exceeding maximum of {_options.MaxFieldCount}");
        }

        return GraphQLValidationResult.Success();
    }

    /// <summary>
    /// Validates that braces are balanced and calculates the maximum nesting depth.
    /// Prevents DoS attacks using unbalanced braces like "{ { { { { { { { { {"
    /// </summary>
    /// <param name="query">The GraphQL query string.</param>
    /// <returns>A tuple of (isBalanced, maxDepth) where isBalanced indicates if braces match.</returns>
    private (bool isBalanced, int maxDepth) ValidateAndCalculateDepth(string query)
    {
        int currentDepth = 0;
        int maxDepth = 0;

        foreach (char c in query)
        {
            if (c == '{')
            {
                currentDepth++;
                maxDepth = Math.Max(maxDepth, currentDepth);

                // Early exit if too deep to prevent DoS with many opening braces
                if (currentDepth > _options.MaxQueryDepth * 2)
                    return (false, currentDepth);
            }
            else if (c == '}')
            {
                currentDepth--;
                // Unbalanced: more closing braces than opening
                if (currentDepth < 0)
                    return (false, 0);
            }
        }

        // Must be balanced at end (no unclosed opening braces)
        if (currentDepth != 0)
            return (false, 0);

        return (true, maxDepth);
    }

    /// <summary>
    /// Counts the number of field selections in a GraphQL query.
    /// </summary>
    private int CountFields(string query)
    {
        // Remove string literals to avoid counting field-like patterns in strings
        var cleanQuery = RemoveStringLiterals(query);

        // Count opening braces as a proxy for selection sets,
        // and add explicit field references
        int fieldCount = 0;
        bool inWord = false;
        bool afterBrace = false;

        for (int i = 0; i < cleanQuery.Length; i++)
        {
            char c = cleanQuery[i];

            if (c == '{')
            {
                afterBrace = true;
            }
            else if (char.IsLetterOrDigit(c) || c == '_')
            {
                if (!inWord && afterBrace)
                {
                    // Check if this is a field name (not a keyword like "query", "mutation", "fragment")
                    var wordStart = i;
                    while (i < cleanQuery.Length && (char.IsLetterOrDigit(cleanQuery[i]) || cleanQuery[i] == '_'))
                    {
                        i++;
                    }
                    var word = cleanQuery.Substring(wordStart, i - wordStart);
                    i--; // Back up one since the loop will increment

                    if (!IsGraphQLKeyword(word))
                    {
                        fieldCount++;
                    }
                }
                inWord = true;
            }
            else
            {
                inWord = false;
                if (!char.IsWhiteSpace(c) && c != '(' && c != ')' && c != ':' && c != '@' && c != '!')
                {
                    afterBrace = c == '{';
                }
            }
        }

        return fieldCount;
    }

    private bool IsGraphQLKeyword(string word)
    {
        return word switch
        {
            "query" or "mutation" or "subscription" or "fragment" or "on" or "true" or "false" or "null" => true,
            _ => false
        };
    }

    /// <summary>
    /// Counts the number of field aliases in a GraphQL query.
    /// Aliases allow the same field to be queried multiple times with different names,
    /// which can be exploited to multiply database queries.
    /// Example: { user1: user { id } user2: user { id } } - this has 2 aliases.
    /// </summary>
    private int CountAliases(string query)
    {
        // Remove string literals to avoid false positives
        var cleanQuery = RemoveStringLiterals(query);

        // Count alias patterns (word: word pattern)
        // Exclude variable definitions like $id: ID! by checking context
        var matches = AliasPattern.Matches(cleanQuery);
        int aliasCount = 0;

        foreach (Match match in matches)
        {
            var alias = match.Groups[1].Value;
            var fieldName = match.Groups[2].Value;

            // Skip if this looks like a type definition (in variable declaration)
            // Variable defs use $ prefix and type names are typically capitalized
            if (IsGraphQLTypeName(fieldName))
            {
                continue;
            }

            // Skip GraphQL keywords used as "aliases" (shouldn't happen but be safe)
            if (IsGraphQLKeyword(alias) || IsGraphQLKeyword(fieldName))
            {
                continue;
            }

            aliasCount++;
        }

        return aliasCount;
    }

    /// <summary>
    /// Checks if a word looks like a GraphQL type name (capitalized, common type patterns).
    /// Used to distinguish field aliases from type declarations.
    /// </summary>
    private bool IsGraphQLTypeName(string word)
    {
        if (string.IsNullOrEmpty(word))
            return false;

        // Common GraphQL scalar types
        return word switch
        {
            "ID" or "Int" or "Float" or "String" or "Boolean" => true,
            _ => false
        };
    }

    /// <summary>
    /// Removes string literals from a query to avoid false positives.
    /// </summary>
    private string RemoveStringLiterals(string query)
    {
        var result = new System.Text.StringBuilder();
        bool inString = false;
        bool escape = false;

        foreach (char c in query)
        {
            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Checks if the query contains introspection queries.
    /// </summary>
    private bool ContainsIntrospection(string query)
    {
        return IntrospectionPattern.IsMatch(query);
    }

    // Compiled regex for efficient mutation detection
    private static readonly Regex MutationOperationPattern = new(
        @"^\s*mutation\s*(?:\w+)?\s*(?:\([^)]*\))?\s*\{",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Determines if a GraphQL query is a mutation operation.
    /// Uses proper pattern matching to detect mutation operations rather than simple string matching.
    /// </summary>
    /// <param name="query">The GraphQL query string</param>
    /// <returns>True if the query is a mutation, false otherwise</returns>
    public bool IsMutation(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        var trimmed = query.TrimStart();

        // Check for explicit mutation operation type at the start
        if (trimmed.StartsWith("mutation", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check for mutation keyword with optional operation name and variables
        // Pattern matches: "mutation", "mutation CreateUser", "mutation CreateUser($input: Input!)"
        if (MutationOperationPattern.IsMatch(query))
            return true;

        // Note: Anonymous operations (queries without explicit type) are treated as queries by default
        // per GraphQL spec. We don't assume mutation for security-conservative reasons because:
        // 1. If no operation type is specified, GraphQL treats it as a query
        // 2. Blocking legitimate queries would impact usability
        // 3. Actual mutations should always be explicitly declared

        return false;
    }
}
