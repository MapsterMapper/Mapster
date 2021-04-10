using System;
using System.Collections.Generic;

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
            this.MapType = MapType.Map | MapType.MapToTarget;
        }
        protected BaseAdaptAttribute(string name)
        {
            this.Name = name;
            this.MapType = MapType.Map | MapType.MapToTarget;
        }

        public Type? Type { get; }
        public string? Name { get; }
        public Type[]? IgnoreAttributes { get; set; }
        public Type[]? IgnoreNoAttributes { get; set; }
        public string[]? IgnoreNamespaces { get; set; }
        public int MaxDepth { get; set; }
        public MapType MapType { get; set; }

        private readonly Dictionary<string, bool> _boolValues = new Dictionary<string, bool>();
        public bool IgnoreNullValues
        {
            get => _boolValues.TryGetValue(nameof(IgnoreNullValues), out var value) && value;
            set => _boolValues[nameof(IgnoreNullValues)] = value;
        }
        public bool MapToConstructor
        {
            get => _boolValues.TryGetValue(nameof(MapToConstructor), out var value) && value;
            set => _boolValues[nameof(MapToConstructor)] = value;
        }
        public bool PreserveReference
        {
            get => _boolValues.TryGetValue(nameof(PreserveReference), out var value) && value;
            set => _boolValues[nameof(PreserveReference)] = value;
        }
        public bool ShallowCopyForSameType
        {
            get => _boolValues.TryGetValue(nameof(ShallowCopyForSameType), out var value) && value;
            set => _boolValues[nameof(ShallowCopyForSameType)] = value;
        }
        public bool RequireDestinationMemberSource
        {
            get => _boolValues.TryGetValue(nameof(RequireDestinationMemberSource), out var value) && value;
            set => _boolValues[nameof(RequireDestinationMemberSource)] = value;
        }

        public bool? GetBooleanSettingValues(string name)
        {
            return _boolValues.TryGetValue(name, out var value) ? (bool?)value : null;
        }
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

    public class ProjectToAttribute : AdaptToAttribute
    {
        public ProjectToAttribute(Type type) : base(type)
        {
            MapType = MapType.Projection;
        }

        public ProjectToAttribute(string name) : base(name)
        {
            MapType = MapType.Projection;
        }
    }
}
