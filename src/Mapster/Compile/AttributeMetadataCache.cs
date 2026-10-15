using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mapster
{
    // Shared by built-in source member models for one root expression, including its inline mappings.
    // The lock protects this cache only; CompileContext's other mutable state is not thread-safe.
    internal sealed class AttributeMetadataCache
    {
        private readonly Dictionary<MemberInfo, IReadOnlyList<CustomAttributeData>> _metadata = new();
        private bool _completed;

        internal IEnumerable<CustomAttributeData> Get(MemberInfo member)
        {
            lock (_metadata)
            {
                if (_completed)
                    return member.GetCustomAttributesData();
                if (!_metadata.TryGetValue(member, out var attributes))
                {
                    attributes = Array.AsReadOnly(new List<CustomAttributeData>(member.GetCustomAttributesData()).ToArray());
                    _metadata.Add(member, attributes);
                }
                return attributes;
            }
        }

        internal void Complete()
        {
            lock (_metadata)
            {
                // Callbacks and CompileException can retain models/context after compilation.
                // Release metadata and prevent retained models from repopulating the cache.
                _completed = true;
                _metadata.Clear();
            }
        }
    }
}
