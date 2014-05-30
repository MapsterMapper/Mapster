using System;
using System.Collections;
using System.Reflection.Emit;

namespace Fapper.Utils
{
    public static class FastObjectFactory
    {
        private static readonly Hashtable _creatorCache = Hashtable.Synchronized(new Hashtable());
        private readonly static Type _coType = typeof(CreateObject);
        public delegate object CreateObject();

        /// <summary>
        /// Create a new instance of the specified type
        /// </summary>
        /// <returns></returns>
        public static CreateObject CreateObjectFactory<T>() where T : class
        {
            Type t = typeof(T);
            var c = _creatorCache[t] as CreateObject;
            if (c == null)
            {
                lock (_creatorCache.SyncRoot)
                {
                    c = _creatorCache[t] as CreateObject;
                    if (c != null)
                    {
                        return c;
                    }
                    var dynMethod = new DynamicMethod("DM$OBJ_FACTORY_" + t.Name, typeof(object), null, t);
                    ILGenerator ilGen = dynMethod.GetILGenerator();

                    ilGen.Emit(OpCodes.Newobj, t.GetConstructor(Type.EmptyTypes));
                    ilGen.Emit(OpCodes.Ret);
                    c = (CreateObject)dynMethod.CreateDelegate(_coType);
                    _creatorCache.Add(t, c);
                }
            }
            return c;
        }
    }
}
