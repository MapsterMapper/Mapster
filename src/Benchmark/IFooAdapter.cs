using Benchmark.Classes;
using Mapster;

namespace Benchmark
{
    [Mapper]
    public interface IFooAdapter
    {
        Foo Adapt(Foo foo);
    }
}