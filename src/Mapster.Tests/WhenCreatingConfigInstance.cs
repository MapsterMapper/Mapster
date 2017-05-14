using Mapster.Tests.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{

    [TestClass]
    public class WhenCreatingConfigInstances
    {
        [TestMethod]
        public void Basic_Poco_Is_Mapped_With_New_Config()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<Customer, CustomerDTO>()
                .Map(dest => dest.Address_Country, src => "TestAddressCountry");

            var customer = new Customer
            {
                Id = 12345,
                Name = "TestName",
                Surname = "TestSurname"
            };

            var customerDto = customer.Adapt<CustomerDTO>(config);

            customerDto.Id.ShouldBe(12345);
            customerDto.Name.ShouldBe("TestName");
            customerDto.Address_Country.ShouldBe("TestAddressCountry");
        }

        [TestMethod]
        public void ForType_Enhances_Config()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<Customer, CustomerDTO>()
                .Map(dest => dest.Address_Country, src => "TestAddressCountry");

            config.ForType<Customer, CustomerDTO>()
                .Map(dest => dest.Name, src => src.Name + "_Enhanced");

            var customer = new Customer
            {
                Id = 12345,
                Name = "TestName",
                Surname = "TestSurname"
            };

            var customerDto = customer.Adapt<CustomerDTO>(config);

            customerDto.Id.ShouldBe(12345);
            customerDto.Name.ShouldBe("TestName_Enhanced");
            customerDto.Address_Country.ShouldBe("TestAddressCountry");
        }

        [TestMethod]
        public void NewConfig_Overwrites_Config()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<Customer, CustomerDTO>()
                .Map(dest => dest.Name, src => src.Name + "_Enhanced");

            config.NewConfig<Customer, CustomerDTO>()
                .Map(dest => dest.Address_Country, src => "TestAddressCountry");

            var customer = new Customer
            {
                Id = 12345,
                Name = "TestName",
                Surname = "TestSurname"
            };

            var customerDto = customer.Adapt<CustomerDTO>(config);

            customerDto.Id.ShouldBe(12345);
            customerDto.Name.ShouldBe("TestName");
            customerDto.Address_Country.ShouldBe("TestAddressCountry");
        }
    }
}