using System;
using Mapster.Tests.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    /// <summary>
    /// Not trying to test core testing here...just a few tests to make sure the extension method approach doesn't hose anything
    /// </summary>
    [TestClass]
    public class WhenMappingWithExtensionMethods
    {

        [TestMethod]
        public void Adapt_With_Source_And_Destination_Type_Succeeds()
        {
            TypeAdapterConfig<Product, ProductDTO>.NewConfig()
                .Compile();

            var product = new Product {Id = Guid.NewGuid(), Title = "ProductA", CreatedUser = new User {Name = "UserA"}};

            var dto = product.Adapt<Product, ProductDTO>();

            dto.ShouldNotBeNull();
            dto.Id.ShouldBe(product.Id);
        }

        [TestMethod]
        public void Adapt_With_Source_And_Destination_Types_And_Config_Succeeds()
        {
            var config = new TypeAdapterConfig();
            config.ForType<Product, ProductDTO>();


            var product = new Product {Id = Guid.NewGuid(), Title = "ProductA", CreatedUser = new User {Name = "UserA"}};

            var dto = product.Adapt<Product, ProductDTO>(config);

            dto.ShouldNotBeNull();
            dto.Id.ShouldBe(product.Id);
        }

        [TestMethod]
        public void Adapt_With_Destination_Type_Succeeds()
        {
            TypeAdapterConfig<Product, ProductDTO>.NewConfig()
                .Compile();

            var product = new Product {Id = Guid.NewGuid(), Title = "ProductA", CreatedUser = new User {Name = "UserA"}};

            var dto = product.Adapt<ProductDTO>();

            dto.ShouldNotBeNull();
            dto.Id.ShouldBe(product.Id);
        }

        [TestMethod]
        public void Adapt_With_Destination_Type_And_Config_Succeeds()
        {
            var config = new TypeAdapterConfig();
            config.ForType<Product, ProductDTO>();


            var product = new Product {Id = Guid.NewGuid(), Title = "ProductA", CreatedUser = new User {Name = "UserA"}};

            var dto = product.Adapt<ProductDTO>(config);

            dto.ShouldNotBeNull();
            dto.Id.ShouldBe(product.Id);
        }

        [TestMethod]
        public void Map_From_Null_Should_Be_Null()
        {
            Product product = null;

            var dto = product.Adapt<ProductDTO>();

            dto.ShouldBeNull();
        }

        [TestMethod]
        public void Map_From_Non_Public_Type()
        {
            var product = new { Id = Guid.NewGuid(), Title = "TestMethod" };

            var dto = product.Adapt<ProductDTO>();

            dto.Id.ShouldBe(product.Id);
            dto.Title.ShouldBe(product.Title);
        }
    }
}
