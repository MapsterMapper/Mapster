using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Fpr.Utils
{
    public static class FastObjectFactory
    {
        private static readonly object _syncLock = new object();
        private static readonly Dictionary<Type, object> _creatorCache = new Dictionary<Type, object>();

        public static void ClearObjectFactory<T>() where T : class
        {
            _creatorCache.Remove(typeof (T));
        }

        /// <summary>
        /// Create a new instance of the specified type
        /// </summary>
        /// <returns></returns>
        public static Func<T> CreateObjectFactory<T>(Func<T> factory = null) 
        {
            Type type = typeof(T);
            object createDelegate;
            if (!_creatorCache.TryGetValue(type, out createDelegate))
            {
                lock (_syncLock)
                {
                    if (!_creatorCache.TryGetValue(type, out createDelegate))
                    {
                        if (factory != null)
                        {
                            createDelegate = factory;
                        }
                        else
                        {
                            var dynMethod = new DynamicMethod("DM$OBJ_FACTORY_" + type.Name, type, null, type);
                            ILGenerator ilGen = dynMethod.GetILGenerator();

                            ilGen.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
                            ilGen.Emit(OpCodes.Ret);
                            createDelegate = dynMethod.CreateDelegate(typeof(Func<T>));
                        }
                        _creatorCache.Add(type, createDelegate);
                    }
                }
            }
            return (Func<T>)createDelegate;
        }
    }
}
