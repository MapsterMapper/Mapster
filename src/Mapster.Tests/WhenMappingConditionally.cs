using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using static Mapster.Tests.WhenMappingConditionally;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingConditionally
    {
        [TestMethod]
        public void False_Condition_Primitive_Does_Not_Map()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name, cond => false)
                .Compile();

            var poco = new SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            SimpleDto dto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBeNull();
        }

        [TestMethod]
        public void Failed_Condition_Primitive_Does_Not_Map()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name, cond => cond.Name != "TestName")
                .Compile();

            var poco = new SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            SimpleDto dto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBeNull();
        }

        [TestMethod]
        public void Map_Multiple_Condition()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => "1", cond => cond.Name == "1")
                .Map(dest => dest.Name, src => "2", cond => cond.Name == "2")
                .Map(dest => dest.Name, src => "3", cond => cond.Name == "3")
                .Map(dest => dest.Name, src => "4", cond => cond.Name == "4")
                .Map(dest => dest.Name, src => "5", cond => cond.Name == "5")
                .Map(dest => dest.Name, src => "0");

            var list = Enumerable.Range(0, 6).Select(i => new SimplePoco {Name = i.ToString()}).ToList();
            var dtos = list.Adapt<List<SimpleDto>>();

            for (var i = 0; i < list.Count; i++)
            {
                dtos[i].Name.ShouldBe(i.ToString());
            }
        }

        [TestMethod]
        public void Passed_Condition_Primitive_Does_Map()
        {
            
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name, cond => cond.Name == "TestName")
                .Compile();

            var poco = new SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            SimpleDto dto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe("TestName");
        }

        [TestMethod]
        public void Should_Support_Null_Propagation()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<Src, Dest>()
                .Map(dest => dest.Start, src => src.Start, src => src.HasStart);

            var poco = new Src {HasStart = false};
            var dto = poco.Adapt<Dest>(config);

            dto.Start.ShouldBeNull();
        }

        [TestMethod]
        public void Map_Condition_Different_src()
        {
            TypeAdapterConfig<DiffPoco, DiffDto>.NewConfig()
                .Map(dest => dest.Value, src => src.ValueInt, cond => cond.Checker == 1)
                .Map(dest => dest.Value, src => src.ValueString, cond => cond.Checker == 2)
                .Map(dest => dest.Value, src => src.ValueBool, cond => cond.Checker == 3)
                .Map(dest => dest.Value, src => src.Checker);

            var src1 = new DiffPoco { Checker = 1};
            var src2 = new DiffPoco { Checker = 2 };
            var src3 = new DiffPoco { Checker = 3 };

            var result1 = src1.Adapt<DiffDto>();
            var result2 = src2.Adapt<DiffDto>();
            var result3 = src3.Adapt<DiffDto>();

            result1.Value.ShouldBe("42");
            result2.Value.ShouldBe("34");
            result3.Value.ShouldBe("True");

        }


        #region TestClasses

        public class DiffPoco
        {
            public int Checker { get; set;  }
            public int ValueInt { get; set; } = 42;
            public string ValueString { get; set; } = "34";
            public bool ValueBool { get; set; } = true;

        }
        public class DiffDto
        {
            public string Value { get; set; }

        }


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

        public class Dest
        {
            public DateTimeOffset? Start { get; set; }
        }

        public class Src
        {
            public DateTimeOffset Start { get; set; }
            public bool HasStart { get; set; }
        }

        #endregion

    }
}