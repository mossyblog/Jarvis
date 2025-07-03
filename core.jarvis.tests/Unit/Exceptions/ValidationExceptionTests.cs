using core.jarvis.Exceptions;
using Shouldly;

namespace core.jarvis.tests.Unit.Exceptions;

public class ValidationExceptionTests
{
    public class SingleFieldConstructor
    {
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

    public class MultipleFieldsConstructor
    {
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