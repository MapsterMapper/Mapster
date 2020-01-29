using System;
using System.Collections.Generic;
using MapsterMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingWithInstance
    {

        [TestMethod]
        public void Mapping_Basic_Poco_Succeeds()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Compile();

            IMapper instance = new Mapper();
            var source = new SimplePoco {Id = Guid.NewGuid(), Name = "TestMethod"};

            var destination = instance.Map<SimpleDto>(source);

            destination.Name.ShouldBe(source.Name);
        }

        [TestMethod]
        public void False_Condition_Primitive_Does_Not_Map()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name, cond => false)
                .Compile();

            var poco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            IMapper instance = new Mapper();
            SimpleDto dto = instance.Map<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBeNull();
        }

        [TestMethod]
        public void Passed_Condition_Primitive_Does_Map()
        {

            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name, cond => cond.Name == "TestName")
                .Compile();

            var poco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            IMapper instance = new Mapper();
            SimpleDto dto = instance.Map<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe("TestName");
        }

        [TestMethod]
        public void Map_With_Adapter()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Compile();

            IMapper instance = new Mapper();
            var source = new SimplePoco {Id = Guid.NewGuid(), Name = "TestMethod"};

            var destination = instance.From(source).AdaptToType<SimpleDto>();

            destination.Name.ShouldBe(source.Name);
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