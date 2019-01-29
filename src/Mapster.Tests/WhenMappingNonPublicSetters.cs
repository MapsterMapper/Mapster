using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingNonPublicSetters
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestMethod]
        public void Non_Public_Destination_Setter_Is_Populated()
        {
            var poco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            SimpleDto dto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
        }

        [TestMethod]
        public void Non_Public_Destination_Collection_Setter_Is_Populated()
        {
            var poco = new CollectionPoco
            {
                Id = Guid.NewGuid(),
                Name = "TestName",
                Children = new List<ChildDto> {new ChildDto {Id = Guid.NewGuid(), Name = "TestChild"}}
            };

            CollectionDto dto = TypeAdapter.Adapt<CollectionPoco, CollectionDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);

            dto.Children.ShouldNotBeNull();
            dto.Children.Count.ShouldBe(1);
            dto.Children[0].Id.ShouldBe(poco.Children[0].Id);
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
            public string Name { get; internal set; }
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

            public List<ChildDto> Children { get; set; }
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
