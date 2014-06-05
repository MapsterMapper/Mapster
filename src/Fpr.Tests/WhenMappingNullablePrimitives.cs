using System;
using NUnit.Framework;
using Should;

namespace Fpr.Tests
{
    public class WhenMappingNullablePrimitives
    {

        [Test]
        public void Can_Map_From_Null_Source_To_Non_Nullable_Existing_Target()
        {
            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName" };

            var dto = new NullablePrimitivesDto();

            TypeAdapter.Adapt(poco, dto);

            dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NullablePrimitivesDto>(poco);

            dto.Id.ShouldEqual(poco.Id);
            dto.Name.ShouldEqual(poco.Name);
            dto.IsImport.ShouldBeFalse();
        }

        [Test]
        public void Can_Map_From_Null_Source_To_Non_Nullable_Target()
        {
            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName" };

            NullablePrimitivesDto dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NullablePrimitivesDto>(poco);

            dto.Id.ShouldEqual(poco.Id);
            dto.Name.ShouldEqual(poco.Name);
            dto.IsImport.ShouldBeFalse();
        }

        [Test]
        public void Can_Map_From_Nullable_Source_To_Nullable_Target()
        {
            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName" };

            NullablePrimitivesPoco2 dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NullablePrimitivesPoco2>(poco);

            dto.Id.ShouldEqual(poco.Id);
            dto.Name.ShouldEqual(poco.Name);
            dto.IsImport.ShouldBeNull();
        }

        [Test]
        public void Can_Map_From_Non_Nullable_Source_To_Nullable_Target()
        {
            var dto = new NullablePrimitivesDto { Id = Guid.NewGuid(), Name = "TestName", IsImport = true};

            NullablePrimitivesPoco poco = TypeAdapter.Adapt<NullablePrimitivesDto, NullablePrimitivesPoco>(dto);

            poco.Id.ShouldEqual(dto.Id);
            poco.Name.ShouldEqual(dto.Name);
            poco.IsImport.Value.ShouldBeTrue();
        }

        #region TestClasses

        public class NullablePrimitivesPoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public bool? IsImport { get; set; }
        }

        public class NullablePrimitivesPoco2
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public bool? IsImport { get; set; }
        }

        public class NullablePrimitivesDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public bool IsImport { get; set; }
        }

        #endregion 
    }
}