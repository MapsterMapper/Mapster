using ExpressionDebugger;
using System.Diagnostics;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace System.Linq.Expressions
{
    public static class ExpressionDebuggerExtensions
    {

        /// <summary>
        /// Compile with debugging info injected
        /// </summary>
        /// <typeparam name="T">Type of lambda expression</typeparam>
        /// <param name="node">Lambda expression</param>
        /// <param name="options">Compilation options</param>
        /// <returns>Generated method</returns>
        public static T CompileWithDebugInfo<T>(this Expression<T> node, ExpressionCompilationOptions? options = null)
        {
            return (T)(object)CompileWithDebugInfo((LambdaExpression)node, options);
        }

        public static Delegate CompileWithDebugInfo(this LambdaExpression node, ExpressionCompilationOptions? options = null)
        {
            try
            {
                var compiler = new ExpressionCompiler(options);
                compiler.AddFile(node);
                var assembly = compiler.CreateAssembly();

                var translator = compiler.Translators[0];
                return translator.CreateDelegate(assembly);
            }
            catch (Exception ex)
            {
                if (options?.ThrowOnFailedCompilation == true)
                    throw;
                Debug.Print(ex.ToString());
                return node.Compile();
            }
        }

        public static Delegate CreateDelegate(this ExpressionTranslator translator, Assembly assembly)
        {
            var definitions = translator.Definitions!;
            var typeName = definitions.Namespace == null
                ? definitions.TypeName
                : definitions.Namespace + "." + definitions.TypeName;
            var type = assembly.GetType(typeName);
            var main = translator.Methods.First();
            var method = type.GetMethod(main.Key)!;
            var obj = definitions.IsStatic ? null : Activator.CreateInstance(type);
            var flag = definitions.IsStatic ? BindingFlags.Static : BindingFlags.Instance;
            foreach (var kvp in translator.Constants)
            {
                var field = type.GetField(kvp.Value, BindingFlags.NonPublic | flag)!;
                field.SetValue(obj, kvp.Key);
            }
            return method.CreateDelegate(main.Value, obj);
        }
    }
}
