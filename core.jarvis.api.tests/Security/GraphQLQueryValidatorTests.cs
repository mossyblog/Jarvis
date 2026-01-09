using core.jarvis.api.Security;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Security;

/// <summary>
/// Tests for GraphQL query validation to prevent DoS attacks.
///
/// INTENT: Verify GraphQL queries are validated against security limits
/// PURPOSE: Prevent denial-of-service through deeply nested or overly complex queries
/// BUSINESS CONTEXT: GraphQL flexibility can be abused without proper limits
/// WHY IMPORTANT: Protects API from query-based DoS attacks
/// ARCHITECTURAL SIGNIFICANCE: Validates security controls at API boundary
/// FUTURE RESILIENCE: Ensures query limits are enforced as API evolves
/// </summary>
public class GraphQLQueryValidatorTests
{
    private readonly GraphQLQueryValidator _validator;
    private readonly GraphQLQueryValidator _productionValidator;

    public GraphQLQueryValidatorTests()
    {
        // Default validator for most tests (non-production mode)
        _validator = new GraphQLQueryValidator(new GraphQLValidationOptions(), isProduction: false);

        // Production validator for introspection tests
        _productionValidator = new GraphQLQueryValidator(new GraphQLValidationOptions { BlockIntrospection = true }, isProduction: true);
    }

    #region Query Depth Tests

    /// <summary>
    /// INTENT: Verify simple queries within depth limit pass validation
    /// </summary>
    [Fact]
    public void Validate_SimpleQuery_ReturnsSuccess()
    {
        // Arrange
        var query = "{ users { id name } }";

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// INTENT: Verify query depth is correctly calculated
    /// </summary>
    [Fact]
    public void Validate_QueryAtMaxDepth_ReturnsSuccess()
    {
        // Arrange - 10 levels of nesting (default max)
        var query = "{ l1 { l2 { l3 { l4 { l5 { l6 { l7 { l8 { l9 { l10 } } } } } } } } } }";
        var validator = new GraphQLQueryValidator(new GraphQLValidationOptions { MaxQueryDepth = 10 }, isProduction: false);

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify queries exceeding max depth are rejected
    /// </summary>
    [Fact]
    public void Validate_QueryExceedsMaxDepth_ReturnsError()
    {
        // Arrange - 11 levels of nesting
        var query = "{ l1 { l2 { l3 { l4 { l5 { l6 { l7 { l8 { l9 { l10 { l11 } } } } } } } } } } }";
        var validator = new GraphQLQueryValidator(new GraphQLValidationOptions { MaxQueryDepth = 10 }, isProduction: false);

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("QUERY_TOO_DEEP");
        result.ErrorMessage.ShouldContain("11");
        result.ErrorMessage.ShouldContain("10");
    }

    /// <summary>
    /// INTENT: Verify custom depth limit is respected
    /// </summary>
    [Fact]
    public void Validate_CustomDepthLimit_EnforcesLimit()
    {
        // Arrange
        var options = new GraphQLValidationOptions { MaxQueryDepth = 3 };
        var validator = new GraphQLQueryValidator(options, isProduction: false);
        var query = "{ a { b { c { d } } } }"; // Depth 4

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("QUERY_TOO_DEEP");
    }

    #endregion

    #region Field Count Tests

    /// <summary>
    /// INTENT: Verify queries with reasonable field count pass
    /// </summary>
    [Fact]
    public void Validate_ReasonableFieldCount_ReturnsSuccess()
    {
        // Arrange
        var query = "{ user { id name email createdAt updatedAt } }";

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify queries exceeding max field count are rejected
    /// </summary>
    [Fact]
    public void Validate_TooManyFields_ReturnsError()
    {
        // Arrange - create query with many fields
        var options = new GraphQLValidationOptions { MaxFieldCount = 5 };
        var validator = new GraphQLQueryValidator(options, isProduction: false);
        var query = "{ users { id name email phone address city state country zip company } }"; // 10 fields in nested

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("TOO_MANY_FIELDS");
    }

    /// <summary>
    /// INTENT: Verify field count at exactly max limit passes
    /// </summary>
    [Fact]
    public void Validate_FieldCountAtLimit_ReturnsSuccess()
    {
        // Arrange - "users" counts as a field too, plus 5 nested fields = 6 total
        var options = new GraphQLValidationOptions { MaxFieldCount = 6 };
        var validator = new GraphQLQueryValidator(options, isProduction: false);
        var query = "{ users { id name email phone address } }"; // 6 fields total (users + 5 nested)

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    #endregion

    #region Query Length Tests

    /// <summary>
    /// INTENT: Verify queries exceeding max length are rejected
    /// </summary>
    [Fact]
    public void Validate_QueryTooLong_ReturnsError()
    {
        // Arrange
        var options = new GraphQLValidationOptions { MaxQueryLength = 100 };
        var validator = new GraphQLQueryValidator(options, isProduction: false);
        var query = new string('a', 101);

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("QUERY_TOO_LONG");
        result.ErrorMessage.ShouldContain("100");
    }

    /// <summary>
    /// INTENT: Verify queries at max length pass
    /// </summary>
    [Fact]
    public void Validate_QueryAtMaxLength_ReturnsSuccess()
    {
        // Arrange
        var options = new GraphQLValidationOptions { MaxQueryLength = 100 };
        var validator = new GraphQLQueryValidator(options, isProduction: false);
        var query = "{ " + new string('a', 96) + " }"; // Exactly 100 chars

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    #endregion

    #region Introspection Tests

    /// <summary>
    /// INTENT: Verify introspection queries are blocked in production
    /// </summary>
    [Fact]
    public void Validate_IntrospectionInProduction_ReturnsError()
    {
        // Arrange
        var query = "{ __schema { types { name } } }";

        // Act
        var result = _productionValidator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INTROSPECTION_BLOCKED");
    }

    /// <summary>
    /// INTENT: Verify __type introspection is blocked in production
    /// </summary>
    [Fact]
    public void Validate_TypeIntrospectionInProduction_ReturnsError()
    {
        // Arrange
        var query = "{ __type(name: \"User\") { name fields { name } } }";

        // Act
        var result = _productionValidator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INTROSPECTION_BLOCKED");
    }

    /// <summary>
    /// INTENT: Verify introspection is allowed in non-production
    /// </summary>
    [Fact]
    public void Validate_IntrospectionInDevelopment_ReturnsSuccess()
    {
        // Arrange
        var query = "{ __schema { types { name } } }";

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify introspection blocking can be disabled
    /// </summary>
    [Fact]
    public void Validate_IntrospectionAllowedWhenConfigured_ReturnsSuccess()
    {
        // Arrange
        var options = new GraphQLValidationOptions { BlockIntrospection = false };
        var validator = new GraphQLQueryValidator(options, isProduction: true);
        var query = "{ __schema { types { name } } }";

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// INTENT: Verify empty query is rejected
    /// </summary>
    [Fact]
    public void Validate_EmptyQuery_ReturnsError()
    {
        // Arrange
        var query = "";

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("EMPTY_QUERY");
    }

    /// <summary>
    /// INTENT: Verify whitespace-only query is rejected
    /// </summary>
    [Fact]
    public void Validate_WhitespaceQuery_ReturnsError()
    {
        // Arrange
        var query = "   \t\n   ";

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("EMPTY_QUERY");
    }

    /// <summary>
    /// INTENT: Verify null query is rejected
    /// </summary>
    [Fact]
    public void Validate_NullQuery_ReturnsError()
    {
        // Act
        var result = _validator.Validate(null!);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("EMPTY_QUERY");
    }

    /// <summary>
    /// INTENT: Verify queries with arguments are handled correctly
    /// </summary>
    [Fact]
    public void Validate_QueryWithArguments_ReturnsSuccess()
    {
        // Arrange
        var query = @"query GetUser($id: ID!) { user(id: $id) { id name email } }";

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify queries with aliases are counted correctly
    /// </summary>
    [Fact]
    public void Validate_QueryWithAliases_CountsFieldsCorrectly()
    {
        // Arrange
        var options = new GraphQLValidationOptions { MaxFieldCount = 3 };
        var validator = new GraphQLQueryValidator(options, isProduction: false);
        var query = "{ user1: user { id } user2: user { id } user3: user { id } user4: user { id } }";

        // Act
        var result = validator.Validate(query);

        // Assert
        // This query has multiple aliased fields - should exceed limit
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("TOO_MANY_FIELDS");
    }

    /// <summary>
    /// INTENT: Verify fragments don't bypass field counting
    /// </summary>
    [Fact]
    public void Validate_QueryWithFragments_ValidatesCorrectly()
    {
        // Arrange
        var query = @"
            query {
                user {
                    ...UserFields
                }
            }
            fragment UserFields on User {
                id
                name
                email
            }
        ";

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify mutation queries are validated
    /// </summary>
    [Fact]
    public void Validate_MutationQuery_ValidatesCorrectly()
    {
        // Arrange
        var query = @"mutation CreateUser($input: UserInput!) { createUser(input: $input) { id name } }";

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify deeply nested mutation is rejected
    /// </summary>
    [Fact]
    public void Validate_DeeplyNestedMutation_ReturnsError()
    {
        // Arrange
        var options = new GraphQLValidationOptions { MaxQueryDepth = 5 };
        var validator = new GraphQLQueryValidator(options, isProduction: false);
        var query = @"mutation { createUser { profile { address { city { country { continent { planet } } } } } } }";

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("QUERY_TOO_DEEP");
    }

    #endregion

    #region Default Options Tests

    /// <summary>
    /// INTENT: Verify default options are sensible
    /// </summary>
    [Fact]
    public void DefaultOptions_HaveSensibleDefaults()
    {
        // Arrange
        var options = new GraphQLValidationOptions();

        // Assert
        options.MaxQueryDepth.ShouldBe(10);
        options.MaxFieldCount.ShouldBe(100);
        options.MaxQueryLength.ShouldBe(10000);
        options.BlockIntrospection.ShouldBeTrue();
        options.MaxAliases.ShouldBe(10);
    }

    #endregion

    #region Alias Multiplication Attack Tests

    /// <summary>
    /// INTENT: Verify queries with too many aliases are rejected (DoS prevention)
    /// SECURITY: Prevents alias multiplication attack where same expensive field is queried multiple times
    /// </summary>
    [Fact]
    public void Validate_TooManyAliases_ReturnsError()
    {
        // Arrange
        var options = new GraphQLValidationOptions { MaxAliases = 3 };
        var validator = new GraphQLQueryValidator(options, isProduction: false);
        // 5 aliases - exceeds limit of 3
        var query = "{ u1: user { id } u2: user { id } u3: user { id } u4: user { id } u5: user { id } }";

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("TOO_MANY_ALIASES");
        result.ErrorMessage.ShouldContain("5");
        result.ErrorMessage.ShouldContain("3");
    }

    /// <summary>
    /// INTENT: Verify queries at alias limit pass validation
    /// </summary>
    [Fact]
    public void Validate_AliasesAtLimit_ReturnsSuccess()
    {
        // Arrange
        var options = new GraphQLValidationOptions { MaxAliases = 3, MaxFieldCount = 100 };
        var validator = new GraphQLQueryValidator(options, isProduction: false);
        // 3 aliases - exactly at limit
        var query = "{ u1: user { id } u2: user { id } u3: user { id } }";

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify alias multiplication attack is blocked
    /// SECURITY: Each alias causes a separate database query execution
    /// </summary>
    [Fact]
    public void Validate_AliasMultiplicationAttack_IsBlocked()
    {
        // Arrange - simulate alias multiplication attack
        var options = new GraphQLValidationOptions { MaxAliases = 10 };
        var validator = new GraphQLQueryValidator(options, isProduction: false);

        // Build an attack query with 15 aliases - each would execute the expensive user query
        var aliases = string.Join(" ", Enumerable.Range(1, 15).Select(i => $"user{i}: user {{ id name email }}"));
        var query = $"{{ {aliases} }}";

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("TOO_MANY_ALIASES");
    }

    /// <summary>
    /// INTENT: Verify variable type definitions are not counted as aliases
    /// </summary>
    [Fact]
    public void Validate_VariableDefinitions_NotCountedAsAliases()
    {
        // Arrange
        var options = new GraphQLValidationOptions { MaxAliases = 1 };
        var validator = new GraphQLQueryValidator(options, isProduction: false);
        // This has variable definitions like $id: ID! which look like aliases but aren't
        var query = @"query GetUser($id: ID!, $name: String) { user(id: $id) { id name } }";

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify queries without aliases pass
    /// </summary>
    [Fact]
    public void Validate_NoAliases_ReturnsSuccess()
    {
        // Arrange
        var options = new GraphQLValidationOptions { MaxAliases = 1 };
        var validator = new GraphQLQueryValidator(options, isProduction: false);
        var query = "{ user { id name email } }";

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    #endregion

    #region Unbalanced Brace DoS Prevention Tests (jarvis-44)

    /// <summary>
    /// INTENT: Verify queries with more opening braces than closing are rejected
    /// SECURITY: Prevents DoS attack using queries like "{ { { { { { {"
    /// </summary>
    [Fact]
    public void Validate_UnbalancedOpeningBraces_ReturnsError()
    {
        // Arrange - Attack pattern with many opening braces
        var query = "query { users { { { { { { { { { { {";

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("UNBALANCED_BRACES");
    }

    /// <summary>
    /// INTENT: Verify queries with more closing braces than opening are rejected
    /// SECURITY: Prevents malformed queries with unbalanced structure
    /// </summary>
    [Fact]
    public void Validate_UnbalancedClosingBraces_ReturnsError()
    {
        // Arrange
        var query = "{ users } } } } }";

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("UNBALANCED_BRACES");
    }

    /// <summary>
    /// INTENT: Verify balanced braces pass validation
    /// </summary>
    [Fact]
    public void Validate_BalancedBraces_ReturnsSuccess()
    {
        // Arrange
        var query = "{ users { profile { settings { theme } } } }";

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// INTENT: Verify mixed content with balanced braces passes
    /// </summary>
    [Fact]
    public void Validate_ComplexBalancedQuery_ReturnsSuccess()
    {
        // Arrange
        var query = @"
            query GetUserData($id: ID!) {
                user(id: $id) {
                    id
                    name
                    profile {
                        avatar
                        settings {
                            theme
                        }
                    }
                }
            }
        ";

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    #endregion
}
