using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenUsingAttributeWithNameMatchingStrategy
    {
        [TestMethod]
        public void Using_Attributes_With_NameMatchingStrategy()
        {
            TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);

            var id = Guid.NewGuid();
            var poco = new SimplePoco(id) { Name = "test" };
            var dto = poco.Adapt<SimpleDto>();
            dto.IdCode.ShouldBe(id);
            dto.Name.ShouldBeNull();
        }

        public class SimplePoco
        {
            public SimplePoco(Guid id) { this.id = id; }

            [AdaptMember("IdCode")]
            private Guid id { get; }

            [AdaptIgnore]
            public string Name { get; set; }
        }

        public class SimpleDto
        {
            public Guid IdCode { get; set; }
            public string Name { get; set; }
        }


    }
}
