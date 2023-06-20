using System.Runtime.CompilerServices;
using FJarvis.Data;
using FJarvis.Data.Data;
using FJarvis.Data.Traits;
using FJarvis.Traits.FlightCentre;
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
        entityManager.GetEntityInfo(entityViaCreate).EntityId.ShouldBe(entityViaCreate.Id);
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
        entityManager.GetTraits<Flight>(entity);
        
        // EntityInfo EntityId should be the same as the above entity.
        entityInfo.EntityId.ShouldBe(entity.Id);
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
        entityManager.HasTrait<Flight>(entity).ShouldBeTrue();
        entityManager.GetEntityInfo(entity).GetBitFlag<Flight>().ShouldBeTrue();
        entityManager.GetEntityInfo(entity).Validate().ShouldBeTrue();
        entityManager.GetEntityInfo(entity).GetSize().ShouldBe(64);
        entityManager.GetEntityInfo(entity).Count<Flight>().ShouldBe(1);
        entityManager.GetEntityInfo(entity).HasTrait(flight.Id).ShouldBeTrue();
        entityManager.GetEntityInfo(entity).GetTraits<Flight>().Count.ShouldBe(1);
    }

    // EntityManager Should Set Multiple Traits for Entity
    [Test]
public void EntityManager_Should_SetMultipleTraitsForEntity()
    {
        // Arrange
        Jarvis.Initialize();
        var entityManager = Jarvis.EntityManager();
        var entity = entityManager.CreatEntity();
        var flight = new Flight();
        var flight2 = new Flight();
        
        // Act
        entityManager.AddTraitData(entity, flight);
        entityManager.AddTraitData(entity, flight2);
        
        // Assert
        entityManager.HasTrait<Flight>(entity).ShouldBeTrue();
        entityManager.GetEntityInfo(entity).GetBitFlag<Flight>().ShouldBeTrue();
        entityManager.GetEntityInfo(entity).Validate().ShouldBeTrue();
        entityManager.GetEntityInfo(entity).GetSize().ShouldBe(64);
        entityManager.GetEntityInfo(entity).Count<Flight>().ShouldBe(2);
        entityManager.GetEntityInfo(entity).Count<Coupon>().ShouldBe(0);
        entityManager.GetEntityInfo(entity).HasTrait(flight.Id).ShouldBeTrue();
        entityManager.GetEntityInfo(entity).HasTrait(flight2.Id).ShouldBeTrue();
        entityManager.GetEntityInfo(entity).GetTraits<Flight>().Count.ShouldBe(2);
    }

    // EntityManager Should Remove Multiple Traits for Entity and return BitFlag to false
    [Test]
    public void EntityManager_Should_RemoveMultipleTraitsForEntity_AndReturnBitFlagToFalse()
    {
        // Arrange
        Jarvis.Initialize();
        var entityManager = Jarvis.EntityManager();
        var entity = entityManager.CreatEntity();
        var flight = new Flight();
        var flight2 = new Flight();
        var coupon = new Coupon();
        
        // Act
        entityManager.AddTraitData(entity, flight, flight2, coupon);
        entityManager.RemoveTraitData(entity, flight, flight2);
        
        // Assert
        entityManager.HasTrait<Flight>(entity).ShouldBeFalse();
        entityManager.GetEntityInfo(entity).GetBitFlag<Flight>().ShouldBeFalse();
        entityManager.GetEntityInfo(entity).Validate().ShouldBeTrue();
        entityManager.GetEntityInfo(entity).GetSize().ShouldBe(64);
        entityManager.GetEntityInfo(entity).Count<Flight>().ShouldBe(0);
        entityManager.GetEntityInfo(entity).Count<Coupon>().ShouldBe(1);
        entityManager.GetEntityInfo(entity).HasTrait(flight.Id).ShouldBeFalse();
        entityManager.GetEntityInfo(entity).HasTrait(flight2.Id).ShouldBeFalse();
        entityManager.GetEntityInfo(entity).GetTraits<Flight>().Count.ShouldBe(0);
    }

    
    /// EntityManager Should Determine if it has a Trait
    [Test]
    public void EntityManager_Should_DetermineIfItHasTrait()
    {
        // Arrange
        Jarvis.Initialize();
        var entityManager = Jarvis.EntityManager();
        var entity = entityManager.CreatEntity();
        var flight = new Flight();
        
        // Act
        entityManager.AddTraitData(entity, flight);
        
        // Assert
        entityManager.HasTrait<Flight>(entity).ShouldBeTrue();
        entityManager.HasTrait<Coupon>(entity).ShouldBeFalse();
    }
    
    // EntityManager should RemoveAll Traits for Entity
    [Test]
    public void RemoveAllTraitsData_Should_RemoveTraitsForAnEntity()
    {
        // Arrange
        Jarvis.Initialize();
        var entityManager = Jarvis.EntityManager();
        var entity = entityManager.CreatEntity();
        var flight = new Flight();
        var flight2 = new Flight();
        var coupon = new Coupon();
        
        // Act
        entityManager.AddTraitData(entity, flight, flight2, coupon);
        entityManager.RemoveAllTraitData(entity);
        
        // Assert
        entityManager.HasTrait<Flight>(entity).ShouldBeFalse();
        entityManager.GetEntityInfo(entity).GetBitFlag<Flight>().ShouldBeFalse();
        entityManager.GetEntityInfo(entity).Validate().ShouldBeFalse();
        entityManager.GetEntityInfo(entity).GetSize().ShouldBe(64);
        entityManager.GetEntityInfo(entity).Count<Flight>().ShouldBe(0);
        entityManager.GetEntityInfo(entity).Count<Coupon>().ShouldBe(0);
        entityManager.GetEntityInfo(entity).HasTrait(flight.Id).ShouldBeFalse();
        entityManager.GetEntityInfo(entity).HasTrait(flight2.Id).ShouldBeFalse();
        entityManager.GetEntityInfo(entity).GetTraits<Flight>().Count.ShouldBe(0);
    }
    
    // SetEntity should fail when there is no Id
    [Test]
    public void EntityManager_Should_ThrowException_When_EntityIdIsEmpty()
    {
        // Arrange
        Jarvis.Initialize();
        var entityManager = Jarvis.EntityManager();
        var entity = entityManager.CreatEntity();
        
        // Act
        var entityInfo = entityManager.GetEntityInfo(entity);
        Should.Throw<Exception>(() => entityInfo.SetEntityId(new Entity()));

    }
    
    [Test]
    public void GetAllTraits_Should_ReturnTotalPerType()
    {
        // Arrange
        Jarvis.Initialize();
        var entityManager = Jarvis.EntityManager();
        var entity = entityManager.CreatEntity();
        var flight = new Flight();
        var flight2 = new Flight();
        var coupon = new Coupon();
        
        // Act
        entityManager.AddTraitData(entity, flight, flight2, coupon);
        
        // Assert
        entityManager.GetTraits<Flight>(entity).Count.ShouldBe(2);
    }

    [Test]
    public void GetEntityQuery_WithAllTraitsSpecified_ShouldReturnOnlyEntitiesWithAllTraits()
    {
        // Arrange
        Jarvis.Initialize();
        
        var entityManager = Jarvis.EntityManager();
        var firstEntity = entityManager.CreatEntity();
        var secondEntity = entityManager.CreatEntity();
        var flight = new Flight();
        var flight2 = new Flight();
        var coupon = new Coupon();
        
        // Act
        entityManager.AddTraitData(firstEntity, flight, flight2, coupon);
        entityManager.AddTraitData(secondEntity,flight);
        
        var queryDescription = new EntityQueryDesc
        {
            All = new TraitType[]
            {
                TraitType.ReadWrite<Coupon>(),
                TraitType.ReadWrite<Flight>(), 
            }
        };
        var query = Jarvis.EntityManager().GetEntityQuery(queryDescription);
        
        // Assert
        entityManager.Count().ShouldBe(2);
        query.Count.ShouldBe(1);
    }
    
    [Test]
    public void GetEntityQuery_WithAnyTraitsSpecified_ShouldReturnOnlyEntitiesWithAllTraits()
    {
        // Arrange
        Jarvis.Initialize();
        
        var entityManager = Jarvis.EntityManager();
        var firstEntity = entityManager.CreatEntity();
        var secondEntity = entityManager.CreatEntity();
        var flight = new Flight();
        var flight2 = new Flight();
        var coupon = new Coupon();
        
        // Act
        entityManager.AddTraitData(firstEntity, flight, flight2, coupon);
        entityManager.AddTraitData(secondEntity,flight);
        
        var queryDescription = new EntityQueryDesc
        {
            Any = new TraitType[]
            {
                TraitType.ReadWrite<Coupon>(),
                TraitType.ReadWrite<Flight>(), 
            }
        };
        var query = Jarvis.EntityManager().GetEntityQuery(queryDescription);
        
        // Assert
        entityManager.Count().ShouldBe(2);
        query.Count.ShouldBe(2);
    }
    
    [Test]
    public void GetEntityQuery_WithNoneTraitsSpecified_ShouldReturnOnlyEntitiesWithAllTraits()
    {
        // Arrange
        Jarvis.Initialize();
        
        var entityManager = Jarvis.EntityManager();
        var firstEntity = entityManager.CreatEntity();
        var secondEntity = entityManager.CreatEntity();
        var flight = new Flight();
        var flight2 = new Flight();
        var coupon = new Coupon();
        
        // Act
        entityManager.AddTraitData(firstEntity, flight, flight2, coupon);
        entityManager.AddTraitData(secondEntity,flight);
        
        var queryDescription = new EntityQueryDesc
        {
            None = new TraitType[]
            {
                TraitType.ReadWrite<Coupon>(),
                TraitType.ReadWrite<Flight>(), 
            }
        };
        var query = Jarvis.EntityManager().GetEntityQuery(queryDescription);
        
        // Assert
        entityManager.Count().ShouldBe(2);
        query.Count.ShouldBe(0);
    }
    
    [Test]
    public void GetEntityQuery_WithMixTraitsSpecified_ShouldReturnOnlyEntitiesWithAllTraits()
    {
        // Arrange
        Jarvis.Initialize();
        
        var entityManager = Jarvis.EntityManager();
        var firstEntity = entityManager.CreatEntity();
        var secondEntity = entityManager.CreatEntity();
        var flight = new Flight();
        var flight2 = new Flight();
        var coupon = new Coupon();
        
        // Act
        entityManager.AddTraitData(firstEntity, flight, flight2, coupon);
        entityManager.AddTraitData(secondEntity,flight);
        
        var queryDescription = new EntityQueryDesc
        {
            All = new TraitType[]
            {
                TraitType.ReadWrite<Flight>(),
            }, 
            None = new TraitType[]
            {
                TraitType.ReadWrite<Coupon>(),
            }
        };
        var query = Jarvis.EntityManager().GetEntityQuery(queryDescription);
        
        // Assert
        entityManager.Count().ShouldBe(2);
        query.Count.ShouldBe(1);
    }
}