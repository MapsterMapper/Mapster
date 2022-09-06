using System;
using System.Collections.Generic;
using System.Reflection;

namespace ExpressionDebugger
{
    public class PropertyDefinitions
    {
        public Type Type { get; set; }
        public string Name { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsInitOnly { get; set; }

        /// <summary>
        /// Set to 2 to mark type as nullable
        /// </summary>
        public byte? NullableContext { get; set; }

        /// <summary>
        /// If type is generic, array or tuple, you can mark nullable for each type
        /// Set to 2 for nullable
        /// </summary>
        public byte[]? Nullable { get; set; }
    }
}
