using Benchmark.Benchmarks;
using BenchmarkDotNet.Running;

namespace Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[]
            {
                typeof(TestSimpleTypes),
                typeof(TestComplexTypes),
                typeof(TestAll),
            });

            switcher.Run(args, new Config());
        }
    }
}
