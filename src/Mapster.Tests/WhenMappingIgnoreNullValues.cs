using System.Collections.Generic;
using NUnit.Framework;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenMappingIgnoreNullValues
    {
        [Test]
        public void Map()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
            TypeAdapterConfig.GlobalSettings.NewConfig<SourceClass, DestClass>()
                .IgnoreNullValues(true)
                .Compile();

            var source = new SourceClass();
            var dest = source.Adapt<DestClass>();

            Assert.AreEqual("Hello", dest.Title);
            Assert.NotNull(dest.Sub);
            Assert.NotNull(dest.List);
        }

        [Test]
        public void Map_To_Target()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
            TypeAdapterConfig.GlobalSettings.NewConfig<SourceClass, DestClass>()
                .IgnoreNullValues(true)
                .Compile();

            var source = new SourceClass();
            var dest = source.Adapt(new DestClass());

            Assert.AreEqual("Hello", dest.Title);
            Assert.NotNull(dest.Sub);
            Assert.NotNull(dest.List);
        }

        public class DestClass
        {
            public string Title { get; set; }
            public List<string> List { get; set; }
            public SubClass Sub { get; set; }

            public DestClass()
            {
                List = new List<string>();
                Sub = new SubClass();
                Title = "Hello";
            }
        }

        public class SourceClass
        {
            public string Title { get; set; }
            public List<string> List { get; set; }
            public SubClass Sub { get; set; }
        }

        public class SubClass
        {
            public string SubName { get; set; }
        }
    }
}
