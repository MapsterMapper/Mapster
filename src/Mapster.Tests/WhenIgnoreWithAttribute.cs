using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenIgnoreWithAttribute
    {
        [TestMethod]
        public void TestSource()
        {
            var poco = new Poco
            {
                Id = "Id",
                Prop1 = "Prop1",
                Prop2 = "Prop2",
                Prop3 = "Prop3"
            };
            var dto = poco.Adapt<Dto>();
            dto.Prop1.ShouldBeNull();
            dto.Prop2.ShouldBeNull();
            dto.Prop3.ShouldBe(poco.Prop3);
        }

        [TestMethod]
        public void TestDest()
        {
            var dto = new Dto
            {
                Id = "Id",
                Prop1 = "Prop1",
                Prop2 = "Prop2",
                Prop3 = "Prop3"
            };
            var poco = dto.Adapt<Poco>();
            poco.Prop1.ShouldBeNull();
            poco.Prop2.ShouldBe(dto.Prop2);
            poco.Prop3.ShouldBeNull();
        }

        public class Poco
        {
            public string Id { get; set; }

            [AdaptIgnore]
            public string Prop1 { get; set; }

            [AdaptIgnore(MemberSide.Source)]
            public string Prop2 { get; set; }

            [AdaptIgnore(MemberSide.Destination)]
            public string Prop3 { get; set; }
        }
        public class Dto
        {
            public string Id { get; set; }
            public string Prop1 { get; set; }
            public string Prop2 { get; set; }
            public string Prop3 { get; set; }
        }
    }
}
