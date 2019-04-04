using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutoMapper;
using Benchmark.Classes;
using Mapster;
using System.Linq.Expressions;

namespace Benchmark
{
    class Program
    {
        static double sAM;
        static double sMP;
        static double sMF;
        static double sEM;

        static Func<LambdaExpression, Delegate> defaultCompiler = TypeAdapterConfig.GlobalSettings.Compiler;

        static void Main(string[] args)
        {
            try
            {
                Mapper.Initialize(cfg =>
                {
                    cfg.CreateMap<Foo, Foo>();
                    cfg.CreateMap<Address, Address>();
                    cfg.CreateMap<Address, AddressDTO>();
                    cfg.CreateMap<Customer, CustomerDTO>();
                });

                TestSimpleTypes();
                TestComplexTypes();

                Console.WriteLine();
                Console.WriteLine($"======================================================================================");
                Console.WriteLine($"| Tests                | Time (ms) | Slower than Mapster | Slower than Mapster + FEC |");
                Console.WriteLine($"======================================================================================");
                Console.WriteLine($"| Mapster v3.1.8       | {  sMP,9} | { sMP / sMP,18:N2}X | {       sMP / sMF,24:N2}X |");
                Console.WriteLine($"| Mapster v3.1.8 + FEC | {  sMF,9} | { sMF / sMP,18:N2}X | {       sMF / sMF,24:N2}X |");
                Console.WriteLine($"| Automapper v7.0.1    | {  sAM,9} | { sAM / sMP,18:N2}X | {       sAM / sMF,24:N2}X |");
                Console.WriteLine($"| ExpressMapper v1.9.1 | {  sEM,9} | { sEM / sMP,18:N2}X | {       sEM / sMF,24:N2}X |");
                Console.WriteLine($"======================================================================================");
                Console.WriteLine();
            }

            finally
            {
                Console.WriteLine("Finish");
                Console.ReadLine();
            }
        }

        static void TestSimpleTypes()
        {
            Console.WriteLine("Test 1 : Simple Types");
            Console.WriteLine("Competitors : Mapster, ExpressMapper, AutoMapper");

            var foo = GetFoo();

            TestSimple(foo, 1000);

            TestSimple(foo, 10000);

            TestSimple(foo, 100000);

            TestSimple(foo, 1000000);

            //Console.WriteLine();
            //Console.WriteLine("Automapper to Mapster ratio: " + (AutomapperTime / MapsterTime).ToString("###.00") + " X slower");
            //Console.WriteLine("ExpressMapper to Mapster ratio: " + (ExpressMapperTime / MapsterTime).ToString("###.00") + " X slower");
            //Console.WriteLine();

        }

        static void TestComplexTypes()
        {
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("Test 2 : Complex Types");
            Console.WriteLine("Competitors : Mapster, ExpressMapper, AutoMapper");

            var customer = GetCustomer();


            //TypeAdapterConfig.GlobalSettings.DestinationTransforms.Upsert<Guid>(x => x);

            //ObjectMapperManager.DefaultInstance.GetMapper<Customer, CustomerDTO>().Map(customer);

            Test(customer, 1000);

            Test(customer, 10000);

            Test(customer, 100000);

            Test(customer, 1000000);

            Test(customer, 10000000);

            //Console.WriteLine();
            //Console.WriteLine("Automapper to Mapster ratio: " + (AutomapperTime/MapsterTime).ToString("###.00") + " X slower");
            //Console.WriteLine("ExpressMapper to Mapster ratio: " + (ExpressMapperTime/MapsterTime).ToString("###.00") + " X slower");
            //Console.WriteLine();
        }

        static void Test(Customer item, int iterations)
        {
            Console.WriteLine();

            Console.WriteLine("Iterations : {0}", iterations);

            //TestCustomerNative(item, iterations);

            TypeAdapterConfig.GlobalSettings.Compiler = defaultCompiler;    //switch compiler
            TypeAdapterConfig.GlobalSettings.Compile(typeof(Customer), typeof(CustomerDTO));    //recompile
            item.Adapt<Customer, CustomerDTO>();    //exercise
            TestMapsterAdapter<Customer, CustomerDTO>(item, iterations, ref sMP);

            //TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();    //switch compiler
            //TypeAdapterConfig.GlobalSettings.Compile(typeof(Customer), typeof(CustomerDTO));    //recompile
            //item.Adapt<Customer, CustomerDTO>();    //exercise
            //TestMapsterAdapter<Customer, CustomerDTO>(item, iterations, ref sMF);
            TestCodeGen(item, iterations, ref sMF);

            ExpressMapper.Mapper.Map<Customer, CustomerDTO>(item);  //exercise
            TestExpressMapper<Customer, CustomerDTO>(item, iterations, ref sEM);

            Mapper.Map<Customer, CustomerDTO>(item);    //exercise
            TestAutoMapper<Customer, CustomerDTO>(item, iterations, ref sAM);
        }

        static void TestSimple(Foo item, int iterations)
        {



            Console.WriteLine();

            Console.WriteLine("Iterations : {0}", iterations);

            TypeAdapterConfig.GlobalSettings.Compiler = defaultCompiler;    //switch compiler
            TypeAdapterConfig.GlobalSettings.Compile(typeof(Foo), typeof(Foo));    //recompile
            item.Adapt<Foo, Foo>(); //exercise
            TestMapsterAdapter<Foo, Foo>(item, iterations, ref sMP);

            //TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();    //switch compiler
            //TypeAdapterConfig.GlobalSettings.Compile(typeof(Foo), typeof(Foo));    //recompile
            //item.Adapt<Foo, Foo>(); //exercise
            //TestMapsterAdapter<Foo, Foo>(item, iterations, ref sMF);
            TestCodeGen(item, iterations, ref sMF);

            //TestValueInjecter<Foo, Foo>(item, iterations);

            ExpressMapper.Mapper.Map<Foo, Foo>(item);   //exercise
            TestExpressMapper<Foo, Foo>(item, iterations, ref sEM);

            Mapper.Map<Foo, Foo>(item);     //exercise
            TestAutoMapper<Foo, Foo>(item, iterations, ref sAM);
        }


        static void TestCustomerNative(Customer item, int iterations)
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


        static void TestMapsterAdapter<TSrc, TDest>(TSrc item, int iterations, ref double counter)
            where TSrc : class
            where TDest : class, new()
        {
            var time = Loop(item, get => get.Adapt<TSrc, TDest>(), iterations);
            Console.WriteLine("Mapster:\t\t" + time);
            counter += time;
        }

        static void TestExpressMapper<TSrc, TDest>(TSrc item, int iterations, ref double counter)
            where TSrc : class
            where TDest : class, new()
        {
            var time = Loop(item, get => ExpressMapper.Mapper.Map<TSrc, TDest>(get), iterations);
            Console.WriteLine("ExpressMapper:\t\t" + time);
            counter += time;
        }

        static void TestAutoMapper<TSrc, TDest>(TSrc item, int iterations, ref double counter)
            where TSrc : class
            where TDest : class, new()
        {
            //if (iterations > 50000)
            //    Console.WriteLine("AutoMapper still working please wait...");

            var time = Loop(item, get => Mapper.Map<TSrc, TDest>(get), iterations);
            Console.WriteLine("AutoMapper:\t\t" + time);
            counter += time;
        }

        static void TestCodeGen(Customer item, int iterations, ref double counter)
        {
            var time = Loop(item, get => CustomerMapper.Map(get), iterations);
            Console.WriteLine("Codegen:\t\t" + time);
            counter += time;
        }

        static void TestCodeGen(Foo item, int iterations, ref double counter)
        {
            var time = Loop(item, get => FooMapper.Map(get), iterations);
            Console.WriteLine("Codegen:\t\t" + time);
            counter += time;
        }

        static long Loop<T>(T item, Action<T> action, int iterations = 1000000)
        {
            return Time(item, a =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    action(a);
                }
            });
        }

        static long Time<T>(T item, Action<T> action)
        {
            var sw = new Stopwatch();
            sw.Start();
            action(item);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }


        #region Data

        static Customer GetCustomer()
        {
            Customer c = new Customer()
            {
                Address = new Address() { City = "istanbul", Country = "turkey", Id = 1, Street = "istiklal cad." },
                HomeAddress = new Address() { City = "istanbul", Country = "turkey", Id = 2, Street = "istiklal cad." },
                Id = 1,
                Name = "Eduardo Najera",
                Credit = 234.7m,
                WorkAddresses = new List<Address>()
                {
                    new Address() {City = "istanbul", Country = "turkey", Id = 5, Street = "istiklal cad."},
                    new Address() {City = "izmir", Country = "turkey", Id = 6, Street = "konak"}
                },
                Addresses = new List<Address>()
                {
                    new Address() {City = "istanbul", Country = "turkey", Id = 3, Street = "istiklal cad."},
                    new Address() {City = "izmir", Country = "turkey", Id = 4, Street = "konak"}
                }.ToArray()
            };

            return c;
        }

        static Foo GetFoo()
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
