using System;

namespace Sample.CodeGen.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NoModifyAttribute : Attribute
    {
    }
}
