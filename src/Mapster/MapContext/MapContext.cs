using Mapster.Utils;
using System;
using System.Collections.Generic;

namespace Mapster
{
    /// <summary>
    /// This class is to send data between mapping process
    /// </summary>
    /// <remarks>
    /// The idea of this class is similar to Transaction & TransactionScope
    /// You can get context by MapContext.Current
    /// And all mapping processes will having only one context
    /// </remarks>
    public class MapContext
    {
        [ThreadStatic]
        private static MapContext? _current;
        public static MapContext? Current
        {
            get { return _current; }
            set { _current = value; }
        }

        private Dictionary<object, object> _references;
        public Dictionary<object, object> References => _references ?? (_references = new Dictionary<object, object>(ReferenceComparer.Default));

        private Dictionary<string, object> _parameters;
        public Dictionary<string, object> Parameters => _parameters ?? (_parameters = new Dictionary<string, object>());
    }
}
