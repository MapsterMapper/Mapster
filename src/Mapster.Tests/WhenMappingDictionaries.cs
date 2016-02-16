using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenMappingDictionaries
    {
        [Test]
        public void Map_Dictionary()
        {
            var source = new Dictionary<string, SimplePoco>
            {
                {"a", new SimplePoco {Id = Guid.NewGuid(), Name = "b"}}
            };
            var dest = source.Adapt<Dictionary<string, SimpleDto>>();

            source.Count.ShouldEqual(dest.Count);
            source["a"].Id.ShouldEqual(dest["a"].Id);
            source["a"].Name.ShouldEqual(dest["a"].Name);
        }

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
    }
}
