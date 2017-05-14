using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingRecordTypes
    {
        [TestMethod]
        public void Map_Dictionary()
        {
            var source = new Dictionary<string, SimplePoco>
            {
                {"a", new SimplePoco {Id = Guid.NewGuid(), Name = "bar"}}
            };
            var dest = source.Adapt<Dictionary<string, SimpleDto>>();

            dest.Count.ShouldBe(source.Count);
            dest["a"].Id.ShouldBe(source["a"].Id);
            dest["a"].Name.ShouldBe(source["a"].Name);
        }

        [TestMethod]
        public void Map_RecordType()
        {
            var source = new SimplePoco {Id = Guid.NewGuid(), Name = "bar"};
            var dest = source.Adapt<RecordType>();

            dest.Id.ShouldBe(source.Id);
            dest.Name.ShouldBe(source.Name);
            dest.Day.ShouldBe(default(DayOfWeek));
            dest.Age.ShouldBe(10);
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

        public class RecordType
        {
            public RecordType(Guid id, DayOfWeek day, string name = "foo", int age = 10)
            {
                this.Id = id;
                this.Day = day;
                this.Name = name;
                this.Age = age;
            }

            public Guid Id { get; }
            public string Name { get; }
            public int Age { get; }
            public DayOfWeek Day { get; }
        }
    }
}
