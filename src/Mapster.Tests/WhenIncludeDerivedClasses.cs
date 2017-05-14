using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenIncludeDerivedClasses
    {
        [TestMethod]
        public void Map_Including_Derived_Class()
        {
            TypeAdapterConfig<Vehicle, VehicleDto>.NewConfig()
                .Include<Car, CarDto>()
                .Compile();

            Vehicle vehicle = new Car { Id = 1, Name = "Car", Make = "Toyota", ChassiNumber = "XXX" };
            var dto = vehicle.Adapt<Vehicle, VehicleDto>();

            dto.ShouldBeOfType<CarDto>();
            ((CarDto)dto).Make.ShouldBe("Toyota");
        }

        #region test classes
        public abstract class Vehicle
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public class Car : Vehicle
        {
            public string Make { get; set; }
            public string ChassiNumber { get; set; }
        }

        public abstract class VehicleDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public class CarDto : VehicleDto
        {
            public string Make { get; set; }
            public string ChassiNumber { get; set; }
        }
        #endregion
    }
}
