using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenAddingCustomMappings
    {
        [TestMethod]
        public void Property_Is_Mapped_To_Different_Property_Successfully()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.AnotherName, src => src.Name)
                .Compile();

            var poco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            var dto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
            dto.AnotherName.ShouldBe(poco.Name);
        }

        [TestMethod]
        public void Property_Is_Mapped_From_Null_Value_Successfully()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.AnotherName, src => (string)null)
                .Compile();

            var poco = new SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            var dto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
            dto.AnotherName.ShouldBeNull();
        }

        #region TestClasses

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public string AnotherName { get; set; }
        }

        public class ChildPoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class ChildDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class CollectionPoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public List<ChildPoco> Children { get; set; }
        }

        public class CollectionDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public IReadOnlyList<ChildDto> Children { get; protected set; }
        }

        #endregion
    }
}