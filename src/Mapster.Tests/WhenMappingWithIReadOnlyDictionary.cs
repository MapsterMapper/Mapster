using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingWithIReadOnlyDictionary
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestCleanup]
        public void TestCleanup()
        {
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

            var dict = TypeAdapter.Adapt<IReadOnlyDictionary<string, object>>(poco);

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

            var config = new TypeAdapterConfig();
            config.NewConfig<SimplePoco, IReadOnlyDictionary<string, object>>()
                .Map("Code", c => c.Id);
            var dict = poco.Adapt<IReadOnlyDictionary<string, object>>(config);

            dict.Count.ShouldBe(2);
            dict["Code"].ShouldBe(poco.Id);
            dict["Name"].ShouldBe(poco.Name);
        }

        [TestMethod]
        public void Object_To_Dictionary_CamelCase()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<SimplePoco, IReadOnlyDictionary<string, object>>()
                .TwoWays()
                .NameMatchingStrategy(NameMatchingStrategy.ToCamelCase);

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };

            var dict = poco.Adapt<SimplePoco, IReadOnlyDictionary<string, object>>(config);

            dict.Count.ShouldBe(2);
            dict["id"].ShouldBe(poco.Id);
            dict["name"].ShouldBe(poco.Name);

            var poco2 = dict.Adapt<SimplePoco>(config);
            poco2.Id.ShouldBe(dict["id"]);
            poco2.Name.ShouldBe(dict["name"]);
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

            IReadOnlyDictionary<string, object> dict = new Dictionary<string, object>
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
            TypeAdapterConfig<SimplePoco, IReadOnlyDictionary<string, object>>.NewConfig()
                .IgnoreNullValues(true);

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = null,
            };

            var dict = TypeAdapter.Adapt<IReadOnlyDictionary<string, object>>(poco);

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
            TypeAdapterConfig<IReadOnlyDictionary<string, object>, SimplePoco>.NewConfig()
                .Compile();

            var poco = TypeAdapter.Adapt<IReadOnlyDictionary<string, object>, SimplePoco>(dict);
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

            TypeAdapterConfig<IReadOnlyDictionary<string, object>, SimplePoco>.NewConfig()
                .Map(c => c.Id, "Code")
                .Compile();

            var poco = TypeAdapter.Adapt<IReadOnlyDictionary<string, object>, SimplePoco>(dict);
            poco.Id.ShouldBe(dict["Code"]);
            poco.Name.ShouldBeNull();
        }

        [TestMethod]
        public void Dictionary_To_Object_CamelCase()
        {
            TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.FromCamelCase);
            TypeAdapterConfig<IReadOnlyDictionary<string, object>, SimplePoco>.NewConfig()
                .Compile();
            var dict = new Dictionary<string, object>
            {
                ["id"] = Guid.NewGuid(),
                ["Name"] = "bar",
                ["foo"] = "test",
            };

            var poco = TypeAdapter.Adapt<IReadOnlyDictionary<string, object>, SimplePoco>(dict);
            poco.Id.ShouldBe(dict["id"]);
            poco.Name.ShouldBeNull();
        }

        [TestMethod]
        public void Dictionary_To_Object_Flexible()
        {
            var config = new TypeAdapterConfig();
            config.Default.NameMatchingStrategy(NameMatchingStrategy.Flexible);
            var dict = new Dictionary<string, object>
            {
                ["id"] = Guid.NewGuid(),
                ["Name"] = "bar",
                ["foo"] = "test",
            };

            var poco = TypeAdapter.Adapt<IReadOnlyDictionary<string, object>, SimplePoco>(dict, config);
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
            IReadOnlyDictionary<string, int> dict = new Dictionary<string, int>
            {
                ["a"] = 1
            };
            var result = dict.Adapt<IReadOnlyDictionary<string, int>>();
            result["a"].ShouldBe(1);
        }

        [TestMethod]
        public void Dictionary_Of_String_Mix()
        {
            TypeAdapterConfig<IReadOnlyDictionary<string, int?>, IReadOnlyDictionary<string, int>>.NewConfig()
                .Map("A", "a")
                .Ignore("c")
                .IgnoreIf((src, dest) => src.Count > 3, "d")
                .IgnoreNullValues(true)
                .NameMatchingStrategy(NameMatchingStrategy.ConvertSourceMemberName(s => "_" + s));
            IReadOnlyDictionary<string, int?> dict = new Dictionary<string, int?>
            {
                ["a"] = 1,
                ["b"] = 2,
                ["c"] = 3,
                ["d"] = 4,
                ["e"] = null,
            };
            var result = dict.Adapt<IReadOnlyDictionary<string, int>>();
            result.Count.ShouldBe(2);
            result["A"].ShouldBe(1);
            result["_b"].ShouldBe(2);
        }

        [TestMethod]
        public void AdaptClassWithIntegerKeyDictionary()
        {
            var instanceWithDictionary = new ClassWithIntKeyDictionary();
            instanceWithDictionary.Dict = new Dictionary<int, SimplePoco>
            {
                { 1 , new SimplePoco { Id =  Guid.NewGuid(), Name = "one"} },
                { 100 , new SimplePoco { Id =  Guid.NewGuid(), Name = "one hundred"} },
            };

            var result = instanceWithDictionary.Adapt<OtherClassWithIntKeyDictionary>();
            result.Dict[1].Name.ShouldBe("one");
            result.Dict[100].Name.ShouldBe("one hundred");
        }

        [TestMethod]
        public void AdaptClassWithIntegerKeyDictionaryInterface()
        {
            var instanceWithDictionary = new ClassWithIntKeyDictionary();
            instanceWithDictionary.Dict = new Dictionary<int, SimplePoco>
            {
                { 1 , new SimplePoco { Id =  Guid.NewGuid(), Name = "one"} },
                { 100 , new SimplePoco { Id =  Guid.NewGuid(), Name = "one hundred"} },
            };

            var result = instanceWithDictionary.Adapt<ClassWithIntKeyIDictionary>();
            result.Dict[1].Name.ShouldBe("one");
            result.Dict[100].Name.ShouldBe("one hundred");
            result.Dict.ShouldNotBeSameAs(instanceWithDictionary.Dict);
        }

        [TestMethod]
        public void AdaptClassWithObjectKeyDictionary()
        {
            var instanceWithDictionary = new ClassWithPocoKeyDictionary();
            instanceWithDictionary.Dict = new Dictionary<SimplePoco, int>
            {
                { new SimplePoco { Id =  Guid.NewGuid(), Name = "one"}, 1 },
                { new SimplePoco { Id =  Guid.NewGuid(), Name = "one hundred"}, 100 },
            };

            var result = instanceWithDictionary.Adapt<OtherClassWithPocoKeyDictionary>();
            result.Dict.Keys.Any(k => k.Name == "one").ShouldBeTrue();
            result.Dict.Keys.Any(k => k.Name == "one hundred").ShouldBeTrue();
        }

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class A
        {
            public IReadOnlyDictionary<int, decimal> Prop { get; set; }
        }

        public class ClassWithIntKeyDictionary
        {
            public IReadOnlyDictionary<int, SimplePoco> Dict { get; set; }
        }

        public class ClassWithIntKeyIDictionary
        {
            public IReadOnlyDictionary<int, SimplePoco> Dict { get; set; }
        }

        public class OtherClassWithIntKeyDictionary
        {
            public IReadOnlyDictionary<int, SimplePoco> Dict { get; set; }
        }

        public class ClassWithPocoKeyDictionary
        {
            public IReadOnlyDictionary<SimplePoco, int> Dict { get; set; }
        }

        public class OtherClassWithPocoKeyDictionary
        {
            public IReadOnlyDictionary<SimplePoco, int> Dict { get; set; }
        }
    }
}
