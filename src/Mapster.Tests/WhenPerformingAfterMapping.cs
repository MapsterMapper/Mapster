using System;
using NUnit.Framework;
using Shouldly;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenPerformingAfterMapping
    {
        [TearDown]
        public void TearDown()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [Test]
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

        [Test]
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
