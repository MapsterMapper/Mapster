using Benchmark.Classes;
using BenchmarkDotNet.Attributes;

namespace Benchmark.Benchmarks
{
    public class TestSimpleTypes
    {
        private Foo fooInstance;

        [Params(1000, 10_000, 100_000, 1_000_000)]
        public int Iterations { get; set; }

        [Benchmark]
        public void MapsterTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(fooInstance, Iterations);
        }
        
        [Benchmark(Description = "Mapster 5.0.0 (Roslyn)")]
        public void RoslynTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(fooInstance, Iterations);
        }

        [Benchmark(Description = "Mapster 5.0.0 (FEC)")]
        public void FecTest()
        {
            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(fooInstance, Iterations);
        }

        [Benchmark]
        public void CodegenTest()
        {
            TestAdaptHelper.TestCodeGen(fooInstance, Iterations);
        }

        [Benchmark]
        public void ExpressMapperTest()
        {
            TestAdaptHelper.TestExpressMapper<Foo, Foo>(fooInstance, Iterations);
        }

        [Benchmark]
        public void AutoMapperTest()
        {
            TestAdaptHelper.TestAutoMapper<Foo, Foo>(fooInstance, Iterations);
        }


        [GlobalSetup(Target = nameof(MapsterTest))]
        public void SetupMapster()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            TestAdaptHelper.ConfigureMapster(fooInstance, MapsterCompilerType.Default);
        }

        [GlobalSetup(Target = nameof(RoslynTest))]
        public void SetupRoslyn()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            TestAdaptHelper.ConfigureMapster(fooInstance, MapsterCompilerType.Roslyn);
        }

        [GlobalSetup(Target = nameof(FecTest))]
        public void SetupFec()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            TestAdaptHelper.ConfigureMapster(fooInstance, MapsterCompilerType.FEC);
        }

        [GlobalSetup(Target = nameof(CodegenTest))]
        public void SetupCodegen()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            FooMapper.Map(fooInstance);
        }

        [GlobalSetup(Target = nameof(ExpressMapperTest))]
        public void SetupExpressMapper()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            TestAdaptHelper.ConfigureExpressMapper(fooInstance);
        }

        [GlobalSetup(Target = nameof(AutoMapperTest))]
        public void SetupAutoMapper()
        {
            fooInstance = TestAdaptHelper.SetupFooInstance();
            TestAdaptHelper.ConfigureAutoMapper(fooInstance);
        }
    }
}