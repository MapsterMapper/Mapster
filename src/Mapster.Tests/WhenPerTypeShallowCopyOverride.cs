using Mapster.Models;
using MapsterMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenPerTypeShallowCopyOverride
    {
        [TestMethod]
        public void GlobalShallowCopy_WithPerTypeDeepCopy_ShouldRestoreViaMapToTarget()
        {
            var cfg = new TypeAdapterConfig();
            cfg.RequireExplicitMapping = true;
            cfg.Default.ShallowCopyForSameType(true);
            cfg.Default.AvoidInlineMapping(true);

            cfg.NewConfig<RandomObject2, RandomObject2>();
            cfg.NewConfig<RandomObject1, RandomObject1>();

            cfg.NewConfig<MyFailStuff, MyFailStuff>()
                .ShallowCopyForSameType(false);

            cfg.GetMergedSettings(new TypeTuple(typeof(MyFailStuff), typeof(MyFailStuff)), MapType.Map)
                .ShallowCopyForSameType.ShouldBe(false);

            cfg.Compile();

            var mapper = new Mapper(cfg);

            var dynamicStuff = new MyFailStuff();
            dynamicStuff.Item1 = new RandomObject1();
            dynamicStuff.Item1.SampleName = "SN1";
            dynamicStuff.Item2 = new RandomObject2();
            dynamicStuff.Item2.SampleNumber = 2;

            var originalStuff = mapper.Map<MyFailStuff, MyFailStuff>(dynamicStuff);

            dynamicStuff.Item1.SampleName = "SN1CHANGED";
            dynamicStuff.Item2.SampleNumber = 3;

            mapper.Map(originalStuff, dynamicStuff);

            dynamicStuff.Item1.SampleName.ShouldBe("SN1");
            dynamicStuff.Item2.SampleNumber.ShouldBe(2);
            ReferenceEquals(dynamicStuff.Item1, originalStuff.Item1).ShouldBeFalse();
            ReferenceEquals(dynamicStuff.Item2, originalStuff.Item2).ShouldBeFalse();
        }

        [TestMethod]
        public void ParentDeepCopyOverride_ShouldNotDisableImplicitNestedShallowCopyForSameType()
        {
            var cfg = new TypeAdapterConfig();
            cfg.Default.ShallowCopyForSameType(true);
            cfg.NewConfig<Container, Container>()
                .ShallowCopyForSameType(false);

            cfg.Compile();

            var src = new Container { Child = new NestedChild { Value = 1 } };
            var dest = src.Adapt<Container>(cfg);

            ReferenceEquals(src.Child, dest.Child).ShouldBeTrue();
        }

        public class Container
        {
            public NestedChild? Child { get; set; }
        }

        public class NestedChild
        {
            public int Value { get; set; }
        }

        public class MyFailStuff
        {
            public MyFailStuff()
            {
                CreateEmptyEntities();
            }

            public void CreateEmptyEntities()
            {
                Item1 ??= new RandomObject1();
                Item2 ??= new RandomObject2();
            }

            public RandomObject1? Item1 { get; set; }

            public RandomObject2? Item2 { get; set; }
        }

        public class RandomObject1
        {
            public string? SampleName { get; set; }
        }

        public class RandomObject2
        {
            public int SampleNumber { get; set; }
        }
    }
}
