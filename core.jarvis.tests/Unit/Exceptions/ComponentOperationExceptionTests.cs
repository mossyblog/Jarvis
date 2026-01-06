using core.jarvis.Exceptions;
using Shouldly;

namespace core.jarvis.tests.Unit.Exceptions;

/// <summary>
/// Tests for ComponentOperationException functionality.
/// </summary>
public class ComponentOperationExceptionTests
{
    /// <summary>
    /// Validates that ComponentOperationException constructor properly sets all properties without inner exception.
    /// 
    /// INTENT: Verify exception initialization with component type, operation, and message
    /// PURPOSE: Ensure exception contains all context needed for component operation error handling
    /// BUSINESS CONTEXT: Component operations like invoice retrieval can fail and need detailed error context
    /// WHY IMPORTANT: Proper error context enables targeted error handling and user feedback
    /// ARCHITECTURAL SIGNIFICANCE: Validates domain exception pattern for component operations
    /// FUTURE RESILIENCE: Ensures component operation exceptions remain informative and actionable
    /// </summary>
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

    /// <summary>
    /// Validates that ComponentOperationException constructor properly wraps inner exceptions with operation context.
    /// 
    /// INTENT: Verify exception chaining preserves underlying error details while adding operation context
    /// PURPOSE: Ensure root cause exceptions are preserved for debugging while providing business context
    /// BUSINESS CONTEXT: Database errors during payment updates need both technical and business context
    /// WHY IMPORTANT: Exception chaining enables both technical debugging and business error handling
    /// ARCHITECTURAL SIGNIFICANCE: Validates exception wrapping pattern for layered error handling
    /// FUTURE RESILIENCE: Ensures exception chains remain intact for comprehensive error analysis
    /// </summary>
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

    /// <summary>
    /// Validates that ComponentOperationException normalizes component and operation names to uppercase in error codes.
    /// 
    /// INTENT: Verify error code generation follows consistent uppercase naming conventions
    /// PURPOSE: Ensure error codes are standardized regardless of input case variations
    /// BUSINESS CONTEXT: Error codes need consistency for monitoring systems and error classification
    /// WHY IMPORTANT: Standardized error codes enable reliable error tracking and automated alerting
    /// ARCHITECTURAL SIGNIFICANCE: Validates error code normalization for consistent error reporting
    /// FUTURE RESILIENCE: Ensures error code formatting remains consistent across different input variations
    /// </summary>
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

    /// <summary>
    /// Validates that ComponentOperationException inherits from DomainException for proper exception hierarchy.
    /// 
    /// INTENT: Verify ComponentOperationException maintains correct inheritance structure
    /// PURPOSE: Ensure exception can be caught as DomainException for general domain error handling
    /// BUSINESS CONTEXT: Component operation errors are domain-level concerns requiring consistent handling
    /// WHY IMPORTANT: Exception hierarchy enables proper catch blocks and error categorization
    /// ARCHITECTURAL SIGNIFICANCE: Validates domain exception pattern compliance
    /// FUTURE RESILIENCE: Ensures exception hierarchy remains stable for error handling strategies
    /// </summary>
    [Fact]
    public void Exception_ShouldBeInstanceOfDomainException()
    {
        // Arrange & Act
        var exception = new ComponentOperationException("Test", "OP", "Message");

        // Assert
        exception.ShouldBeAssignableTo<DomainException>();
    }

    /// <summary>
    /// Validates that ComponentOperationException can be thrown and caught as its specific type.
    /// 
    /// INTENT: Verify exception can be raised and handled as ComponentOperationException
    /// PURPOSE: Ensure exception type is properly throwable for specific component operation error handling
    /// BUSINESS CONTEXT: Specific component operation errors need targeted error recovery strategies
    /// WHY IMPORTANT: Type-specific catches enable precise error handling and component-specific responses
    /// ARCHITECTURAL SIGNIFICANCE: Validates exception usability in standard .NET error patterns
    /// FUTURE RESILIENCE: Ensures exception remains usable in standard exception handling workflows
    /// </summary>
    [Fact]
    public void Exception_ShouldBeThrowableAndCatchable()
    {
        // Act & Assert
        Should.Throw<ComponentOperationException>(() => 
            throw new ComponentOperationException("Test", "OP", "Message"));
    }

    /// <summary>
    /// Validates that ComponentOperationException can be caught as the base DomainException type.
    /// 
    /// INTENT: Verify exception polymorphism works correctly for base type catching
    /// PURPOSE: Ensure exception can be handled through base exception type for general error handling
    /// BUSINESS CONTEXT: Generic domain error handlers need to catch all domain exceptions uniformly
    /// WHY IMPORTANT: Base type catching enables centralized error handling while preserving type information
    /// ARCHITECTURAL SIGNIFICANCE: Validates polymorphic exception handling patterns in the domain layer
    /// FUTURE RESILIENCE: Ensures base exception handling remains functional as exception hierarchy evolves
    /// </summary>
    [Fact]
    public void Exception_ShouldBeCatchableAsDomainException()
    {
        // Act & Assert
        Should.Throw<DomainException>(() => 
            throw new ComponentOperationException("Test", "OP", "Message"));
    }

    /// <summary>
    /// Validates that ComponentOperationException provides context information for error analysis.
    /// 
    /// INTENT: Verify exception context contains component type and operation details
    /// PURPOSE: Ensure error context is available for debugging and error analysis tools
    /// BUSINESS CONTEXT: Error analysis tools need structured context to categorize and resolve issues
    /// WHY IMPORTANT: Structured error context enables automated error tracking and resolution workflows
    /// ARCHITECTURAL SIGNIFICANCE: Validates exception context pattern for enhanced error diagnostics
    /// FUTURE RESILIENCE: Ensures error context remains accessible for evolving error analysis requirements
    /// </summary>
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

}