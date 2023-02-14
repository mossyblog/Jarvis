using System.Runtime.CompilerServices;
using FJarvis.Data;
using FJarvis.Data.Traits;
using Shouldly;

namespace FJarvis.Entities.Tests;

public class EntityManagerTests
{
    [SetUp]
    public void Setup()
    {
    }

    // EnityManager Should Create & Initialize an Entity with Default Values
    [Test]
    public void EntityManager_Should_CreateAndInitializeEntity_With_DefaultValues()
    {
        // Arrange
        Jarvis.Initialize();
        var entityManager = Jarvis.EntityManager();
        
        // Act
        var entityViaCreate = entityManager.CreatEntity();
        var entityViaInstance = new Entity();
        
        // Assert
        entityViaCreate.Id.ShouldNotBe(Guid.Empty);
        entityViaInstance.Id.ShouldBe(Guid.Empty);
        
        entityViaCreate.UpdatedAt.ShouldNotBe(DateTime.MinValue);
        entityViaCreate.CreatedAt.ShouldNotBe(DateTime.MinValue);
        entityViaInstance.UpdatedAt.ShouldBe(DateTime.MinValue);
        entityViaInstance.CreatedAt.ShouldBe(DateTime.MinValue);
        
        entityManager.EntityExists(entityViaInstance).ShouldBeFalse();
        entityManager.EntityExists(entityViaCreate).ShouldBeTrue();
        entityManager.GetEntityInfo(entityViaCreate).EntityId.ShouldBe(entityViaCreate);
        Should.Throw<Exception>(() => entityManager.GetEntityInfo(entityViaInstance));
    }
    
    // EntityManager Should Return EntityInfo for Entity with default Headers.
    [Test]
    public void EntityManager_Should_ReturnEntityInfoForEntity_With_DefaultHeaders()
    {
        // Arrange
        Jarvis.Initialize();
        var entityManager = Jarvis.EntityManager();
        var entity = entityManager.CreatEntity();
        
        // Act
        var entityInfo = entityManager.GetEntityInfo(entity);
        
        // Assert
        
        // EntityInfo EntityId should be the same as the above entity.
        entityInfo.EntityId.ShouldBe(entity);
        entityInfo.GetSize().ShouldBe(64);
        entityInfo.HasTraits().ShouldBeFalse();
        entityInfo.Validate().ShouldBeFalse();
    }
   
    // EntityManager Should Set Trait for Entity
    [Test]
    public void EntityManager_Should_SetTraitForEntity()
    {
        // Arrange
        Jarvis.Initialize();
        var entityManager = Jarvis.EntityManager();
        var entity = entityManager.CreatEntity();
        var flight = new Flight();
        
        // Act
        entityManager.AddTraitData(entity, flight);
        
        // Assert
        
    }

    
   
}