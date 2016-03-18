using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace Mapster.Tests
{
    public class WhenMappingWithDictionary
    {
        [TearDown]
        public void TearDown()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [Test]
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

        [Test]
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

        [Test]
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

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
    }
}
