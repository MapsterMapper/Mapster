using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenSettingMapToTargetWith
    {
        [TestMethod]
        public void MapToTargetWith_Should_Work_With_Adapt_To_Target()
        {
            var a = new Foo { A = 1 };
            var b = new Bar { A = 2 };

            //This will not work as expected => b.A will be 1, ignoring the mapping defined
            var config = new TypeAdapterConfig();
            config.NewConfig<double, double>().MapToTargetWith((x, y) => 5);
            a.Adapt(b, config);
            b.A.ShouldBe(5);
        }

        internal class Foo
        {
            public double A { get; set; }
        }

        internal class Bar
        {
            public double A { get; set; }
        }


    }
}
