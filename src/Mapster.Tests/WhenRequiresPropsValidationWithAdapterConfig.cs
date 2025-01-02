using System;
using Mapster.Tests.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenRequiresPropsValidationWithAdapterConfig
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.Exact);
        }
        
        [TestMethod]
        public void DestinationProps_Not_Exist_In_Source_But_Configured()
        {
            var product = new Product {Id = Guid.NewGuid(), Title = "ProductA", CreatedUser = new User {Name = "UserA"}};

            var adapterSettings = TypeAdapterConfig<Product, ProductDTO>.NewConfig()
                .Map(dest => dest.CreatedUserName, src => $"{src.CreatedUser.Name} {src.CreatedUser.Surname}");

            var dto = product.ValidateAndAdapt<Product, ProductDTO>(adapterSettings.Config);

            dto.ShouldNotBeNull();
            dto.CreatedUserName.ShouldBe($"{product.CreatedUser.Name} {product.CreatedUser.Surname}");
        }
        
        [TestMethod]
        public void DestinationProps_Not_Exist_In_Source_And_MisConfigured()
        {
            var product = new Product {Id = Guid.NewGuid(), Title = "ProductA", CreatedUser = new User {Name = "UserA"}};

            var adapterSettings = TypeAdapterConfig<Product, ProductDTO>.NewConfig();

            ProductDTO productDtoRef;
            var notExistingPropName = nameof(productDtoRef.CreatedUserName);

            var ex = Should.Throw<Exception>(() => product.ValidateAndAdapt<Product, ProductDTO>(adapterSettings.Config));
            
            ex.Message.ShouldContain(notExistingPropName);
            ex.Message.ShouldContain(nameof(Product));
        }
    }
}
