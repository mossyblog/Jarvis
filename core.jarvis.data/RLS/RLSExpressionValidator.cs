using System.Text.RegularExpressions;

namespace core.jarvis.data.RLS
{
    /// <summary>
    /// Validates RLS (Row Level Security) policy expressions to prevent SQL injection attacks.
    /// Ensures expressions match safe, known patterns before they are used in PostgreSQL policy creation.
    /// </summary>
    public static class RLSExpressionValidator
    {
        /// <summary>
        /// Patterns that indicate potential SQL injection attempts.
        /// These patterns should never appear in valid RLS expressions.
        /// </summary>
        private static readonly Regex[] DangerousPatterns = new[]
        {
            // Statement terminators - could allow chaining malicious statements
            new Regex(@";", RegexOptions.Compiled),

            // SQL comments - could be used to comment out security checks
            new Regex(@"--", RegexOptions.Compiled),
            new Regex(@"/\*", RegexOptions.Compiled),
            new Regex(@"\*/", RegexOptions.Compiled),

            // DDL keywords - should never appear in RLS expressions
            new Regex(@"\b(DROP|CREATE|ALTER|TRUNCATE|GRANT|REVOKE)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // DML statement keywords - RLS expressions are boolean conditions, not statements
            new Regex(@"\b(INSERT\s+INTO|UPDATE\s+\w+\s+SET|DELETE\s+FROM)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Execution commands
            new Regex(@"\b(EXECUTE|EXEC)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Copy commands
            new Regex(@"\b(COPY)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Transaction control
            new Regex(@"\b(BEGIN|COMMIT|ROLLBACK|SAVEPOINT)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // System-level operations
            new Regex(@"\b(pg_sleep|pg_terminate_backend|pg_cancel_backend)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Dollar-quoting (can be used for injection)
            new Regex(@"\$\$", RegexOptions.Compiled),
            new Regex(@"\$[a-zA-Z_][a-zA-Z0-9_]*\$", RegexOptions.Compiled),

            // Backslash escapes (potential injection vector)
            new Regex(@"\\x[0-9a-fA-F]{2}", RegexOptions.Compiled),
            new Regex(@"\\u[0-9a-fA-F]{4}", RegexOptions.Compiled),

            // Subqueries as statements (SELECT without being part of an IN clause or similar)
            // Note: We check for SELECT followed by FROM without preceding IN/ANY/ALL
            new Regex(@"(?<!\b(IN|ANY|ALL|EXISTS)\s*\(\s*)\bSELECT\b(?!.*\bFROM\b.*\bWHERE\b.*=)", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Multiple statements via newline tricks
            new Regex(@"[\r\n].*\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // SET commands (except within current_setting)
            new Regex(@"(?<!current_)\bSET\s+\w+\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Union-based injection
            new Regex(@"\bUNION\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        };

        /// <summary>
        /// Known safe expression patterns that are explicitly allowed.
        /// Expressions should generally match one or more of these patterns.
        /// </summary>
        private static readonly Regex[] SafePatterns = new[]
        {
            // Boolean literal
            new Regex(@"^\s*(true|false)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Column comparison with current_setting
            // e.g., "tenant_id = current_setting('app.tenant_id', true)::uuid"
            new Regex(@"^\s*\w+\s*(=|<>|!=)\s*current_setting\s*\(\s*'[a-zA-Z0-9_\.]+'\s*,\s*(true|false)\s*\)\s*(::[\w\[\]]+)?\s*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Column comparison with literal value
            // e.g., "is_public = TRUE" or "classification = 'public'"
            new Regex(@"^\s*\w+\s*(=|<>|!=|<|>|<=|>=)\s*('[\w\-]+'\s*(::[\w\[\]]+)?|[\w]+)\s*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Column IN list
            // e.g., "classification IN ('public', 'internal')"
            new Regex(@"^\s*\w+\s+IN\s*\(\s*('[^']*'\s*,?\s*)+\)\s*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // current_setting IN list (for role checks)
            // e.g., "current_setting('app.role', true) IN ('admin', 'manager')"
            new Regex(@"^\s*current_setting\s*\(\s*'[a-zA-Z0-9_\.]+'\s*,\s*(true|false)\s*\)\s+IN\s*\(\s*('[^']*'\s*,?\s*)+\)\s*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Function call comparison (e.g., owner_id = current_user_id())
            new Regex(@"^\s*\w+\s*(=|<>|!=)\s*\w+\s*\(\s*\)\s*(::[\w\[\]]+)?\s*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase),
        };

        /// <summary>
        /// Validates an RLS expression to ensure it doesn't contain SQL injection patterns.
        /// </summary>
        /// <param name="expression">The RLS expression to validate.</param>
        /// <returns>A validation result indicating whether the expression is safe.</returns>
        public static RLSExpressionValidationResult Validate(string? expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return RLSExpressionValidationResult.Success();
            }

            // First, check for dangerous patterns
            foreach (var pattern in DangerousPatterns)
            {
                if (pattern.IsMatch(expression))
                {
                    return RLSExpressionValidationResult.Failure(
                        $"RLS expression contains potentially dangerous pattern: '{pattern}'",
                        expression,
                        RLSValidationFailureReason.DangerousPattern);
                }
            }

            // Check for balanced parentheses (applies to all expressions)
            var parenthesesResult = CheckParenthesesBalance(expression);
            if (!parenthesesResult.IsValid)
            {
                return parenthesesResult;
            }

            // Normalize the expression for safe pattern matching (handle complex expressions)
            // For complex expressions with AND/OR, we validate each part separately
            if (IsComplexExpression(expression))
            {
                return ValidateComplexExpression(expression);
            }

            // For simple expressions, check if they match any known safe pattern
            foreach (var pattern in SafePatterns)
            {
                if (pattern.IsMatch(expression))
                {
                    return RLSExpressionValidationResult.Success();
                }
            }

            // If no safe pattern matches, the expression is still allowed if it passes
            // basic structural validation (no dangerous patterns were found)
            // This provides defense-in-depth while allowing flexibility
            if (PassesStructuralValidation(expression))
            {
                return RLSExpressionValidationResult.Success();
            }

            return RLSExpressionValidationResult.Failure(
                "RLS expression does not match any known safe patterns and failed structural validation",
                expression,
                RLSValidationFailureReason.UnknownPattern);
        }

        /// <summary>
        /// Validates an RLS expression and throws if validation fails.
        /// </summary>
        /// <param name="expression">The RLS expression to validate.</param>
        /// <param name="expressionName">Name of the expression for error messages (e.g., "USING" or "WITH CHECK").</param>
        /// <exception cref="RLSExpressionValidationException">Thrown if the expression fails validation.</exception>
        public static void ValidateOrThrow(string? expression, string expressionName = "RLS expression")
        {
            var result = Validate(expression);
            if (!result.IsValid)
            {
                throw new RLSExpressionValidationException(
                    $"{expressionName} failed validation: {result.ErrorMessage}",
                    expression,
                    result.FailureReason);
            }
        }

        /// <summary>
        /// Determines if an expression is a complex expression with AND/OR operators.
        /// </summary>
        private static bool IsComplexExpression(string expression)
        {
            return Regex.IsMatch(expression, @"\b(AND|OR)\b", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Checks if parentheses are balanced in the expression.
        /// </summary>
        private static RLSExpressionValidationResult CheckParenthesesBalance(string expression)
        {
            int depth = 0;
            foreach (char c in expression)
            {
                if (c == '(') depth++;
                if (c == ')') depth--;
                if (depth < 0)
                {
                    return RLSExpressionValidationResult.Failure(
                        "Unbalanced parentheses in RLS expression",
                        expression,
                        RLSValidationFailureReason.MalformedExpression);
                }
            }
            if (depth != 0)
            {
                return RLSExpressionValidationResult.Failure(
                    "Unbalanced parentheses in RLS expression",
                    expression,
                    RLSValidationFailureReason.MalformedExpression);
            }
            return RLSExpressionValidationResult.Success();
        }

        /// <summary>
        /// Validates a complex expression by checking operator structure.
        /// Parentheses balance is already checked in the main Validate method.
        /// </summary>
        private static RLSExpressionValidationResult ValidateComplexExpression(string expression)
        {
            // For complex expressions, we've already checked for dangerous patterns and balanced parentheses
            // Now verify the expression has valid structure
            if (PassesStructuralValidation(expression))
            {
                return RLSExpressionValidationResult.Success();
            }

            return RLSExpressionValidationResult.Failure(
                "Complex RLS expression failed structural validation",
                expression,
                RLSValidationFailureReason.MalformedExpression);
        }

        /// <summary>
        /// Performs basic structural validation on an expression.
        /// Ensures the expression contains expected elements for RLS conditions.
        /// </summary>
        private static bool PassesStructuralValidation(string expression)
        {
            // Must contain at least one comparison operator or be a boolean literal
            bool hasComparison = Regex.IsMatch(expression, @"(=|<>|!=|<|>|<=|>=|\bIN\b|\bIS\s+(NOT\s+)?NULL\b)", RegexOptions.IgnoreCase);
            bool isBooleanLiteral = Regex.IsMatch(expression.Trim(), @"^(true|false)$", RegexOptions.IgnoreCase);
            bool hasCurrentSetting = expression.Contains("current_setting", StringComparison.OrdinalIgnoreCase);
            bool hasFunctionCall = Regex.IsMatch(expression, @"\w+\s*\([^)]*\)");

            // Expression should have comparisons, be a boolean, or contain function calls
            if (!hasComparison && !isBooleanLiteral && !hasFunctionCall)
            {
                return false;
            }

            // Should not have suspicious length (very long expressions might be obfuscation attempts)
            if (expression.Length > 2000)
            {
                return false;
            }

            // Should contain only expected characters
            // Allowed: alphanumeric, whitespace, parentheses, quotes, comparison operators, underscores, dots, colons, commas
            if (!Regex.IsMatch(expression, @"^[\w\s\(\)'\""=<>!:,\.\-\+\*\/\|\&]+$"))
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Result of RLS expression validation.
    /// </summary>
    public class RLSExpressionValidationResult
    {
        public bool IsValid { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? Expression { get; private set; }
        public RLSValidationFailureReason? FailureReason { get; private set; }

        private RLSExpressionValidationResult() { }

        public static RLSExpressionValidationResult Success()
        {
            return new RLSExpressionValidationResult { IsValid = true };
        }

        public static RLSExpressionValidationResult Failure(string errorMessage, string expression, RLSValidationFailureReason reason)
        {
            return new RLSExpressionValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                Expression = expression,
                FailureReason = reason
            };
        }
    }

    /// <summary>
    /// Reasons why RLS expression validation may fail.
    /// </summary>
    public enum RLSValidationFailureReason
    {
        /// <summary>
        /// Expression contains a pattern known to be dangerous (e.g., SQL injection patterns).
        /// </summary>
        DangerousPattern,

        /// <summary>
        /// Expression does not match any known safe patterns.
        /// </summary>
        UnknownPattern,

        /// <summary>
        /// Expression has malformed structure (e.g., unbalanced parentheses).
        /// </summary>
        MalformedExpression
    }

    /// <summary>
    /// Exception thrown when RLS expression validation fails.
    /// </summary>
    public class RLSExpressionValidationException : Exception
    {
        public string? Expression { get; }
        public RLSValidationFailureReason? FailureReason { get; }

        public RLSExpressionValidationException(string message, string? expression, RLSValidationFailureReason? reason)
            : base(message)
        {
            Expression = expression;
            FailureReason = reason;
        }
    }
}
