using Benchmark.Classes;
using BenchmarkDotNet.Attributes;

namespace Benchmark.Benchmarks
{
    public class TestAll
    {
        private Foo _fooInstance;
        private Customer _customerInstance;

        [Params(100_000)]//, 1_000_000)]
        public int Iterations { get; set; }

        [Benchmark(Description = "Mapster 7.2.0")]
        public void MapsterTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(_fooInstance, Iterations);
            TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(_customerInstance, Iterations);
        }

        [Benchmark(Description = "Mapster 7.2.0 (Roslyn)")]
        public void RoslynTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(_fooInstance, Iterations);
            TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(_customerInstance, Iterations);
        }

        [Benchmark(Description = "Mapster 7.2.0 (FEC)")]
        public void FecTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(_fooInstance, Iterations);
            TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(_customerInstance, Iterations);
        }

        [Benchmark(Description = "Mapster 7.2.0 (Codegen)")]
        public void CodegenTest()
        {
            TestAdaptHelper.TestCodeGen(_fooInstance, Iterations);
            TestAdaptHelper.TestCodeGen(_customerInstance, Iterations);
        }

        [Benchmark(Description = "ExpressMapper 1.9.1")]
        public void ExpressMapperTest()
        {
            TestAdaptHelper.TestExpressMapper<Foo, Foo>(_fooInstance, Iterations);
            TestAdaptHelper.TestExpressMapper<Customer, CustomerDTO>(_customerInstance, Iterations);
        }

        [Benchmark(Description = "AutoMapper 10.1.1")]
        public void AutoMapperTest()
        {
            TestAdaptHelper.TestAutoMapper<Foo, Foo>(_fooInstance, Iterations);
            TestAdaptHelper.TestAutoMapper<Customer, CustomerDTO>(_customerInstance, Iterations);
        }

        [GlobalSetup(Target = nameof(MapsterTest))]
        public void SetupMapster()
        {
            _fooInstance = TestAdaptHelper.SetupFooInstance();
            _customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureMapster(_fooInstance, MapsterCompilerType.Default);
            TestAdaptHelper.ConfigureMapster(_customerInstance, MapsterCompilerType.Default);
        }

        [GlobalSetup(Target = nameof(RoslynTest))]
        public void SetupRoslyn()
        {
            _fooInstance = TestAdaptHelper.SetupFooInstance();
            _customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureMapster(_fooInstance, MapsterCompilerType.Roslyn);
            TestAdaptHelper.ConfigureMapster(_customerInstance, MapsterCompilerType.Roslyn);
        }

        [GlobalSetup(Target = nameof(FecTest))]
        public void SetupFec()
        {
            _fooInstance = TestAdaptHelper.SetupFooInstance();
            _customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureMapster(_fooInstance, MapsterCompilerType.FEC);
            TestAdaptHelper.ConfigureMapster(_customerInstance, MapsterCompilerType.FEC);
        }

        [GlobalSetup(Target = nameof(CodegenTest))]
        public void SetupCodegen()
        {
            _fooInstance = TestAdaptHelper.SetupFooInstance();
            _customerInstance = TestAdaptHelper.SetupCustomerInstance();
            FooMapper.Map(_fooInstance);
            CustomerMapper.Map(_customerInstance);
        }

        [GlobalSetup(Target = nameof(ExpressMapperTest))]
        public void SetupExpressMapper()
        {
            _fooInstance = TestAdaptHelper.SetupFooInstance();
            _customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureExpressMapper(_fooInstance);
            TestAdaptHelper.ConfigureExpressMapper(_customerInstance);
        }

        [GlobalSetup(Target = nameof(AutoMapperTest))]
        public void SetupAutoMapper()
        {
            _fooInstance = TestAdaptHelper.SetupFooInstance();
            _customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureAutoMapper(_fooInstance);
            TestAdaptHelper.ConfigureAutoMapper(_customerInstance);
        }

    }
}