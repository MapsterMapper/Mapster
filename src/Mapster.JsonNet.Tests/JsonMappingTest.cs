using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace Mapster.JsonNet.Tests
{
    [TestClass]
    public class JsonMappingTest
    {
        private static TypeAdapterConfig _config;

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            _config = new TypeAdapterConfig();
            _config.EnableJsonMapping();
        }

        [TestMethod]
        public void JsonToJson()
        {
            var json = new JObject();
            var result = json.Adapt<JObject>(_config);
            result.ShouldBe(json);
        }

        [TestMethod]
        public void FromString()
        {
            var str = @"{ ""foo"": ""bar"" }";
            var result = str.Adapt<JObject>(_config);
            result["foo"].ShouldBe("bar");
        }

        [TestMethod]
        public void ToStringTest()
        {
            var json = new JObject {["foo"] = "bar"};
            var result = json.Adapt<string>(_config);
            result.ShouldContainWithoutWhitespace(@"{""foo"":""bar""}");
        }

        [TestMethod]
        public void FromObject()
        {
            var obj = new Mock {foo = "bar"};
            var result = obj.Adapt<JObject>(_config);
            result["foo"].ShouldBe("bar");
        }

        [TestMethod]
        public void ToObject()
        {
            var json = new JObject { ["foo"] = "bar" };
            var result = json.Adapt<Mock>(_config);
            result.foo.ShouldBe("bar");
        }
    }

    public class Mock
    {
        public string foo { get; set; }
    }
}
