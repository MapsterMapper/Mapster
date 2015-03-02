using System;
using System.Reflection.Emit;

namespace Mapster.Utils
{
    public static class FastObjectFactory
    {

        /// <summary>
        /// Create a new instance of the specified type
        /// </summary>
        /// <returns></returns>
        public static Func<T> CreateObjectFactory<T>(Func<T> factory = null)
        {
            Type type = typeof (T);
            object createDelegate;

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
                createDelegate = dynMethod.CreateDelegate(typeof (Func<T>));
            }
            return (Func<T>) createDelegate;
        }
    }
}
