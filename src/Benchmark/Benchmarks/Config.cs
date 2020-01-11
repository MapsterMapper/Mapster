using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace Benchmark.Benchmarks
{
    public class Config : ManualConfig
    {
        public Config()
        {
            Add(ConsoleLogger.Default);

            Add(CsvExporter.Default);
            Add(MarkdownExporter.GitHub);
            Add(HtmlExporter.Default);

            Add(MemoryDiagnoser.Default);
            Add(TargetMethodColumn.Method);

            Add(StatisticColumn.Mean);
            Add(StatisticColumn.StdDev);
            Add(StatisticColumn.Error);

            Add(BaselineRatioColumn.RatioMean);
            Add(DefaultColumnProviders.Metrics);

            Add(Job.ShortRun
                .WithLaunchCount(1)
                .WithWarmupCount(2)
                .WithIterationCount(10)
            );

            Options |= ConfigOptions.JoinSummary;
        }
    }
}