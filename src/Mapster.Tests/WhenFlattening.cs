using NUnit.Framework;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Mapster.Tests
{
    #region TestObject

    public class A
    { 
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal GetTotal()
        {
            return X + Y;
        }
    }

    public class B
    {
        public decimal Total { get; set; }
    }

    public class C
    {
        public B BClass { get; set; }
    }

    public class D
    {
        public decimal BClassTotal { get; set; }
    }

    public class E
    {
        public decimal BClass_Total { get; set; }
    }

    #endregion

    [TestFixture]
    public class WhenFlattening
    {
        [Test]
        public void GetMethodTest()
        {
            var b = TypeAdapter.Adapt<A, B>(new A { X = 100, Y = 50 });
            
            Assert.IsNotNull(b);
            Assert.IsTrue(b.Total == 150);
        }

        [Test]
        public void PropertyTest()
        {
            var d = TypeAdapter.Adapt<C, D>(new C { BClass = new B { Total = 250 } });

            Assert.IsNotNull(d);
            Assert.IsTrue(d.BClassTotal == 250);
        }

        [Test]
        public void PropertyTest_NameWithUnderscore()
        {
            var e = TypeAdapter.Adapt<C, E>(new C { BClass = new B { Total = 250 } });

            Assert.IsNotNull(e);
            Assert.IsTrue(e.BClass_Total == 250);
        }
    }
}
