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
            TypeAdapterConfig<SimplePocoBaseBase, SimpleDto>.NewConfig()
                .AfterMapping((src, dest) => dest.Name += "!!!");
            TypeAdapterConfig<SimplePocoBase, SimpleDto>.NewConfig()
                .AfterMapping((src, dest) => dest.Name += "***");
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .AfterMapping((src, dest) => dest.Name += "xxx");

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };
            var result = TypeAdapter.Adapt<SimpleDto>(poco);

            result.Id.ShouldBe(poco.Id);
            result.Name.ShouldBe(poco.Name + "!!!***xxx");
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

        [TestMethod]
        public void MapToType_Support_Destination_Parameter()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .AfterMapping((src, result, destination) => result.Name += $"{destination.Name}xxx");

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };

            // check expression is successfully compiled
            Assert.ThrowsException<NullReferenceException>(() => poco.Adapt<SimpleDto>());
        }

        [TestMethod]
        public void MapToTarget_Support_Destination_Parameter()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .ConstructUsing((simplePoco, dto) => new SimpleDto())
                .AfterMapping((src, result, destination) => result.Name += $"{destination.Name}xxx");

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };
            var oldDto = new SimpleDto { Name = "zzz", };
            var result = poco.Adapt(oldDto);

            result.ShouldNotBeSameAs(oldDto);
            result.Id.ShouldBe(poco.Id);
            result.Name.ShouldBe(poco.Name + "zzzxxx");
        }

        public interface IValidatable
        {
            void Validate();
        }

        public class SimplePocoBaseBase
        {
            public string Name { get; set; }
        }
        public class SimplePocoBase : SimplePocoBaseBase
        {
        }

        public class SimplePoco : SimplePocoBase
        {
            public Guid Id { get; set; }
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
