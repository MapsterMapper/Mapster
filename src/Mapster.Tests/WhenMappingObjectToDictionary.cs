using System;
using System.Collections.Generic;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    public class WhenMappingObjectToDictionary
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

            dict.Count.ShouldEqual(2);
            dict["Id"].ShouldEqual(poco.Id);
            dict["Name"].ShouldEqual(poco.Name);
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

            dict.Count.ShouldEqual(1);
            dict["Id"].ShouldEqual(poco.Id);
        }

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
    }
}
