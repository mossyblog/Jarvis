using FJarvis.Data;
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

}