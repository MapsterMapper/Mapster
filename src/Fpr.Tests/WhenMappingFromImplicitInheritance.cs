using System;
using NUnit.Framework;
using Should;

namespace Fpr.Tests
{
    [TestFixture]
    public class WhenMappingFromImplicitInheritance
    {
        [SetUp]
        public void Setup()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.Clear();
            TypeAdapterConfig<DerivedPoco, SimpleDto>.Clear();
            TypeAdapterConfig<DoubleDerivedPoco, SimpleDto>.Clear();
            TypeAdapterConfig<DerivedPoco, DerivedDto>.Clear();
            TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = false;
        }

        [Test]
        public void Base_Configuration_Applies_To_Derived_Class_If_No_Explicit_Configuration()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix");

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldEqual(source.Id);
            dto.Name.ShouldEqual(source.Name + "_Suffix");
        }

        [Test]
        public void Base_Configuration_Map_Condition_Applies_To_Derived_Class()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix", src => src.Name == "SourceName");

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldEqual(source.Id);
            dto.Name.ShouldEqual(source.Name + "_Suffix");

            var source2 = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName3"
            };

            dto = TypeAdapter.Adapt<SimpleDto>(source2);

            dto.Id.ShouldEqual(source.Id);
            dto.Name.ShouldBeNull();
        }

        [Test]
        public void Base_Configuration_DestinationTransforms_Apply_To_Derived_Class()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .DestinationTransforms.Upsert<string>(x => x.Trim());

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName    "
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldEqual(source.Id);
            dto.Name.ShouldEqual(source.Name.Trim());
        }

        [Test]
        public void Ignores_Are_Derived_From_Base_Configurations()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Ignore(dest => dest.Name);

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldEqual(source.Id);
            dto.Name.ShouldBeNull();
        }

        [Test]
        public void Base_Configuration_Doesnt_Apply_To_Derived_Class_If_Explicit_Configuration_Exists()
        {

            TypeAdapterConfig<DerivedPoco, SimpleDto>.NewConfig();

            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix");

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldEqual(source.Id);
            dto.Name.ShouldEqual(source.Name);
        }

        [Test]
        public void Base_Configuration_Applies_To_Double_Derived_Class_If_No_Explicit_Configuration()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix");

            var source = new DoubleDerivedPoco()
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldEqual(source.Id);
            dto.Name.ShouldEqual(source.Name + "_Suffix");
        }

        [Test]
        public void Derived_Class_Stops_At_First_Valid_Base_Configuration()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix");

            TypeAdapterConfig<DerivedPoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Derived");

            var source = new DoubleDerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldEqual(source.Id);
            dto.Name.ShouldEqual(source.Name + "_Derived");
        }

        [Test]
        public void Derived_Config_Shares_Base_Config_Properties()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreNullValues(true)
                .NewInstanceForSameType(true)
                .MaxDepth(5);

            var derivedConfig = TypeAdapterConfig<DerivedPoco, SimpleDto>.ConfigSettings;

            derivedConfig.IgnoreNullValues.ShouldEqual(true);
            derivedConfig.NewInstanceForSameType.ShouldEqual(true);
            derivedConfig.MaxDepth.ShouldEqual(5);
        }

        [Test]
        public void Derived_Config_Shares_Base_Dest_Config_Properties()
        {
            TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = true;
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreNullValues(true)
                .NewInstanceForSameType(true)
                .MaxDepth(5);

            var derivedConfig = TypeAdapterConfig<DerivedPoco, DerivedDto>.ConfigSettings;

            derivedConfig.IgnoreNullValues.ShouldEqual(true);
            derivedConfig.NewInstanceForSameType.ShouldEqual(true);
            derivedConfig.MaxDepth.ShouldEqual(5);
        }

        [Test]
        public void Derived_Config_Doesnt_Share_Base_Dest_Config_Properties_If_Disabled()
        {
            TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = false;
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreNullValues(true)
                .NewInstanceForSameType(true)
                .MaxDepth(5);

            var derivedConfig = TypeAdapterConfig<DerivedPoco, DerivedDto>.ConfigSettings;

            derivedConfig.ShouldBeNull();
        }

        [Test]
        public void Ignores_Are_Derived_From_Base_Dest_Configurations()
        {
            TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = true;
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix");

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<DerivedDto>(source);

            dto.Id.ShouldEqual(source.Id);
            dto.Name.ShouldEqual(source.Name + "_Suffix");
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

        public class DoubleDerivedPoco : DerivedPoco
        {
        }

        public class DerivedDto : SimpleDto
        {
        }

        #endregion

    }

}