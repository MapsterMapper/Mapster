using System;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenAddingCustomAdapter
    {
        [TearDown]
        public void Teardown()
        {
            TypeAdapterConfig.GlobalSettings.CustomAdapters.Clear();
        }

        [Test]
        public void Map_Using_Json_Adapter()
        {
            TypeAdapterConfig.GlobalSettings.CustomAdapters.Add(new JsonAdapter());

            var poco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            var json = TypeAdapter.Adapt<SimplePoco, JObject>(poco);
            var poco2 = TypeAdapter.Adapt<JObject, SimplePoco>(json);

            poco2.Id.ShouldEqual(poco.Id);
            poco2.Name.ShouldEqual(poco.Name);
        }

        #region TestClasses

        public class JsonAdapter : ITypeAdapter
        {
            public bool CanAdapt(Type sourceType, Type desinationType)
            {
                return typeof (JToken).IsAssignableFrom(sourceType) ||
                       typeof (JToken).IsAssignableFrom(desinationType);
            }

            public Func<TSource, TDestination> CreateAdaptFunc<TSource, TDestination>()
            {
                if (typeof (JToken).IsAssignableFrom(typeof (TSource)))
                    return src => ((JToken) (object) src).ToObject<TDestination>();
                else
                    return src => (TDestination) (object) JToken.FromObject(src);
            }
        }

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        #endregion
    }
}