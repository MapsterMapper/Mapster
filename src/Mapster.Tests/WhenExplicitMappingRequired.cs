using System;
using System.Collections.Generic;
using System.Reflection;
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
            BaseTypeAdapterConfig.GlobalSettings.RequireExplicitMapping = false;
        }


        [Test]
        public void Unmapped_Classes_Should_Throw()
        {
            try
            {
                //this is to prevent TypeInitializeException
                TypeAdapterConfig<SimplePoco, SimpleDto>.Clear();

                BaseTypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
                TypeAdapterConfig<SimplePoco, SimpleDto>.Clear();

                var simplePoco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

                TypeAdapter.Adapt<SimplePoco, SimpleDto>(simplePoco);
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
            BaseTypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;

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