using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;


namespace Mapster.Tests
{
    [TestFixture]
    public class WhenUsingIgnoreCaseSetting
    {
        [Test]
        public void MapWithCaseSensitiveAsDefault()
        {
            var a = new ClassA()
            {
                Value_A = 123,
                VALUE_B = "abc"
            };
            TypeAdapterConfig<ClassA, ClassB>.NewConfig();
            var b = a.Adapt<ClassB>();
            Assert.AreEqual(a.Value_A, b.Value_A);
            Assert.AreNotEqual(a.VALUE_B, b.Value_B);
        }

        [Test]
        public void MapWithCaseInSensitive()
        {
            var a = new ClassA()
            {
                Value_A = 123,
                VALUE_B = "abc"
            };

            TypeAdapterConfig<ClassA, ClassB>.NewConfig().Settings.IgnoreCaseSensitiveNames = true;
            var b = a.Adapt<ClassB>();
            Assert.AreEqual(a.Value_A, b.Value_A);
            Assert.AreEqual(a.VALUE_B, b.Value_B);
        }
    }

    public class ClassA
    {
        public int Value_A { get; set; }
        public string VALUE_B { get; set; }
    }
    public class ClassB
    {
        public int Value_A { get; set; }
        public string Value_B { get; set; }
    }
}
