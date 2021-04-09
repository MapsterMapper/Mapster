using Mapster.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;

namespace Mapster.Tests
{
    [TestClass]
    public class DynamicTypeGeneratorTests
    {
        private interface INotVisibleInterface
        {
            int Id { get; set; }
            string Name { get; set; }
        }

        public interface ISimpleInterface
        {
            int Id { get; set; }
            string Name { get; set; }
        }

        public interface IInheritedInterface : ISimpleInterface
        {
            int Value { get; set; }
        }

        public class Foo
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public interface IComplexBaseInterface
        {
            int Id { get; set; }
            Foo Foo { get; set; }
            void BaseMethod();
        }

        public interface IComplexInterface : IComplexBaseInterface
        {
            string Name { get; set; }
            int? NullableInt { get; set; }
            IEnumerable<int> Ints { get; set; }
            IEnumerable<Foo> Foos { get; set; }
            int[] IntArray { get; set; }
            Foo[] FooArray { get; set; }
            void SimpleMethod();
            int ComplexMethod(byte b, ref int i, out string s);
        }

        [TestMethod]
        public void ThrowExceptionWhenInterfaceIsNotVisible()
        {
            void action() => DynamicTypeGenerator.GetTypeForInterface(typeof(INotVisibleInterface));

            var ex = Should.Throw<InvalidOperationException>(action);
            ex.Message.ShouldContain("not accessible");
        }

        [TestMethod]
        public void CreateTypeForSimpleInterface()
        {
            Type iClass = DynamicTypeGenerator.GetTypeForInterface(typeof(ISimpleInterface));

            ISimpleInterface instance = (ISimpleInterface)Activator.CreateInstance(iClass);

            instance.ShouldNotBeNull();

            instance.Id = 42;
            instance.Name = "Lorem ipsum";

            instance.Id.ShouldBe(42);
            instance.Name.ShouldBe("Lorem ipsum");
        }

        [TestMethod]
        public void CreateTypeForInheritedInterface()
        {
            Type iClass = DynamicTypeGenerator.GetTypeForInterface(typeof(IInheritedInterface));

            IInheritedInterface instance = (IInheritedInterface)Activator.CreateInstance(iClass);

            instance.ShouldNotBeNull();

            instance.Id = 42;
            instance.Name = "Lorem ipsum";
            instance.Value = 24;

            instance.Id.ShouldBe(42);
            instance.Name.ShouldBe("Lorem ipsum");
            instance.Value.ShouldBe(24);
        }

        [TestMethod]
        public void CreateTypeForComplexInterface()
        {
            Type iClass = DynamicTypeGenerator.GetTypeForInterface(typeof(IComplexInterface));

            IComplexInterface instance = (IComplexInterface)Activator.CreateInstance(iClass);

            instance.ShouldNotBeNull();

            instance.Id = 42;
            instance.Foo = new Foo();
            instance.NullableInt = 123;
            instance.Name = "Lorem ipsum";
            instance.Ints = new List<int>();
            instance.IntArray = new int[2];
            instance.Foos = new List<Foo>();
            instance.FooArray = new Foo[2];

            instance.Id.ShouldBe(42);
            instance.Foo.ShouldNotBeNull();
            instance.NullableInt.ShouldBe(123);
            instance.Name.ShouldBe("Lorem ipsum");

            int i = 0;
            string s = null;
            Should.Throw<NotImplementedException>(() => instance.BaseMethod(), "Call BaseMethod.");
            Should.Throw<NotImplementedException>(() => instance.SimpleMethod(), "Call SimpleMethod.");
            Should.Throw<NotImplementedException>(() => instance.ComplexMethod(123, ref i, out s), "Call ComplexMethod.");
        }

        [TestMethod]
        public void WhenMappingDerivedFromInterfaceWithoutMembers()
        {
            //Arrange
            var config = TypeAdapterConfig<ISource, IDestination>.NewConfig()
                .Include<Source, Destination>(); // and possibly many more

            //Act && Assert
            Should.NotThrow(() => config.Compile());
        }

        public interface ISource
        {
        }

        public class Source : ISource
        {
            public string Tag { get; set; }
        }

        public interface IDestination
        {
        }

        public class Destination : IDestination
        {
            public string Tag { get; set; }
        }
    }
}
