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