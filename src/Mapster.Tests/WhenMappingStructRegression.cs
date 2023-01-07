using Mapster;
using MapsterMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mapster.Tests.WhenMappingStructRegression;

namespace Mapster.Tests
{
    /// <summary>
    /// Regression tests for https://github.com/MapsterMapper/Mapster/issues/510
    /// </summary>
    [TestClass]
    public class WhenMappingStructRegression
    {
        public struct SourceClass
        {
            public string Name { get; set; }
        }

        public struct SourceStruct
        {
            public string Name { get; set; }
        }
        public struct DestinationStruct
        {
            public string Name { get; set; }

            public string Ignore { get; set; }
        }

        [TestMethod]
        public void TestMapStructToExistingStruct()
        {

            TypeAdapterConfig<SourceStruct, DestinationStruct>
                .ForType()
                .Ignore(s => s.Ignore);

            var source = new SourceStruct
            {
                Name = "Some Name",
            };
            var dest = new DestinationStruct
            {
                Ignore = "Ignored property",
            };
            dest = source.Adapt(dest);

            dest.Ignore.ShouldBe("Ignored property");
            dest.Name.ShouldBe("Some Name");
        }

        [TestMethod]
        public void TestMapClassToExistingStruct()
        {

            TypeAdapterConfig<SourceClass, DestinationStruct>
                .ForType()
                .Ignore(s => s.Ignore);

            var source = new SourceClass
            {
                Name = "Some Name",
            };
            var dest = new DestinationStruct
            {
                Ignore = "Ignored property",
            };
            dest = source.Adapt(dest);

            dest.Ignore.ShouldBe("Ignored property");
            dest.Name.ShouldBe("Some Name");
        }
    }
}