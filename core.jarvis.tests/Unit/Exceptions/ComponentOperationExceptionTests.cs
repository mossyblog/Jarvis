using core.jarvis.Exceptions;
using Shouldly;

namespace core.jarvis.tests.Unit.Exceptions;

/// <summary>
/// Tests for ComponentOperationException functionality.
/// </summary>
public class ComponentOperationExceptionTests
{
    [Fact]
    public void Constructor_WithoutInnerException_ShouldSetProperties()
    {
        // Arrange
        var componentType = "Invoice";
        var operation = "GET";
        var message = "Failed to retrieve invoice";

        // Act
        var exception = new ComponentOperationException(componentType, operation, message);

        // Assert
        exception.ComponentType.ShouldBe(componentType);
        exception.Operation.ShouldBe(operation);
        exception.Message.ShouldBe(message);
        exception.Code.ShouldBe("INVOICE_GET_FAILED");
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetProperties()
    {
        // Arrange
        var componentType = "Payment";
        var operation = "UPDATE";
        var message = "Failed to update payment";
        var innerException = new InvalidOperationException("Database error");

        // Act
        var exception = new ComponentOperationException(componentType, operation, message, innerException);

        // Assert
        exception.ComponentType.ShouldBe(componentType);
        exception.Operation.ShouldBe(operation);
        exception.Message.ShouldBe(message);
        exception.Code.ShouldBe("PAYMENT_UPDATE_FAILED");
        exception.InnerException.ShouldBe(innerException);
    }

    [Fact]
    public void Constructor_WithLowerCaseComponentAndOperation_ShouldGenerateUpperCaseCode()
    {
        // Arrange
        var componentType = "workorder";
        var operation = "create";
        var message = "Failed to create work order";

        // Act
        var exception = new ComponentOperationException(componentType, operation, message);

        // Assert
        exception.ComponentType.ShouldBe(componentType);
        exception.Operation.ShouldBe(operation);
        exception.Code.ShouldBe("WORKORDER_CREATE_FAILED");
    }

    [Fact]
    public void Exception_ShouldBeInstanceOfDomainException()
    {
        // Arrange & Act
        var exception = new ComponentOperationException("Test", "OP", "Message");

        // Assert
        exception.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void Exception_ShouldBeThrowableAndCatchable()
    {
        // Act & Assert
        Should.Throw<ComponentOperationException>(() => 
            throw new ComponentOperationException("Test", "OP", "Message"));
    }

    [Fact]
    public void Exception_ShouldBeCatchableAsDomainException()
    {
        // Act & Assert
        Should.Throw<DomainException>(() => 
            throw new ComponentOperationException("Test", "OP", "Message"));
    }

    [Fact]
    public void Context_ShouldContainComponentTypeAndOperation()
    {
        // Arrange
        var componentType = "Customer";
        var operation = "DELETE";
        var message = "Failed to delete customer";

        // Act
        var exception = new ComponentOperationException(componentType, operation, message);

        // Assert
        exception.Context.ShouldNotBeNull();
        var context = exception.Context as dynamic;
        ((string)context!.ComponentType).ShouldBe(componentType);
        ((string)context!.Operation).ShouldBe(operation);
    }

    [Fact]
    public void Constructor_WithNullValues_ShouldStillWork()
    {
        // Act & Assert
        Should.NotThrow(() => 
            new ComponentOperationException(null!, null!, null!));
    }

    [Fact]
    public void Constructor_WithEmptyValues_ShouldStillWork()
    {
        // Act
        var exception = new ComponentOperationException("", "", "");

        // Assert
        exception.ComponentType.ShouldBe("");
        exception.Operation.ShouldBe("");
        exception.Code.ShouldBe("_FAILED");
    }
}