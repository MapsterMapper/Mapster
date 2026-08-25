using System;
using System.Collections.Generic;

namespace ExpressionDebugger
{
    public class TypeDefinitions
    {
        public string? Namespace { get; set; }
        public string? TypeName { get; set; }
        public bool IsStatic { get; set; }
        public bool IsInternal { get; set; }
        public IEnumerable<Type>? Implements { get; set; }
        public bool PrintFullTypeName { get; set; }
        public bool IsRecordType { get; set; }

        /// <summary>
        /// Set to 2 to mark all properties as nullable
        /// </summary>
        public byte? NullableContext { get; set; }
    }
}