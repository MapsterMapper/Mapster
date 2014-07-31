using System;
using NUnit.Framework;
using Should;

namespace Fpr.Tests
{
    [TestFixture]
    public class WhenMappingWithExplicitInheritance
    {
        [SetUp]
        public void Setup()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.Clear();
            TypeAdapterConfig<DerivedPoco, SimpleDto>.Clear();
            TypeAdapterConfig<DerivedPoco, DerivedDto>.Clear();
            TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = false;
        }


        [Test]
        public void Base_Configuration_Map_Condition_Applies_To_Derived_Class()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix", src => src.Name == "SourceName");

            TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
                .Inherits<SimplePoco, SimpleDto>();

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<DerivedDto>(source);

            dto.Id.ShouldEqual(source.Id);
            dto.Name.ShouldEqual(source.Name + "_Suffix");

            var source2 = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName3"
            };

            var dto2 = TypeAdapter.Adapt<DerivedDto>(source2);

            dto2.Id.ShouldEqual(source.Id);
            dto2.Name.ShouldBeNull();
        }

        [Test]
        public void Base_Configuration_DestinationTransforms_Apply_To_Derived_Class()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .DestinationTransforms.Upsert<string>(x => x.Trim());

            TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
                .Inherits<SimplePoco, SimpleDto>();

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName    "
            };

            var dto = TypeAdapter.Adapt<DerivedDto>(source);

            dto.Id.ShouldEqual(source.Id);
            dto.Name.ShouldEqual(source.Name.Trim());
        }

        [Test]
        public void Ignores_Are_Derived_From_Base_Configurations()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Ignore(dest => dest.Name);

            TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
               .Inherits<SimplePoco, SimpleDto>();

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<DerivedDto>(source);

            dto.Id.ShouldEqual(source.Id);
            dto.Name.ShouldBeNull();
        }


        [Test]
        public void Derived_Config_Shares_Base_Config_Properties()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreNullValues(true)
                .NewInstanceForSameType(true)
                .MaxDepth(5);

            TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
                .Inherits<SimplePoco, SimpleDto>();

            var derivedConfig = TypeAdapterConfig<DerivedPoco, DerivedDto>.ConfigSettings;

            derivedConfig.IgnoreNullValues.ShouldEqual(true);
            derivedConfig.NewInstanceForSameType.ShouldEqual(true);
            derivedConfig.MaxDepth.ShouldEqual(5);
        }


        [Test]
        public void Invalid_Source_Cast_Throws_Exception()
        {
            Assert.Throws<InvalidCastException>(() => TypeAdapterConfig<SimpleDto, DerivedDto>.NewConfig()
                .Inherits<SimplePoco, SimpleDto>());

        }

        [Test]
        public void Invalid_Destination_Cast_Throws_Exception()
        {
            Assert.Throws<InvalidCastException>(() => TypeAdapterConfig<DerivedPoco, SimplePoco>.NewConfig()
                .Inherits<SimplePoco, SimpleDto>());

        }

        #region Test Classes

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