using FJarvis.Data;
using FJarvis.Data.Traits;
using Shouldly;

namespace FJarvis.Entities.Tests;

public class EntityInfoTests
{
    [Test]
    public void EntityInfo_Should_FailOnDefaultConstructor()
    {
        // Arrange
        Should.Throw<Exception>(() => new EntityInfo());
        
    }
    
    
    
}