using System;
using System.Collections.Concurrent;
using System.Reflection.Emit;

namespace Fpr.Utils
{
    public static class FastObjectFactory
    {
        private static readonly ConcurrentDictionary<Type, Func<Object>> _creatorCache = new ConcurrentDictionary<Type, Func<Object>>();
        private readonly static Type _coType = typeof(Func<Object>);

        public static void ClearObjectFactory<T>() where T : class
        {
            Func<Object> removed;
            _creatorCache.TryRemove(typeof (T), out removed);
        }

        /// <summary>
        /// Create a new instance of the specified type
        /// </summary>
        /// <returns></returns>
        public static Func<Object> CreateObjectFactory<T>() 
        {
            Type type = typeof(T);
            Func<Object> createDelegate;
            if (!_creatorCache.TryGetValue(type, out createDelegate))
            {
                    var dynMethod = new DynamicMethod("DM$OBJ_FACTORY_" + type.Name, typeof(object), null, type);
                    ILGenerator ilGen = dynMethod.GetILGenerator();

                    ilGen.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
                    ilGen.Emit(OpCodes.Ret);
                    createDelegate = (Func<Object>)dynMethod.CreateDelegate(_coType);
                    _creatorCache.TryAdd(type, createDelegate);
            }
            return createDelegate;
        }
    }
}
