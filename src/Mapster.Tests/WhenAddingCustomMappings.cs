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
                .Map(dest => dest.LastModified, src => DateTime.Now)
                .Map(dest => dest.FileData, src => new FileData { Content = src.FileContent })
                .Compile();

            var poco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName", FileContent = "Foo"};

            var dto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
            dto.AnotherName.ShouldBe(poco.Name);
            dto.LastModified.Ticks.ShouldBeGreaterThan(0);
            dto.FileData.Content.ShouldBe("Foo");
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
            public string FileContent { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public string AnotherName { get; set; }
            public DateTime LastModified { get; set; }
            public FileData FileData { get; set; }
        }

        public class FileData
        {
            public string Content { get; set; }
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

            public IReadOnlyList<ChildDto> Children { get; internal set; }
        }

        #endregion
    }
}