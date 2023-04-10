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
        var bitmask = Jarvis.EntityManager().GetEntityInfo(entity);

        // Act
        bitmask.Resize(24);

        // Assert

        // Test to see if the Bitmask is 64bits
        bitmask.GetSize().ShouldBe(64, "The size of the Bitmask should not go below 64 bits");
    }

    /// <summary>
    ///  This test will check to ensure the Bitmask doesn't go below the existing Bits.
    /// </summary>
    [Test]
    public void Bitmask_Should_NotGoBelowExistingSize()
    {
        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask = Jarvis.EntityManager().GetEntityInfo(entity);

        // Act
        bitmask.Resize(128);

        // Should throw an exception
        Should.Throw<Exception>(() => bitmask.Resize(24));

        // Assert

        // Test to see if the Bitmask is 64bits
        bitmask.GetSize().ShouldBe(128, "The size of the Bitmask should not go below the existing size");
    }

    /// <summary>
    ///  This test will check to ensure the minimum size of the Bitmask is 64 bits
    /// </summary>
    [Test]
    public void Bitmask_Should_CreateDefaultSizeOf64Bits()
    {
        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask = Jarvis.EntityManager().GetEntityInfo(entity);

        // Act
        var result = bitmask.GetSize();

        // Assert

        // Test to see if the Bitmask is 64bits
        result.ShouldBe(64, "The size of the Bitmask should be 64 bits");
    }

    /// <summary>
    ///  This test will check to see if the Bitmask is a multiple of 64 bits
    /// </summary>
    [Test]
    public void Bitmask_Should_IncreaseSizeBy64Bits()
    {
        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask = Jarvis.EntityManager().GetEntityInfo(entity);

        // Act
        bitmask.Resize(65);
        var Result65 = bitmask.GetSize();

        bitmask.Resize(128);
        var Result128 = bitmask.GetSize();

        bitmask.Resize(127);
        var Result127 = bitmask.GetSize();

        // Assert

        Result65.ShouldBe(128, "The size of the Bitmask should be a multiple of 64 bits");
        Result127.ShouldBe(128, "The size of the Bitmask should be a multiple of 64 bits");
        Result128.ShouldBe(128, "The size of the Bitmask should be a multiple of 64 bits");
    }

    /// <summary>
    ///  This test will check to see if any of the bits are set
    /// </summary>
    [Test]
    public void Bitmask_Should_Validate()
    {
        // Arrange

        var entity = Jarvis.EntityManager().CreatEntity();
        var entityInfo = Jarvis.EntityManager().GetEntityInfo(entity);

        // Assert

        entityInfo.Validate().ShouldBe(false, "There is no Traits set, therefore the bitmask will be empty");
    }


    /// <summary>
    ///  This test will check to see if EntityInfo has bitflag set
    /// </summary>
    [Test]
    public void Bitmask_Should_HaveTraitFlag()
    {
        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask = Jarvis.EntityManager().GetEntityInfo(entity);

        // Act
        bitmask.RegisterTrait(entity, new Flight());

        // Assert
        bitmask.GetBitFlag<Flight>().ShouldBeTrue("The Bitmask should have the Flight Trait Flag");
        bitmask.GetBitFlag<Coupon>().ShouldBeFalse("The Bitmask should not have the Coupon Trait Flag");
    }

    /*
       
       0   1   2   3   4   5   6   7   8   9  10  11  12  13  14  15  16  17  18  19  20  21  22  23  24  25  26  27  28  29  30  31  32  33  34  35  36  37  38  39  40  41  42  43  44  45  46  47  48  49  50  51  52  53  54  55  56  57  58  59  60  61  62  63
       +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
       | 0 | 1 | 0 | 0 | 4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
       +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
        */

    [Test]
    public void CompressBitmask_Should_ReturnExpectedCompressedBitmask()
    {
        /*
         This test is verifying that the CompressBitmask and DecompressBitmask methods of an entity bitmask are working correctly, 
         by compressing a known binary string into a compressed string, then decompressing it back into 
         the original boolean array and checking that it matches the expected value.
         */

        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask = Jarvis.EntityManager().GetEntityInfo(entity);

        // Act
        bitmask.RegisterTrait(entity, new Flight());
        bitmask.RegisterTrait(entity, new Coupon());

        // Assert
        string expectedMask = "0100100000000000000000000000000000000000000000000000000000000000;";
        var actualMask = bitmask.CompressBitmask(expectedMask);
        var result = bitmask.DecompressBitmask(actualMask);

        // Assert that the decompressed bitmask matches the expected boolean array
        bool[] expectedBits = new bool[]
        {
            false, true, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false
        };
        Assert.AreEqual(expectedBits, result);
    }

    [Test]
    public void DecompressBitmask_Should_ReturnExpectedCompressedBitmask()
    {
        /*
          This test is verifying that the CompressBitmask and DecompressBitmask methods of an entity bitmask are working correctly, 
          by adding and removing a trait from an entity bitmask, then compressing the bitmask and comparing it to the expected value.
          */

        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask = Jarvis.EntityManager().GetEntityInfo(entity);

        var flightTrait = new Flight();
        // Act
        bitmask.RegisterTrait(entity, flightTrait);
        bitmask.RegisterTrait(entity, new Coupon());

        // Assert
        var withTrait = bitmask.GetBitmask();

        bitmask.RemoveTrait(flightTrait);

        var withOutTrait = bitmask.GetBitmask();

        Assert.AreEqual("5188146770730811392;", withTrait);
        Assert.AreEqual("576460752303423488;", withOutTrait);
    }

    [Test]
    public void GetBitmask_ShouldReturnValidBitmask()
    {
        // Create a new EntityInfo object
        var entity = Jarvis.EntityManager().CreatEntity();
        var entityInfo = Jarvis.EntityManager().GetEntityInfo(entity);

        // Set the size of the Bitmask to 64
        entityInfo.Resize(64);

        // Generate a random number of traits to register to the Entity
        var numTraits = new System.Random().Next(1, 20);

        // Register the random number of traits to the Entity
        for (var i = 0; i < numTraits; i++)
        {
            var index = new System.Random().Next(0, 64);
            entityInfo.bitFlags.Set(index, true);
        }

        // Get the Bitmask
        var bitmask = entityInfo.GetBitmask();

        var decompressedBitmask = entityInfo.DecompressBitmask(bitmask);
        Assert.AreEqual(entityInfo.bitFlags, decompressedBitmask);

        // Decompress the bitmask back into a boolean array
        var decompressedBitFlags = entityInfo.DecompressBitmask(bitmask);
        // Assert that the decompressed bitFlags array matches the original bitFlags array
        Assert.AreEqual(entityInfo.bitFlags, decompressedBitFlags);
    }

    [Test]
    public void Resize_ShouldIncreaseSize()
    {
        // Create a new EntityInfo object
        var entity = Jarvis.EntityManager().CreatEntity();
        var entityInfo = Jarvis.EntityManager().GetEntityInfo(entity);

        // Resize the EntityInfo to 128 bits
        entityInfo.Resize(128);

        // Assert that the EntityInfo size is now 128 bits
        Assert.AreEqual(128, entityInfo.bitFlags.Length);
    }

    [Test]
    public void Set_ShouldSetCorrectBit()
    {
        // Create a new EntityInfo object
        var entity = Jarvis.EntityManager().CreatEntity();
        var entityInfo = Jarvis.EntityManager().GetEntityInfo(entity);

        // Set the 10th bit to true
        entityInfo.bitFlags.Set(10, true);

        // Assert that the 10th bit is true
        Assert.IsTrue(entityInfo.bitFlags.Get(10));
    }

    [Test]
    public void Clear_ShouldClearCorrectBit()
    {
        // Create a new EntityInfo object
        var entity = Jarvis.EntityManager().CreatEntity();
        var entityInfo = Jarvis.EntityManager().GetEntityInfo(entity);

        // Set the 5th bit to true
        entityInfo.bitFlags.Set(5, true);

        // Clear the 5th bit
        entityInfo.Clear(5);

        // Assert that the 5th bit is false
        Assert.IsFalse(entityInfo.bitFlags.Get(5));
    }

    [Test]
    public void ClearAll_ShouldClearAllBits()
    {
        // Create a new EntityInfo object
        var entity = Jarvis.EntityManager().CreatEntity();
        var entityInfo = Jarvis.EntityManager().GetEntityInfo(entity);

        // Set several random bits to true
        entityInfo.bitFlags.Set(10, true);
        entityInfo.bitFlags.Set(23, true);
        entityInfo.bitFlags.Set(37, true);

        // Clear all bits
        entityInfo.Clear();

        // Assert that all bits are false
        for (int i = 0; i < entityInfo.bitFlags.Length; i++)
        {
            Assert.IsFalse(entityInfo.bitFlags.Get(i));
        }
    }
    
    [Test]
    public void CompressBitmask_Should_ReturnExpectedCompressedString_When_MultipleChunks()
    {
        // Arrange
        var entity = Jarvis.EntityManager().CreatEntity();
        var bitmask = Jarvis.EntityManager().GetEntityInfo(entity);

        // Act
        bitmask.RegisterTrait(entity, new Flight());
        bitmask.RegisterTrait(entity, new Coupon());

        // Assert
        
        string expectedMask = "0100100000000000000000000000000000000000000000000000000000000000;0000100000000000000000000000000000000000000000000000000000000000;";
        var actualMask = bitmask.CompressBitmask(expectedMask);
        var result = bitmask.DecompressBitmask(actualMask);

        // Assert that the decompressed bitmask matches the expected boolean array
        bool[] expectedBits = new bool[]
        {
            false, true, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false,
            
            false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false
        };
        
        Assert.AreEqual("5188146770730811392", actualMask.Split(";")[0]);
        Assert.AreEqual("576460752303423488", actualMask.Split(";")[1]);
        
        Assert.AreEqual(expectedBits, result);
        
        string badMask = "0100100000000000000000000000000000000000000000000000000000000000;0000;";
        Should.Throw<ArgumentException>(() => bitmask.CompressBitmask(badMask));
    }

}