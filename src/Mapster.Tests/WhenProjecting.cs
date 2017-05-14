using System;
using System.Collections.Generic;
using System.Linq;
using Mapster.Tests.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenProjecting
    {
        [TestMethod]
        public void TestTypeConversion()
        {
            TypeTestClassA testA = new TypeTestClassA();
            testA.A = 5;
            testA.B = 2;
            testA.C = 4.5;

            var list = new List<TypeTestClassA>() { testA };

            var bList = list.AsQueryable().ProjectToType<TypeTestClassB>().ToList();

            Assert.IsNotNull(bList);

            Assert.IsTrue(bList.Count == 1);
            Assert.IsTrue(bList[0].A == 5);
            Assert.IsTrue(bList[0].B == 2);
            Assert.IsTrue(bList[0].C == 4.5m);
        }

        [TestMethod]
        public void TestProjectionConfiguration()
        {
            ConfigTestClassA testA = new ConfigTestClassA();
            testA.A = 5;
            testA.B = "2";
            testA.C = 4.5;

            var list = new List<ConfigTestClassA>() { testA };
            
            TypeAdapterConfig<ConfigTestClassA, ConfigTestClassB>
                .NewConfig()
                .Ignore(dest => dest.A)
                .Map(dest => dest.B, src => Convert.ToInt32(src.B))
                .Map(dest => dest.C, src => src.C.ToString());

            var bList = list.AsQueryable().ProjectToType<ConfigTestClassB>().ToList();

            Assert.IsNotNull(bList);

            Assert.IsTrue(bList.Count == 1);
            Assert.IsTrue(bList[0].A == null);
            Assert.IsTrue(bList[0].B == int.Parse(testA.B));
            Assert.IsTrue(bList[0].C == testA.C.ToString());
        }

        [TestMethod]
        public void TestPocoTypeMapping()
        {
            var products = new[]
            {
                new Product {Id = Guid.NewGuid(), Title = "ProductA", CreatedUser = new User {Name = "UserA"}, OrderLines = new List<OrderLine>()},
                new Product {Id = Guid.NewGuid(), Title = "ProductB", CreatedUser = null, OrderLines = new List<OrderLine>()},
            };

            var resultQuery = products.AsQueryable().ProjectToType<ProductDTO>();
            var expectedQuery = from Param_0 in products.AsQueryable()
                                select new ProductDTO
                                {
                                    Id = Param_0.Id,
                                    Title = Param_0.Title,
                                    CreatedUserName = Param_0.CreatedUser.Name,
                                    ModifiedUser = Param_0.ModifiedUser == null ? null : new UserDTO { Id = Param_0.ModifiedUser.Id, Email = Param_0.ModifiedUser.Email },
                                    OrderLines = (from Param_1 in Param_0.OrderLines
                                                  select new OrderLineListDTO
                                                  {
                                                      Id = Param_1.Id,
                                                      UnitPrice = Param_1.UnitPrice,
                                                      Amount = Param_1.Amount,
                                                      Discount = Param_1.Discount,
                                                  }).ToList()
                                };
            resultQuery.ToString().ShouldBe(expectedQuery.ToString());
        }
    }
}
