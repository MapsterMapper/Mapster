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
                var poco = new Poco {Id = "bar"};
                var dto = mapper.Map<Poco, Dto>(poco);
                dto.Name.ShouldBe("foo");
            }
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void NoServiceAdapter_InjectionError()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<Poco, Dto>()
                .Map(dto => dto.Name, _ => MapContext.Current.GetService<IMockService>().GetName());

            IServiceCollection sc = new ServiceCollection();
            sc.AddScoped<IMockService, MockService>();
            sc.AddSingleton(config);
            sc.AddScoped<IMapper, Mapper>();

            var sp = sc.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var mapper = scope.ServiceProvider.GetService<IMapper>();
                var poco = new Poco {Id = "bar"};
                var dto = mapper.Map<Poco, Dto>(poco);
                dto.Name.ShouldBe("foo");
            }
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
