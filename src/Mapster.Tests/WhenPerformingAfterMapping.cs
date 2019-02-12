using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenPerformingAfterMapping
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestMethod]
        public void After_Mapping()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .AfterMapping((src, dest) => dest.Name += "xxx");

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };
            var result = TypeAdapter.Adapt<SimpleDto>(poco);

            result.Id.ShouldBe(poco.Id);
            result.Name.ShouldBe(poco.Name + "xxx");
        }

        [TestMethod]
        public void After_Mapping_With_DestinationType_Setting()
        {
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IValidatable>()
                .AfterMapping(dest => dest.Validate());

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };
            var result = TypeAdapter.Adapt<SimpleDto>(poco);

            result.IsValidated.ShouldBeTrue();
        }


        [TestMethod]
        public void No_Compile_Error_When_ConstructUsing_ForDestinationType()
        {
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IValidatable>()
                .ConstructUsing(() => new SimpleDto());
            TypeAdapterConfig.GlobalSettings.Compile();
        }


        public interface IValidatable
        {
            void Validate();
        }

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDto : IValidatable
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public bool IsValidated { get; private set; }

            public void Validate()
            {
                this.IsValidated = true;
            }
        }
    }
}
