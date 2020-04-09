using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

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
            idto.ShouldSatisfyAllConditions(
                () => idto.Id.ShouldBe(dto.Id),
                () => idto.Name.ShouldBe(dto.Name),
                () => idto.UnmappedTarget.ShouldBeNull()
            );
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

            var config = new TypeAdapterConfig();

            IInheritedDto idto = dto.Adapt<IInheritedDto>(config);

            idto.ShouldNotBeNull();
            idto.ShouldSatisfyAllConditions(
                () => idto.Id.ShouldBe(dto.Id),
                () => idto.Name.ShouldBe(dto.Name),
                () => idto.DateOfBirth.ShouldBe(dto.DateOfBirth),
                () => idto.UnmappedTarget.ShouldBeNull()
            );
        }

        [TestMethod]
        public void MapToInstanceWithInterface()
        {
            var dto = new InheritedDto
            {
                Id = 1,
                Name = "Test",
                DateOfBirth = new DateTime(1978, 12, 10),
                UnmappedSource = "Lorem ipsum"
            };

            var config = new TypeAdapterConfig();

            IInheritedDto target = new ImplementedDto();
            dto.Adapt(target, config);

            target.ShouldNotBeNull();
            target.ShouldSatisfyAllConditions(
                () => target.Id.ShouldBe(dto.Id),
                () => target.Name.ShouldBe(dto.Name),
                () => target.DateOfBirth.ShouldBe(dto.DateOfBirth),
                () => target.UnmappedTarget.ShouldBeNull()
            );
        }

        [TestMethod]
        public void MapToReadOnlyInterface()
        {
            var dto = new Dto
            {
                Id = 1,
                Name = "Test",
                UnmappedSource = "Lorem ipsum"
            };

            var idto = dto.Adapt<IReadOnlyInterface>();

            idto.ShouldNotBeNull();
            idto.ShouldSatisfyAllConditions(
                () => idto.Id.ShouldBe(dto.Id),
                () => idto.Name.ShouldBe(dto.Name)
            );
        }


        [TestMethod]
        public void MapToComplexInterface()
        {
            var subItem = new ComplexDto
            {
                Name = "Inner lrem ipsum",
                Int32 = 420,
                Int64 = long.MaxValue,
                NullInt1 = null,
                NullInt2 = 240,
                Floatn = 2.2F,
                Doublen = 4.4,
                DateTime = new DateTime(1978, 12, 10),
                SubItem = null,
                Dtos = new List<ComplexDto>(),
                DtoArr = null,
                Ints = new List<int>(),
                IntArr = null
            };

            var dto = new ComplexDto
            {
                Name = "Lorem ipsum",
                Int32 = 42,
                Int64 = long.MaxValue,
                NullInt1 = null,
                NullInt2 = 24,
                Floatn = 1.2F,
                Doublen = 2.4,
                DateTime = new DateTime(1978, 12, 10),
                SubItem = subItem,
                Dtos = new List<ComplexDto>(new[] { subItem, null }),
                DtoArr = new ComplexDto[] { null, subItem },
                Ints = new List<int>(new[] { 1, 2 }),
                IntArr = new[] { 3, 4 }
            };

            IComplexInterface idto = dto.Adapt<IComplexInterface>();

            idto.ShouldNotBeNull();
            idto.ShouldSatisfyAllConditions(
                () => idto.Name.ShouldBe(dto.Name),
                () => idto.Int32.ShouldBe(dto.Int32),
                () => idto.Int64.ShouldBe(dto.Int64),
                () => idto.NullInt1.ShouldBeNull(),
                () => idto.NullInt2.ShouldBe(dto.NullInt2),
                () => idto.Floatn.ShouldBe(dto.Floatn),
                () => idto.Doublen.ShouldBe(dto.Doublen),
                () => idto.DateTime.ShouldBe(dto.DateTime)
            );
            idto.SubItem.ShouldSatisfyAllConditions(
                () => idto.SubItem.Name.ShouldBe(dto.SubItem.Name),
                () => idto.SubItem.Int32.ShouldBe(dto.SubItem.Int32),
                () => idto.SubItem.Int64.ShouldBe(dto.SubItem.Int64),
                () => idto.SubItem.NullInt1.ShouldBeNull(),
                () => idto.SubItem.NullInt2.ShouldBe(dto.SubItem.NullInt2),
                () => idto.SubItem.Floatn.ShouldBe(dto.SubItem.Floatn),
                () => idto.SubItem.Doublen.ShouldBe(dto.SubItem.Doublen),
                () => idto.SubItem.DateTime.ShouldBe(dto.SubItem.DateTime)
            );
            idto.ShouldSatisfyAllConditions(
                () => idto.Dtos.Count().ShouldBe(dto.Dtos.Count()),
                () => idto.DtoArr.Length.ShouldBe(dto.DtoArr.Length),
                () => idto.Ints.First().ShouldBe(dto.Ints.First()),
                () => idto.Ints.Last().ShouldBe(dto.Ints.Last()),
                () => idto.IntArr[0].ShouldBe(dto.IntArr[0]),
                () => idto.IntArr[1].ShouldBe(dto.IntArr[1])
            );
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
            idto.ShouldSatisfyAllConditions(
                () => idto.Id.ShouldBe(dto.Id),
                () => idto.Name.ShouldBe(dto.Name),
                () => Should.Throw<NotImplementedException>(() => idto.DoSomething())
            );
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
        
        [TestMethod]
        public void MapToInheritedInterfaceWithoutProperties()
        {            
            var config = TypeAdapterConfig.GlobalSettings;
            TypeAdapterConfig<IInheritedDtoWithoutProperties, InheritedDto>.NewConfig()
                .Map(d => d.Id, s => s.Id)
                .Map(d => d.Name, s => s.Name)
                .IgnoreNonMapped(true);

            config.Compile();

            /// doesn't reach this point
            var dto = new ImplementedDto
            {
                Id = 1,
                Name = "Test",
                DateOfBirth = new DateTime(1978, 12, 10),
            } as IInheritedDtoWithoutProperties;

            var idto = dto.Adapt<InheritedDto>(config);
            idto.Id.ShouldBe(dto.Id);
            idto.Name.ShouldBe(dto.Name);
            idto.DateOfBirth.ShouldBe(default);
            idto.UnmappedSource.ShouldBeNull();
        }

        public interface IInheritedDtoWithoutProperties : IInheritedDto
        {
        }
        private interface INotVisibleInterface
        {
            int Id { get; set; }
            string Name { get; set; }
        }

        public interface IReadOnlyInterface
        {
            int Id { get; }
            string Name { get; }
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

        public class ImplementedDto : IInheritedDtoWithoutProperties
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime DateOfBirth { get; set; }
            public string UnmappedTarget { get; set; }
        }

        public interface IComplexInterface
        {
            string Name { get; set; }
            int Int32 { get; set; }
            long Int64 { get; set; }
            int? NullInt1 { get; set; }
            int? NullInt2 { get; set; }
            float Floatn { get; set; }
            double Doublen { get; set; }
            DateTime DateTime { get; set; }
            ComplexDto SubItem { get; set; }
            IEnumerable<ComplexDto> Dtos { get; set; }
            ComplexDto[] DtoArr { get; set; }
            IEnumerable<int> Ints { get; set; }
            int[] IntArr { get; set; }
        }

        public class ComplexDto
        {
            public string Name { get; set; }
            public int Int32 { get; set; }
            public long Int64 { set; get; }
            public int? NullInt1 { get; set; }
            public int? NullInt2 { get; set; }
            public float Floatn { get; set; }
            public double Doublen { get; set; }
            public DateTime DateTime { get; set; }
            public ComplexDto SubItem { get; set; }
            public IEnumerable<ComplexDto> Dtos { get; set; }
            public ComplexDto[] DtoArr { get; set; }
            public IEnumerable<int> Ints { get; set; }
            public int[] IntArr { get; set; }
        }
    }
}
