using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingIEnumerableClasses
    {
        public class ClassA
        {
            public int Id { get; set; }
        }

        public class ClassB : IEnumerable
        {
            public int Id { get; set; }

            public IEnumerator GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void Map_To_IEnumerable_Class_Should_Pass()
        {
            ClassA classA = new ClassA()
            {
                Id = 123
            };

            ClassB classB = classA.Adapt<ClassB>();
            classB.Id.ShouldBe(classA.Id);
        }

    }
}
