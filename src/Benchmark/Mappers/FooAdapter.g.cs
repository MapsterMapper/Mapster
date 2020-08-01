
using System;
using System.Linq;
using Benchmark;
using Benchmark.Classes;
using Mapster;
using Mapster.Utils;


namespace Benchmark
{
    public partial class FooAdapter : IFooAdapter
    {
        public Foo Adapt(Foo p1)
        {
            return p1 == null ? null : new Foo()
            {
                Name = p1.Name,
                Int32 = p1.Int32,
                Int64 = p1.Int64,
                NullInt = p1.NullInt,
                Floatn = p1.Floatn,
                Doublen = p1.Doublen,
                DateTime = p1.DateTime,
                Foo1 = TypeAdapter<Foo, Foo>.Map.Invoke(p1.Foo1),
                Foos = p1.Foos == null ? null : p1.Foos.Select<Foo, Foo>(funcMain1),
                FooArr = funcMain2(p1.FooArr),
                IntArr = funcMain3(p1.IntArr),
                Ints = p1.Ints == null ? null : MapsterHelper.ToEnumerable<int>(p1.Ints)
            };
        }
        
        private Foo funcMain1(Foo p2)
        {
            return TypeAdapter<Foo, Foo>.Map.Invoke(p2);
        }
        
        private Foo[] funcMain2(Foo[] p3)
        {
            if (p3 == null)
            {
                return null;
            }
            Foo[] result = new Foo[p3.Length];
            
            int v = 0;
            
            int i = 0;
            int len = p3.Length;
            
            while (i < len)
            {
                Foo item = p3[i];
                result[v++] = TypeAdapter<Foo, Foo>.Map.Invoke(item);
                i++;
            }
            return result;
            
        }
        
        private int[] funcMain3(int[] p4)
        {
            if (p4 == null)
            {
                return null;
            }
            int[] result = new int[p4.Length];
            Array.Copy(p4, 0, result, 0, p4.Length);
            return result;
            
        }
    }
}