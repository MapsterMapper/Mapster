using System;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.DependencyInjection.Tests
{
    [TestClass]
    public class InjectionTest
    {
        [TestMethod]
        public void Injection()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<Poco, Dto>()
                .Map(dto => dto.Name, _ => MapContext.Current.GetService<IMockService>().GetName());

            IServiceCollection sc = new ServiceCollection();
            sc.AddScoped<IMockService, MockService>();
            sc.AddSingleton(config);
            sc.AddScoped<IMapper, ServiceMapper>();

            var sp = sc.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var mapper = scope.ServiceProvider.GetService<IMapper>();
                var poco = new Poco { Id = "bar" };
                var dto = mapper.Map<Poco, Dto>(poco);
                dto.Name.ShouldBe("foo");
            }
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void NoServiceAdapter_InjectionError()
        {
            var expectedValue = MapContext.Current.GetService<IMockService>().GetName();
            var config = ConfigureMapping(expectedValue);

            IServiceCollection sc = new ServiceCollection();
            sc.AddScoped<IMockService, MockService>();
            sc.AddSingleton(config);
            // We should use ServiceMapper in normal code
            // but for this test we want to be sure the code will generate the InvalidOperationException
            sc.AddScoped<IMapper, Mapper>();

            var sp = sc.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var mapper = scope.ServiceProvider.GetService<IMapper>();
            MapToDto(mapper, expectedValue);
        }

        private static TypeAdapterConfig ConfigureMapping(string expectedValue)
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<Poco, Dto>()
                .Map(dto => dto.Name, _ => expectedValue);
            return config;
        }

        private static void MapToDto(IMapper mapper, string expectedValue)
        {
            var poco = new Poco { Id = "bar" };
            var dto = mapper.Map<Poco, Dto>(poco);
            dto.Name.ShouldBe(expectedValue);
        }

        [TestMethod]
        public void GetMapperInstanceFromServiceCollection_ShouldNotFaceException()
        {
            const string expectedValue = "foobar";
            var config = ConfigureMapping(expectedValue);
            ServiceCollection serviceCollection = new();
            serviceCollection.AddSingleton(config);
            serviceCollection.AddMapster();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            MapToDto(mapper, expectedValue);
        }
    }

    public interface IMockService
    {
        string GetName();
    }

    public class MockService : IMockService
    {
        public string GetName()
        {
            return "foo";
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