using Benchmark.Classes;
using BenchmarkDotNet.Attributes;

namespace Benchmark.Benchmarks
{
    public class TestAll
    {
        private Foo fooInstance;
        private Customer customerInstance;

        [Params(1000, 10_000, 100_000, 1_000_000)]
        public int Iterations { get; set; }

        [Benchmark]
        public void MapsterTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(fooInstance, Iterations);
            TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(customerInstance, Iterations);
        }

        [Benchmark]
        public void CodegenTest()
        {
            TestAdaptHelper.TestCodeGen(fooInstance, Iterations);
            TestAdaptHelper.TestCodeGen(customerInstance, Iterations);
        }

        [Benchmark]
        public void ExpressMapperTest()
        {
            TestAdaptHelper.TestExpressMapper<Foo, Foo>(fooInstance, Iterations);
            TestAdaptHelper.TestExpressMapper<Customer, CustomerDTO>(customerInstance, Iterations);
        }

        [Benchmark]
        public void AutoMapperTest()
        {
            TestAdaptHelper.TestAutoMapper<Foo, Foo>(fooInstance, Iterations);
            TestAdaptHelper.TestAutoMapper<Customer, CustomerDTO>(customerInstance, Iterations);
        }

        [GlobalSetup]
        public void Setup()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            TestAdaptHelper.Configure(fooInstance);
            customerInstance = TestAdaptHelper.SetupCustomerInstance();
            TestAdaptHelper.Configure(customerInstance);
        }
    }
}