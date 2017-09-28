using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

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

        [TestMethod]
        public void Map_Including_Derived_Class_With_List()
        {
            TypeAdapterConfig<Vehicle, VehicleDto>.NewConfig()
                .Include<Car, CarDto>()
                .Include<Bike, BikeDto>()
                .Compile();

            var vehicles = new List<Vehicle>
            {
                new Car {Id = 1, Name = "Car", Make = "Toyota", ChassiNumber = "XXX"},
                new Bike {Id = 2, Name = "Bike", Brand = "BMX"},
            };
            var dto = vehicles.Adapt<List<Vehicle>, IList<VehicleDto>>();

            ((CarDto)dto[0]).Make.ShouldBe("Toyota");
            ((BikeDto)dto[1]).Brand.ShouldBe("BMX");
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
        public class Bike : Vehicle
        {
            public string Brand { get; set; }
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
        public class BikeDto : VehicleDto
        {
            public string Brand { get; set; }
        }
        #endregion
    }
}
