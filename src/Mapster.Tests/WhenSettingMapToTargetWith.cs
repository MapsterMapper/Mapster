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

            var config = new TypeAdapterConfig();
            config.Default.ShallowCopyForSameType(true);

            config.NewConfig<double, double>().MapToTargetWith((x, y) => 5);
            a.Adapt(b, config);
            b.A.ShouldBe(5);
        }

        [TestMethod]
        public void MapWith_Should_Work_With_Adapt_To_Target()
        {
            var a = new List<double> {1, 2, 3};

            var config = new TypeAdapterConfig();
            config.Default.ShallowCopyForSameType(true);

            config.NewConfig<double, double>().MapWith(_ => 5);
            var b = a.Adapt<List<double>>(config);
            b.ShouldBe(new List<double>{ 5, 5, 5});
        }

        [TestMethod]
        public void MapWith_Should_Work_With_Adapt_To_Target_For_Collection()
        {
            var a = new List<double> {1, 2, 3};

            var config = new TypeAdapterConfig();
            config.Default.ShallowCopyForSameType(true);

            config.NewConfig<List<double>, List<double>>().MapWith(_ => new List<double>{ 5, 5, 5});
            var b = a.Adapt<List<double>>(config);
            b.ShouldBe(new List<double>{ 5, 5, 5});
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
