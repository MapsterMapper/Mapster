using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenPassingRuntimeValue
    {
        [TestMethod]
        public void Passing_Runtime_Value()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.CreatedBy,
                    src => MapContext.Current.Parameters["user"]);

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };
            var dto = poco.BuildAdapter()
                .AddParameters("user", this.User)
                .AdaptToType<SimpleDto>();

            dto.CreatedBy.ShouldBe(this.User);
        }

        private string User { get; } = Guid.NewGuid().ToString("N");

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string CreatedBy { get; set; }
        }

    }
}
