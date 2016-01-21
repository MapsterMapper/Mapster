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
            try
            {
                //compile first to prevent type initialize exception
                TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig().Compile();

                TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
                TypeAdapterConfig<SimplePoco, SimpleDto>.Clear();

                var simplePoco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

                TypeAdapter.Adapt<SimpleDto>(simplePoco);
                Assert.Fail();
            }
            catch (InvalidOperationException ex)
            {
                ex.Message.ShouldContain("SimplePoco");
                ex.Message.ShouldContain("SimpleDto");
            }
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

        [Test]
        public void Mapped_Classes_Succeed_With_Child_Mapping()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;

            TypeAdapterConfig<CollectionPoco, CollectionDto>.NewConfig();

            var collectionPoco = new CollectionPoco { Id = Guid.NewGuid(), Name = "TestName", Children = new List<ChildPoco>() };

            var collectionDto = TypeAdapter.Adapt<CollectionPoco, CollectionDto>(collectionPoco);

            collectionDto.Name.ShouldEqual(collectionPoco.Name);
        }

		[Test]
		public void Mapped_Classes_Succeed_When_Enumerable_Is_Mapped()
		{
			TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;

			TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig();

			var simplePoco = new List<SimplePoco>
			{
				new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"},
				new SimplePoco {Id = Guid.NewGuid(), Name = "TestName2"}
			};

			var results = TypeAdapter.Adapt<IList<SimpleDto>>(simplePoco);

			results.Count.ShouldEqual(2);
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