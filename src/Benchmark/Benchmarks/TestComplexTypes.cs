using Benchmark.Classes;
using BenchmarkDotNet.Attributes;

namespace Benchmark.Benchmarks
{
    public class TestComplexTypes
    {
        private Customer customerInstance;

        [Params(1000, 10_000, 100_000, 1_000_000)]
        public int Iterations { get; set; }

        [Benchmark]
        public void MapsterTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(customerInstance, Iterations);
        }

        [Benchmark]
        public void CodegenTest()
        {
            TestAdaptHelper.TestCodeGen(customerInstance, Iterations);
        }

        [Benchmark]
        public void ExpressMapperTest()
        {
            TestAdaptHelper.TestExpressMapper<Customer, CustomerDTO>(customerInstance, Iterations);
        }

        [Benchmark]
        public void AutoMapperTest()
        {
            TestAdaptHelper.TestAutoMapper<Customer, CustomerDTO>(customerInstance, Iterations);
        }

        [GlobalSetup]
        public void Setup()
        {
            customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.Configure(customerInstance);
        }
    }
}