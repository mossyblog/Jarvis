using FJarvis.Data;
using FJarvis.Data.Data;
using FJarvis.Traits.FlightCentre;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Shouldly;

namespace FJarvis.Entities.Tests;

   
    public class MemoryProviderTest
    {
        [SetUp]
        public void Setup()
        {
            
        }

        public void QueryTest()
        {
            var queryDescription = new EntityQueryDesc
            {
                All = new TraitType[] { TraitType.ReadWrite<Flight>(), TraitType.ReadOnly<Coupon>() },
            };
            
            var query = Jarvis.EntityManager().GetEntityQuery(queryDescription);
        }
        
    }
