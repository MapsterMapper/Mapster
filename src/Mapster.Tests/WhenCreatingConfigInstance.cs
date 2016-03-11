using Mapster.Tests.Classes;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{

    [TestFixture]
    public class WhenCreatingConfigInstances
    {
        [Test]
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

            customerDto.Id.ShouldEqual(12345);
            customerDto.Name.ShouldEqual("TestName");
            customerDto.Address_Country.ShouldEqual("TestAddressCountry");
        }

        [Test]
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

            customerDto.Id.ShouldEqual(12345);
            customerDto.Name.ShouldEqual("TestName_Enhanced");
            customerDto.Address_Country.ShouldEqual("TestAddressCountry");
        }

        [Test]
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

            customerDto.Id.ShouldEqual(12345);
            customerDto.Name.ShouldEqual("TestName");
            customerDto.Address_Country.ShouldEqual("TestAddressCountry");
        }
    }
}