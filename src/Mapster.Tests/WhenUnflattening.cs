using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenUnflattening
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestMethod]
        public void ShouldUnflattening()
        {
            TypeAdapterConfig<ModelDto, ModelObject>.NewConfig()
                .Unflattening(true);

            var src = new ModelDto
            {
                BaseDate = new DateTime(2007, 4, 5),
                SubProperName = "Some name",
                SubSubSubCoolProperty = "Cool daddy-o",
                Sub2ProperName = "Sub 2 name",
                SubWithExtraNameProperName = "Some other name",
            };

            var dest = src.Adapt<ModelObject>();

            dest.Sub.ProperName.ShouldBe(src.SubProperName);
            dest.Sub.SubSub.CoolProperty.ShouldBe(src.SubSubSubCoolProperty);
            dest.Sub2.ProperName.ShouldBe(src.Sub2ProperName);
            dest.SubWithExtraName.ProperName.ShouldBe(src.SubWithExtraNameProperName);
        }

        [TestMethod]
        public void ShouldUnflattening_When2WaysMapping()
        {
            TypeAdapterConfig<ModelObject, ModelDto>.NewConfig()
                .TwoWays();

            var src = new ModelDto
            {
                BaseDate = new DateTime(2007, 4, 5),
                SubProperName = "Some name",
                SubSubSubCoolProperty = "Cool daddy-o",
                Sub2ProperName = "Sub 2 name",
                SubWithExtraNameProperName = "Some other name",
            };
            var dest = src.Adapt<ModelObject>();

            dest.Sub.ProperName.ShouldBe(src.SubProperName);
            dest.Sub.SubSub.CoolProperty.ShouldBe(src.SubSubSubCoolProperty);
            dest.Sub2.ProperName.ShouldBe(src.Sub2ProperName);
            dest.SubWithExtraName.ProperName.ShouldBe(src.SubWithExtraNameProperName);
        }

        public class ModelObject
        {
            public DateTime BaseDate { get; set; }
            public ModelSubObject Sub { get; set; }
            public ModelSubObject Sub2 { get; set; }
            public ModelSubObject SubWithExtraName { get; set; }
        }

        public class ModelSubObject
        {
            public string ProperName { get; set; }
            public ModelSubSubObject SubSub { get; set; }
        }

        public class ModelSubSubObject
        {
            public string CoolProperty { get; set; }
        }

        public class ModelDto
        {
            public DateTime BaseDate { get; set; }
            public string SubProperName { get; set; }
            public string Sub2ProperName { get; set; }
            public string SubWithExtraNameProperName { get; set; }
            public string SubSubSubCoolProperty { get; set; }
        }
    }
}
