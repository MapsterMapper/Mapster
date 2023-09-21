using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

namespace Mapster.Tests
{
    /// <summary>
    /// Regression test for https://github.com/MapsterMapper/Mapster/issues/450
    /// </summary>
    [TestClass]
    public class WhenMappingWithAdaptIgnoreRegression
    {
        public abstract class Base
        {
            public DateTime CreatedOn { get; private set; }

            public DateTime UpdatedOn { get; set; }

            public int State { get; set; }

            public Base()
            {
                CreatedOn = DateTime.UtcNow;
                UpdatedOn = DateTime.UtcNow;
                State = 1;
            }
        }

        public class Poco : Base
        {
            public string Name { get; set; }
        }

        public class Dto
        {
            public string Name { get; set; }
        }

        [TestMethod]
        public void TestMapStructToExistingStruct()
        {
            TypeAdapterConfig<Dto, Poco>
                .ForType()
                .Ignore(s => s.State)
                .Ignore(s => s.CreatedOn)
                .Ignore(s => s.UpdatedOn);

            var destination = new Poco() { Name = "Destination", State = 2 };
            var source = new Dto() { Name = "Source" };
            var result = source.Adapt(destination);
            result.State.ShouldBe(2);
            result.Name.ShouldBe("Source");
        }
    }
}