using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingStruct
    {
        class MyClassA
        {
            public MyStruct MyStruct { get; set; }
        }
        class MyClassB
        {
            public MyStruct MyStruct { get; set; }
        }

        struct MyStruct
        {
            public MyStruct(string prop) : this()
            {
                Property = prop;
            }

            public string Property { get; set; }
        }

        [TestMethod]
        public void TestMapping()
        {
            var a = new MyClassA();

            a.MyStruct = new MyStruct("A");

            var b = a.Adapt<MyClassB>();

            b.MyStruct.Property.ShouldBe("A");
        }

        [TestMethod]
        public void TestMappingToTarget()
        {
            var a = new MyClassA();
            var b = new MyClassB();

            a.MyStruct = new MyStruct("A");

            a.Adapt(b);

            b.MyStruct.Property.ShouldBe("A");
        }
    }
}
