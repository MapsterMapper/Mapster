using System;
using System.Collections.Generic;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    
    [TestFixture]
    public class WhenExplicitMappingRequired
    {
        [TestFixtureTearDown]
        public void TearDown()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = false;
        }


        [Test]
        public void Unmapped_Classes_Should_Throw()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;

            TypeAdapterConfig<SimplePoco, SimpleDto>.Clear();

            var simplePoco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => TypeAdapter.Adapt<SimplePoco, SimpleDto>(simplePoco));

            Console.WriteLine(exception.Message);

            exception.Message.ShouldContain("SimplePoco");
            exception.Message.ShouldContain("SimpleDto");
        }

        [Test]
        public void Mapped_Classes_Succeed()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;

            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig();

            var simplePoco = new SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            var simpleDto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(simplePoco);

            simpleDto.Name.ShouldEqual(simplePoco.Name);
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