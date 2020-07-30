using Benchmark.Classes;
using BenchmarkDotNet.Attributes;

namespace Benchmark.Benchmarks
{
    public class TestComplexTypes
    {
        private Customer _customerInstance;

        [Params(1000, 10_000, 100_000, 1_000_000)]
        public int Iterations { get; set; }

        [Benchmark]
        public void MapsterTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(_customerInstance, Iterations);
        }
        
        [Benchmark(Description = "Mapster 6.0.0 (Roslyn)")]
        public void RoslynTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(_customerInstance, Iterations);
        }

        [Benchmark(Description = "Mapster 6.0.0 (FEC)")]
        public void FecTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(_customerInstance, Iterations);
        }

        [Benchmark]
        public void CodegenTest()
        {
            TestAdaptHelper.TestCodeGen(_customerInstance, Iterations);
        }

        [Benchmark]
        public void ExpressMapperTest()
        {
            TestAdaptHelper.TestExpressMapper<Customer, CustomerDTO>(_customerInstance, Iterations);
        }

        [Benchmark]
        public void AutoMapperTest()
        {
            TestAdaptHelper.TestAutoMapper<Customer, CustomerDTO>(_customerInstance, Iterations);
        }

        [GlobalSetup(Target = nameof(MapsterTest))]
        public void SetupMapster()
        {
            _customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureMapster(_customerInstance, MapsterCompilerType.Default);
        }

        [GlobalSetup(Target = nameof(RoslynTest))]
        public void SetupRoslyn()
        {
            _customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureMapster(_customerInstance, MapsterCompilerType.Roslyn);
        }

        [GlobalSetup(Target = nameof(FecTest))]
        public void SetupFec()
        {
            _customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureMapster(_customerInstance, MapsterCompilerType.FEC);
        }

        [GlobalSetup(Target = nameof(CodegenTest))]
        public void SetupCodegen()
        {
            _customerInstance = TestAdaptHelper.SetupCustomerInstance();
            CustomerMapper.Map(_customerInstance);
        }

        [GlobalSetup(Target = nameof(ExpressMapperTest))]
        public void SetupExpressMapper()
        {
            _customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureExpressMapper(_customerInstance);
        }

        [GlobalSetup(Target = nameof(AutoMapperTest))]
        public void SetupAutoMapper()
        {
            _customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.ConfigureAutoMapper(_customerInstance);
        }
    }
}