using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenIgnoringNonMapped
    {
        [TestMethod]
        public void Should_Ignore_Non_Mapped()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .IgnoreNonMapped(true)
                .Compile();

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name1 = "Name1",
                Name2 = "Name2",
                Name3 = "Name3"
            };

            var dto = poco.Adapt<SimplePoco, SimpleDto>();

            dto.Id.ShouldBe(poco.Id);
            dto.Name1.ShouldBeNull();
            dto.Name2.ShouldBeNull();
            dto.Name3.ShouldBeNull();
        }

        #region test classes
        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name1 { get; set; }
            public string Name2 { get; set; }
            public string Name3 { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name1 { get; set; }
            public string Name2 { get; set; }
            public string Name3 { get; set; }
        }
        #endregion
    }
}
