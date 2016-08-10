using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenMappingPrivateFieldsAndProperties
    {
        [TearDown]
        public void TearDown()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [Test]
        public void Default_Settings_Should_Not_Map_Private_Fields()
        {
            TypeAdapterConfig<CustomerWithPrivateField, CustomerDTO>
                .NewConfig()
                .NameMatchingStrategy(NameMatchingStrategy.Flexible);

            var customerId = 1;
            var aCustomer = new CustomerWithPrivateField(customerId);

            var dto = aCustomer.Adapt<CustomerDTO>();

            dto.Id.ShouldNotBe(customerId);
        }

        [Test]
        public void Default_Settings_Should_Not_Map_Private_Properties()
        {
            var customerName = "Customer 1";
            var aCustomer = new CustomerWithPrivateProperty(customerName);

            var dto = aCustomer.Adapt<CustomerDTO>();

            dto.Name.ShouldNotBe(customerName);
        }

        [Test]
        public void Should_Map_Private_Field_To_New_Object_Correctly()
        {
            SetUpMappingPrivateFields();

            var customerId = 1;
            var aCustomer = new CustomerWithPrivateField(customerId);

            var dto = aCustomer.Adapt<CustomerDTO>();

            dto.Id.ShouldBe(customerId);
        }

        [Test]
        public void Should_Map_Private_Property_To_New_Object_Correctly()
        {
            SetUpMappingPrivateProperties();

            var customerName = "Customer 1";
            var aCustomer = new CustomerWithPrivateProperty(customerName);

            var dto = aCustomer.Adapt<CustomerDTO>();

            dto.Name.ShouldBe(customerName);
        }

        private void SetUpMappingPrivateFields()
        {
            var config = TypeAdapterConfig<CustomerWithPrivateField, CustomerDTO>.NewConfig();
            config.Settings.ValueAccessingStrategies.Add(ValueAccessingStrategy.PrivatePropertyOrField);
            config.NameMatchingStrategy(NameMatchingStrategy.Flexible);
        }

        private void SetUpMappingPrivateProperties()
        {
            TypeAdapterConfig<CustomerWithPrivateProperty, CustomerDTO>
                  .NewConfig()
                      .Settings.ValueAccessingStrategies.Add(ValueAccessingStrategy.PrivatePropertyOrField);

        }

        #region Test Classes

        public class CustomerWithPrivateField
        {
            private int _id;

            private CustomerWithPrivateField() { }

            public CustomerWithPrivateField(int id)
            {
                _id = id;
            }

            public bool HasId(int id)
            {
                return _id == id;
            }
        }

        public class CustomerWithPrivateProperty
        {
            private string Name { get; set; }

            private CustomerWithPrivateProperty() { }

            public CustomerWithPrivateProperty(string name)
            {
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
