using core.jarvis.Exceptions;
using Shouldly;

namespace core.jarvis.tests.Unit.Exceptions;

/// <summary>
/// Tests for ValidationException functionality and constructor variations.
/// </summary>
public class ValidationExceptionTests
{
    /// <summary>
    /// Tests for single field validation error constructor.
    /// </summary>
    public class SingleFieldConstructor
    {
        /// <summary>
        /// Validates that ValidationException constructor properly initializes single field validation errors.
        /// 
        /// INTENT: Verify single field validation error construction and property initialization
        /// PURPOSE: Ensure single validation errors are properly encapsulated for error handling
        /// BUSINESS CONTEXT: Form validation often focuses on individual field errors like "email is required"
        /// WHY IMPORTANT: Single field errors enable targeted user feedback and form validation
        /// ARCHITECTURAL SIGNIFICANCE: Validates simple validation error pattern for single field scenarios
        /// FUTURE RESILIENCE: Ensures single field validation remains functional as validation evolves
        /// </summary>
        [Fact]
        public void ShouldSetFieldAndError()
        {
            // Arrange
            var field = "email";
            var error = "Email is required";

            // Act
            var exception = new ValidationException(field, error);

            // Assert
            exception.Code.ShouldBe("VALIDATION_ERROR");
            exception.Message.ShouldBe($"Validation failed for {field}");
            exception.Errors.ShouldContainKey(field);
            exception.Errors[field].ShouldBe(new[] { error });
        }
    }

    /// <summary>
    /// Tests for multiple field validation error constructor.
    /// </summary>
    public class MultipleFieldsConstructor
    {
        /// <summary>
        /// Validates that ValidationException constructor properly initializes multiple field validation errors.
        /// 
        /// INTENT: Verify multiple field validation error construction and error dictionary handling
        /// PURPOSE: Ensure complex validation scenarios with multiple field errors are properly handled
        /// BUSINESS CONTEXT: Complex forms require validation of multiple fields simultaneously
        /// WHY IMPORTANT: Multiple field errors enable comprehensive form validation feedback
        /// ARCHITECTURAL SIGNIFICANCE: Validates complex validation error pattern for multi-field scenarios
        /// FUTURE RESILIENCE: Ensures multiple field validation remains functional for complex forms
        /// </summary>
        [Fact]
        public void ShouldSetAllErrors()
        {
            // Arrange
            var errors = new Dictionary<string, string[]>
            {
                ["email"] = new[] { "Email is required", "Email format is invalid" },
                ["password"] = new[] { "Password is too short" }
            };

            // Act
            var exception = new ValidationException(errors);

            // Assert
            exception.Code.ShouldBe("VALIDATION_ERROR");
            exception.Message.ShouldBe("Validation failed for multiple fields");
            exception.Errors.ShouldBe(errors);
        }

    }
}