using System;
using System.Linq.Expressions;

namespace ExpressionDebugger
{
    public static class ExpressionTranslatorExtensions
    {
        /// <summary>
        /// Generate script text
        /// </summary>
        public static string ToScript(this Expression node, ExpressionDefinitions? definitions = null)
        {
            var translator = ExpressionTranslator.Create(node, definitions);
            return translator.ToString();
        }
    }
}
