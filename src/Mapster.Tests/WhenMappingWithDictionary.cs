using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingWithDictionary
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
            TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.Exact);
        }

        [TestMethod]
        public void Object_To_Dictionary()
        {
            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };

            var dict = TypeAdapter.Adapt<Dictionary<string, object>>(poco);

            dict.Count.ShouldBe(2);
            dict["Id"].ShouldBe(poco.Id);
            dict["Name"].ShouldBe(poco.Name);
        }


        [TestMethod]
        public void Object_To_Dictionary_Map()
        {
            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };

            TypeAdapterConfig<SimplePoco, Dictionary<string, object>>.NewConfig()
                .Map("Code", c => c.Id);
            var dict = TypeAdapter.Adapt<Dictionary<string, object>>(poco);

            dict.Count.ShouldBe(2);
            dict["Code"].ShouldBe(poco.Id);
            dict["Name"].ShouldBe(poco.Name);
        }

        [TestMethod]
        public void Object_To_Dictionary_CamelCase()
        {
            TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.ToCamelCase);
            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };

            var dict = TypeAdapter.Adapt<Dictionary<string, object>>(poco);

            dict.Count.ShouldBe(2);
            dict["id"].ShouldBe(poco.Id);
            dict["name"].ShouldBe(poco.Name);
        }

        [TestMethod]
        public void Object_To_Dictionary_Flexible()
        {
            TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.Flexible);
            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };

            var dict = new Dictionary<string, object>
            {
                ["id"] = Guid.NewGuid()
            };

            TypeAdapter.Adapt(poco, dict);

            dict.Count.ShouldBe(2);
            dict["id"].ShouldBe(poco.Id);
            dict["Name"].ShouldBe(poco.Name);
        }

        [TestMethod]
        public void Object_To_Dictionary_Ignore_Null_Values()
        {
            TypeAdapterConfig<SimplePoco, Dictionary<string, object>>.NewConfig()
                .IgnoreNullValues(true);

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = null,
            };

            var dict = TypeAdapter.Adapt<Dictionary<string, object>>(poco);

            dict.Count.ShouldBe(1);
            dict["Id"].ShouldBe(poco.Id);
        }

        [TestMethod]
        public void Dictionary_To_Object()
        {
            var dict = new Dictionary<string, object>
            {
                ["Id"] = Guid.NewGuid(),
                ["Foo"] = "test",
            };

            var poco = TypeAdapter.Adapt<SimplePoco>(dict);
            poco.Id.ShouldBe(dict["Id"]);
            poco.Name.ShouldBeNull();
        }


        [TestMethod]
        public void Dictionary_To_Object_Map()
        {
            var dict = new Dictionary<string, object>
            {
                ["Code"] = Guid.NewGuid(),
                ["Foo"] = "test",
            };

            TypeAdapterConfig<Dictionary<string, object>, SimplePoco>.NewConfig()
                .Map(c => c.Id, "Code");

            var poco = TypeAdapter.Adapt<SimplePoco>(dict);
            poco.Id.ShouldBe(dict["Code"]);
            poco.Name.ShouldBeNull();
        }

        [TestMethod]
        public void Dictionary_To_Object_CamelCase()
        {
            TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.FromCamelCase);
            var dict = new Dictionary<string, object>
            {
                ["id"] = Guid.NewGuid(),
                ["Name"] = "bar",
                ["foo"] = "test",
            };

            var poco = TypeAdapter.Adapt<SimplePoco>(dict);
            poco.Id.ShouldBe(dict["id"]);
            poco.Name.ShouldBeNull();
        }

        [TestMethod]
        public void Dictionary_To_Object_Flexible()
        {
            TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.Flexible);
            var dict = new Dictionary<string, object>
            {
                ["id"] = Guid.NewGuid(),
                ["Name"] = "bar",
                ["foo"] = "test",
            };

            var poco = TypeAdapter.Adapt<SimplePoco>(dict);
            poco.Id.ShouldBe(dict["id"]);
            poco.Name.ShouldBe(dict["Name"]);
        }

        [TestMethod]
        public void Dictionary_Of_Int()
        {
            var result = TypeAdapter.Adapt<A, A>(new A { Prop = new Dictionary<int, decimal> { { 1, 2m } } });
            result.Prop[1].ShouldBe(2m);
        }

        [TestMethod]
        public void Dictionary_Of_String()
        {
            var dict = new Dictionary<string, int>
            {
                ["a"] = 1
            };
            var result = dict.Adapt<Dictionary<string, int>>();
            result["a"].ShouldBe(1);
        }

        [TestMethod]
        public void Dictionary_Of_String_Mix()
        {
            TypeAdapterConfig<Dictionary<string, int?>, Dictionary<string, int>>.NewConfig()
                .Map("A", "a")
                .Ignore("c")
                .IgnoreIf((src, dest) => src.Count > 3, "d")
                .IgnoreNullValues(true)
                .NameMatchingStrategy(NameMatchingStrategy.ConvertSourceMemberName(s => "_" + s));
            var dict = new Dictionary<string, int?>
            {
                ["a"] = 1,
                ["b"] = 2,
                ["c"] = 3,
                ["d"] = 4,
                ["e"] = null,
            };
            var result = dict.Adapt<Dictionary<string, int>>();
            result.Count.ShouldBe(2);
            result["A"].ShouldBe(1);
            result["_b"].ShouldBe(2);
        }

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class A
        {
            public Dictionary<int, decimal> Prop { get; set; }
        }
    }
}
