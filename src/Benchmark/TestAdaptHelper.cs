using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutoMapper;
using Benchmark.Classes;
using Mapster;

namespace Benchmark
{
    public static class TestAdaptHelper
    {
        static TestAdaptHelper()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Foo, Foo>();
                cfg.CreateMap<Address, Address>();
                cfg.CreateMap<Address, AddressDTO>();
                cfg.CreateMap<Customer, CustomerDTO>();
            });

            TypeAdapterConfig.GlobalSettings.Compiler = TypeAdapterConfig.GlobalSettings.Compiler; //switch compiler
        }

        public static Customer SetupCustomerInstance()
        {
            return new Customer
            {
                Address = new Address { City = "istanbul", Country = "turkey", Id = 1, Street = "istiklal cad." },
                HomeAddress = new Address { City = "istanbul", Country = "turkey", Id = 2, Street = "istiklal cad." },
                Id = 1,
                Name = "Eduardo Najera",
                Credit = 234.7m,
                WorkAddresses = new List<Address>
                {
                    new Address {City = "istanbul", Country = "turkey", Id = 5, Street = "istiklal cad."},
                    new Address {City = "izmir", Country = "turkey", Id = 6, Street = "konak"}
                },
                Addresses = new[]
                {
                    new Address {City = "istanbul", Country = "turkey", Id = 3, Street = "istiklal cad."},
                    new Address {City = "izmir", Country = "turkey", Id = 4, Street = "konak"}
                }
            };
        }

        public static Foo SetupFooInstance()
        {
            return new Foo
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
                    new Foo {Name = "j3", Int32 = 12345, NullInt = 54321}
                },
                FooArr = new[]
                {
                    new Foo {Name = "a1"},
                    new Foo {Name = "a2"},
                    new Foo {Name = "a3"}
                },
                IntArr = new[] { 1, 2, 3, 4, 5 },
                Ints = new[] { 7, 8, 9 }
            };
        }

        public static void Configure(Foo fooInstance)
        {
            TypeAdapterConfig.GlobalSettings.Compile(typeof(Foo), typeof(Foo)); //recompile
            fooInstance.Adapt<Foo, Foo>(); //exercise

            ExpressMapper.Mapper.Map<Foo, Foo>(fooInstance); //exercise

            Mapper.Map<Foo, Foo>(fooInstance); //exercise
        }

        public static void Configure(Customer customerInstance)
        {
            TypeAdapterConfig.GlobalSettings.Compile(typeof(Customer), typeof(CustomerDTO));    //recompile
            customerInstance.Adapt<Customer, CustomerDTO>();    //exercise

            ExpressMapper.Mapper.Map<Customer, CustomerDTO>(customerInstance);  //exercise

            Mapper.Map<Customer, CustomerDTO>(customerInstance);    //exercise
        }

        public static void TestMapsterAdapter<TSrc, TDest>(TSrc item, int iterations, ref double counter)
            where TSrc : class
            where TDest : class, new()
        {
            var time = LoopWithTime(item, get => get.Adapt<TSrc, TDest>(), iterations);
            counter += time;

            Console.WriteLine("Mapster:\t\t" + time);
        }

        public static void TestMapsterAdapter<TSrc, TDest>(TSrc item, int iterations)
            where TSrc : class
            where TDest : class, new()
        {
            Loop(item, get => get.Adapt<TSrc, TDest>(), iterations);
        }

        public static void TestExpressMapper<TSrc, TDest>(TSrc item, int iterations, ref double counter)
            where TSrc : class
            where TDest : class, new()
        {
            var time = LoopWithTime(item, get => Mapper.Map<TSrc, TDest>(get), iterations);
            counter += time;

            Console.WriteLine("ExpressMapper:\t\t" + time);
        }

        public static void TestExpressMapper<TSrc, TDest>(TSrc item, int iterations)
            where TSrc : class
            where TDest : class, new()
        {
            Loop(item, get => Mapper.Map<TSrc, TDest>(get), iterations);
        }

        public static void TestAutoMapper<TSrc, TDest>(TSrc item, int iterations, ref double counter)
            where TSrc : class
            where TDest : class, new()
        {
            var time = LoopWithTime(item, get => Mapper.Map<TSrc, TDest>(get), iterations);
            counter += time;

            Console.WriteLine("AutoMapper:\t\t" + time);
        }

        public static void TestAutoMapper<TSrc, TDest>(TSrc item, int iterations)
            where TSrc : class
            where TDest : class, new()
        {
            Loop(item, get => Mapper.Map<TSrc, TDest>(get), iterations);
        }

        public static void TestCodeGen(Foo item, int iterations, ref double counter)
        {
            var time = LoopWithTime(item, get => FooMapper.Map(get), iterations);
            counter += time;

            Console.WriteLine("Codegen:\t\t" + time);
        }

        public static void TestCodeGen(Foo item, int iterations)
        {
            Loop(item, get => FooMapper.Map(get), iterations);
        }

        public static void TestCodeGen(Customer item, int iterations, ref double counter)
        {
            var time = LoopWithTime(item, get => CustomerMapper.Map(get), iterations);
            counter += time;

            Console.WriteLine("Codegen:\t\t" + time);
        }

        public static void TestCodeGen(Customer item, int iterations)
        {
            Loop(item, get => CustomerMapper.Map(get), iterations);
        }

        public static void TestCustomerNative(Customer item, int iterations)
        {
            Console.WriteLine("Handwritten Mapper:\t" + LoopWithTime(item, get =>
            {
                var dto = new CustomerDTO();

                dto.Id = get.Id;
                dto.Name = get.Name;
                dto.AddressCity = get.Address.City;

                dto.Address = new Address { Id = get.Address.Id, Street = get.Address.Street, Country = get.Address.Country, City = get.Address.City };

                dto.HomeAddress = new AddressDTO { Id = get.HomeAddress.Id, Country = get.HomeAddress.Country, City = get.HomeAddress.City };

                dto.Addresses = new AddressDTO[get.Addresses.Length];
                for (int i = 0; i < get.Addresses.Length; i++)
                {
                    dto.Addresses[i] = new AddressDTO { Id = get.Addresses[i].Id, Country = get.Addresses[i].Country, City = get.Addresses[i].City };
                }

                dto.WorkAddresses = new List<AddressDTO>();
                foreach (var workAddress in get.WorkAddresses)
                {
                    dto.WorkAddresses.Add(new AddressDTO { Id = workAddress.Id, Country = workAddress.Country, City = workAddress.City });
                }

            }, iterations));
        }

        private static void Loop<T>(T item, Action<T> action, int iterations)
        {
            for (var i = 0; i < iterations; i++) action(item);
        }

        private static long LoopWithTime<T>(T item, Action<T> action, int iterations)
        {
            return Time(item, a =>
            {
                for (var i = 0; i < iterations; i++) action(a);
            });
        }

        private static long Time<T>(T item, Action<T> action)
        {
            var sw = Stopwatch.StartNew();
            action(item);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }
}