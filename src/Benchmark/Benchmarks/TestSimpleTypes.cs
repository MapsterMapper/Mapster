using Benchmark.Classes;
using BenchmarkDotNet.Attributes;

namespace Benchmark.Benchmarks
{
    public class TestSimpleTypes
    {
        private Foo _fooInstance;

        [Params(1000, 10_000, 100_000, 1_000_000)]
        public int Iterations { get; set; }

        [Benchmark]
        public void MapsterTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(_fooInstance, Iterations);
        }
        
        [Benchmark(Description = "Mapster 6.0.0 (Roslyn)")]
        public void RoslynTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(_fooInstance, Iterations);
        }

        [Benchmark(Description = "Mapster 6.0.0 (FEC)")]
        public void FecTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(_fooInstance, Iterations);
        }

        [Benchmark]
        public void CodegenTest()
        {
            TestAdaptHelper.TestCodeGen(_fooInstance, Iterations);
        }

        [Benchmark]
        public void ExpressMapperTest()
        {
            TestAdaptHelper.TestExpressMapper<Foo, Foo>(_fooInstance, Iterations);
        }

        [Benchmark]
        public void AutoMapperTest()
        {
            TestAdaptHelper.TestAutoMapper<Foo, Foo>(_fooInstance, Iterations);
        }


        [GlobalSetup(Target = nameof(MapsterTest))]
        public void SetupMapster()
        {
            _fooInstance = TestAdaptHelper.SetupFooInstance();
            TestAdaptHelper.ConfigureMapster(_fooInstance, MapsterCompilerType.Default);
        }

        [GlobalSetup(Target = nameof(RoslynTest))]
        public void SetupRoslyn()
        {
            _fooInstance = TestAdaptHelper.SetupFooInstance();
            TestAdaptHelper.ConfigureMapster(_fooInstance, MapsterCompilerType.Roslyn);
        }

        [GlobalSetup(Target = nameof(FecTest))]
        public void SetupFec()
        {
            _fooInstance = TestAdaptHelper.SetupFooInstance();
            TestAdaptHelper.ConfigureMapster(_fooInstance, MapsterCompilerType.FEC);
        }

        [GlobalSetup(Target = nameof(CodegenTest))]
        public void SetupCodegen()
        {
            _fooInstance = TestAdaptHelper.SetupFooInstance();
            FooMapper.Map(_fooInstance);
        }

        [GlobalSetup(Target = nameof(ExpressMapperTest))]
        public void SetupExpressMapper()
        {
            _fooInstance = TestAdaptHelper.SetupFooInstance();
            TestAdaptHelper.ConfigureExpressMapper(_fooInstance);
        }

        [GlobalSetup(Target = nameof(AutoMapperTest))]
        public void SetupAutoMapper()
        {
            _fooInstance = TestAdaptHelper.SetupFooInstance();
            TestAdaptHelper.ConfigureAutoMapper(_fooInstance);
        }
    }
}