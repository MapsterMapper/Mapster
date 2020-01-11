using System;
using Benchmark.Benchmarks;
using Benchmark.Classes;
using BenchmarkDotNet.Running;

namespace Benchmark
{
    class Program
    {
        private static double sAM;
        private static double sMP;
        private static double sMF;
        private static double sEM;

        static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[]
            {
                typeof(TestSimpleTypes),
                typeof(TestComplexTypes)
            });

            switcher.Run(args, new Config());
            return;

            try
            {
                Foo fooInstance = TestAdaptHelper.SetupFooInstance();
                TestAdaptHelper.Configure(fooInstance);

                Customer customerInstance = TestAdaptHelper.SetupCustomerInstance();
                TestAdaptHelper.Configure(customerInstance);

                TestSimpleTypes(fooInstance);
                TestComplexTypes(customerInstance);

                Console.WriteLine();
                Console.WriteLine($"========================================================================================");
                Console.WriteLine($"| Tests                | Time (ms) | Slower than Mapster | Slower than Mapster Codegen |");
                Console.WriteLine($"========================================================================================");
                Console.WriteLine($"| Mapster v4.0         | {  sMP,9} | { sMP / sMP,18:N2}X |   {       sMP / sMF,24:N2}X |");
                Console.WriteLine($"| Mapster v4.0 Codegen | {  sMF,9} | { sMF / sMP,18:N2}X |   {       sMF / sMF,24:N2}X |");
                Console.WriteLine($"| Automapper v8.0.0    | {  sAM,9} | { sAM / sMP,18:N2}X |   {       sAM / sMF,24:N2}X |");
                Console.WriteLine($"| ExpressMapper v1.9.1 | {  sEM,9} | { sEM / sMP,18:N2}X |   {       sEM / sMF,24:N2}X |");
                Console.WriteLine($"========================================================================================");
                Console.WriteLine();
            }
            finally
            {
                Console.WriteLine("Finish");
                Console.ReadLine();
            }
        }

        static void TestSimpleTypes(Foo fooInstance)
        {
            Console.WriteLine("Test 1 : Simple Types");
            Console.WriteLine("Competitors : Mapster, ExpressMapper, AutoMapper");


            TestSimple(fooInstance, 1000);

            TestSimple(fooInstance, 10000);

            TestSimple(fooInstance, 100000);

            TestSimple(fooInstance, 1000000);

            //Console.WriteLine();
            //Console.WriteLine("Automapper to Mapster ratio: " + (AutomapperTime / MapsterTime).ToString("###.00") + " X slower");
            //Console.WriteLine("ExpressMapper to Mapster ratio: " + (ExpressMapperTime / MapsterTime).ToString("###.00") + " X slower");
            //Console.WriteLine();

        }

        static void TestComplexTypes(Customer customerInstance)
        {
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("Test 2 : Complex Types");
            Console.WriteLine("Competitors : Mapster, ExpressMapper, AutoMapper");

            //TypeAdapterConfig.GlobalSettings.DestinationTransforms.Upsert<Guid>(x => x);

            //ObjectMapperManager.DefaultInstance.GetMapper<Customer, CustomerDTO>().Map(customer);

            Test(customerInstance, 1000);

            Test(customerInstance, 10000);

            Test(customerInstance, 100000);

            Test(customerInstance, 1000000);

            Test(customerInstance, 10000000);

            //Console.WriteLine();
            //Console.WriteLine("Automapper to Mapster ratio: " + (AutomapperTime/MapsterTime).ToString("###.00") + " X slower");
            //Console.WriteLine("ExpressMapper to Mapster ratio: " + (ExpressMapperTime/MapsterTime).ToString("###.00") + " X slower");
            //Console.WriteLine();
        }

        static void Test(Customer item, int iterations)
        {
            Console.WriteLine();

            Console.WriteLine("Iterations : {0}", iterations);

            //TestCustomerNative(item, iterations);

            TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(item, iterations, ref sMP);

            //TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();    //switch compiler
            //TypeAdapterConfig.GlobalSettings.Compile(typeof(Customer), typeof(CustomerDTO));    //recompile
            //item.Adapt<Customer, CustomerDTO>();    //exercise
            //TestAdaptHelper.TestMapsterAdapter<Customer, CustomerDTO>(item, iterations, ref sMF);

            TestAdaptHelper.TestCodeGen(item, iterations, ref sMF);

            TestAdaptHelper.TestExpressMapper<Customer, CustomerDTO>(item, iterations, ref sEM);

            TestAdaptHelper.TestAutoMapper<Customer, CustomerDTO>(item, iterations, ref sAM);
        }

        static void TestSimple(Foo item, int iterations)
        {
            Console.WriteLine();

            Console.WriteLine("Iterations : {0}", iterations);

            TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(item, iterations, ref sMP);

            //TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();    //switch compiler
            //TypeAdapterConfig.GlobalSettings.Compile(typeof(Foo), typeof(Foo));    //recompile
            //item.Adapt<Foo, Foo>(); //exercise
            //TestAdaptHelper.TestMapsterAdapter<Foo, Foo>(item, iterations, ref sMF);

            TestAdaptHelper.TestCodeGen(item, iterations, ref sMF);

            TestAdaptHelper.TestExpressMapper<Foo, Foo>(item, iterations, ref sEM);

            TestAdaptHelper.TestAutoMapper<Foo, Foo>(item, iterations, ref sAM);
        }
    }
}
