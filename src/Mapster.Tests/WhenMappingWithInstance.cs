using System;
using System.Collections.Generic;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenMappingWithInstance
    {

        [Test]
        public void Mapping_Basic_Poco_Succeeds()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig();
            IAdapter instance = TypeAdapter.GetInstance();
            var source = new SimplePoco { Id = new Guid(), Name = "Test" };

            var destination = instance.Adapt<SimpleDto>(source);

            destination.Name.ShouldEqual(source.Name);
        }

        [Test]
        public void False_Condition_Primitive_Does_Not_Map()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name, cond => false);

            var poco = new SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            IAdapter instance = TypeAdapter.GetInstance();
            SimpleDto dto = instance.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldEqual(poco.Id);
            dto.Name.ShouldBeNull();
        }

        [Test]
        public void Passed_Condition_Primitive_Does_Map()
        {

            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name, cond => cond.Name == "TestName");

            var poco = new SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            IAdapter instance = TypeAdapter.GetInstance();
            SimpleDto dto = instance.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldEqual(poco.Id);
            dto.Name.ShouldEqual("TestName");
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