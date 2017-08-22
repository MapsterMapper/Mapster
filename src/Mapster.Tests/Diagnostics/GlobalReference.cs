using System.Collections.Generic;

namespace ExpressionDebugger
{
    public static class GlobalReference
    {
        private static readonly List<object> _references = new List<object>();
        private static readonly Dictionary<object, int> _dict = new Dictionary<object, int>();

        public static int GetIndex(object obj)
        {
            if (_dict.TryGetValue(obj, out var id))
                return id;
            lock (_references)
            {
                if (_dict.TryGetValue(obj, out id))
                    return id;
                id = _references.Count;
                _references.Add(obj);
                _dict[obj] = id;
                return id;
            }
        }

        public static object GetObject(int i)
        {
            return _references[i];
        }
    }
}
