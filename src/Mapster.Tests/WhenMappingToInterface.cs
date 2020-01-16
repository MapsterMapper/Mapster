using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingToInterface
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestMethod]
        public void MapToInterface()
        {
            var dto = new Dto
            {
                Id = 1,
                Name = "Test",
                UnmappedSource = "Lorem ipsum"
            };

            IDto idto = dto.Adapt<IDto>();

            idto.ShouldNotBeNull();
            idto.Id.ShouldBe(dto.Id);
            idto.Name.ShouldBe(dto.Name);
            idto.UnmappedTarget.ShouldBeNull();
        }

        [TestMethod]
        public void MapToInheritedInterface()
        {
            var dto = new InheritedDto
            {
                Id = 1,
                Name = "Test",
                DateOfBirth = new DateTime(1978, 12, 10),
                UnmappedSource = "Lorem ipsum"
            };

            IInheritedDto idto = dto.Adapt<IInheritedDto>();

            idto.ShouldNotBeNull();
            idto.Id.ShouldBe(dto.Id);
            idto.Name.ShouldBe(dto.Name);
            idto.DateOfBirth.ShouldBe(dto.DateOfBirth);
            idto.UnmappedTarget.ShouldBeNull();
        }

        [TestMethod]
        public void MapToInterfaceWithMethods()
        {
            var dto = new Dto
            {
                Id = 1,
                Name = "Test",
                UnmappedSource = "Lorem ipsum"
            };

            IInterfaceWithMethods idto = dto.Adapt<IInterfaceWithMethods>();

            idto.ShouldNotBeNull();
            idto.Id.ShouldBe(dto.Id);
            idto.Name.ShouldBe(dto.Name);
            Should.Throw<NotImplementedException>(() => idto.DoSomething());
        }

        [TestMethod]
        public void MapToNotVisibleInterfaceThrows()
        {
            var dto = new Dto
            {
                Id = 1,
                Name = "Test",
                UnmappedSource = "Lorem ipsum"
            };

            var ex = Should.Throw<CompileException>(() => dto.Adapt<INotVisibleInterface>());
            ex.InnerException.ShouldBeOfType<InvalidOperationException>();
            ex.InnerException.Message.ShouldContain("not accessible", "Correct InvalidOperationException must be thrown.");
        }

        private interface INotVisibleInterface
        {
            int Id { get; set; }
            string Name { get; set; }
        }

        public interface IInterfaceWithMethods
        {
            int Id { get; set; }
            string Name { get; set; }
            void DoSomething();
        }

        public interface IDto
        {
            int Id { get; set; }
            string Name { get; set; }
            string UnmappedTarget { get; set; }
        }

        public interface IInheritedDto : IDto
        {
            DateTime DateOfBirth { get; set; }
        }

        public class Dto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string UnmappedSource { get; set; }
        }

        public class InheritedDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime DateOfBirth { get; set; }
            public string UnmappedSource { get; set; }
        }
    }
}
