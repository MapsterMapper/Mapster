using System;
using System.Diagnostics;
using Mapster.Adapters;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    #region Test Objects

    public enum Departments
    {
        Finance = 0,
        IT = 1,
        Sales = 2
    }

    public class Employee
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public int Department { get; set; }
    }

    public class EmployeeWithStringEnum
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
    }

    public class EmployeeDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Departments Department { get; set; }
    }

    #endregion

    [TestFixture]
    public class WhenMappingEnums
    {
        [Test]
        public void Int_Is_Mapped_To_Enum()
        {
            TypeAdapterConfig<Employee, EmployeeDTO>
                .NewConfig();

            var employee = new Employee { Id = Guid.NewGuid(), Name = "Timuçin", Surname = "KIVANÇ", Department = (int)Departments.IT  };

            var dto = TypeAdapter<Employee, EmployeeDTO>.Adapt(employee);

            Assert.IsNotNull(dto);
          
            Assert.IsTrue(dto.Id == employee.Id &&
                dto.Name == employee.Name &&
                dto.Department == Departments.IT);
        }

        [Test]
        public void String_Is_Mapped_To_Enum()
        {
            var employee = new EmployeeWithStringEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = Departments.IT.ToString() };

            var dto = TypeAdapter<EmployeeWithStringEnum, EmployeeDTO>.Adapt(employee);

            dto.ShouldNotBeNull();

            dto.Id.ShouldEqual(employee.Id);
            dto.Name.ShouldEqual(employee.Name);
            dto.Department.ShouldEqual(Departments.IT);
        }

        [Test]
        public void Null_String_Is_Mapped_To_Enum_Default()
        {
            TypeAdapterConfig<EmployeeWithStringEnum, EmployeeDTO>
                .NewConfig();

            var employee = new EmployeeWithStringEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = null };

            var dto = TypeAdapter.Adapt<EmployeeWithStringEnum, EmployeeDTO>(employee);

            dto.ShouldNotBeNull();

            dto.Id.ShouldEqual(employee.Id);
            dto.Name.ShouldEqual(employee.Name);
            dto.Department.ShouldEqual(Departments.Finance);
        }

        [Test]
        public void Empty_String_Is_Mapped_To_Enum_Default()
        {
            TypeAdapterConfig<EmployeeWithStringEnum, EmployeeDTO>
                .NewConfig();

            var employee = new EmployeeWithStringEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = "" };

            var dto = TypeAdapter.Adapt<EmployeeWithStringEnum, EmployeeDTO>(employee);

            dto.ShouldNotBeNull();

            dto.Id.ShouldEqual(employee.Id);
            dto.Name.ShouldEqual(employee.Name);
            dto.Department.ShouldEqual(Departments.Finance);

        }

        [Test]
        public void Enum_Is_Mapped_To_String()
        {
            var employeeDto = new EmployeeDTO { Id = Guid.NewGuid(), Name = "Timuçin", Department = Departments.IT };

            var poco = TypeAdapter.Adapt<EmployeeDTO, EmployeeWithStringEnum>(employeeDto);

            poco.ShouldNotBeNull();

            poco.Id.ShouldEqual(employeeDto.Id);
            poco.Name.ShouldEqual(employeeDto.Name);
            poco.Department.ShouldEqual(employeeDto.Department.ToString());
        }

        [Test, Explicit]
        public void MapEnumToStringSpeedTest()
        {
            TypeAdapterConfig<EmployeeDTO, EmployeeWithStringEnum>
                .NewConfig();
                //.Map(dest => dest.Department, src => src.Department.ToFastString());

            var employeeDto = new EmployeeDTO { Id = Guid.NewGuid(), Name = "Timuçin", Department = Departments.IT };

            var timer = Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++)
            {
                var poco = TypeAdapter.Adapt<EmployeeDTO, EmployeeWithStringEnum>(employeeDto);
            }
            timer.Stop();
            Console.WriteLine("Enum to string Elapsed time ms: " + timer.ElapsedMilliseconds);
        }

    }
}
