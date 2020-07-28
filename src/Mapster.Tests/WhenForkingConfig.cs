using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;

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

        [TestMethod]
        public void Fork_Setting()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<SimplePoco, SimpleDto>()
                .Fork(cfg =>
                    cfg.ForType<string, string>()
                        .MapToTargetWith((src, dest) => string.IsNullOrEmpty(src) ? dest : src));

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name1 = "Name1",
                Name2 = "",
            };

            var dto = new SimpleDto
            {
                Id = poco.Id,
                Name1 = "Foo",
                Name2 = "Bar",
            };

            poco.Adapt(dto, config);

            dto.Name1.ShouldBe(poco.Name1);
            dto.Name2.ShouldBe("Bar");

            var str = poco.Name2.Adapt(dto.Name2, config);
            str.ShouldBe(poco.Name2);
        }

        [TestMethod]
        public void Fork_Setting_2()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<ParentPoco, ParentPoco>()
                .Fork(cfg => cfg.Default.PreserveReference(true));

            var grandChild = new GrandChildPoco
            {
                Id = Guid.NewGuid().ToString()
            };
            var child = new ChildPoco
            {
                GrandChildren = new List<GrandChildPoco>
                {
                    grandChild, grandChild
                }
            };
            var parent = new ParentPoco
            {
                Children = new List<ChildPoco>
                {
                    child, child
                }
            };

            var cloned = parent.Adapt<ParentPoco>(config);

            cloned.Children[0].GrandChildren[0].ShouldBeSameAs(cloned.Children[1].GrandChildren[1]);
            
            var childCloned = child.Adapt<ChildPoco>(config);

            childCloned.GrandChildren[0].ShouldNotBeSameAs(childCloned.GrandChildren[1]);
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

        class ParentPoco
        {
            public string Id { get; set; }
            public List<ChildPoco> Children { get; set; }
        }
        class ChildPoco
        {
            public string Id { get; set; }
            public List<GrandChildPoco> GrandChildren { get; set; }
        }
        class GrandChildPoco
        {
            public string Id { get; set; }
        }

        #endregion

    }
}
