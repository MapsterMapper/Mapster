using Mapster.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

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

        public interface IComplexBaseInterface
        {
            int Id { get; set; }
            void BaseMethod();
        }

        public interface IComplexInterface : IComplexBaseInterface
        {
            string Name { get; set; }
            int ReadOnlyProp { get; }
            int WriteOnlyProp { set; }
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
            instance.ShouldBeAssignableTo<ISimpleInterface>();

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
            instance.ShouldBeAssignableTo<IInheritedInterface>();

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
            instance.ShouldBeAssignableTo<IComplexInterface>();

            instance.Id = 42;
            instance.Name = "Lorem ipsum";
            instance.WriteOnlyProp = 24;

            instance.Id.ShouldBe(42);
            instance.Name.ShouldBe("Lorem ipsum");
            instance.ReadOnlyProp.ShouldBe(0);

            int i = 0;
            string s = null;
            Should.Throw<NotImplementedException>(() => instance.BaseMethod(), "Call BaseMethod.");
            Should.Throw<NotImplementedException>(() => instance.SimpleMethod(), "Call SimpleMethod.");
            Should.Throw<NotImplementedException>(() => instance.ComplexMethod(123, ref i, out s), "Call ComplexMethod.");
        }
    }
}
