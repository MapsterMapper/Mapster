using System;
using System.Collections.Generic;
using NUnit.Framework;
using Should;

namespace Fpr.Tests
{
    [TestFixture]
    public class WhenMappingNonPublicSetters
    {
        [Test]
        public void Non_Public_Destination_Setter_Is_Populated()
        {
            var poco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            SimpleDto dto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldEqual(poco.Id);
            dto.Name.ShouldEqual(poco.Name);
        }

        [Test]
        public void Non_Public_Destination_Collection_Setter_Is_Populated()
        {
            var poco = new CollectionPoco
            {
                Id = Guid.NewGuid(),
                Name = "TestName",
                Children = new List<ChildDto> {new ChildDto {Id = Guid.NewGuid(), Name = "TestChild"}}
            };

            CollectionDto dto = TypeAdapter.Adapt<CollectionPoco, CollectionDto>(poco);

            dto.Id.ShouldEqual(poco.Id);
            dto.Name.ShouldEqual(poco.Name);

            dto.Children.ShouldNotBeNull();
            dto.Children.Count.ShouldEqual(1);
            dto.Children[0].Id.ShouldEqual(poco.Children[0].Id);
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
            public string Name { get; protected set; }
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

            public IReadOnlyList<ChildDto> Children { get; protected set; }
        }

        #endregion
    }
}
