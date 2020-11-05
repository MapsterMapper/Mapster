using System;

namespace Mapster
{
    [AttributeUsage(AttributeTargets.Class 
                    | AttributeTargets.Struct 
                    | AttributeTargets.Interface, AllowMultiple = true)]
    public abstract class BaseAdaptAttribute : Attribute
    {
        protected BaseAdaptAttribute(Type type)
        {
            this.Type = type;
        }
        protected BaseAdaptAttribute(string name)
        {
            this.Name = name;
        }

        public Type? Type { get; }
        public string? Name { get; }
        public Type[]? IgnoreAttributes { get; set; }
        public Type[]? IgnoreNoAttributes { get; set; }
        public string[]? IgnoreNamespaces { get; set; }
        public bool IgnoreNullValues { get; set; }
        public bool MapToConstructor { get; set; }
        public int MaxDepth { get; set; }
        public bool PreserveReference { get; set; }
        public bool ShallowCopyForSameType { get; set; }
        public MapType MapType { get; set; }
    }

    public class AdaptFromAttribute : BaseAdaptAttribute
    {
        public AdaptFromAttribute(Type type) : base(type) { }
        public AdaptFromAttribute(string name) : base(name) { }
    }

    public class AdaptToAttribute : BaseAdaptAttribute
    {
        public AdaptToAttribute(Type type) : base(type) { }
        public AdaptToAttribute(string name) : base(name) { }
    }

    public class AdaptTwoWaysAttribute : AdaptToAttribute
    {
        public AdaptTwoWaysAttribute(Type type) : base(type) { }
        public AdaptTwoWaysAttribute(string name) : base(name) { }
    }
}
