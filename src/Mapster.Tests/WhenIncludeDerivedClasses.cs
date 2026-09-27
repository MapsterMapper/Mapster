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

        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/801
        /// </summary>
        [TestMethod]
        public void CompileProjection_Including_Derived_Class()
        {
            TypeAdapterConfig<PocoA801, DtoA801>.NewConfig()
                .Include<PocoDerived801, DtoDerived801>()
                .CompileProjection();
        }

        [TestMethod]
        public void TypesWithIncludeSettingsIgnoreNullValueCorrect()
        {
            TypeAdapterConfig<Vehicle, VehicleDto>.NewConfig()
                .Include<Car, CarDto>()
                .AfterMapping((src, dest) => dest.Name += "xxx")
                .Compile();

            TypeAdapterConfig<VehicleInsaider, VehicleDtoInsaider>.NewConfig()
                 .IgnoreNullValues(true)
                 .Compile();

            var vehicleNull = new VehicleInsaider();
            var vehicleDto = new VehicleDtoInsaider() { Vehicle = new CarDto { Id = 1, Name = "Car", Make = "Toyota", ChassiNumber = "XXX" } };


            var update = vehicleNull.Adapt(vehicleDto);

            update.Vehicle.ShouldBeOfType<CarDto>();
            ((CarDto)update.Vehicle).Make.ShouldBe("Toyota");
        }


        public class VehicleInsaider
        {
            public Vehicle Vehicle { get; set; }
        }

        public class VehicleDtoInsaider
        {
            public VehicleDto Vehicle { get; set; }
        }

        public abstract class PocoA801
        {
            public int Id { get; set; }
        }

        public class PocoDerived801 : PocoA801
        {
            public int DerivedVal { get; set; }
        }

        public abstract class DtoA801
        {
            public int Id { get; set; }
        }

        public class DtoDerived801 : DtoA801
        {
            public int DerivedVal { get; set; }
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
