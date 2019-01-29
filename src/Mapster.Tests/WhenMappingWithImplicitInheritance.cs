using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Mapster.Models;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingWithImplicitInheritance
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestCleanup]
        public void Cleanup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
            TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = false;
        }

        [TestMethod]
        public void Base_Configuration_Applies_To_Derived_Class_If_No_Explicit_Configuration()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix")
                .Compile();

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBe(source.Name + "_Suffix");
        }

        [TestMethod]
        public void Base_Configuration_Map_Condition_Applies_To_Derived_Class()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix", src => src.Name == "SourceName")
                .Compile();
            TypeAdapterConfig<DerivedPoco, SimpleDto>.Clear();

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBe(source.Name + "_Suffix");

            var source2 = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName3"
            };

            dto = TypeAdapter.Adapt<SimpleDto>(source2);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBeNull();
        }

        [TestMethod]
        public void Base_Configuration_DestinationTransforms_Apply_To_Derived_Class()
        {
            var config = TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig();
            config.AddDestinationTransform((string x) => x.Trim());
            config.Compile();
            TypeAdapterConfig<DerivedPoco, SimpleDto>.Clear();

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName    "
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBe(source.Name.Trim());
        }

        [TestMethod]
        public void Ignores_Are_Derived_From_Base_Configurations()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Ignore(dest => dest.Name)
                .Compile();
            TypeAdapterConfig<DerivedPoco, SimpleDto>.Clear();

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBeNull();
        }

        //[TestMethod]
        //public void Base_Configuration_Doesnt_Apply_To_Derived_Class_If_Explicit_Configuration_Exists()
        //{

        //    TypeAdapterConfig<DerivedPoco, SimpleDto>.NewConfig().Compile();

        //    TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
        //        .Map(dest => dest.Name, src => src.Name + "_Suffix")
        //        .Compile();

        //    var source = new DerivedPoco
        //    {
        //        Id = new Guid(),
        //        Name = "SourceName"
        //    };

        //    var dto = TypeAdapter.Adapt<SimpleDto>(source);

        //    dto.Id.ShouldBe(source.Id);
        //    dto.Name.ShouldBe(source.Name);
        //}

        [TestMethod]
        public void Base_Configuration_Applies_To_Double_Derived_Class_If_No_Explicit_Configuration()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix")
                .Compile();
            TypeAdapterConfig<DoubleDerivedPoco, SimpleDto>.Clear();

            var source = new DoubleDerivedPoco()
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBe(source.Name + "_Suffix");
        }

        [TestMethod]
        public void Derived_Class_Stops_At_First_Valid_Base_Configuration()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix")
                .Compile();

            TypeAdapterConfig<DerivedPoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Derived")
                .Compile();
            TypeAdapterConfig<DoubleDerivedPoco, SimpleDto>.Clear();

            var source = new DoubleDerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<SimpleDto>(source);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBe(source.Name + "_Derived");
        }

        [TestMethod]
        public void Derived_Config_Shares_Base_Config_Properties()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreNullValues(true)
                .ShallowCopyForSameType(true)
                //.MaxDepth(5)
                .Compile();

            var tuple = new TypeTuple(typeof(DerivedPoco), typeof(SimpleDto));
            var derivedConfig = TypeAdapterConfig.GlobalSettings.GetMergedSettings(tuple, MapType.Map);

            derivedConfig.IgnoreNullValues.ShouldBe(true);
            derivedConfig.ShallowCopyForSameType.ShouldBe(true);
            //derivedConfig.MaxDepth.ShouldBe(5);
        }

        [TestMethod]
        public void Derived_Config_Shares_Base_Dest_Config_Properties()
        {
            TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = true;
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreNullValues(true)
                .ShallowCopyForSameType(true)
                //.MaxDepth(5)
                .Compile();

            var tuple = new TypeTuple(typeof(DerivedPoco), typeof(DerivedDto));
            var derivedConfig = TypeAdapterConfig.GlobalSettings.GetMergedSettings(tuple, MapType.Map);

            derivedConfig.IgnoreNullValues.ShouldBe(true);
            derivedConfig.ShallowCopyForSameType.ShouldBe(true);
            //derivedConfig.MaxDepth.ShouldBe(5);
        }

        [TestMethod]
        public void Derived_Config_Doesnt_Share_Base_Dest_Config_Properties_If_Disabled()
        {
            TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = false;
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreNullValues(true)
                .ShallowCopyForSameType(true)
                //.MaxDepth(5)
                .Compile();

            var tuple = new TypeTuple(typeof(DerivedPoco), typeof(DerivedDto));
            var derivedConfig = TypeAdapterConfig.GlobalSettings.GetMergedSettings(tuple, MapType.Map);

            derivedConfig.IgnoreNullValues.ShouldBeNull();
            derivedConfig.ShallowCopyForSameType.ShouldBeNull();
        }

        [TestMethod]
        public void Ignores_Are_Derived_From_Base_Dest_Configurations()
        {
            TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = true;
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Name, src => src.Name + "_Suffix")
                .Compile();

            var source = new DerivedPoco
            {
                Id = new Guid(),
                Name = "SourceName"
            };

            var dto = TypeAdapter.Adapt<DerivedDto>(source);

            dto.Id.ShouldBe(source.Id);
            dto.Name.ShouldBe(source.Name + "_Suffix");
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

        public class DoubleDerivedPoco : DerivedPoco
        {
        }

        public class DerivedDto : SimpleDto
        {
        }

        #endregion

    }

}