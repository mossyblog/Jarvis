using core.jarvis.Exceptions;
using Shouldly;

namespace core.jarvis.tests.Unit.Exceptions;

/// <summary>
/// Tests for EntityNotFoundException functionality.
/// </summary>
public class EntityNotFoundExceptionTests
{
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

    [Fact]
    public void Constructor_WithNullEntityType_ShouldStillWork()
    {
        // Arrange
        var entityId = Guid.NewGuid();

        // Act
        var exception = new EntityNotFoundException(entityId, null!);

        // Assert
        exception.EntityId.ShouldBe(entityId);
        exception.EntityType.ShouldBeNull();
        exception.Code.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldStillWork()
    {
        // Arrange
        var entityId = Guid.Empty;
        var entityType = "TestComponent";

        // Act
        var exception = new EntityNotFoundException(entityId, entityType);

        // Assert
        exception.EntityId.ShouldBe(Guid.Empty);
        exception.EntityType.ShouldBe(entityType);
    }

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