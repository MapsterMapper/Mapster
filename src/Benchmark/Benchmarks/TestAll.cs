using Benchmark.Classes;
using BenchmarkDotNet.Attributes;

namespace Benchmark.Benchmarks
{
    public class TestAll
    {
        private Foo fooInstance;
        private Customer customerInstance;

        [Params(100_000)]//, 1_000_000)]
        public int Iterations { get; set; }

        [Benchmark(Description = "Mapster 5.0.0")]
        public void MapsterTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(fooInstance, Iterations);
            TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(customerInstance, Iterations);
        }

        [Benchmark(Description = "Mapster 5.0.0 (Roslyn)")]
        public void RoslynTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(fooInstance, Iterations);
            TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(customerInstance, Iterations);
        }

        [Benchmark(Description = "Mapster 5.0.0 (FEC)")]
        public void FecTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(fooInstance, Iterations);
            TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(customerInstance, Iterations);
        }

        [Benchmark(Description = "Mapster 5.0.0 (Codegen)")]
        public void CodegenTest()
        {
            TestAdaptHelper.TestCodeGen(fooInstance, Iterations);
            TestAdaptHelper.TestCodeGen(customerInstance, Iterations);
        }

        [Benchmark(Description = "ExpressMapper 1.9.1")]
        public void ExpressMapperTest()
        {
            TestAdaptHelper.TestExpressMapper<Foo, Foo>(fooInstance, Iterations);
            TestAdaptHelper.TestExpressMapper<Customer, CustomerDTO>(customerInstance, Iterations);
        }

        [Benchmark(Description = "AutoMapper 9.0.0")]
        public void AutoMapperTest()
        {
            TestAdaptHelper.TestAutoMapper<Foo, Foo>(fooInstance, Iterations);
            TestAdaptHelper.TestAutoMapper<Customer, CustomerDTO>(customerInstance, Iterations);
        }

        [GlobalSetup(Target = nameof(MapsterTest))]
        public void SetupMapster()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureMapster(fooInstance, MapsterCompilerType.Default);
            TestAdaptHelper.ConfigureMapster(customerInstance, MapsterCompilerType.Default);
        }

        [GlobalSetup(Target = nameof(RoslynTest))]
        public void SetupRoslyn()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureMapster(fooInstance, MapsterCompilerType.Roslyn);
            TestAdaptHelper.ConfigureMapster(customerInstance, MapsterCompilerType.Roslyn);
        }

        [GlobalSetup(Target = nameof(FecTest))]
        public void SetupFec()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureMapster(fooInstance, MapsterCompilerType.FEC);
            TestAdaptHelper.ConfigureMapster(customerInstance, MapsterCompilerType.FEC);
        }

        [GlobalSetup(Target = nameof(CodegenTest))]
        public void SetupCodegen()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            customerInstance = TestAdaptHelper.SetupCustomerInstance();
            FooMapper.Map(fooInstance);
            CustomerMapper.Map(customerInstance);
        }

        [GlobalSetup(Target = nameof(ExpressMapperTest))]
        public void SetupExpressMapper()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureExpressMapper(fooInstance);
            TestAdaptHelper.ConfigureExpressMapper(customerInstance);
        }

        [GlobalSetup(Target = nameof(AutoMapperTest))]
        public void SetupAutoMapper()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureAutoMapper(fooInstance);
            TestAdaptHelper.ConfigureAutoMapper(customerInstance);
        }

    }
}