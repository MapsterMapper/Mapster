using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionDebugger.Tests
{
    [TestClass]
    public class DebugInfoInjectorTest
    {
        [TestMethod]
        public void TestBinary()
        {
            Expression<Func<int, int, int>> fn = (a, b) => a + b;
            var str = fn.ToScript();
            Assert.AreEqual(@"
public int Main(int a, int b)
{
    return a + b;
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestBinary_PowerAssign()
        {
            var exp = Expression.PowerAssign(Expression.Variable(typeof(double), "d"), Expression.Constant(2d));
            var str = exp.ToScript();
            Assert.AreEqual("d = Math.Pow(d, 2d)", str);
        }

        [TestMethod]
        public void TestBinary_ArrayIndex()
        {
            Expression<Func<int[], int>> fn = a => a[0];
            var str = fn.ToScript();
            Assert.AreEqual(@"
public int Main(int[] a)
{
    return a[0];
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestBlock()
        {
            var p1 = Expression.Variable(typeof(int));
            var block = Expression.Block(new[] { p1 }, Expression.Add(p1, p1));
            var str = block.ToScript();
            Assert.AreEqual(@"
int p1;

p1 + p1;".Trim(), str);
        }

        [TestMethod]
        public void Test_Conditional()
        {
            Expression<Func<int, int>> fn = a => a < 0 ? a - 1 : a + 1;
            var str = fn.ToScript();
            Assert.AreEqual(@"
public int Main(int a)
{
    return a < 0 ? a - 1 : a + 1;
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestConditional_Block()
        {
            var exp = Expression.Condition(
                Expression.Equal(Expression.Constant(1), Expression.Constant(2)),
                Expression.Constant(4),
                Expression.Constant(3),
                typeof(void));
            var str = exp.ToScript();
            Assert.AreEqual(@"
if (1 == 2)
{
    4;
}
else
{
    3;
}".Trim()
            , str);
        }

        [TestMethod]
        public void TestConditional_Block_Chain()
        {
            var exp = Expression.Condition(
                Expression.Equal(Expression.Constant(1), Expression.Constant(2)),
                Expression.Constant(4),
                Expression.Condition(
                    Expression.Equal(Expression.Constant(5), Expression.Constant(6)),
                    Expression.Constant(3),
                    Expression.Constant(2),
                    typeof(void)),
                typeof(void));
            var str = exp.ToScript();
            Assert.AreEqual(@"
if (1 == 2)
{
    4;
}
else if (5 == 6)
{
    3;
}
else
{
    2;
}".Trim()
            , str);
        }

        [TestMethod]
        public void TestConstants()
        {
            Expression<Func<string, char>> fn = s => s == "x" || s == @"\" || s == null || s.IsNormalized() == false || s.GetType() == typeof(string) ? 'x' : s[0];
            var str = fn.ToScript();
            Assert.AreEqual(@"
public char Main(string s)
{
    return s == ""x"" || s == @""\"" || s == null || s.IsNormalized() == false || s.GetType() == typeof(string) ? 'x' : s[0];
}".Trim()
                , str);

            Expression<Func<string>> fn2 = () => 1f.ToString() + 2m.ToString() + ((byte)1).ToString() + DayOfWeek.Friday.ToString() + default(DateTime).ToString();
            var str2 = fn2.ToScript();
            Assert.AreEqual(@"
public string Main()
{
    return 1f.ToString() + 2m.ToString() + ((byte)1).ToString() + DayOfWeek.Friday.ToString() + default(DateTime).ToString();
}".Trim()
                , str2);
        }

        [TestMethod]
        public void TestConstant_DateTime()
        {
            var now = DateTime.Now;
            var expr = Expression.Constant(now);
            var script = expr.ToScript();
            Assert.AreEqual(@"
private DateTime DateTime1;
DateTime1".Trim(), script);
        }

//        [TestMethod]
//        public void TestDynamic()
//        {
//            var dynType = typeof(ExpandoObject);
//            var p1 = Expression.Variable(dynType);
//            var line1 = Expression.Dynamic(Binder.Convert(CSharpBinderFlags.None, typeof(Poco), dynType), typeof(object), p1);
//            var line2 = Expression.Dynamic(Binder.GetMember(CSharpBinderFlags.None, "Blah", dynType,
//                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) }), typeof(object), p1);
//            var line3 = Expression.Dynamic(Binder.SetMember(CSharpBinderFlags.None, "Blah", dynType,
//                new[]
//                {
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                }), typeof(object), p1, Expression.Constant(0));
//            var line4 = Expression.Dynamic(Binder.GetIndex(CSharpBinderFlags.None, dynType,
//                new[]
//                {
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                }), typeof(object), p1, Expression.Constant(1));
//            var line5 = Expression.Dynamic(Binder.SetIndex(CSharpBinderFlags.None, dynType,
//                new[]
//                {
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                }), typeof(object), p1, Expression.Constant(1), Expression.Constant(0));
//            var line6 = Expression.Dynamic(Binder.InvokeMember(CSharpBinderFlags.None, "Blah", null, dynType,
//                new[]
//                {
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                }), typeof(object), p1, Expression.Constant(2));
//            var line7 = Expression.Dynamic(Binder.Invoke(CSharpBinderFlags.None, dynType,
//                new[]
//                {
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                }), typeof(object), p1, Expression.Constant(2));
//            var line8 = Expression.Dynamic(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.Negate, dynType,
//                new[]
//                {
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                }), typeof(object), p1);
//            var line9 = Expression.Dynamic(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.Add, dynType,
//                new[]
//                {
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
//                }), typeof(object), p1, Expression.Constant(3));
//            var expr = Expression.Block(new[] { p1 }, line1, line2, line3, line4, line5, line6, line7, line8, line9);
//            var str = expr.ToScript();
//            Assert.AreEqual(@"
//dynamic p1;

//(Poco)p1;
//p1.Blah;
//p1.Blah = 0;
//p1[1];
//p1[1] = 0;
//p1.Blah(2);
//p1(2);
//-p1;
//p1 + 3;"
//                , str);
//        }

        [TestMethod]
        public void TestDefault()
        {
            var exp = Expression.Default(typeof(int));
            var str = exp.ToScript();
            Assert.AreEqual("default(int)", str);
        }

        [TestMethod]
        public void TestGroup()
        {
            Expression<Func<int, int>> fn = x => -(-x) + 1 + x - (1 - x);
            var str = fn.ToScript();
            Assert.AreEqual(@"
public int Main(int x)
{
    return -(-x) + 1 + x - (1 - x);
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestGroup_MultiLine()
        {
            var p = Expression.Variable(typeof(int), "p");
            var exp = Expression.Add(
                p,
                Expression.Block(
                new Expression[] {
                    Expression.Call(typeof(Console).GetMethod(nameof(Console.WriteLine), new [] { typeof(int) }), p),
                    p,
                }
            ));
            var str = exp.ToScript();
            Assert.AreEqual(@"p + (new Func<int>(() => {
    Console.WriteLine(p);
    return p;
}))()"
                , str);
        }

        [TestMethod]
        public void TestIndex()
        {
            var p1 = Expression.Parameter(typeof(int[]));
            var expr = Expression.MakeIndex(p1, null, new[] { Expression.Constant(1) });
            var str = expr.ToScript();
            Assert.AreEqual("p1[1]", str);
        }

        [TestMethod]
        public void TestLambda()
        {
            var p1 = Expression.Parameter(typeof(int));
            var func1 = Expression.Lambda(
                Expression.Increment(p1),
                p1);
            var expr = Expression.Block(
                Expression.Add(
                    Expression.Invoke(func1, Expression.Constant(1)),
                    Expression.Invoke(func1, Expression.Constant(1))));
            var str = expr.ToScript();
            Assert.AreEqual(@"
func1(1) + func1(1);

private int func1(int p1)
{
    return p1 + 1;
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestLambda_Inline()
        {
            Expression<Func<IQueryable<int>, IQueryable<int>>> fn = q => q.Where(it => it > 0);
            var str = fn.ToScript();
            Assert.AreEqual(@"
public IQueryable<int> Main(IQueryable<int> q)
{
    return q.Where<int>(it => it > 0);
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestListInit()
        {
            Expression<Func<List<int>>> list = () => new List<int>() { 1, 2, 3 };
            var str = list.ToScript();
            Assert.AreEqual(@"
public List<int> Main()
{
    return new List<int>() {1, 2, 3};
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestListInit_Dictionary()
        {
            Expression<Func<Dictionary<int, int>>> list = () => new Dictionary<int, int>()
            {
                {1, 2},
                {3, 4}
            };
            var str = list.ToScript();
            Assert.AreEqual(@"
public Dictionary<int, int> Main()
{
    return new Dictionary<int, int>() {{1, 2}, {3, 4}};
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestLoop()
        {
            var @break = Expression.Label();
            var @continue = Expression.Label();
            var @return = Expression.Label();
            var p1 = Expression.Parameter(typeof(int));
            var expr = Expression.Loop(
                Expression.Condition(
                    Expression.GreaterThanOrEqual(p1, Expression.Constant(1)),
                    Expression.Condition(
                        Expression.Equal(p1, Expression.Constant(1)),
                        Expression.Return(@return, p1),
                        Expression.Block(
                            Expression.PostDecrementAssign(p1),
                            Expression.Continue(@continue))),
                    Expression.Break(@break)),
                @break,
                @continue);

            var str = expr.ToScript();
            Assert.AreEqual(@"
while (p1 >= 1)
{
    if (p1 == 1)
    {
        return p1;
    }
    else
    {
        p1--;
        continue;
    }
}".Trim()
            , str);
        }

        [TestMethod]
        public void TestLoop2()
        {
            var label = Expression.Label();
            var expr = Expression.Block(
                Expression.Loop(Expression.Goto(label)),
                Expression.Label(label));
            var str = expr.ToScript();
            Assert.AreEqual(@"
while (true)
{
    goto label1;
}
label1:".Trim()
                , str);
        }

        [TestMethod]
        public void TestMemberAccess()
        {
            Expression<Func<int>> fn = () => DateTime.Now.Day;
            var str = fn.ToScript();
            Assert.AreEqual(@"
public int Main()
{
    return DateTime.Now.Day;
}".Trim()
                , str);
        }

        private class Poco
        {
            public string Name { get; set; }
            public Poco Parent { get; } = new Poco();
            public List<Poco> Children { get; } = new List<Poco>();
        }

        [TestMethod]
        public void TestMemberInit()
        {
            Expression<Func<Poco>> fn = () => new Poco()
            {
                Name = "1",
                Parent = { Name = "2" },
                Children = { new Poco(), new Poco() }
            };
            var str = fn.ToScript();
            Assert.AreEqual(@"
public DebugInfoInjectorTest.Poco Main()
{
    return new DebugInfoInjectorTest.Poco()
    {
        Name = ""1"",
        Parent = {Name = ""2""},
        Children = {new DebugInfoInjectorTest.Poco(), new DebugInfoInjectorTest.Poco()}
    };
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestMethodCall_Default()
        {
            Expression<Func<Dictionary<int, int>, int>> fn = dict => dict[0];
            var str = fn.ToScript();
            Assert.AreEqual(@"
public int Main(Dictionary<int, int> dict)
{
    return dict[0];
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestMethodCall()
        {
            Expression<Func<List<int>, string>> fn = list => list.ToString();
            var str = fn.ToScript();
            Assert.AreEqual(@"
public string Main(List<int> list)
{
    return list.ToString();
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestMethodCall_Static()
        {
            Expression<Func<int, int, int>> fn = (a, b) => Math.Max(a, b);
            var str = fn.ToScript();
            Assert.AreEqual(@"
public int Main(int a, int b)
{
    return Math.Max(a, b);
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestNewArray()
        {
            Expression<Func<int, int[]>> fn = i => new[] { i };
            var str = fn.ToScript();
            Assert.AreEqual(@"
public int[] Main(int i)
{
    return new int[] {i};
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestNewArray_Bound()
        {
            Expression<Func<int, int[]>> fn = i => new int[i];
            var str = fn.ToScript();
            Assert.AreEqual(@"
public int[] Main(int i)
{
    return new int[i];
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestParameter_Reserved()
        {
            Expression<Func<int?, int>> fn = @null => @null.Value;
            var str = fn.ToScript();
            Assert.AreEqual(@"
public int Main(int? @null)
{
    return @null.Value;
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestSwitch()
        {
            var p1 = Expression.Parameter(typeof(int));
            var expr = Expression.Switch(
                p1,
                Expression.Constant(0),
                Expression.SwitchCase(Expression.Constant(1), Expression.Constant(1)));
            var str = expr.ToScript();
            Assert.AreEqual(@"
switch (p1)
{
    case 1:
        1;
        break;
    default:
        0;
        break;
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestTryCatch()
        {
            var p1 = Expression.Parameter(typeof(double));
            var tryCatch = Expression.TryCatchFinally(
                Expression.Block(
                    typeof(void),
                    Expression.Assign(
                        p1,
                        Expression.Divide(Expression.Constant(1.0), Expression.Constant(0.0)))),
                Expression.Throw(Expression.New(typeof(NotSupportedException))),
                Expression.Catch(
                    Expression.Parameter(typeof(DivideByZeroException)),
                    Expression.Rethrow(),
                    Expression.Constant(true)));
            var str = tryCatch.ToScript();
            Assert.AreEqual(@"
try
{
    p1 = 1d / 0d;
}
catch (DivideByZeroException p2) when (true)
{
    throw;
}
finally
{
    throw new NotSupportedException();
}".Trim()
        , str);
        }

        [TestMethod]
        public void TestTryFault()
        {
            var expr = Expression.TryFault(
                Expression.Constant(1),
                Expression.Constant("blah"));
            var str = expr.ToScript();
            Assert.AreEqual(@"
bool fault1 = true;
try
{
    1;
    fault1 = false;
}
finally
{
    if (fault1)
    {
        ""blah"";
    }
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestTypeBinary()
        {
            Expression<Func<object, bool>> fn = o => o is Array;
            var str = fn.ToScript();
            Assert.AreEqual(@"
public bool Main(object o)
{
    return o is Array;
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestUnary_Convert()
        {
            Expression<Func<double, int>> fn = d => (int)d;
            var str = fn.ToScript();
            Assert.AreEqual(@"
public int Main(double d)
{
    return (int)d;
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestUnary_ArrayLength()
        {
            Expression<Func<int[], int>> fn = a => a.Length;
            var str = fn.ToScript();
            Assert.AreEqual(@"
public int Main(int[] a)
{
    return a.Length;
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestUnary_As()
        {
            Expression<Func<Expression, Expression>> fn = expr => expr as UnaryExpression;
            var str = fn.ToScript();
            Assert.AreEqual(@"
public Expression Main(Expression expr)
{
    return expr as UnaryExpression;
}".Trim()
                , str);
        }

        internal static int GetInternal() => 1;

        [TestMethod]
        public void TestToString()
        {
            var call = Expression.Call(
                typeof(DebugInfoInjectorTest).GetMethod(nameof(GetInternal),
                    BindingFlags.Static | BindingFlags.NonPublic)
            );
            var exp = Expression.Lambda<Func<int>>(call);
            var str = exp.ToScript(new ExpressionDefinitions
            {
                IsStatic = true,
                MethodName = "Main",
                Namespace = "ExpressionDebugger.Tests",
                TypeName = "MockClass"
            });
            Assert.AreEqual(@"
using System;

namespace ExpressionDebugger.Tests
{
    public static partial class MockClass
    {
        private static Func<int> GetInternal1;
        
        public static int Main()
        {
            return GetInternal1.Invoke();
        }
    }
}".Trim()
                , str);
        }

        [TestMethod]
        public void TestExpression()
        {
            Expression<Func<Data, Data>> lambda = data => new Data {Id = data.Id + "1", Records = data.Records.Select(it => it + 1)};
            var str = lambda.ToScript(new ExpressionDefinitions {IsExpression = true});
            Assert.AreEqual(@"
public Expression<Func<DebugInfoInjectorTest.Data, DebugInfoInjectorTest.Data>> Main => data => new DebugInfoInjectorTest.Data()
{
    Id = data.Id + ""1"",
    Records = data.Records.Select<int, int>(it => it + 1)
};".Trim(), str);
        }

        [TestMethod]
        public void TestExtensionMethod()
        {
            var p1 = Expression.Parameter(typeof(int));
            var p2 = Expression.Parameter(typeof(int));
            var lambda = Expression.Lambda<Func<int, int, int>>(
                Expression.Add(p1, p2),
                p1, p2);
            var translator = new ExpressionTranslator(new TypeDefinitions
            {
                IsStatic = true,
                Namespace = "ExpressionDebugger.Tests",
                TypeName = "MockClass"
            });
            translator.VisitLambda(lambda, ExpressionTranslator.LambdaType.ExtensionMethod, "Add");
            var str = translator.ToString();
            Assert.AreEqual(@"
namespace ExpressionDebugger.Tests
{
    public static partial class MockClass
    {
        public static int Add(this int p1, int p2)
        {
            return p1 + p2;
        }
    }
}".Trim(), str);
        }

        [TestMethod]
        public void TestProperties()
        {
            var translator = new ExpressionTranslator(new TypeDefinitions
            {
                IsStatic = false,
                Namespace = "ExpressionDebugger.Tests",
                TypeName = "MockClass",
                NullableContext = 2,
            });
            translator.Properties.Add(new PropertyDefinitions
            {
                Name = "Prop1",
                Type = typeof(string),
                NullableContext = 0,
            });
            translator.Properties.Add(new PropertyDefinitions
            {
                Name = "Prop2",
                Type = typeof(List<Dictionary<int, string[]>>),
                IsReadOnly = true,
                Nullable = new byte[] { 1, 2, 1, 2 }
            });
            translator.Properties.Add(new PropertyDefinitions
            {
                Name = "Prop3",
                Type = typeof(string),
                IsInitOnly = true,
                NullableContext = 0,
            });
            var str = translator.ToString();
            Assert.AreEqual(@"
using System.Collections.Generic;

namespace ExpressionDebugger.Tests
{
    public partial class MockClass
    {
        public string Prop1 { get; set; }
        public List<Dictionary<int, string?[]>?> Prop2 { get; }
        public string Prop3 { get; init; }
        
        public MockClass(List<Dictionary<int, string?[]>?> prop2)
        {
            this.Prop2 = prop2;
        }
    }
}".Trim(), str);
        }

        
        [TestMethod]
        public void TestRecordType()
        {
            var translator = new ExpressionTranslator(new TypeDefinitions
            {
                IsStatic = false,
                Namespace = "ExpressionDebugger.Tests",
                TypeName = "MockClass",
                IsRecordType = true,
            });
            translator.Properties.Add(new PropertyDefinitions
            {
                Name = "Prop1",
                Type = typeof(string)
            });
            translator.Properties.Add(new PropertyDefinitions
            {
                Name = "Prop2",
                Type = typeof(string),
                IsReadOnly = true
            });
            translator.Properties.Add(new PropertyDefinitions
            {
                Name = "Prop3",
                Type = typeof(string),
                IsInitOnly = true
            });
            var str = translator.ToString();
            Assert.AreEqual(@"
namespace ExpressionDebugger.Tests
{
    public partial record MockClass(string prop2)
    {
        public string Prop1 { get; set; }
        public string Prop3 { get; init; }
    }
}".Trim(), str);
        }

        public class Data
        {
            public string Id { get; set; }
            public IEnumerable<int> Records { get; set; }
        }
    }
}
