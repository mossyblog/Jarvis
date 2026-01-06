using core.jarvis.Exceptions;
using Shouldly;

namespace core.jarvis.tests.Unit.Exceptions;

/// <summary>
/// Tests for EntityNotFoundException functionality.
/// </summary>
public class EntityNotFoundExceptionTests
{
    /// <summary>
    /// Validates that EntityNotFoundException constructor properly initializes all properties with valid parameters.
    /// 
    /// INTENT: Verify exception constructor sets EntityId, EntityType, Code, and Message properties correctly
    /// PURPOSE: Ensure exception instances contain all necessary context for error handling and debugging
    /// BUSINESS CONTEXT: When entities are not found, detailed error information helps identify missing data
    /// WHY IMPORTANT: Exception properties enable proper error reporting and troubleshooting workflows
    /// ARCHITECTURAL SIGNIFICANCE: Validates domain exception constructor pattern for consistent error context
    /// FUTURE RESILIENCE: Ensures exception structure remains intact as error handling evolves
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_ShouldSetProperties()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entityType = "TestComponent";

        // Act
        var exception = new EntityNotFoundException(entityId, entityType);

        // Assert
        exception.EntityId.ShouldBe(entityId);
        exception.EntityType.ShouldBe(entityType);
        exception.Code.ShouldBe("NOT_FOUND");
        exception.Message.ShouldContain("TestComponent");
        exception.Message.ShouldContain(entityId.ToString());
    }


    /// <summary>
    /// Validates that EntityNotFoundException inherits from DomainException for proper exception hierarchy.
    /// 
    /// INTENT: Verify EntityNotFoundException maintains correct inheritance structure
    /// PURPOSE: Ensure exception can be caught as DomainException for general domain error handling
    /// BUSINESS CONTEXT: Domain exceptions need consistent handling across the application layer
    /// WHY IMPORTANT: Exception hierarchy enables proper catch blocks and error categorization
    /// ARCHITECTURAL SIGNIFICANCE: Validates domain exception pattern compliance
    /// FUTURE RESILIENCE: Ensures exception hierarchy remains stable for error handling strategies
    /// </summary>
    [Fact]
    public void Exception_ShouldBeInstanceOfDomainException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var componentType = "TestComponent";

        // Act
        var exception = new EntityNotFoundException(entityId, componentType);

        // Assert
        exception.ShouldBeAssignableTo<DomainException>();
    }

    /// <summary>
    /// Validates that EntityNotFoundException can be thrown and caught as its specific type.
    /// 
    /// INTENT: Verify exception can be raised and handled as EntityNotFoundException
    /// PURPOSE: Ensure exception type is properly throwable for specific error handling
    /// BUSINESS CONTEXT: Specific exception handling allows targeted error recovery strategies
    /// WHY IMPORTANT: Type-specific catches enable precise error handling and user feedback
    /// ARCHITECTURAL SIGNIFICANCE: Validates exception usability in standard .NET error patterns
    /// FUTURE RESILIENCE: Ensures exception remains usable in standard exception handling workflows
    /// </summary>
    [Fact]
    public void Exception_ShouldBeThrowableAndCatchable()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entityType = "TestComponent";

        // Act & Assert
        Should.Throw<EntityNotFoundException>(() => 
            throw new EntityNotFoundException(entityId, entityType));
    }

    /// <summary>
    /// Validates that EntityNotFoundException can be caught as the base DomainException type.
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
        // Arrange
        var entityId = Guid.NewGuid();
        var entityType = "TestComponent";

        // Act & Assert
        Should.Throw<DomainException>(() => 
            throw new EntityNotFoundException(entityId, entityType));
    }
}