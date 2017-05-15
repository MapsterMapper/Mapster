using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenForkingConfig
    {
        [TestMethod]
        public void Forked_Config_Should_Not_Apply_To_Parent()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<SimplePoco, SimpleDto>()
                .Map(dest => dest.Name2, src => src.Name2 + "Parent");

            var fork = config.Fork(child =>
                child.ForType<SimplePoco, SimpleDto>()
                    .Map(dest => dest.Name1, src => src.Name1 + "Child"));

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name1 = "Name1",
                Name2 = "Name2",
            };

            var dtoInline = poco.Adapt<SimplePoco, SimpleDto>(fork);
            dtoInline.Id.ShouldBe(poco.Id);
            dtoInline.Name1.ShouldBe("Name1Child");
            dtoInline.Name2.ShouldBe("Name2Parent");

            var dtoParent = poco.Adapt<SimplePoco, SimpleDto>(config);
            dtoParent.Id.ShouldBe(poco.Id);
            dtoParent.Name1.ShouldBe("Name1");
            dtoParent.Name2.ShouldBe("Name2Parent");
        }

        #region test classes
        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name1 { get; set; }
            public string Name2 { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name1 { get; set; }
            public string Name2 { get; set; }
        }
        #endregion

    }
}
