using System;
using Mapster.Tests.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenRequiresPropsValidation
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
        public void DestinationProps_Exist_In_Source()
        {
            var product = new Product {Id = Guid.NewGuid(), Title = "ProductA", CreatedUser = new User {Name = "UserA"}};

            var dto = product.ValidateAndAdapt<Product, ProductNestedDTO>();

            dto.ShouldNotBeNull();
            dto.Id.ShouldBe(product.Id);
        }
        
        [TestMethod]
        public void DestinationProps_Not_Exist_In_Source()
        {
            var product = new Product {Id = Guid.NewGuid(), Title = "ProductA", CreatedUser = new User {Name = "UserA"}};
            
            ProductDTO productDtoRef;
            var notExistingPropName = nameof(productDtoRef.CreatedUserName);

            var ex = Should.Throw<Exception>(() => product.ValidateAndAdapt<Product, ProductDTO>());
            
            ex.Message.ShouldContain(notExistingPropName);
            ex.Message.ShouldContain(nameof(Product));
        }
    }
}
