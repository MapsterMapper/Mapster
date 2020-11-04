using Mapster.Utils;
using System;
using System.Collections.Generic;
using System.Threading;

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
#if NETSTANDARD
        private static readonly AsyncLocal<MapContext?> _localContext = new AsyncLocal<MapContext?>();
        public static MapContext? Current
        {
            get => _localContext.Value;
            set => _localContext.Value = value;
        }
#else
        [field: ThreadStatic]
        public static MapContext? Current { get; set; }
#endif

        private Dictionary<ReferenceTuple, object>? _references;
        public Dictionary<ReferenceTuple, object> References => _references ??= new Dictionary<ReferenceTuple, object>();

        private Dictionary<string, object>? _parameters;
        public Dictionary<string, object> Parameters => _parameters ??= new Dictionary<string, object>();
    }
}
