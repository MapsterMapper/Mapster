using System;
using System.Collections.Generic;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    
    [TestFixture]
    public class WhenExplicitMappingRequired
    {
        [TearDown]
        public void TearDown()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = false;
			TypeAdapterConfig.GlobalSettings.Clear();
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
        public void Mapped_Classes_Succeed_With_Mapped_Enum()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
            TypeAdapterConfig<SimpleEnumPoco, SimpleDto>.NewConfig();

            var simpleEnumPoco = new SimpleEnumPoco { Id = Guid.NewGuid(), Name = NameEnum.Martha };

            var simpleDto = TypeAdapter.Adapt<SimpleEnumPoco, SimpleDto>(simpleEnumPoco);

            simpleDto.Name.ShouldEqual(simpleEnumPoco.Name.ToString());
        }

		[Test]
		public void Mapped_Classes_With_Mapped_Enum_Compiles()
		{
			TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
			TypeAdapterConfig<SimpleEnumPoco, SimpleDto>.NewConfig();

			TypeAdapterConfig.GlobalSettings.Compile();

			var simpleEnumPoco = new SimpleEnumPoco { Id = Guid.NewGuid(), Name = NameEnum.Martha };
			var simpleDto = TypeAdapter.Adapt<SimpleEnumPoco, SimpleDto>(simpleEnumPoco);

			simpleDto.Name.ShouldEqual(simpleEnumPoco.Name.ToString());
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
        public void Mapped_List_Of_Classes_Succeed()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;

            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig();

            var simplePocos = new[]
            {
                new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"},
				new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"}
			};

            var simpleDtos = TypeAdapter.Adapt<SimplePoco[], List<SimpleDto>>(simplePocos);

            simpleDtos[0].Name.ShouldEqual(simplePocos[0].Name);
        }

		[Test]
		public void Mapped_Classes_Succeed_With_Child_Mapping()
		{
			TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;

			TypeAdapterConfig<CollectionPoco, CollectionDto>.NewConfig();
			TypeAdapterConfig<ChildPoco, ChildDto>.NewConfig();

			var collectionPoco = new CollectionPoco { Id = Guid.NewGuid(), Name = "TestName", Children = new List<ChildPoco>() };

			var collectionDto = TypeAdapter.Adapt<CollectionPoco, CollectionDto>(collectionPoco);

			collectionDto.Name.ShouldEqual(collectionPoco.Name);
		}

		[Test]
		public void Mapped_Classes_Succeed_When_List_To_IList_Is_Mapped()
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


		public enum NameEnum
	    {
		    Billy = 0,
			Martha = 1,
			Marcus = 2
	    }

	    public class SimpleEnumPoco
	    {
			public Guid Id { get; set; }
			public NameEnum Name { get; set; }
		}

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