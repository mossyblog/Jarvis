using core.jarvis.Exceptions;
using Shouldly;

namespace core.jarvis.tests.Unit.Exceptions;

public class DomainExceptionTests
{
    // Create a concrete implementation for testing
    private class TestDomainException : DomainException
    {
        public TestDomainException(string code, string message, object? context = null)
            : base(code, message, context)
        {
        }

        public TestDomainException(string code, string message, Exception innerException, object? context = null)
            : base(code, message, innerException, context)
        {
        }
    }

    [Fact]
    public void Constructor_WithoutInnerException_ShouldSetProperties()
    {
        // Arrange
        var code = "TEST_ERROR";
        var message = "Test error message";
        var context = new { Id = 123, Name = "Test" };

        // Act
        var exception = new TestDomainException(code, message, context);

        // Assert
        exception.Code.ShouldBe(code);
        exception.Message.ShouldBe(message);
        exception.Context.ShouldBe(context);
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetProperties()
    {
        // Arrange
        var code = "TEST_ERROR";
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");
        var context = new { Id = 123 };

        // Act
        var exception = new TestDomainException(code, message, innerException, context);

        // Assert
        exception.Code.ShouldBe(code);
        exception.Message.ShouldBe(message);
        exception.InnerException.ShouldBe(innerException);
        exception.Context.ShouldBe(context);
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldAllowNullContext()
    {
        // Act
        var exception = new TestDomainException("CODE", "Message", context: null);

        // Assert
        exception.Context.ShouldBeNull();
    }
}