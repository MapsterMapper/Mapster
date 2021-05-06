using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AutoMapper;
using Benchmark.Classes;
using FastExpressionCompiler;
using Mapster;

namespace Benchmark
{
    public static class TestAdaptHelper
    {
        private static readonly IMapper _mapper = new Mapper(new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Foo, Foo>();
            cfg.CreateMap<Address, Address>();
            cfg.CreateMap<Address, AddressDTO>();
            cfg.CreateMap<Customer, CustomerDTO>();
        }));

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

        private static readonly Func<LambdaExpression, Delegate> _defaultCompiler = TypeAdapterConfig.GlobalSettings.Compiler;

        private static void SetupCompiler(MapsterCompilerType type)
        {
            TypeAdapterConfig.GlobalSettings.Compiler = type switch
            {
                MapsterCompilerType.Default => _defaultCompiler,
                MapsterCompilerType.Roslyn => exp => exp.CompileWithDebugInfo(),
                MapsterCompilerType.FEC => exp => exp.CompileFast(),
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };
        }
        public static void ConfigureMapster(Foo fooInstance, MapsterCompilerType type)
        {
            SetupCompiler(type);
            TypeAdapterConfig.GlobalSettings.Compile(typeof(Foo), typeof(Foo)); //recompile
            fooInstance.Adapt<Foo, Foo>(); //exercise
        }
        public static void ConfigureExpressMapper(Foo fooInstance)
        {
            ExpressMapper.Mapper.Map<Foo, Foo>(fooInstance); //exercise
        }
        public static void ConfigureAutoMapper(Foo fooInstance)
        {
            _mapper.Map<Foo, Foo>(fooInstance); //exercise
        }

        public static void ConfigureMapster(Customer customerInstance, MapsterCompilerType type)
        {
            SetupCompiler(type);
            TypeAdapterConfig.GlobalSettings.Compile(typeof(Customer), typeof(CustomerDTO));    //recompile
            customerInstance.Adapt<Customer, CustomerDTO>();    //exercise
        }
        public static void ConfigureExpressMapper(Customer customerInstance)
        {
            ExpressMapper.Mapper.Map<Customer, CustomerDTO>(customerInstance);  //exercise
        }
        public static void ConfigureAutoMapper(Customer customerInstance)
        {
            _mapper.Map<Customer, CustomerDTO>(customerInstance);    //exercise
        }

        public static void TestMapsterAdapter<TSrc, TDest>(TSrc item, int iterations)
            where TSrc : class
            where TDest : class, new()
        {
            Loop(item, get => get.Adapt<TSrc, TDest>(), iterations);
        }

        public static void TestExpressMapper<TSrc, TDest>(TSrc item, int iterations)
            where TSrc : class
            where TDest : class, new()
        {
            Loop(item, get => ExpressMapper.Mapper.Map<TSrc, TDest>(get), iterations);
        }

        public static void TestAutoMapper<TSrc, TDest>(TSrc item, int iterations)
            where TSrc : class
            where TDest : class, new()
        {
            Loop(item, get => _mapper.Map<TSrc, TDest>(get), iterations);
        }

        public static void TestCodeGen(Foo item, int iterations)
        {
            Loop(item, get => FooMapper.Map(get), iterations);
        }

        public static void TestCodeGen(Customer item, int iterations)
        {
            Loop(item, get => CustomerMapper.Map(get), iterations);
        }

        private static void Loop<T>(T item, Action<T> action, int iterations)
        {
            for (var i = 0; i < iterations; i++) action(item);
        }
    }

    public enum MapsterCompilerType
    {
        Default,
        Roslyn,
        FEC,
    }
}