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
        /// Validates that Guard.Against() throws BusinessRuleException when condition is true.
        /// 
        /// INTENT: Verify Guard.Against enforces business rule violations with proper exception type
        /// PURPOSE: Ensure business rule guards fail fast when validation conditions are met
        /// BUSINESS CONTEXT: Business rules like "cannot approve paid invoice" need enforcement
        /// WHY IMPORTANT: Business rule violations should immediately prevent further processing
        /// ARCHITECTURAL SIGNIFICANCE: Validates guard pattern for business rule enforcement
        /// FUTURE RESILIENCE: Ensures business rule guards remain consistent across handlers
        /// </summary>
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
        /// Validates that Guard.Against() does not throw when condition is false.
        /// 
        /// INTENT: Verify Guard.Against allows processing when validation conditions pass
        /// PURPOSE: Ensure business rule guards don't interfere with valid operations
        /// BUSINESS CONTEXT: Valid business states should allow operations to proceed normally
        /// WHY IMPORTANT: Guards should not create false positives that block valid operations
        /// ARCHITECTURAL SIGNIFICANCE: Validates guard pattern doesn't introduce unnecessary barriers
        /// FUTURE RESILIENCE: Ensures guards remain permissive for valid business scenarios
        /// </summary>
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
        /// 
        /// INTENT: Verify null parameter validation throws appropriate exception type
        /// PURPOSE: Ensure null checks prevent null reference exceptions in business logic
        /// BUSINESS CONTEXT: Handler parameters must be validated to prevent runtime errors
        /// WHY IMPORTANT: Null validation prevents cascading errors in business operations
        /// ARCHITECTURAL SIGNIFICANCE: Validates parameter validation pattern for dependency safety
        /// FUTURE RESILIENCE: Ensures null checks remain consistent across all handlers
        /// </summary>
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
        /// Validates that Guard.AgainstNull does not throw for non-null values.
        /// 
        /// INTENT: Verify null guards allow valid non-null parameters to pass through
        /// PURPOSE: Ensure null validation doesn't interfere with valid parameter values
        /// BUSINESS CONTEXT: Valid handler parameters should not be blocked by validation
        /// WHY IMPORTANT: Parameter validation should not create false positives
        /// ARCHITECTURAL SIGNIFICANCE: Validates guard pattern allows valid inputs
        /// FUTURE RESILIENCE: Ensures parameter validation remains transparent for valid inputs
        /// </summary>
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
        /// Validates that Guard.AgainstEmpty throws ValidationException for null/empty/whitespace strings.
        /// 
        /// INTENT: Verify string validation rejects empty, null, and whitespace-only values
        /// PURPOSE: Ensure string parameters have meaningful content for business operations
        /// BUSINESS CONTEXT: Business operations require valid string inputs like customer names, descriptions
        /// WHY IMPORTANT: Empty strings can cause business logic failures or invalid data storage
        /// ARCHITECTURAL SIGNIFICANCE: Validates comprehensive string validation pattern
        /// FUTURE RESILIENCE: Ensures string validation catches all forms of empty input
        /// </summary>
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
        /// Validates that Guard.AgainstEmpty does not throw for non-empty strings.
        /// 
        /// INTENT: Verify string validation allows valid string content to pass through
        /// PURPOSE: Ensure string validation doesn't interfere with meaningful content
        /// BUSINESS CONTEXT: Valid string inputs should proceed to business logic without hindrance
        /// WHY IMPORTANT: String validation should not block legitimate business data
        /// ARCHITECTURAL SIGNIFICANCE: Validates guard pattern transparency for valid inputs
        /// FUTURE RESILIENCE: Ensures string validation remains permissive for valid content
        /// </summary>
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
        /// Validates that Guard.AgainstEmptyGuid throws ValidationException for Guid.Empty.
        /// 
        /// INTENT: Verify GUID validation rejects empty GUID values
        /// PURPOSE: Ensure entity identifiers are valid for database operations
        /// BUSINESS CONTEXT: Entity operations require valid GUIDs to identify records
        /// WHY IMPORTANT: Empty GUIDs cause database query failures and data integrity issues
        /// ARCHITECTURAL SIGNIFICANCE: Validates entity identifier validation pattern
        /// FUTURE RESILIENCE: Ensures GUID validation remains consistent across entity operations
        /// </summary>
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
        /// Validates that Guard.AgainstEmptyGuid does not throw for valid GUIDs.
        /// 
        /// INTENT: Verify GUID validation allows valid GUID values to pass through
        /// PURPOSE: Ensure GUID validation doesn't interfere with legitimate entity identifiers
        /// BUSINESS CONTEXT: Valid entity GUIDs should proceed to database operations normally
        /// WHY IMPORTANT: GUID validation should not block legitimate entity references
        /// ARCHITECTURAL SIGNIFICANCE: Validates guard pattern transparency for valid identifiers
        /// FUTURE RESILIENCE: Ensures GUID validation remains permissive for valid entity IDs
        /// </summary>
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
        /// Validates that Guard.AgainstOutOfRange throws ValidationException for out-of-range values.
        /// 
        /// INTENT: Verify range validation rejects values outside specified bounds
        /// PURPOSE: Ensure numeric parameters fall within acceptable business ranges
        /// BUSINESS CONTEXT: Business operations have valid ranges like age 0-120, percentage 0-100
        /// WHY IMPORTANT: Out-of-range values can cause business logic errors or invalid calculations
        /// ARCHITECTURAL SIGNIFICANCE: Validates range validation pattern for numeric constraints
        /// FUTURE RESILIENCE: Ensures range validation remains consistent across numeric parameters
        /// </summary>
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
        /// Validates that Guard.AgainstOutOfRange does not throw for in-range values.
        /// 
        /// INTENT: Verify range validation allows values within specified bounds to pass through
        /// PURPOSE: Ensure range validation doesn't interfere with valid numeric parameters
        /// BUSINESS CONTEXT: Valid numeric inputs should proceed to business calculations normally
        /// WHY IMPORTANT: Range validation should not block legitimate numeric values
        /// ARCHITECTURAL SIGNIFICANCE: Validates guard pattern transparency for valid ranges
        /// FUTURE RESILIENCE: Ensures range validation remains permissive for valid numeric inputs
        /// </summary>
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
        /// Validates that Guard.AgainstOutOfRange works with decimal values.
        /// 
        /// INTENT: Verify range validation supports decimal precision for financial calculations
        /// PURPOSE: Ensure decimal range validation works correctly for monetary values
        /// BUSINESS CONTEXT: Financial operations require precise decimal range validation
        /// WHY IMPORTANT: Decimal precision is critical for financial calculations and validations
        /// ARCHITECTURAL SIGNIFICANCE: Validates range validation supports various numeric types
        /// FUTURE RESILIENCE: Ensures decimal range validation remains accurate for financial data
        /// </summary>
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