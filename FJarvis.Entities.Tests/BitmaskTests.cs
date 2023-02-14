using System.Collections;
using FJarvis.Data;
using FJarvis.Data.Traits;
using FJarvis.Traits.FlightCentre;
using Shouldly;

namespace JarvisBot.Tests;

public class BitmaskTests
{
    private Journal _journal;

    [SetUp]
    public void Setup()
    {
        Jarvis.Initialize();
    }
    
    /// <summary>
    ///  This test will check to see if the Bitmask is 64 bits even if the size is set to 24 bits
    /// </summary>
    [Test]
    public void Bitmask_Should_NotGoBelow64Bits()
    {
        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask =  Jarvis.EntityManager().GetEntityInfo(entity);
        
        // Act
        bitmask.Resize(24);
        
        // Assert
        
        // Test to see if the Bitmask is 64bits
        bitmask.GetSize().ShouldBe( 64, "The size of the Bitmask should not go below 64 bits");
    }
    
    /// <summary>
    ///  This test will check to ensure the Bitmask doesn't go below the existing Bits.
    /// </summary>
    [Test]
    public void Bitmask_Should_NotGoBelowExistingSize()
    {
        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask =  Jarvis.EntityManager().GetEntityInfo(entity);
        
        // Act
        bitmask.Resize(128);
        
        // Should throw an exception
        Should.Throw<Exception>(() => bitmask.Resize(24));
        
        // Assert
        
        // Test to see if the Bitmask is 64bits
        bitmask.GetSize().ShouldBe( 128, "The size of the Bitmask should not go below the existing size");
    }
    
    /// <summary>
    ///  This test will check to ensure the minimum size of the Bitmask is 64 bits
    /// </summary>
    [Test]
    public void Bitmask_Should_CreateDefaultSizeOf64Bits()
    {
        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask =  Jarvis.EntityManager().GetEntityInfo(entity);
        
        // Act
        var result = bitmask.GetSize();
        
        // Assert
        
        // Test to see if the Bitmask is 64bits
        result.ShouldBe( 64, "The size of the Bitmask should be 64 bits");
    }
    
    /// <summary>
    ///  This test will check to see if the Bitmask is a multiple of 64 bits
    /// </summary>
    [Test]
    public void Bitmask_Should_IncreaseSizeBy64Bits()
    {
        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask =  Jarvis.EntityManager().GetEntityInfo(entity);
        
        // Act
        bitmask.Resize(65);
        var Result65 = bitmask.GetSize();
        
        bitmask.Resize(128);
        var Result128 = bitmask.GetSize();
        
        bitmask.Resize(127);
        var Result127 = bitmask.GetSize();
        
        // Assert
        
        Result65.ShouldBe(128, "The size of the Bitmask should be a multiple of 64 bits");
        Result127.ShouldBe( 128, "The size of the Bitmask should be a multiple of 64 bits");
        Result128.ShouldBe( 128, "The size of the Bitmask should be a multiple of 64 bits");
        
    }

    /// <summary>
    ///  This test will check to see if any of the bits are set
    /// </summary>
    [Test]
    public void Bitmask_Should_Validate()
    {
        // Arrange
        
        var entity = Jarvis.EntityManager().CreatEntity();
        var entityInfo =  Jarvis.EntityManager().GetEntityInfo(entity);
        
        // Assert
        
        entityInfo.Validate().ShouldBe( false, "There is no Traits set, therefore the bitmask will be empty");
    }
    

    /// <summary>
    ///  This test will check to see if EntityInfo has bitflag set
    /// </summary>
    [Test]
    public void Bitmask_Should_HaveTraitFlag()
    {
        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask =  Jarvis.EntityManager().GetEntityInfo(entity);
        
        // Act
        bitmask.SetBitFlag(entity, new Flight());
        
        // Assert
        bitmask.HasBitFlag<Flight>().ShouldBeTrue( "The Bitmask should have the Flight Trait Flag");
        bitmask.HasBitFlag<Coupon>().ShouldBeFalse( "The Bitmask should not have the Coupon Trait Flag");
    }

    [Test]
    public void Bitmask_Should_ReturnUlongBitmask()
    {
        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask =  Jarvis.EntityManager().GetEntityInfo(entity);
        
        // Act
        bitmask.SetBitFlag(entity, new Flight());
        
        // Assert
        /*
        
        0   1   2   3   4   5   6   7   8   9  10  11  12  13  14  15  16  17  18  19  20  21  22  23  24  25  26  27  28  29  30  31  32  33  34  35  36  37  38  39  40  41  42  43  44  45  46  47  48  49  50  51  52  53  54  55  56  57  58  59  60  61  62  63
        +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
        | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
        +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
         */

        ulong expectedMask = 0x0000000000000002;
        ulong actualMask = bitmask.GetBitmask();
        Assert.AreEqual(expectedMask, actualMask, $"Expected: {expectedMask:x}, Actual: {actualMask:x}"); 

    }
}