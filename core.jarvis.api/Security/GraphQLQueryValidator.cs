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

        // Check query depth
        var depth = CalculateQueryDepth(query);
        if (depth > _options.MaxQueryDepth)
        {
            return GraphQLValidationResult.Failure(
                "QUERY_TOO_DEEP",
                $"Query depth {depth} exceeds maximum allowed depth of {_options.MaxQueryDepth}");
        }

        // Check field count
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
    /// Calculates the maximum nesting depth of a GraphQL query.
    /// </summary>
    private int CalculateQueryDepth(string query)
    {
        int maxDepth = 0;
        int currentDepth = 0;

        foreach (char c in query)
        {
            if (c == '{')
            {
                currentDepth++;
                maxDepth = Math.Max(maxDepth, currentDepth);
            }
            else if (c == '}')
            {
                currentDepth = Math.Max(0, currentDepth - 1);
            }
        }

        return maxDepth;
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
}
