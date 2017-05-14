using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingNullablePrimitives
    {

        [TestMethod]
        public void Can_Map_From_Null_Source_To_Non_Nullable_Existing_Target()
        {
            TypeAdapterConfig<NullablePrimitivesPoco, NonNullablePrimitivesDto>.Clear();

            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName" };

            var dto = new NonNullablePrimitivesDto();

            TypeAdapter.Adapt(poco, dto);

            dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NonNullablePrimitivesDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
            dto.IsImport.ShouldBeFalse();
        }

        [TestMethod]
        public void Can_Map_From_Null_Source_To_Non_Nullable_Target()
        {
            TypeAdapterConfig<NullablePrimitivesPoco, NonNullablePrimitivesDto>.Clear();

            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName" };

            NonNullablePrimitivesDto dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NonNullablePrimitivesDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
            dto.IsImport.ShouldBeFalse();
        }

        [TestMethod]
        public void Can_Map_From_Nullable_Source_To_Nullable_Target()
        {
            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName" };

            NullablePrimitivesPoco2 dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NullablePrimitivesPoco2>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
            dto.IsImport.ShouldBeNull();
            dto.Amount.ShouldBeNull();
        }

        [TestMethod]
        public void Can_Map_From_Nullable_Source_To_Nullable_Existing_Target()
        {
            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName" };

            NullablePrimitivesPoco2 dto = new NullablePrimitivesPoco2
            {
                IsImport = true,
                Amount = 1,
            };

            TypeAdapter.Adapt(poco, dto);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
            dto.IsImport.ShouldBeNull();
            dto.Amount.ShouldBeNull();
        }

        [TestMethod]
        public void Can_Map_From_Nullable_Source_With_Values_To_Non_Nullable_Target()
        {
            TypeAdapterConfig<NullablePrimitivesPoco, NonNullablePrimitivesDto>.NewConfig()
                .Compile();
            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName", IsImport = true, Amount = 10};

            NonNullablePrimitivesDto dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NonNullablePrimitivesDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
            dto.IsImport.ShouldBeTrue();
            dto.Amount.ShouldBe(10);
        }

        [TestMethod]
        public void Can_Map_From_Nullable_Source_Without_Values_To_Non_Nullable_Target()
        {
            TypeAdapterConfig<NullablePrimitivesPoco, NonNullablePrimitivesDto>.NewConfig()
                .Compile();
            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName"};

            NonNullablePrimitivesDto dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NonNullablePrimitivesDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
            dto.IsImport.ShouldBeFalse();
            dto.Amount.ShouldBe(0);
        }

        [TestMethod]
        public void Can_Map_From_Nullable_Source_With_Values_To_Explicitly_Mapped_Non_Nullable_Target()
        {
            TypeAdapterConfig<NullablePrimitivesPoco, NonNullablePrimitivesDto>.NewConfig()
                .Map(dest => dest.Amount, src => src.Amount)
                .Map(dest => dest.IsImport, src => src.IsImport)
                .Map(dest => dest.MyFee, src => src.Fee)
                .Compile();

            var poco = new NullablePrimitivesPoco { Id = Guid.NewGuid(), Name = "TestName", Fee = 20, IsImport = true, Amount = 10};

            NonNullablePrimitivesDto dto = TypeAdapter.Adapt<NullablePrimitivesPoco, NonNullablePrimitivesDto>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
            dto.IsImport.ShouldBeTrue();
            dto.Amount.ShouldBe(10);
            dto.MyFee.ShouldBe(20);
        }

        [TestMethod]
        public void Can_Map_From_Non_Nullable_Source_To_Nullable_Target()
        {
            var dto = new NonNullablePrimitivesDto { Id = Guid.NewGuid(), Name = "TestName", IsImport = true};

            NullablePrimitivesPoco poco = TypeAdapter.Adapt<NonNullablePrimitivesDto, NullablePrimitivesPoco>(dto);

            poco.Id.ShouldBe(dto.Id);
            poco.Name.ShouldBe(dto.Name);
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