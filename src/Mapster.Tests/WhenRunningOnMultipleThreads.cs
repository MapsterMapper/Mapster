using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenRunningOnMultipleThreads
    {

        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig<Customer, CustomerDTO>.Clear();
            TypeAdapterConfig<Address, AddressDTO>.Clear();
        }

        [TestMethod]
        public void Can_Set_Up_Mapping_On_Multiple_Threads()
        {
            Parallel.For(1, 5, x => TypeAdapterConfig<Customer, CustomerDTO>.NewConfig());
        }

        [TestMethod]
        public void Can_Set_Up_Adapt_On_Multiple_Threads()
        {
            Parallel.For(1, 5, x => TypeAdapter.Adapt<Customer, CustomerDTO>(GetCustomer()));
        }


        private static Customer GetCustomer()
        {
            Customer c = new Customer()
            {
                Address = new Address() { City = "istanbul", Country = "turkey", Id = 1, Street = "istiklal cad." },
                HomeAddress = new Address() { City = "istanbul", Country = "turkey", Id = 2, Street = "istiklal cad." },
                Id = 1,
                Name = "Kıvanç",
                Credit = 234.7m,
                WorkAddresses = new List<Address>() { 
                    new Address() { City = "istanbul", Country = "turkey", Id = 5, Street = "istiklal cad." },
                    new Address() { City = "izmir", Country = "turkey", Id = 6, Street = "konak" }
                },
                Addresses = new List<Address>() { 
                    new Address() { City = "istanbul", Country = "turkey", Id = 3, Street = "istiklal cad." },
                    new Address() { City = "izmir", Country = "turkey", Id = 4, Street = "konak" }
                }.ToArray()
            };

            return c;
        }


        #region TestMethod Classes

        public class Address
        {
            public int Id { get; set; }
            public string Street { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
        }

        public class AddressDTO
        {
            public int Id { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
        }

        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal? Credit { get; set; }
            public Address Address { get; set; }
            public Address HomeAddress { get; set; }
            public Address[] Addresses { get; set; }
            public ICollection<Address> WorkAddresses { get; set; }
        }

        public class CustomerDTO
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Address Address { get; set; }
            public AddressDTO HomeAddress { get; set; }
            public AddressDTO[] Addresses { get; set; }
            public List<AddressDTO> WorkAddresses { get; set; }
            public string AddressCity { get; set; }
        }

        #endregion

    }
}