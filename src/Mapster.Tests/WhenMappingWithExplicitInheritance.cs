using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingWithExplicitInheritance
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.Clear();
            TypeAdapterConfig<DerivedPoco, SimpleDto>.Clear();
            TypeAdapterConfig<DerivedPoco, DerivedDto>.Clear();
            TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = false;
        }

        [TestMethod]
        public void Base_Configuration_Map_Condition_Applies_To_Derived_Class()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix", src => src.Name == "SourceName")
                .Compile();

            TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
                .Inherits<SimplePoco, SimpleDto>()
                .Compile();

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<DerivedDto>(source);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBe(source.Name + "_Suffix");

            var source2 = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName3"
            };

            var dto2 = TypeAdapter.Adapt<DerivedDto>(source2);

            dto2.Id.ShouldBe(source.Id);
            dto2.Name.ShouldBeNull();
        }

        [TestMethod]
        public void Base_Configuration_DestinationTransforms_Apply_To_Derived_Class()
        {
            var config = TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig();
            config.AddDestinationTransform((string x) => x.Trim());
            config.Compile();

            TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
                .Inherits<SimplePoco, SimpleDto>()
                .Compile();

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName    "
            };

            var dto = TypeAdapter.Adapt<DerivedDto>(source);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBe(source.Name.Trim());
        }

        [TestMethod]
        public void Ignores_Are_Derived_From_Base_Configurations()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Ignore(dest => dest.Name)
                .Compile();

            TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
               .Inherits<SimplePoco, SimpleDto>()
               .Compile();

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<DerivedDto>(source);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBeNull();
        }

        [TestMethod]
        public void Derived_Config_Shares_Base_Config_Properties()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreNullValues(true)
                .ShallowCopyForSameType(true)
                //.MaxDepth(5)
                .Compile();

            var derivedConfig = TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
                .Inherits<SimplePoco, SimpleDto>().Settings;

            derivedConfig.IgnoreNullValues.ShouldBe(true);
            derivedConfig.ShallowCopyForSameType.ShouldBe(true);
            //derivedConfig.MaxDepth.ShouldBe(5);
        }


        [TestMethod]
        public void Invalid_Source_Cast_Throws_Exception()
        {
            Should.Throw<InvalidCastException>(() => TypeAdapterConfig<SimpleDto, DerivedDto>.NewConfig()
                .Inherits<SimplePoco, SimpleDto>());

        }

        [TestMethod]
        public void Invalid_Destination_Cast_Throws_Exception()
        {
            Should.Throw<InvalidCastException>(() => TypeAdapterConfig<DerivedPoco, SimplePoco>.NewConfig()
                .Inherits<SimplePoco, SimpleDto>());

        }

        [TestMethod]
        public void InheritsLasyLoad__IsWork()
        {
            TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
               .Inherits<SimplePoco, SimpleDto>()
               .Compile();

            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Inherits<RootPoco, RootDto>()
                .Ignore(dest => dest.Name)
                .Compile();

            TypeAdapterConfig<RootPoco, RootDto>.NewConfig()
                .Map(dest => dest.NumberDto, src => 42)
                .Compile();

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<DerivedDto>(source);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBe("SourceName"); // Inherits Ignore not work
            dto.NumberDto.ShouldBe(0); // Inherits not work

            Setup(); // clean config

            TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
                .InheritsLazy<SimplePoco, SimpleDto>()
                .Compile();

            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .InheritsLazy<RootPoco, RootDto>()
                .Ignore(dest => dest.Name)
                .Compile();

            TypeAdapterConfig<RootPoco, RootDto>.NewConfig()
                .Map(dest => dest.NumberDto, src => 42)
                .Compile();

            dto = TypeAdapter.Adapt<DerivedDto>(source);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBeNull();  // InheritsLazy Ignore is work
            dto.NumberDto.ShouldBe(42); // InheritsLazy is work
        }


        #region TestMethod Classes

        public class RootPoco
        {
            public int Number { get; set; }
        }

        public class RootDto
        {
            public int NumberDto { get; set; }
        }


        public class SimplePoco: RootPoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDto : RootDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class DerivedPoco : SimplePoco
        {
        }


        public class DerivedDto : SimpleDto
        {
        }

        #endregion

    }

}