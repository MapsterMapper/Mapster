using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    /// <summary>
    /// https://github.com/MapsterMapper/Mapster/issues/952
    /// </summary>
    [TestClass]
    public class WhenExplicitNullDestinationTransformRegression
    {
        [TestMethod]
        public void ExplicitNullMapping_ShouldNotBeOverridden_ByDefaultEmptyCollectionTransform()
        {
            var config = new TypeAdapterConfig();

            config.Default
                .AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);

            config.NewConfig<Foo952, FooDto952>()
                .Map(d => d.Strings, _ => (string[]?)null);

            var foo = new Foo952([]);

            var dto = foo.Adapt<FooDto952>(config);

            dto.Strings.ShouldBeNull();
        }

        record Foo952(string?[] Strings);

        class FooDto952
        {
            public string[]? Strings { get; set; }
        }
    }
}
