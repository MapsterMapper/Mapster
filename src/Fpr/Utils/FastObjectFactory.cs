using System;
using System.Collections.Concurrent;
using System.Reflection.Emit;

namespace Fpr.Utils
{
    public static class FastObjectFactory
    {
        private static readonly ConcurrentDictionary<Type, CreateObject> _creatorCache = new ConcurrentDictionary<Type, CreateObject>();
        private readonly static Type _coType = typeof(CreateObject);
        public delegate object CreateObject();

        public static void ClearObjectFactory<T>() where T : class
        {
            CreateObject removed;
            _creatorCache.TryRemove(typeof (T), out removed);
        }

        /// <summary>
        /// Create a new instance of the specified type
        /// </summary>
        /// <returns></returns>
        public static CreateObject CreateObjectFactory<T>() 
        {
            Type type = typeof(T);
            CreateObject createDelegate;
            if (!_creatorCache.TryGetValue(type, out createDelegate))
            {
                    var dynMethod = new DynamicMethod("DM$OBJ_FACTORY_" + type.Name, typeof(object), null, type);
                    ILGenerator ilGen = dynMethod.GetILGenerator();

                    ilGen.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
                    ilGen.Emit(OpCodes.Ret);
                    createDelegate = (CreateObject)dynMethod.CreateDelegate(_coType);
                    _creatorCache.TryAdd(type, createDelegate);
            }
            return createDelegate;
        }
    }
}
