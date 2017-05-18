using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingDerived
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestMethod]
        public void WhenCompilingConfigDerivedWithoutMembers()
        {
            //Arrange
            var config = TypeAdapterConfig<Entity, DerivedDto>.NewConfig()
                                                           .ConstructUsing(entity => new DerivedDto(entity.Id))
                                                           .Ignore(domain => domain.Id)
                                                           ;

            //Act && Assert
            Should.NotThrow(() => config.Compile());
        }

        [TestMethod]
        public void WhenMappingDerivedWithoutMembers()
        {
            //Arrange
            var inputEntity = new Entity {Id = 2L};

            var config = TypeAdapterConfig<Entity, DerivedDto>.NewConfig()
                                                           .ConstructUsing(entity => new DerivedDto(entity.Id))
                                                           .Ignore(domain => domain.Id)
                                                           ;
            config.Compile();
            //Act
            var result = TypeAdapter.Adapt<Entity, DerivedDto>(inputEntity);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(inputEntity.Id, result.Id);
        }

        internal class BaseDto
        {
            public long Id { get; set; }

            protected BaseDto(long id)
            {
                Id = id;
            }
        }

        internal class Entity
        {
            public long Id { get; set; }
        }

        internal class DerivedDto : BaseDto
        {
            public DerivedDto(long id) : base(id) { }
        }
    }
}
