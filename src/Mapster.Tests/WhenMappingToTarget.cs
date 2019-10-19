using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingToTarget
    {
        [TestMethod]
        public void MappingToTarget_With_SameList()
        {
            var a = new Foo { A = 1, List = new List<int> {1,2,3} };
            var b = new Bar { A = 2, List = a.List};

            a.Adapt(b);
            b.A.ShouldBe(1);
            b.List.ShouldBe(new List<int>{1,2,3});
        }

        internal class Foo
        {
            public double A { get; set; }
            public List<int> List { get; set; }
        }

        internal class Bar
        {
            public double A { get; set; }
            public List<int> List { get; set; }
        }
    }
}
