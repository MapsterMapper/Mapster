using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    #region TestMethod Objects

    public enum Departments
    {
        Finance = 0,
        IT = 1,
        Sales = 2
    }

    public enum EmployeeDepartments
    {
        Finance = 2,
        IT = 3,
        Sales = 4
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

    public class EmployeeWithEnum
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public EmployeeDepartments Department { get; set; }
    }

    public class EmployeeDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Departments Department { get; set; }
    }

    #endregion

    [TestClass]
    public class WhenMappingEnums
    {
        [TestMethod]
        public void Int_Is_Mapped_To_Enum()
        {
            TypeAdapterConfig<Employee, EmployeeDTO>
                .NewConfig();

            var employee = new Employee { Id = Guid.NewGuid(), Name = "Timuçin", Surname = "KIVANÇ", Department = (int)Departments.IT  };

            var dto = TypeAdapter.Adapt<Employee, EmployeeDTO>(employee);

            Assert.IsNotNull(dto);
          
            Assert.IsTrue(dto.Id == employee.Id &&
                dto.Name == employee.Name &&
                dto.Department == Departments.IT);
        }

        [TestMethod]
        public void String_Is_Mapped_To_Enum()
        {
            var employee = new EmployeeWithStringEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = Departments.IT.ToString() };

            var dto = TypeAdapter.Adapt<EmployeeWithStringEnum, EmployeeDTO>(employee);

            dto.ShouldNotBeNull();

            dto.Id.ShouldBe(employee.Id);
            dto.Name.ShouldBe(employee.Name);
            dto.Department.ShouldBe(Departments.IT);
        }

        [TestMethod]
        public void String_Is_Mapped_To_Nullable_Enum()
        {
            var department = Departments.IT.ToString();
            var value = TypeAdapter.Adapt<string, Departments?>(department);
            value.ShouldBe(Departments.IT);
        }

        [TestMethod]
        public void Null_String_Is_Mapped_To_Nullable_Enum()
        {
            string department = null;
            var value = TypeAdapter.Adapt<string, Departments?>(department);
            value.ShouldBeNull();
        }

        [TestMethod]
        public void Nullable_Enum_Is_Mapped_To_String()
        {
            var department = Departments.IT;
            var value = TypeAdapter.Adapt<Departments?, string>(department);
            value.ShouldBe(Departments.IT.ToString());
        }

        [TestMethod]
        public void Null_Nullable_Enum_Is_Mapped_To_String()
        {
            Departments? department = null;
            var value = TypeAdapter.Adapt<Departments?, string>(department);
            value.ShouldBeNull();
        }

        [TestMethod]
        public void Enum_Is_Mapped_To_Enum()
        {
            var employee = new EmployeeWithEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = EmployeeDepartments.IT };

            TypeAdapterConfig.GlobalSettings.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
            var dto = TypeAdapter.Adapt<EmployeeWithEnum, EmployeeDTO>(employee);

            dto.ShouldNotBeNull();

            dto.Id.ShouldBe(employee.Id);
            dto.Name.ShouldBe(employee.Name);
            dto.Department.ShouldBe(Departments.IT);
        }

        [TestMethod]
        public void Null_String_Is_Mapped_To_Enum_Default()
        {
            TypeAdapterConfig<EmployeeWithStringEnum, EmployeeDTO>
                .NewConfig();

            var employee = new EmployeeWithStringEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = null };

            var dto = TypeAdapter.Adapt<EmployeeWithStringEnum, EmployeeDTO>(employee);

            dto.ShouldNotBeNull();

            dto.Id.ShouldBe(employee.Id);
            dto.Name.ShouldBe(employee.Name);
            dto.Department.ShouldBe(Departments.Finance);
        }

        [TestMethod]
        public void Empty_String_Is_Mapped_To_Enum_Default()
        {
            TypeAdapterConfig<EmployeeWithStringEnum, EmployeeDTO>
                .NewConfig();

            var employee = new EmployeeWithStringEnum { Id = Guid.NewGuid(), Name = "Timuçin", Department = "" };

            var dto = TypeAdapter.Adapt<EmployeeWithStringEnum, EmployeeDTO>(employee);

            dto.ShouldNotBeNull();

            dto.Id.ShouldBe(employee.Id);
            dto.Name.ShouldBe(employee.Name);
            dto.Department.ShouldBe(Departments.Finance);

        }

        [TestMethod]
        public void Enum_Is_Mapped_To_String()
        {
            var employeeDto = new EmployeeDTO { Id = Guid.NewGuid(), Name = "Timuçin", Department = Departments.IT };

            var poco = TypeAdapter.Adapt<EmployeeDTO, EmployeeWithStringEnum>(employeeDto);

            poco.ShouldNotBeNull();

            poco.Id.ShouldBe(employeeDto.Id);
            poco.Name.ShouldBe(employeeDto.Name);
            poco.Department.ShouldBe(employeeDto.Department.ToString());
        }

        [TestMethod]
        public void Flag_Enum_Is_Supported()
        {
            Assert_Flag_Enum(0, "Zero");
            Assert_Flag_Enum(1, "1");
            Assert_Flag_Enum(2, "Two");
            Assert_Flag_Enum(3, "3");
            Assert_Flag_Enum(4, "Four");
            Assert_Flag_Enum(5, "5");
            Assert_Flag_Enum(6, "Six");
            Assert_Flag_Enum(7, "7");
            Assert_Flag_Enum(8, "Eight");
            Assert_Flag_Enum(9, "9");
            Assert_Flag_Enum(10, "Two, Eight");
        }

        private static void Assert_Flag_Enum(int value, string result)
        {
            var e = (Flags) value;
            var str = TypeAdapter.Adapt<Flags, string>(e);
            str.ShouldBe(result);
            var e2 = TypeAdapter.Adapt<string, Flags>(str);
            e2.ShouldBe(e);
        }

        [TestMethod, TestCategory("speed")]
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

        [TestMethod]
        public void MapEnumToClass()
        {
            TypeAdapterConfig<CustomerType, EnumViewModel>.NewConfig()
                .Map(dest => dest.Text, src => src.ToString())
                .Map(dest => dest.Value, src => (int)src);

            var e = CustomerType.Private;
            var result = e.Adapt<EnumViewModel>();
            result.Value.ShouldBe(1);
            result.Text.ShouldBe("Private");
        }

        [Flags]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        internal enum Flags
        {
            Zero = 0,
            Two = 2,
            Four = 4,
            Six = 6,
            Eight = 8,
        }

        public enum CustomerType
        {
            Company, Private
        }

        public class EnumViewModel
        {
            public int Value { get; set; }
            public string Text { get; set; }
        }
    }
}
