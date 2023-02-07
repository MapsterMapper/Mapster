using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

namespace Mapster.Tests
{

    /// <summary>
    /// Regression for https://github.com/MapsterMapper/Mapster/issues/485
    /// </summary>
    [TestClass]
    public class WhenMappingWithSecondSourceObject
    {
        public interface ISomeType
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
        }
        public class ConcreteType1 : ISomeType
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public string Address { get; set; }
        }

        public class ConcreteType2
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [TestMethod]
        public void TestMapFromSecondSourceObject()
        {
            var c1 = new ConcreteType1
            {
                Id = 1,
                Name = "Name 1",
                Address = "Address 1"
            };

            var c2 = new ConcreteType2
            {
                Id = 2,
                Name = "Name 2"
            };

            var generatedType = c1.Adapt<ISomeType>();

            generatedType.Id.ShouldBe(1);
            generatedType.Name.ShouldBe("Name 1");
            generatedType.Address.ShouldBe("Address 1");

            generatedType = c2.Adapt(generatedType);

            generatedType.Id.ShouldBe(2);
            generatedType.Name.ShouldBe("Name 2");
            generatedType.Address.ShouldBe("Address 1");
        }
    }
}