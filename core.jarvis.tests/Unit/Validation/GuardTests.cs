using core.jarvis.Exceptions;
using core.jarvis.Validation;
using Shouldly;

namespace core.jarvis.tests.Unit.Validation;

/// <summary>
/// Tests for Guard validation utility methods.
/// </summary>
/// <remarks>
/// <para><strong>BUSINESS CONTEXT:</strong> Guard methods provide consistent validation patterns across
/// all business logic handlers. They enforce input validation, business rules, and parameter checking.</para>
/// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Guards are used throughout handlers to provide
/// fail-fast behavior and consistent error messaging for validation failures.</para>
/// <para><strong>TEST STRATEGY:</strong> Focus on validating guard conditions, exception types,
/// and error messages to ensure consistent validation behavior across the system.</para>
/// </remarks>
public class GuardTests
{
    public class Against
    {
        /// <summary>
        /// Validates that Guard.Against() throws BusinessRuleException when the condition is true (rule violated).
        /// </summary>
        /// <remarks>
        /// <para><strong>INTENT:</strong> Validates Guard.Against() throws when business rule condition is violated.</para>
        /// <para><strong>PURPOSE:</strong> Ensures business rule violations are properly caught and reported.</para>
        /// <para><strong>BUSINESS CONTEXT:</strong> Business rules like "invoice amount must be positive" use
        /// Guard.Against() to enforce constraints. When violated, operations should stop immediately.</para>
        /// <para><strong>WHY IMPORTANT:</strong> Business rule enforcement prevents invalid business states
        /// and data corruption. Clear error messages help developers understand what went wrong.</para>
        /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Consistent exception type (BusinessRuleException)
        /// enables uniform error handling across all business logic layers.</para>
        /// </remarks>
        [Fact]
        public void WhenConditionIsTrue_ShouldThrowBusinessRuleException()
        {
            // Act & Assert
            var exception = Should.Throw<BusinessRuleException>(() => 
                Guard.Against(true, "Test failed"));

            exception.Message.ShouldBe("Test failed");
            exception.Code.ShouldBe("RULE_GUARD");
        }

        /// <summary>
        /// Validates that Guard.Against() does not throw when the condition is false (rule satisfied).
        /// </summary>
        /// <remarks>
        /// <para><strong>INTENT:</strong> Validates Guard.Against() allows execution when business rule is satisfied.</para>
        /// <para><strong>PURPOSE:</strong> Ensures valid business states don't trigger false positive exceptions.</para>
        /// <para><strong>BUSINESS CONTEXT:</strong> When business rules are satisfied, operations should proceed
        /// normally without any validation interference or performance impact.</para>
        /// <para><strong>WHY IMPORTANT:</strong> Guards should be transparent when conditions are valid,
        /// allowing business logic to execute efficiently without validation overhead.</para>
        /// </remarks>
        [Fact]
        public void WhenConditionIsFalse_ShouldNotThrow()
        {
            // Act & Assert
            Should.NotThrow(() => Guard.Against(false, "Test passed"));
        }
    }

    public class AgainstNull
    {
        /// <summary>
        /// Validates that Guard.AgainstNull() throws ArgumentNullException when value is null.
        /// </summary>
        /// <remarks>
        /// <para><strong>INTENT:</strong> Validates Guard.AgainstNull() throws when null values are provided.</para>
        /// <para><strong>PURPOSE:</strong> Ensures null parameter validation works consistently across all handlers.</para>
        /// <para><strong>BUSINESS CONTEXT:</strong> Handler methods receive parameters that must not be null
        /// to perform business operations correctly. Null values indicate programming errors or invalid input.</para>
        /// <para><strong>WHY IMPORTANT:</strong> Null reference exceptions would occur later in business logic
        /// without early validation. Clear parameter names help identify the source of invalid input.</para>
        /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Consistent ArgumentNullException type enables
        /// standard .NET error handling patterns and clear parameter identification.</para>
        /// </remarks>
        [Fact]
        public void WhenValueIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            string? nullValue = null;

            // Act & Assert
            var exception = Should.Throw<ArgumentNullException>(() => 
                Guard.AgainstNull(nullValue, "testParam"));

            exception.ParamName.ShouldBe("testParam");
        }

        /// <summary>
        /// Validates that Guard.AgainstNull does not throw for valid non-null values.
        /// </summary>
        /// <remarks>
        /// <para><strong>INTENT:</strong> Validates Guard.AgainstNull allows valid non-null values to pass through.</para>
        /// <para><strong>PURPOSE:</strong> Ensures null validation guards don't reject legitimate values, maintaining normal operation flow.</para>
        /// <para><strong>BUSINESS CONTEXT:</strong> Valid object references, strings, and other reference types must pass validation to enable normal business operations in component handlers.</para>
        /// <para><strong>WHY IMPORTANT:</strong> False positives in null validation would block legitimate operations and create unnecessary friction in business logic execution.</para>
        /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Precise null validation enables reliable object reference management while avoiding unnecessary restrictions on valid operations.</para>
        /// <para><strong>FUTURE RESILIENCE:</strong> As component handlers process diverse object types, consistent null validation ensures reliable reference management across all plugin types.</para>
        /// </remarks>
        [Fact]
        public void WhenValueIsNotNull_ShouldNotThrow()
        {
            // Arrange
            var value = "test";

            // Act & Assert
            Should.NotThrow(() => Guard.AgainstNull(value, "testParam"));
        }
    }

    public class AgainstEmpty
    {
        /// <summary>
        /// Validates that Guard.AgainstEmpty throws ValidationException for null, empty, or whitespace-only strings.
        /// </summary>
        /// <remarks>
        /// <para><strong>INTENT:</strong> Validates Guard.AgainstEmpty rejects null, empty, and whitespace-only strings.</para>
        /// <para><strong>PURPOSE:</strong> Ensures string validation guards prevent invalid empty values from entering business logic.</para>
        /// <para><strong>BUSINESS CONTEXT:</strong> Empty strings often represent incomplete user input or missing configuration values that would cause business rule violations downstream.</para>
        /// <para><strong>WHY IMPORTANT:</strong> String validation is fundamental to data integrity - empty values can cause null reference exceptions, SQL errors, or invalid business state.</para>
        /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Guard clauses provide fail-fast validation at system boundaries, preventing invalid state propagation through the component handler pipeline.</para>
        /// <para><strong>FUTURE RESILIENCE:</strong> As plugin handlers expand, consistent string validation prevents component-specific bugs and maintains uniform error handling across the ECS framework.</para>
        /// </remarks>
        /// <param name="value">Test string value (null, empty, or whitespace-only)</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void WhenStringIsNullOrWhitespace_ShouldThrowValidationException(string? value)
        {
            // Act & Assert
            var exception = Should.Throw<ValidationException>(() => 
                Guard.AgainstEmpty(value, "testParam"));

            exception.Errors.ShouldContainKey("testParam");
            exception.Errors["testParam"].ShouldContain("testParam cannot be empty");
        }

        /// <summary>
        /// Validates that Guard.AgainstEmpty does not throw for valid non-empty strings.
        /// </summary>
        /// <remarks>
        /// <para><strong>INTENT:</strong> Validates Guard.AgainstEmpty allows valid non-empty strings to pass through.</para>
        /// <para><strong>PURPOSE:</strong> Ensures string validation guards don't reject legitimate values, maintaining normal operation flow.</para>
        /// <para><strong>BUSINESS CONTEXT:</strong> Valid string inputs (component names, entity descriptions, configuration values) must pass validation to enable normal business operations.</para>
        /// <para><strong>WHY IMPORTANT:</strong> False positives in validation would block legitimate business operations and create user friction in the ECS system.</para>
        /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Guard validation must be precise - rejecting invalid input while allowing valid operations maintains system reliability without unnecessary restrictions.</para>
        /// <para><strong>FUTURE RESILIENCE:</strong> As component handlers process diverse string inputs (names, descriptions, JSON), reliable validation ensures consistent behavior across all plugin types.</para>
        /// </remarks>
        [Fact]
        public void WhenStringHasValue_ShouldNotThrow()
        {
            // Act & Assert
            Should.NotThrow(() => Guard.AgainstEmpty("valid value", "testParam"));
        }
    }

    public class AgainstEmptyGuid
    {
        /// <summary>
        /// Validates that Guard.AgainstEmptyGuid throws ValidationException for Guid.Empty values.
        /// </summary>
        /// <remarks>
        /// <para><strong>INTENT:</strong> Validates Guard.AgainstEmptyGuid rejects empty GUID values (00000000-0000-0000-0000-000000000000).</para>
        /// <para><strong>PURPOSE:</strong> Ensures GUID validation guards prevent uninitialized or default GUID values from entering business logic.</para>
        /// <para><strong>BUSINESS CONTEXT:</strong> Empty GUIDs typically represent uninitialized entity IDs, correlation IDs, or session tokens that would break entity relationships and audit trails.</para>
        /// <para><strong>WHY IMPORTANT:</strong> Entity operations depend on valid GUIDs for relationships - empty GUIDs cause data integrity violations and break component associations in the ECS pattern.</para>
        /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> GUID validation is critical for entity identity in the plugin handler system - handlers use entity IDs for component association and transaction grouping.</para>
        /// <para><strong>FUTURE RESILIENCE:</strong> As entity relationships become more complex across plugins, strict GUID validation prevents orphaned components and maintains referential integrity.</para>
        /// </remarks>
        [Fact]
        public void WhenGuidIsEmpty_ShouldThrowValidationException()
        {
            // Act & Assert
            var exception = Should.Throw<ValidationException>(() => 
                Guard.AgainstEmptyGuid(Guid.Empty, "testParam"));

            exception.Errors.ShouldContainKey("testParam");
            exception.Errors["testParam"].ShouldContain("testParam cannot be empty");
        }

        /// <summary>
        /// Validates that Guard.AgainstEmptyGuid does not throw for valid non-empty GUID values.
        /// </summary>
        /// <remarks>
        /// <para><strong>INTENT:</strong> Validates Guard.AgainstEmptyGuid allows valid non-empty GUID values to pass through.</para>
        /// <para><strong>PURPOSE:</strong> Ensures GUID validation guards don't reject legitimate entity identifiers, maintaining normal operation flow.</para>
        /// <para><strong>BUSINESS CONTEXT:</strong> Valid entity IDs, correlation IDs, and session tokens must pass validation to enable component operations and maintain audit trails.</para>
        /// <para><strong>WHY IMPORTANT:</strong> False positives in GUID validation would block legitimate entity operations and break component handler functionality in the ECS system.</para>
        /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Precise GUID validation enables reliable entity identity management while avoiding unnecessary restrictions on valid operations.</para>
        /// <para><strong>FUTURE RESILIENCE:</strong> As plugin handlers expand to manage more entity types, consistent GUID validation ensures reliable identity management across all component operations.</para>
        /// </remarks>
        [Fact]
        public void WhenGuidIsValid_ShouldNotThrow()
        {
            // Arrange
            var validGuid = Guid.NewGuid();

            // Act & Assert
            Should.NotThrow(() => Guard.AgainstEmptyGuid(validGuid, "testParam"));
        }
    }

    public class AgainstOutOfRange
    {
        /// <summary>
        /// Validates that Guard.AgainstOutOfRange throws ValidationException for values outside specified bounds.
        /// </summary>
        /// <remarks>
        /// <para><strong>INTENT:</strong> Validates Guard.AgainstOutOfRange rejects values outside the acceptable numeric range.</para>
        /// <para><strong>PURPOSE:</strong> Ensures range validation guards prevent invalid numeric values from entering business logic calculations.</para>
        /// <para><strong>BUSINESS CONTEXT:</strong> Range validation applies to percentages, quantities, priority levels, and other bounded numeric inputs that must stay within business-defined limits.</para>
        /// <para><strong>WHY IMPORTANT:</strong> Out-of-range values can cause calculation errors, invalid business state, or security vulnerabilities when used in financial or operational calculations.</para>
        /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Range guards provide input sanitization at component boundaries, ensuring handlers receive valid numeric inputs for business rule calculations.</para>
        /// <para><strong>FUTURE RESILIENCE:</strong> As component handlers process diverse numeric inputs (amounts, percentages, counts), consistent range validation prevents calculation errors across all plugin types.</para>
        /// </remarks>
        /// <param name="value">Test numeric value outside valid range</param>
        /// <param name="min">Minimum acceptable value</param>
        /// <param name="max">Maximum acceptable value</param>
        [Theory]
        [InlineData(0, 1, 10)]
        [InlineData(11, 1, 10)]
        [InlineData(-5, 0, 100)]
        public void WhenValueIsOutOfRange_ShouldThrowValidationException(int value, int min, int max)
        {
            // Act & Assert
            var exception = Should.Throw<ValidationException>(() => 
                Guard.AgainstOutOfRange(value, min, max, "testParam"));

            exception.Errors.ShouldContainKey("testParam");
            exception.Errors["testParam"].ShouldContain($"testParam must be between {min} and {max}");
        }

        /// <summary>
        /// Validates that Guard.AgainstOutOfRange does not throw for values within specified bounds.
        /// </summary>
        /// <remarks>
        /// <para><strong>INTENT:</strong> Validates Guard.AgainstOutOfRange allows values within the acceptable numeric range.</para>
        /// <para><strong>PURPOSE:</strong> Ensures range validation guards don't reject legitimate numeric values, maintaining normal calculation flow.</para>
        /// <para><strong>BUSINESS CONTEXT:</strong> Valid numeric inputs (percentages, quantities, priority levels) within business-defined bounds must pass validation to enable normal operations.</para>
        /// <para><strong>WHY IMPORTANT:</strong> False positives in range validation would block legitimate numeric operations and create user friction in business calculations.</para>
        /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Precise range validation enables reliable numeric processing while avoiding unnecessary restrictions on valid business operations.</para>
        /// <para><strong>FUTURE RESILIENCE:</strong> As component handlers process diverse numeric ranges across different business domains, reliable validation ensures consistent behavior without blocking legitimate values.</para>
        /// </remarks>
        /// <param name="value">Test numeric value within valid range</param>
        /// <param name="min">Minimum acceptable value</param>
        /// <param name="max">Maximum acceptable value</param>
        [Theory]
        [InlineData(5, 1, 10)]
        [InlineData(1, 1, 10)]
        [InlineData(10, 1, 10)]
        public void WhenValueIsInRange_ShouldNotThrow(int value, int min, int max)
        {
            // Act & Assert
            Should.NotThrow(() => Guard.AgainstOutOfRange(value, min, max, "testParam"));
        }

        /// <summary>
        /// Validates that Guard.AgainstOutOfRange throws ValidationException for decimal values outside specified bounds.
        /// </summary>
        /// <remarks>
        /// <para><strong>INTENT:</strong> Validates Guard.AgainstOutOfRange works correctly with decimal precision numeric types.</para>
        /// <para><strong>PURPOSE:</strong> Ensures range validation supports financial calculations and high-precision numeric operations.</para>
        /// <para><strong>BUSINESS CONTEXT:</strong> Financial amounts, interest rates, and measurement values often require decimal precision and must stay within specific bounds for business rule compliance.</para>
        /// <para><strong>WHY IMPORTANT:</strong> Decimal validation is critical for financial accuracy - incorrect ranges could cause monetary calculation errors or compliance violations.</para>
        /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Decimal range validation ensures component handlers can safely process financial data with appropriate precision and boundary checking.</para>
        /// <para><strong>FUTURE RESILIENCE:</strong> As financial and measurement components expand, reliable decimal validation maintains calculation accuracy across all monetary and precision-sensitive operations.</para>
        /// </remarks>
        [Fact]
        public void WithDecimals_WhenValueIsOutOfRange_ShouldThrowValidationException()
        {
            // Act & Assert
            var exception = Should.Throw<ValidationException>(() => 
                Guard.AgainstOutOfRange(10.5m, 1.0m, 10.0m, "testParam"));

            exception.Errors["testParam"].ShouldContain("testParam must be between 1.0 and 10.0");
        }
    }
}