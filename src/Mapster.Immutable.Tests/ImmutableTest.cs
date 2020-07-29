using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Immutable.Tests
{
    [TestClass]
    public class TestImmutable
    {
        [TestMethod]
        public void TestImmutableArray()
        {
            var config = new TypeAdapterConfig();
            config.EnableImmutableMapping();

            var list = new[] {1, 2, 3, 4};
            var array = list.Adapt<ImmutableArray<int>>(config);
            array.ShouldBe(list);
        }

        [TestMethod]
        public void TestImmutableDictionary()
        {
            var config = new TypeAdapterConfig();
            config.EnableImmutableMapping();

            var poco = new {Name = "Foo", Id = "Bar"};
            var dict = poco.Adapt<ImmutableDictionary<string, string>>(config);
            dict["Name"].ShouldBe(poco.Name);
            dict["Id"].ShouldBe(poco.Id);
        }

        [TestMethod]
        public void TestImmutableDictionary2()
        {
            var config = new TypeAdapterConfig();
            config.EnableImmutableMapping();

            var list = new Dictionary<int, int>
            {
                [1] = 2,
                [3] = 4,
            };
            var dict = list.Adapt<ImmutableDictionary<int, int>>(config);
            dict.ShouldBe(list);
        }
    }
}
