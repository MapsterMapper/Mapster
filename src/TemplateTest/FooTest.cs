using ExpressionDebugger;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace TemplateTest
{
    [TestClass]
    public class FooTest
    {
        [TestMethod]
        public void TestCreateMapExpression()
        {
            TypeAdapterConfig.GlobalSettings.SelfContainedCodeGeneration = true;
            var foo = default(Foo);
            var def = new ExpressionDefinitions
            {
                IsStatic = true,
                MethodName = "Map",
                Namespace = "Benchmark",
                TypeName = "FooMapper"
            };
            var code = foo.BuildAdapter()
                .CreateMapExpression<Foo>()
                .ToScript(def);
            code = code.Replace("TypeAdapter<Foo, Foo>.Map.Invoke", "Map");

            Assert.IsNotNull(code);
        }
    }

    public class Foo
    {
        public string Name { get; set; }

        public int Int32 { get; set; }

        public long Int64 { set; get; }

        public int? NullInt { get; set; }

        public float Floatn { get; set; }

        public double Doublen { get; set; }

        public DateTime DateTime { get; set; }

        public Foo Foo1 { get; set; }

        public IEnumerable<Foo> Foos { get; set; }

        public Foo[] FooArr { get; set; }

        public int[] IntArr { get; set; }

        public IEnumerable<int> Ints { get; set; }
    }
}