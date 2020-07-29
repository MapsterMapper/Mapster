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
}
