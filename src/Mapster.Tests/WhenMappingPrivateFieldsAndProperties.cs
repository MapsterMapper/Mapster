using NUnit.Framework;
using Shouldly;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenMappingPrivateFieldsAndProperties
    {
        [Test]
        public void Default_Settings_Should_Not_Map_Private_Fields_To_New_Object()
        {
            TypeAdapterConfig<CustomerWithPrivateField, CustomerDTO>
                .NewConfig()
                .NameMatchingStrategy(NameMatchingStrategy.Flexible);

            var customerId = 1;
            var customerName = "Customer 1";
            var aCustomer = new CustomerWithPrivateField(customerId, customerName);

            var dto = aCustomer.Adapt<CustomerDTO>();

            Assert.NotNull(dto);
            dto.Id.ShouldNotBe(customerId);
            dto.Name.ShouldBe(customerName);
        }

        [Test]
        public void Default_Settings_Should_Not_Map_Private_Properties_To_New_Object()
        {
            var customerId = 1;
            var customerName = "Customer 1";
            var aCustomer = new CustomerWithPrivateProperty(customerId, customerName);

            var dto = aCustomer.Adapt<CustomerDTO>();

            Assert.NotNull(dto);
            dto.Id.ShouldBe(customerId);
            dto.Name.ShouldNotBe(customerName);
        }

        [Test]
        public void Should_Map_Private_Field_To_New_Object_Correctly()
        {
            SetUpMappingNonPublicFields<CustomerWithPrivateField, CustomerDTO>();

            var customerId = 1;
            var customerName = "Customer 1";
            var aCustomer = new CustomerWithPrivateField(customerId, customerName);

            var dto = aCustomer.Adapt<CustomerDTO>();

            Assert.NotNull(dto);
            dto.Id.ShouldBe(customerId);
            dto.Name.ShouldBe(customerName);
        }

        [Test]
        public void Should_Map_Private_Property_To_New_Object_Correctly()
        {
            SetUpMappingNonPublicProperties<CustomerWithPrivateProperty, CustomerDTO>();

            var customerId = 1;
            var customerName = "Customer 1";
            var aCustomer = new CustomerWithPrivateProperty(customerId, customerName);

            var dto = aCustomer.Adapt<CustomerDTO>();

            Assert.NotNull(dto);
            dto.Id.ShouldBe(customerId);
            dto.Name.ShouldBe(customerName);
        }

        [Test]
        public void Should_Map_To_Private_Fields_Correctly()
        {
            SetUpMappingNonPublicFields<CustomerDTO, CustomerWithPrivateField>();
            
            var dto = new CustomerDTO
            {
                Id = 1,
                Name = "Customer 1"
            };

            var customer = dto.Adapt<CustomerWithPrivateField>();

            Assert.NotNull(customer);
            Assert.IsTrue(customer.HasId(dto.Id));
            customer.Name.ShouldBe(dto.Name);            
        }

        [Test]
        public void Should_Map_To_Private_Properties_Correctly()
        {
            SetUpMappingNonPublicFields<CustomerDTO, CustomerWithPrivateProperty>();

            var dto = new CustomerDTO
            {
                Id = 1,
                Name = "Customer 1"
            };

            var customer = dto.Adapt<CustomerWithPrivateProperty>();

            Assert.NotNull(customer);
            customer.Id.ShouldBe(dto.Id);
            Assert.IsTrue(customer.HasName(dto.Name));
        }

        private void SetUpMappingNonPublicFields<TSource, TDestination>()
        {
            var config = TypeAdapterConfig<TSource, TDestination>.NewConfig();
            config.EnableNonPublicMembers();
            config.NameMatchingStrategy(NameMatchingStrategy.Flexible);
        }

        private void SetUpMappingNonPublicProperties<TSource, TDestination>()
        {
            TypeAdapterConfig<TSource, TDestination>
                  .NewConfig()
                  .EnableNonPublicMembers();

        }

        #region Test Classes

        public class CustomerWithPrivateField
        {
            private int _id;
            public string Name { get; private set; }

            private CustomerWithPrivateField() { }

            public CustomerWithPrivateField(int id, string name)
            {
                _id = id;
                Name = name;
            }

            public bool HasId(int id)
            {
                return _id == id;
            }
        }

        public class CustomerWithPrivateProperty
        {
            public int Id { get; private set; }
            private string Name { get; set; }

            private CustomerWithPrivateProperty() { }

            public CustomerWithPrivateProperty(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public bool HasName(string name)
            {
                return Name == name;
            }
        }

        public class CustomerDTO
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        #endregion
    }
}
