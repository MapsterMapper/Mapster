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

        #region TestMethod Classes

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDto
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