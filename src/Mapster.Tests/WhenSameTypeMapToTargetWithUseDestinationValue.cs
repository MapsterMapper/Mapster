using MapsterMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    /// <summary>
    /// https://github.com/MapsterMapper/Mapster/issues/938
    /// Same-type MapToTarget with ctor-init nested objects: use UseDestinationValue on members,
    /// not per-type ShallowCopyForSameType(false). See maintainer guidance on #974.
    /// </summary>
    [TestClass]
    public class WhenSameTypeMapToTargetWithUseDestinationValue
    {
        [TestMethod]
        public void MapToTarget_ShouldRestoreNestedMembers_WhenGlobalShallowCopyEnabled()
        {
            var cfg = new TypeAdapterConfig();
            cfg.RequireExplicitMapping = true;
            cfg.Default.AvoidInlineMapping(true);

            cfg.NewConfig<RandomObject2, RandomObject2>();
            cfg.NewConfig<RandomObject1, RandomObject1>()
                .Include<RandomObject2, RandomObject2>();

            cfg.NewConfig<MyFailStuff, MyFailStuff>()
                .UseDestinationValue(x => x.Item1)
                .UseDestinationValue(x => x.Item2);

            cfg.Compile();

            var mapper = new Mapper(cfg);

            var dynamicStuff = new MyFailStuff();
            dynamicStuff.Item1 = new RandomObject1();
            dynamicStuff.Item1.SampleName = "SN1";
            dynamicStuff.Item2 = new RandomObject2 { SampleNumber = 2 };

            var originalStuff = mapper.Map<MyFailStuff, MyFailStuff>(dynamicStuff);

            dynamicStuff.Item1.SampleName = "SN1CHANGED";
            dynamicStuff.Item2.SampleNumber = 3;

            mapper.Map(originalStuff, dynamicStuff);

            dynamicStuff.Item1.SampleName.ShouldBe("SN1");
            dynamicStuff.Item2.SampleNumber.ShouldBe(2);
            ReferenceEquals(dynamicStuff.Item1, originalStuff.Item1).ShouldBeFalse();
            ReferenceEquals(dynamicStuff.Item2, originalStuff.Item2).ShouldBeFalse();
        }

        public class RandomObject1
        {
            public string? SampleName { get; set; }
        }

        public class RandomObject2 : RandomObject1
        {
            public int SampleNumber { get; set; }
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
    }
}
