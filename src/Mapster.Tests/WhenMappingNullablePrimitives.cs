using System;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    public class WhenMappingNullablePrimitives
    {

        [Test]
        public void Can_Map_From_Null_Source_To_Non_Nullable_Existing_Target()
        {
            TypeAdapterConfig<NullablePrimitivesPoco, NonNullablePrimitivesDto>.NewConfig().IgnoreNullValues(true);

            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName" };

            var dto = new NonNullablePrimitivesDto();

            TypeAdapter.Adapt(poco, dto);

            dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NonNullablePrimitivesDto>(poco);

            dto.Id.ShouldEqual(poco.Id);
            dto.Name.ShouldEqual(poco.Name);
            dto.IsImport.ShouldBeFalse();
        }

        [Test]
        public void Can_Map_From_Null_Source_To_Non_Nullable_Target()
        {
            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName" };

            NonNullablePrimitivesDto dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NonNullablePrimitivesDto>(poco);

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
            dto.Amount.ShouldBeNull();
        }

        [Test]
        public void Can_Map_From_Nullable_Source_To_Nullable_Existing_Target()
        {
            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName" };

            NullablePrimitivesPoco2 dto = new NullablePrimitivesPoco2
            {
                IsImport = true,
                Amount = 1,
            };

            TypeAdapter.Adapt(poco, dto);

            dto.Id.ShouldEqual(poco.Id);
            dto.Name.ShouldEqual(poco.Name);
            dto.IsImport.ShouldBeNull();
            dto.Amount.ShouldBeNull();
        }

        [Test]
        public void Can_Map_From_Nullable_Source_With_Values_To_Non_Nullable_Target()
        {
            TypeAdapterConfig<NullablePrimitivesPoco, NonNullablePrimitivesDto>.NewConfig();
            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName", IsImport = true, Amount = 10};

            NonNullablePrimitivesDto dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NonNullablePrimitivesDto>(poco);

            dto.Id.ShouldEqual(poco.Id);
            dto.Name.ShouldEqual(poco.Name);
            dto.IsImport.ShouldBeTrue();
            dto.Amount.ShouldEqual(10);
        }

        [Test]
        public void Can_Map_From_Nullable_Source_Without_Values_To_Non_Nullable_Target()
        {
            TypeAdapterConfig<NullablePrimitivesPoco, NonNullablePrimitivesDto>.NewConfig();
            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName"};

            NonNullablePrimitivesDto dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NonNullablePrimitivesDto>(poco);

            dto.Id.ShouldEqual(poco.Id);
            dto.Name.ShouldEqual(poco.Name);
            dto.IsImport.ShouldBeFalse();
            dto.Amount.ShouldEqual(0);
        }

        [Test]
        public void Can_Map_From_Nullable_Source_With_Values_To_Explicitly_Mapped_Non_Nullable_Target()
        {
            TypeAdapterConfig<NullablePrimitivesPoco, NonNullablePrimitivesDto>.NewConfig()
                .Map(dest => dest.Amount, src => src.Amount)
                .Map(dest => dest.IsImport, src => src.IsImport)
                .Map(dest => dest.MyFee, src => src.Fee);

            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName", Fee = 20, IsImport = true, Amount = 10};

            NonNullablePrimitivesDto dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NonNullablePrimitivesDto>(poco);

            dto.Id.ShouldEqual(poco.Id);
            dto.Name.ShouldEqual(poco.Name);
            dto.IsImport.ShouldBeTrue();
            dto.Amount.ShouldEqual(10);
            dto.MyFee.ShouldEqual(20);
        }

        [Test]
        public void Can_Map_From_Non_Nullable_Source_To_Nullable_Target()
        {
            var dto = new NonNullablePrimitivesDto { Id = Guid.NewGuid(), Name = "TestName", IsImport = true};

            NullablePrimitivesPoco poco = TypeAdapter.Adapt<NonNullablePrimitivesDto, NullablePrimitivesPoco>(dto);

            poco.Id.ShouldEqual(dto.Id);
            poco.Name.ShouldEqual(dto.Name);
            poco.IsImport.GetValueOrDefault().ShouldBeTrue();
        }

        #region TestClasses

        public class NullablePrimitivesPoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public bool? IsImport { get; set; }

            public decimal? Amount { get; set; }

            public decimal? Fee { get; set; }
        }

        public class NullablePrimitivesPoco2
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public bool? IsImport { get; set; }

            public decimal? Amount { get; set; }

            public decimal? Fee { get; set; }
        }

        public class NonNullablePrimitivesDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public bool IsImport { get; set; }

            public decimal Amount { get; set; }

            public decimal MyFee { get; set; }
        }

        #endregion 
    }
}