using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
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

    public class F
    {
        public decimal? BClass_Total { get; set; }
    }

    public class ModelObject
    {
        public DateTime BaseDate { get; set; }

        public ModelSubObject Sub { get; set; }

        public ModelSubObject Sub2 { get; set; }

        public ModelSubObject SubWithExtraName { get; set; }
    }

    public class ModelSubObject
    {
        public string ProperName { get; set; }

        public ModelSubSubObject SubSub { get; set; }
    }

    public class ModelSubSubObject
    {
        public string CoolProperty { get; set; }
    }

    public class ModelDto
    {
        public DateTime BaseDate { get; set; }

        public string SubProperName { get; set; }

        public string Sub2ProperName { get; set; }

        public string SubWithExtraNameProperName { get; set; }

        public string SubSubSubCoolProperty { get; set; }
    }


    #endregion

    [TestClass]
    public class WhenFlattening
    {
        [TestMethod]
        public void GetMethodTest()
        {
            var b = TypeAdapter.Adapt<A, B>(new A { X = 100, Y = 50 });
            
            Assert.IsNotNull(b);
            Assert.IsTrue(b.Total == 150);
        }

        [TestMethod]
        public void PropertyTest()
        {
            var d = TypeAdapter.Adapt<C, D>(new C { BClass = new B { Total = 250 } });

            Assert.IsNotNull(d);
            Assert.IsTrue(d.BClassTotal == 250);
        }

        [TestMethod]
        public void PropertyTest_NameWithUnderscore()
        {
            var e = TypeAdapter.Adapt<C, E>(new C { BClass = new B { Total = 250 } });

            Assert.IsNotNull(e);
            Assert.IsTrue(e.BClass_Total == 250);
        }

        [TestMethod]
        public void PropertyTest_NullPropagation()
        {
            var f = TypeAdapter.Adapt<C, F>(new C { BClass = new B { Total = 250 } });

            Assert.IsNotNull(f);
            Assert.IsTrue(f.BClass_Total == 250);

            var f2 = TypeAdapter.Adapt<C, F>(new C { BClass = null });

            Assert.IsNotNull(f2);
            Assert.IsNull(f2.BClass_Total);
        }

        [TestMethod]
        public void ShouldUseNestedObjectPropertyMembers()
        {
            var src = new ModelObject
            {
                BaseDate = new DateTime(2007, 4, 5),
                Sub = new ModelSubObject
                {
                    ProperName = "Some name",
                    SubSub = new ModelSubSubObject
                    {
                        CoolProperty = "Cool daddy-o"
                    }
                },
                Sub2 = new ModelSubObject
                {
                    ProperName = "Sub 2 name"
                },
                SubWithExtraName = new ModelSubObject
                {
                    ProperName = "Some other name"
                },
            };
            var dest = src.Adapt<ModelDto>();

            dest.Sub2ProperName.ShouldBe("Sub 2 name");
            dest.SubWithExtraNameProperName.ShouldBe("Some other name");
        }

    }
}
