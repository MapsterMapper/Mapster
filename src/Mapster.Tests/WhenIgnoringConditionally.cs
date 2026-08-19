using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenIgnoringConditionally
    {

        #region Tests

        [TestMethod]
        public void True_Constant_Ignores_Map()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreIf((src, dest) => true, dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "TestName" };
            SimpleDto dto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(1);
            dto.Name.ShouldBeNull();
        }

        [TestMethod]
        public void True_Constant_Ignores_Map_To_Target()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreIf((src, dest) => true, dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "TestName" };
            var dto = new SimpleDto { Id = 999, Name = "DtoName" };
            TypeAdapter.Adapt(poco, dto);

            dto.Id.ShouldBe(1);
            dto.Name.ShouldBe("DtoName");
        }

        [TestMethod]
        public void True_Condition_Ignores_Map()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreIf((src, dest) => src.Name == "TestName", dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "TestName" };
            SimpleDto dto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(1);
            dto.Name.ShouldBeNull();
        }

        [TestMethod]
        public void True_Condition_Ignores_Map_To_Target()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreIf((src, dest) => src.Name == "TestName", dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "TestName" };
            var dto = new SimpleDto { Id = 999, Name = "DtoName" };
            TypeAdapter.Adapt(poco, dto);

            dto.Id.ShouldBe(1);
            dto.Name.ShouldBe("DtoName");
        }

        [TestMethod]
        public void Null_Condition_Ignores_Map()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreIf(null, dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "TestName" };
            var dto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(1);
            dto.Name.ShouldBeNull();
        }

        [TestMethod]
        public void Null_Condition_Ignores_Map_To_Target()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreIf(null, dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "TestName" };
            var dto = new SimpleDto { Id = 999, Name = "DtoName" };
            TypeAdapter.Adapt(poco, dto);

            dto.Id.ShouldBe(1);
            dto.Name.ShouldBe("DtoName");
        }

        [TestMethod]
        public void True_Condition_On_Target_Ignores_Map()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreIf((src, dest) => !string.IsNullOrEmpty(dest.Name), dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "TestName" };
            var dto = TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco);

            dto.Id.ShouldBe(1);
            dto.Name.ShouldBe("TestName");
        }

        [TestMethod]
        public void True_Condition_On_Target_Ignores_Map_To_Target()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreIf((src, dest) => !string.IsNullOrEmpty(dest.Name), dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "TestName" };
            var dto = new SimpleDto { Id = 999, Name = "DtoName" };
            TypeAdapter.Adapt(poco, dto);

            dto.Id.ShouldBe(1);
            dto.Name.ShouldBe("DtoName");
        }

        [TestMethod]
        public void False_Condition_Does_Not_Ignore()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreIf((src, dest) => src.Name == "TestName", dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "NotTestName" };
            var dto = new SimpleDto { Id = 999, Name = "DtoName" };
            TypeAdapter.Adapt(poco, dto);

            dto.Id.ShouldBe(1);
            dto.Name.ShouldBe("NotTestName");
        }

        [TestMethod]
        public void IgnoreIf_Can_Be_Combined()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreIf((src, dest) => src.Name == "NotTestName", dest => dest.Name)
                .IgnoreIf((src, dest) => src.Name == "TestName", dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "NotTestName" };
            var dto = new SimpleDto { Id = 999, Name = "DtoName" };
            TypeAdapter.Adapt(poco, dto);

            dto.Id.ShouldBe(1);
            dto.Name.ShouldBe("DtoName");
        }

        [TestMethod]
        public void IgnoreIf_Apply_To_RecordType()
        {
            TypeAdapterConfig<SimplePoco, SimpleRecord>.NewConfig()
                .IgnoreIf((src, dest) => src.Name == "TestName", dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "TestName" };

            var srt = poco.BuildAdapter().CreateMapToTargetExpression<SimpleRecord>();
            var dto = TypeAdapter.Adapt<SimplePoco, SimpleRecord>(poco);

            dto.Id.ShouldBe(1);
            dto.Name.ShouldBeNull();
        }

        // Regression test for https://github.com/MapsterMapper/Mapster/issues/1007
        // Unlike SimpleRecord above (which maps every member through the primary
        // constructor), NativeRecord is a real C# `record` whose Name property is
        // NOT part of the primary constructor — it's a plain settable auto-property.
        // Members mapped that way go through RecordTypeAdapter.RecordInlineExpression,
        // a separate code path from constructor-argument mapping, which used to
        // ignore member.Ignore.Condition entirely.
        [TestMethod]
        public void IgnoreIf_Apply_To_RecordType_Property_Not_In_Constructor_Map()
        {
            TypeAdapterConfig<SimplePoco, NativeRecord>.NewConfig()
                .IgnoreIf((src, dest) => src.Name == "TestName", dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "TestName" };
            var dto = TypeAdapter.Adapt<SimplePoco, NativeRecord>(poco);

            dto.Id.ShouldBe(1);
            dto.Name.ShouldBeNull();
        }

        [TestMethod]
        public void IgnoreIf_Apply_To_RecordType_Property_Not_In_Constructor_MapToTarget()
        {
            TypeAdapterConfig<SimplePoco, NativeRecord>.NewConfig()
                .IgnoreIf((src, dest) => src.Name == "TestName", dest => dest.Name)
                .Compile();

            var poco = new SimplePoco { Id = 1, Name = "TestName" };
            var dto = new NativeRecord { Id = 999, Name = "DtoName" };

            var result = TypeAdapter.Adapt(poco, dto);

            result.Id.ShouldBe(1);
            result.Name.ShouldBe("DtoName");
        }

        #endregion


        #region TestClasses

        public class SimplePoco
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [AdaptWith(AdaptDirectives.DestinationAsRecord)]
        public class SimpleRecord
        {
            public int Id { get; private set; }
            public string Name { get; private set; }

            public SimpleRecord(int id, string name)
            {
                this.Id = id;
                this.Name = name;
            }
        }

        public record NativeRecord
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        #endregion

    }
}
