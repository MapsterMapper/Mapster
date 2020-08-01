
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Benchmark;
using Benchmark.Classes;


namespace Benchmark
{
    public partial class CustomerAdapter : ICustomerAdapter
    {
        public Expression<Func<Customer, CustomerDTO>> Project => p1 => new CustomerDTO()
        {
            Id = p1.Id,
            Name = p1.Name,
            Address = p1.Address,
            HomeAddress = p1.HomeAddress == null ? null : new AddressDTO()
            {
                Id = p1.HomeAddress.Id,
                City = p1.HomeAddress.City,
                Country = p1.HomeAddress.Country
            },
            Addresses = p1.Addresses.Select<Address, AddressDTO>(p2 => new AddressDTO()
            {
                Id = p2.Id,
                City = p2.City,
                Country = p2.Country
            }).ToArray<AddressDTO>(),
            WorkAddresses = p1.WorkAddresses.Select<Address, AddressDTO>(p3 => new AddressDTO()
            {
                Id = p3.Id,
                City = p3.City,
                Country = p3.Country
            }).ToList<AddressDTO>(),
            AddressCity = p1.Address.City
        };
        public CustomerDTO Adapt(Customer p4)
        {
            return p4 == null ? null : new CustomerDTO()
            {
                Id = p4.Id,
                Name = p4.Name,
                Address = p4.Address == null ? null : new Address()
                {
                    Id = p4.Address.Id,
                    Street = p4.Address.Street,
                    City = p4.Address.City,
                    Country = p4.Address.Country
                },
                HomeAddress = p4.HomeAddress == null ? null : new AddressDTO()
                {
                    Id = p4.HomeAddress.Id,
                    City = p4.HomeAddress.City,
                    Country = p4.HomeAddress.Country
                },
                Addresses = funcMain1(p4.Addresses),
                WorkAddresses = funcMain2(p4.WorkAddresses),
                AddressCity = p4.Address == null ? null : p4.Address.City
            };
        }
        public Customer Adapt(CustomerDTO p7)
        {
            return p7 == null ? null : new Customer()
            {
                Id = p7.Id,
                Name = p7.Name,
                Address = p7.Address == null ? null : new Address()
                {
                    Id = p7.Address.Id,
                    Street = p7.Address.Street,
                    City = p7.Address.City,
                    Country = p7.Address.Country
                },
                HomeAddress = p7.HomeAddress == null ? null : new Address()
                {
                    Id = p7.HomeAddress.Id,
                    City = p7.HomeAddress.City,
                    Country = p7.HomeAddress.Country
                },
                Addresses = funcMain3(p7.Addresses),
                WorkAddresses = funcMain4(p7.WorkAddresses)
            };
        }
        public Customer Adapt(CustomerDTO p10, Customer p11)
        {
            if (p10 == null)
            {
                return null;
            }
            Customer result = p11 ?? new Customer();
            
            result.Id = p10.Id;
            result.Name = p10.Name;
            result.Address = funcMain5(p10.Address, result.Address);
            result.HomeAddress = funcMain6(p10.HomeAddress, result.HomeAddress);
            result.Addresses = funcMain7(p10.Addresses, result.Addresses);
            result.WorkAddresses = funcMain8(p10.WorkAddresses, result.WorkAddresses);
            return result;
            
        }
        
        private AddressDTO[] funcMain1(Address[] p5)
        {
            if (p5 == null)
            {
                return null;
            }
            AddressDTO[] result = new AddressDTO[p5.Length];
            
            int v = 0;
            
            int i = 0;
            int len = p5.Length;
            
            while (i < len)
            {
                Address item = p5[i];
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
        
        private List<AddressDTO> funcMain2(ICollection<Address> p6)
        {
            if (p6 == null)
            {
                return null;
            }
            List<AddressDTO> result = new List<AddressDTO>(p6.Count);
            
            IEnumerator<Address> enumerator = p6.GetEnumerator();
            
            while (enumerator.MoveNext())
            {
                Address item = enumerator.Current;
                result.Add(item == null ? null : new AddressDTO()
                {
                    Id = item.Id,
                    City = item.City,
                    Country = item.Country
                });
            }
            return result;
            
        }
        
        private Address[] funcMain3(AddressDTO[] p8)
        {
            if (p8 == null)
            {
                return null;
            }
            Address[] result = new Address[p8.Length];
            
            int v = 0;
            
            int i = 0;
            int len = p8.Length;
            
            while (i < len)
            {
                AddressDTO item = p8[i];
                result[v++] = item == null ? null : new Address()
                {
                    Id = item.Id,
                    City = item.City,
                    Country = item.Country
                };
                i++;
            }
            return result;
            
        }
        
        private ICollection<Address> funcMain4(List<AddressDTO> p9)
        {
            if (p9 == null)
            {
                return null;
            }
            ICollection<Address> result = new List<Address>(p9.Count);
            
            int i = 0;
            int len = p9.Count;
            
            while (i < len)
            {
                AddressDTO item = p9[i];
                result.Add(item == null ? null : new Address()
                {
                    Id = item.Id,
                    City = item.City,
                    Country = item.Country
                });
                i++;
            }
            return result;
            
        }
        
        private Address funcMain5(Address p12, Address p13)
        {
            if (p12 == null)
            {
                return null;
            }
            Address result = p13 ?? new Address();
            
            result.Id = p12.Id;
            result.Street = p12.Street;
            result.City = p12.City;
            result.Country = p12.Country;
            return result;
            
        }
        
        private Address funcMain6(AddressDTO p14, Address p15)
        {
            if (p14 == null)
            {
                return null;
            }
            Address result = p15 ?? new Address();
            
            result.Id = p14.Id;
            result.City = p14.City;
            result.Country = p14.Country;
            return result;
            
        }
        
        private Address[] funcMain7(AddressDTO[] p16, Address[] p17)
        {
            if (p16 == null)
            {
                return null;
            }
            Address[] result = new Address[p16.Length];
            
            int v = 0;
            
            int i = 0;
            int len = p16.Length;
            
            while (i < len)
            {
                AddressDTO item = p16[i];
                result[v++] = item == null ? null : new Address()
                {
                    Id = item.Id,
                    City = item.City,
                    Country = item.Country
                };
                i++;
            }
            return result;
            
        }
        
        private ICollection<Address> funcMain8(List<AddressDTO> p18, ICollection<Address> p19)
        {
            if (p18 == null)
            {
                return null;
            }
            ICollection<Address> result = new List<Address>(p18.Count);
            
            int i = 0;
            int len = p18.Count;
            
            while (i < len)
            {
                AddressDTO item = p18[i];
                result.Add(item == null ? null : new Address()
                {
                    Id = item.Id,
                    City = item.City,
                    Country = item.Country
                });
                i++;
            }
            return result;
            
        }
    }
}