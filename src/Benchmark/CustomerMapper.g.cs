
using System.Collections.Generic;
using Benchmark.Classes;


namespace Benchmark
{
    public static partial class CustomerMapper
    {
        public static CustomerDTO Map(Customer p1)
        {
            return p1 == null ? null : new CustomerDTO()
            {
                Id = p1.Id,
                Name = p1.Name,
                Address = p1.Address == null ? null : new Address()
                {
                    Id = p1.Address.Id,
                    Street = p1.Address.Street,
                    City = p1.Address.City,
                    Country = p1.Address.Country
                },
                HomeAddress = p1.HomeAddress == null ? null : new AddressDTO()
                {
                    Id = p1.HomeAddress.Id,
                    City = p1.HomeAddress.City,
                    Country = p1.HomeAddress.Country
                },
                Addresses = func1(p1.Addresses),
                WorkAddresses = func2(p1.WorkAddresses),
                AddressCity = p1.Address == null ? null : p1.Address.City
            };
        }
        
        private static AddressDTO[] func1(Address[] p2)
        {
            if (p2 == null)
            {
                return null;
            }
            AddressDTO[] result = new AddressDTO[p2.Length];
            
            int v = 0;
            
            int i = 0;
            int len = p2.Length;
            
            while (i < len)
            {
                Address item = p2[i];
                result[v++] = item == null ? null : new AddressDTO()
                {
                    Id = item.Id,
                    City = item.City,
                    Country = item.Country
                };
                i++;
            }
            return result;
            
        }
        
        private static List<AddressDTO> func2(ICollection<Address> p3)
        {
            if (p3 == null)
            {
                return null;
            }
            List<AddressDTO> result = new List<AddressDTO>(p3.Count);
            
            ICollection<AddressDTO> list = result;
            
            IEnumerator<Address> enumerator = p3.GetEnumerator();
            
            while (enumerator.MoveNext())
            {
                Address item = enumerator.Current;
                list.Add(item == null ? null : new AddressDTO()
                {
                    Id = item.Id,
                    City = item.City,
                    Country = item.Country
                });
            }
            return result;
            
        }
    }
}
