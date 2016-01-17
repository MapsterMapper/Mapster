using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutoMapper;
using Benchmark.Classes;
using DeepCloning;
using Mapster;
using Omu.ValueInjecter;

namespace Benchmark
{
    class Program
    {
        private static double AutomapperTime;
        private static double MapsterTime;
        private static double ExpressMapperTime;

        static void Main(string[] args)
        {
            try
            {
                TestSimpleTypes();
                TestComplexTypes();
            }

            finally
            {
                Console.WriteLine("Finish");
                Console.ReadLine();
            }
        }

        private static void TestSimpleTypes()
        {
            Console.WriteLine("Test 1 : Simple Types");
            Console.WriteLine("Competitors : Mapster, Fast Mapper, Value Injecter, AutoMapper");

            var foo = GetFoo();

            Mapper.CreateMap<Foo, Foo>();

            TypeAdapter.Adapt<Foo, Foo>(foo); // cache mapping strategy

            ExpressMapper.Mapper.Map<Foo, Foo>(foo);

            TestSimple(foo, 1000);

            TestSimple(foo, 10000);

            //TestSimple(foo, 100000);

            //TestSimple(foo, 1000000);
        }

        private static void TestComplexTypes()
        {
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("Test 2 : Complex Types");
            Console.WriteLine("Competitors : Handwriting Mapper, Mapster, FastMapper, AutoMapper");
            Console.WriteLine("(Value Injecter cannot convert complex type, Value injecter need a custom injecter)");

            var customer = GetCustomer();

            Mapper.CreateMap<Address, Address>();
            Mapper.CreateMap<Address, AddressDTO>();
            Mapper.CreateMap<Customer, CustomerDTO>();

            //TypeAdapterConfig.GlobalSettings.DestinationTransforms.Upsert<Guid>(x => x);
            TypeAdapter.Adapt<Customer, CustomerDTO>(customer); // cache mapping strategy

            //ObjectMapperManager.DefaultInstance.GetMapper<Customer, CustomerDTO>().Map(customer);
            ExpressMapper.Mapper.Map<Customer, CustomerDTO>(customer);

            Test(customer, 1000);

            Test(customer, 10000);

            Test(customer, 100000);

            Test(customer, 1000000);

            Test(customer, 10000000);

            Console.WriteLine();
            Console.WriteLine("Automapper to Mapster ratio: " + (AutomapperTime / MapsterTime).ToString("###.00") + " X slower");
            Console.WriteLine("ExpressMapper to Mapster ratio: " + (ExpressMapperTime / MapsterTime).ToString("###.00") + " X slower");
            Console.WriteLine();
        }

        private static void Test(Customer item, int iterations)
        {
            Console.WriteLine();

            Console.WriteLine("Iterations : {0}", iterations);

            TestCustomerNative(item, iterations);

            TestMapsterAdapter<Customer, CustomerDTO>(item, iterations);

            TestExpressMapper<Customer, CustomerDTO>(item, iterations);

            //TestAutoMapper<Customer, CustomerDTO>(item, iterations);
        }

        private static void TestSimple(Foo item, int iterations)
        {
            Console.WriteLine();

            Console.WriteLine("Iterations : {0}", iterations);

            TestMapsterAdapter<Foo, Foo>(item, iterations);

            TestValueInjecter<Foo, Foo>(item, iterations);

            TestExpressMapper<Foo, Foo>(item, iterations);

            TestAutoMapper<Foo, Foo>(item, iterations);
        }


        private static void TestCustomerNative(Customer item, int iterations)
        {
            Console.WriteLine("Handwritten Mapper:\t" + Loop<Customer>(item, get =>
            {
                var dto = new CustomerDTO();
  
                dto.Id = get.Id;
                dto.Name = get.Name;
                dto.AddressCity = get.Address.City;

                dto.Address = new Address() { Id = get.Address.Id, Street = get.Address.Street, Country = get.Address.Country, City = get.Address.City };
             
                dto.HomeAddress = new AddressDTO() { Id = get.HomeAddress.Id, Country = get.HomeAddress.Country, City = get.HomeAddress.City };
               
                dto.Addresses = new AddressDTO[get.Addresses.Length];
                for (int i = 0; i < get.Addresses.Length; i++)
                {
                    dto.Addresses[i] = new AddressDTO() { Id = get.Addresses[i].Id, Country = get.Addresses[i].Country, City = get.Addresses[i].City };
                }

                dto.WorkAddresses = new List<AddressDTO>();
                foreach (var workAddress in get.WorkAddresses)
	            {
                    dto.WorkAddresses.Add(new AddressDTO() { Id = workAddress.Id, Country = workAddress.Country, City = workAddress.City });
                }

            }, iterations));
        }


        private static void TestMapsterAdapter<TSrc, TDest>(TSrc item, int iterations)
            where TSrc : class
            where TDest : class, new()
        {
            MapsterTime = Loop(item, get => TypeAdapter.Adapt<TSrc, TDest>(get), iterations);
            Console.WriteLine("Mapster:\t\t" + MapsterTime);
        }


        private static void TestValueInjecter<TSrc, TDest>(TSrc item, int iterations)
            where TSrc : class
            where TDest : class, new()
        {
            if (iterations > 50000)
                Console.WriteLine("ValueInjecter still working please wait...");

            Console.WriteLine("ValueInjecter:\t\t" + Loop<TSrc>(item, get => new TDest().InjectFrom<FastDeepCloneInjection>(item), iterations));  
        }

        private static void TestExpressMapper<TSrc, TDest>(TSrc item, int iterations)
            where TSrc : class
            where TDest : class, new()
        {
            ExpressMapperTime = Loop(item, get => ExpressMapper.Mapper.Map<TSrc, TDest>(get), iterations);
            Console.WriteLine("ExpressMapper:\t\t" + ExpressMapperTime);
        }

        private static void TestAutoMapper<TSrc, TDest>(TSrc item, int iterations)
            where TSrc : class
            where TDest : class, new()
        {
            if (iterations > 50000)
                Console.WriteLine("AutoMapper still working please wait...");

            AutomapperTime = Loop(item, get => Mapper.Map<TSrc, TDest>(get), iterations);
            Console.WriteLine("AutoMapper:\t\t" + AutomapperTime);
        }

        private static long Loop<T>(T item, Action<T> action, int iterations = 1000000)
        {
            return Time(item, a =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    action(a);
                }
            });
        }

        private static long Time<T>(T item, Action<T> action)
        {
            var sw = new Stopwatch();
            sw.Start();
            action(item);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }


        #region Data

        private static Customer GetCustomer()
        {
            Customer c = new Customer()
            {
                Address = new Address() { City = "istanbul", Country = "turkey", Id = 1, Street = "istiklal cad." },
                HomeAddress = new Address() { City = "istanbul", Country = "turkey", Id = 2, Street = "istiklal cad." },
                Id = 1,
                Name = "Eduardo Najera",
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

        private static Foo GetFoo()
        {
            var o = new Foo
            {
                Name = "foo",
                Int32 = 12,
                Int64 = 123123,
                NullInt = 16,
                DateTime = DateTime.Now,
                Doublen = 2312112,
                Foo1 = new Foo { Name = "foo one" },
                Foos = new List<Foo>
                                       {
                                           new Foo {Name = "j1", Int64 = 123, NullInt = 321},
                                           new Foo {Name = "j2", Int32 = 12345, NullInt = 54321},
                                           new Foo {Name = "j3", Int32 = 12345, NullInt = 54321},
                                       },
                FooArr = new[]
                                         {
                                             new Foo {Name = "a1"},
                                             new Foo {Name = "a2"},
                                             new Foo {Name = "a3"},
                                         },
                IntArr = new[] { 1, 2, 3, 4, 5 },
                Ints = new[] { 7, 8, 9 },
            };

            return o;
        }

        #endregion

    }
}
