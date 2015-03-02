using System;
using System.Collections.Generic;
using System.Linq;
using Mapster.Tests.Classes;
using NUnit.Framework;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenProjecting
    {
      
        [Test]
        public void TestPrimitiveAndFlatteningProjection()
        {
            var products = GetProducts();

            var productDtos = products.AsQueryable().Project().To<ProductListDTO>().ToList();

            Assert.IsNotNull(productDtos);
            Assert.IsTrue(productDtos.Count == 2);

            var iPhone = productDtos.First();
            Assert.IsTrue(iPhone.Title == "iPhone 5S" &&
                iPhone.Description == "New generation smart phone" &&
                iPhone.UnitPrice == 650 &&
                iPhone.AmountInStock == 12000 &&
                iPhone.CreatedDate > DateTime.Now.AddMonths(-4) &&
                iPhone.ModifiedDate > DateTime.Now.AddDays(-3) &&
                iPhone.CreatedUserName == "Timuçin");

            Assert.IsNull(iPhone.ModifiedUserName);
        }

        [Test]
        public void TestNestedProjection()
        {
            var products = GetProducts();
            var productDtos = products.AsQueryable().Project().To<ProductNestedDTO>().ToList();

            Assert.IsNotNull(productDtos);
            Assert.IsTrue(productDtos.Count == 2);

            var galaxy = productDtos.Last();

            Assert.IsTrue(galaxy.Title == "Samsung Galaxy S5");
            Assert.IsTrue(galaxy.CreatedUser.Id != Guid.Empty && galaxy.CreatedUser.Email == "timucinkivanc@hotmail.com");

            Assert.IsNull(galaxy.ModifiedUser);
        }

        [Test]
        public void TestCollectionProjection()
        {
            var products = GetProducts();
            /*
            // Native Select
            var productCollectionDTO = products.AsQueryable()
                .Select(product => new ProductCollectionDTO
                {
                    Id = product.Id,
                    Title = product.Title,
                    OrderLines = product.OrderLines.Select(orderLine => new OrderLineDTO
                    {
                        Id = orderLine.Id,
                        Amount = orderLine.Amount,
                        Discount = orderLine.Discount,
                        UnitPrice = orderLine.UnitPrice,
                        Order = orderLine.Order == null ? (OrderDTO)null : new OrderDTO
                        {
                            Id = orderLine.Order.Id,
                            DeliveryDate = orderLine.Order.DeliveryDate,
                            IsDelivered = orderLine.Order.IsDelivered,
                            OrderDate = orderLine.Order.OrderDate,
                            OrderLines = orderLine.Order.OrderLines.Select(orderOrderLine => new OrderLineListDTO
                            {
                                Amount = orderOrderLine.Amount,
                                Discount = orderOrderLine.Discount,
                                Id = orderOrderLine.Id,
                                UnitPrice = orderOrderLine.UnitPrice
                            })
                        }
                    }),
                    CreatedUser = product.CreatedUser == null ? (UserDTO)null : new UserDTO
                    {
                        Id = product.CreatedUser.Id,
                        Email = product.CreatedUser.Email,
                    },
                    ModifiedUser = product.ModifiedUser == null ? (UserDTO)null : new UserDTO
                    {
                        Id = product.ModifiedUser.Id,
                        Email = product.ModifiedUser.Email
                    },
                    CreatedUserEmail = product.CreatedUser == null ? (string)null : product.CreatedUser.Email
                })
                .ToList();
            */

            var productDtos = products.AsQueryable().Project().To<ProductCollectionDTO>().ToList();

            Assert.IsNotNull(productDtos);
            Assert.IsTrue(productDtos.Count == 2);

            var galaxy = productDtos.Last();

            Assert.IsTrue(galaxy.Id != Guid.Empty && galaxy.Title == "Samsung Galaxy S5");
            Assert.IsNotNull(galaxy.OrderLines);
            Assert.IsTrue(galaxy.OrderLines.Count() == 1);

            var ordLine = galaxy.OrderLines.First();

            Assert.IsTrue(ordLine.Id != Guid.Empty && ordLine.UnitPrice == 585 && ordLine.Discount == 10 && ordLine.Amount == 1);

            var order = ordLine.Order;

            Assert.IsNotNull(order);

            Assert.IsTrue(order.Id != Guid.Empty && order.IsDelivered && order.OrderDate > DateTime.Now.AddDays(-3) && order.DeliveryDate > DateTime.Now.AddDays(-1));

            var orderOrderLines = order.OrderLines;

            Assert.IsNotNull(orderOrderLines);
            Assert.IsTrue(orderOrderLines.Count() == 2);

            var ordOrderLine = orderOrderLines.First();

            Assert.IsTrue(ordOrderLine.UnitPrice == 585 && ordOrderLine.Amount == 1 && ordOrderLine.Discount == 10);

            Assert.IsNotNull(galaxy.CreatedUser);
            Assert.IsTrue(galaxy.CreatedUser.Email == "timucinkivanc@hotmail.com");

            Assert.IsNull(galaxy.ModifiedUser);

            Assert.IsTrue(galaxy.CreatedUserEmail == "timucinkivanc@hotmail.com");
        }

        [Test]
        public void TestTypeConversion()
        {
            TypeTestClassA testA = new TypeTestClassA();
            testA.A = 5;
            testA.B = 2;
            testA.C = 4.5;

            var list = new List<TypeTestClassA>() { testA };

            var bList = list.AsQueryable().Project().To<TypeTestClassB>().ToList();

            Assert.IsNotNull(bList);

            Assert.IsTrue(bList.Count == 1);
            Assert.IsTrue(bList[0].A == 5);
            Assert.IsTrue(bList[0].B == 2);
            Assert.IsTrue(bList[0].C == 4.5m);
        }

        [Test]
        public void TestProjectionConfiguration()
        {
            ConfigTestClassA testA = new ConfigTestClassA();
            testA.A = 5;
            testA.B = "2";
            testA.C = 4.5;

            var list = new List<ConfigTestClassA>() { testA };
            
            TypeAdapterConfig<ConfigTestClassA, ConfigTestClassB>
                .NewConfig()
                .IgnoreMember(dest => dest.A)
                .Map(dest => dest.B, src => Convert.ToInt32(src.B))
                .Map(dest => dest.C, src => src.C.ToString());

            var bList = list.AsQueryable().Project().To<ConfigTestClassB>().ToList();

            Assert.IsNotNull(bList);

            Assert.IsTrue(bList.Count == 1);
            Assert.IsTrue(bList[0].A == null);
            Assert.IsTrue(bList[0].B == int.Parse(testA.B));
            Assert.IsTrue(bList[0].C == testA.C.ToString());
        }

        [Test]
        public void TestMaxDepth()
        {
            MaxDepthTestSource obj = new MaxDepthTestSource();
            obj.Name = "111";
            obj.Source = new MaxDepthTestDest()
            {
                Name = "222",
                Source = new MaxDepthTestSource()
                {
                    Name = "333",
                    Source = new MaxDepthTestDest()
                    {
                        Name = "444",
                        Source = new MaxDepthTestSource() { Name = "555" }
                    }
                }
            };

            var list = new List<MaxDepthTestSource>() { obj };

            TypeAdapterConfig<MaxDepthTestSource, MaxDepthTestSourceDTO>
                .NewConfig()
                .MaxDepth(2);

            var bList = list.AsQueryable().Project().To<MaxDepthTestSourceDTO>().ToList();

            Assert.IsNotNull(bList);

            Assert.IsTrue(bList.Count == 1);
            Assert.IsTrue(bList[0].Name == "111");
            Assert.IsTrue(bList[0].Source.Name == "222");
            Assert.IsTrue(bList[0].Source.Source.Name == null);
            Assert.IsTrue(bList[0].Source.Source.Source == null);
        }

        [Test]
        public void TestMaxDepthListProperty()
        {
            MaxDepthTestListSource obj = new MaxDepthTestListSource();
            obj.Name = "111";
            obj.Source = new List<MaxDepthTestListDest>() { new MaxDepthTestListDest()
            {
                Name = "222",
                Source = new List<MaxDepthTestListSource> { new MaxDepthTestListSource()
                    {
                        Name = "333",
                        Source = new List<MaxDepthTestListDest> { new MaxDepthTestListDest()
                            {
                                Name = "444",
                                Source = new List<MaxDepthTestListSource> { new MaxDepthTestListSource() { Name = "555" } }
                            }
                        }
                    }
                }
            } 
            
            };

            var list = new List<MaxDepthTestListSource>() { obj };

            TypeAdapterConfig<MaxDepthTestListSource, MaxDepthTestListSourceDTO>
                .NewConfig()
                .MaxDepth(2);

            var bList = list.AsQueryable().Project().To<MaxDepthTestListSourceDTO>().ToList();

            Assert.IsNotNull(bList);

            Assert.IsTrue(bList.Count == 1);
            Assert.IsTrue(bList[0].Name == "111");
            Assert.IsTrue(bList[0].Source.First().Name == "222");
            Assert.IsTrue(bList[0].Source.First().Source.Name == null);
            Assert.IsTrue(bList[0].Source.First().Source.Source == null);
        }

        private List<Product> GetProducts()
        {
            var user = new User()
            {
                Id = Guid.NewGuid(),
                Name = "Timuçin",
                Surname = "KIVANÇ",
                Email = "timucinkivanc@hotmail.com",
                CreatedDate = DateTime.Now.AddYears(-3),
                ModifiedDate = DateTime.Now.AddMonths(-1)
            };

            var customer = new Customer()
            {
                Id = 1,
                Name = "Bill",
                Surname = "Gates",
                Address = new Address() { City = "İstanbul", Country = "Türkiye" }
            };

            var order = new Order()
            {
                Id = Guid.NewGuid(),
                Customer = customer,
                CustomerId = customer.Id,
                DeliveryDate = DateTime.Now,
                IsDelivered = true,
                OrderDate = DateTime.Now.AddDays(-2),
                SequenceNumberOrder = 1,
                ShippingInformationShippingAddress = "Ataşehir",
                ShippingInformationShippingCity = "İstanbul",
                ShippingInformationShippingName = "Home",
                ShippingInformationShippingZipCode = "34000"
            };

            var iPhone = new Product()
            {
                Id = Guid.NewGuid(),
                Title = "iPhone 5S",
                Description = "New generation smart phone",
                UnitPrice = 650,
                AmountInStock = 12000,
                CreatedDate = DateTime.Now.AddMonths(-3),
                ModifiedDate = DateTime.Now.AddDays(-2),
                CreatedUser = user
            };

            var galaxy = new Product()
            {
                Id = Guid.NewGuid(),
                Title = "Samsung Galaxy S5",
                Description = "New generation smart phone",
                UnitPrice = 600,
                AmountInStock = 9000,
                CreatedDate = DateTime.Now.AddMonths(-3),
                ModifiedDate = DateTime.Now.AddDays(-2),
                CreatedUser = user
            };

            var orderLines = new List<OrderLine>() { 
                new OrderLine { Amount = 1, Discount = 10, Id = Guid.NewGuid(), Order = order, OrderId = order.Id, UnitPrice = 585, Product = iPhone, ProductId = iPhone.Id },
                new OrderLine { Amount = 1, Discount = 10, Id = Guid.NewGuid(), Order = order, OrderId = order.Id, UnitPrice = 585, Product = galaxy, ProductId = galaxy.Id }
            };

            order.OrderLines = orderLines;
            iPhone.OrderLines = new List<OrderLine> { orderLines.First() };
            galaxy.OrderLines = new List<OrderLine> { orderLines.Last() };

            return new List<Product>() { iPhone, galaxy };
        }
    }
}
