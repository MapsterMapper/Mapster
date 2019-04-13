
using System;
using System.Linq;
using Benchmark.Classes;
using Mapster;
using Mapster.Utils;


namespace Benchmark
{
    public static partial class FooMapper
    {
        public static Foo Map(Foo p1)
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
                Foo1 = Map(p1.Foo1),
                Foos = p1.Foos == null ? null : p1.Foos.Select<Foo, Foo>(func1),
                FooArr = func2(p1.FooArr),
                IntArr = func3(p1.IntArr),
                Ints = p1.Ints == null ? null : MapsterHelper.ToEnumerable<int>(p1.Ints)
            };
        }
        
        private static Foo func1(Foo p2)
        {
            return Map(p2);
        }
        
        private static Foo[] func2(Foo[] p3)
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
                result[v++] = Map(item);
                i++;
            }
            return result;
            
        }
        
        private static int[] func3(int[] p4)
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
