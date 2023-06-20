using FJarvis.Data;
using FJarvis.Data.Data;
using FJarvis.Traits.FlightCentre;
using Shouldly;

namespace FJarvis.Entities.Tests;

public class JarvisTests
{
    // Test should Validate Jarvis EntityManager is Accessible via Singleton
    [Test]
    public void EntityManager_Should_BeAccessibleViaSingleton()
    {
        // Arrange
        Jarvis.Initialize();
        
        var entityManager = Jarvis.EntityManager();
        
        // Act
        var entityManager2 = Jarvis.EntityManager();
        
        // Assert
        entityManager.ShouldBeSameAs(entityManager2);
    }
    
    // Test Should Validate Journal is Accessible via Singleton
    [Test]
    public void Journal_Should_BeAccessibleViaSingleton()
    {
        // Arrange
        Jarvis.Initialize();
        var journal = Jarvis.Journal();
        
        // Act
        var journal2 = Jarvis.Journal();
        
        // Assert
        journal.ShouldBeSameAs(journal2);
    }

    [Test]
    public void Journal_Should_ReturnHeadersAsBitMasks_ForEachEntity()
    {
        Jarvis.Initialize();
        
        var journal = Jarvis.Journal();
        var entityManager = Jarvis.EntityManager();
        
        var firstEntity = entityManager.CreatEntity();
        var secondEntity = entityManager.CreatEntity();

        var flight = new Flight();
        var coupon = new Coupon();
        
        Jarvis.EntityManager().AddTraitData(firstEntity, flight);
        Jarvis.EntityManager().AddTraitData(secondEntity, flight, coupon);

        var headers = journal.Headers().ToList();
        headers.Count.ShouldBe(2);
        headers[0].Bitmask.ShouldBe("4611686018427387904;");
        headers[1].Bitmask.ShouldBe("5188146770730811392;");

    }

}