using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Async.Tests
{
    [TestClass]
    public class AsyncTest
    {
        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            TypeAdapterConfig<Poco, Dto>.Clear();
        }

        [TestMethod]
        public async Task Async()
        {
            TypeAdapterConfig<Poco, Dto>.NewConfig()
                .AfterMappingAsync(async dest => { dest.Name = await GetName(); });

            var poco = new Poco {Id = "foo"};
            var dto = await poco.BuildAdapter().AdaptToTypeAsync<Dto>();
            dto.Name.ShouldBe("bar");
        }


        [TestMethod]
        public async Task AsyncError()
        {
            TypeAdapterConfig<Poco, Dto>.NewConfig()
                .AfterMappingAsync(async dest => { dest.Name = await GetNameError(); });

            var poco = new Poco {Id = "foo"};
            try
            {
                var dto = await poco.BuildAdapter().AdaptToTypeAsync<Dto>();
                Assert.Fail("should error");
            }
            catch (Exception ex)
            {
                ex.Message.ShouldBe("bar");
            }
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void Sync()
        {
            TypeAdapterConfig<Poco, Dto>.NewConfig()
                .AfterMappingAsync(async dest => { dest.Name = await GetName(); });

            var poco = new Poco {Id = "foo"};
            var dto = poco.Adapt<Dto>();
            dto.Name.ShouldBe("bar");
        }

        [TestMethod]
        public async Task NestedAsync()
        {
            TypeAdapterConfig<DoCar, DtoCar>.NewConfig();
            TypeAdapterConfig<DoOwner, DtoOwner>.NewConfig();
            TypeAdapterConfig<DoCarOwnership, DtoCarOwnership>.NewConfig()
                .Ignore(dest => dest.Car)
                .Ignore(dest => dest.Owner)
                .AfterMappingAsync(async (src, dest) =>
                {
                    dest.Owner = await GetOwner(src.Owner);
                })
                .AfterMappingAsync(async (src, dest) =>
                {
                    dest.Car = await GetCar(src.Car);
                });

            var dtoOwnership = await new DoCarOwnership()
            {
                Id = "1",
                Car = "1",
                Owner = "1"
            }
            .BuildAdapter()
            .AdaptToTypeAsync<DtoCarOwnership>();

            dtoOwnership.Car.ShouldNotBeNull();
            dtoOwnership.Car.Make.ShouldBe("Car Maker Inc");
            dtoOwnership.Owner.ShouldNotBeNull();
            dtoOwnership.Owner.Name.ShouldBe("John Doe");
        }

        private static async Task<string> GetName()
        {
            await Task.Delay(1);
            return "bar";
        }

        private static async Task<string> GetNameError()
        {
            await Task.Delay(1);
            throw new Exception("bar");
        }

        private static async Task<DtoCar> GetCar(string id)
        {
            await Task.Delay(1);
            return await new DoCar()
            {
                Id = id,
                Make = "Car Maker Inc",
                Model = "Generic",
            }.BuildAdapter().AdaptToTypeAsync<DtoCar>();
        }

        private static async Task<DtoOwner> GetOwner(string id)
        {
            await Task.Delay(1);
            return await new DoOwner()
            {
                Id = id,
                Name = "John Doe"
            }.BuildAdapter().AdaptToTypeAsync<DtoOwner>();
        }
    }

    public class Poco
    {
        public string Id { get; set; }
    }
    public class Dto
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class DtoCarOwnership
    {
        public string Id { get; set; }
        public DtoOwner Owner { get; set; }
        public DtoCar Car { get; set; }
    }

    public class DoCarOwnership
    {
        public string Id { get; set; }
        public string Owner { get; set; }
        public string Car { get; set; }
    }

    public class DtoOwner
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class DoOwner
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class DtoCar
    {
        public string Id { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
    }

    public class DoCar
    {
        public string Id { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
    }
}
