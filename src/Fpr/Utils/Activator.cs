using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Fpr.Utils
{
    public static class Activator<T> where T : class, new()
    {
        public static Func<T> CreateInstance { get; private set; }
        static Activator()
        {
            CreateInstance = typeof(T).GetConstructorDelegate<T>();
        }
    }

    public static class ActivatorExtensions
    {
        private static readonly ConcurrentDictionary<Type, Func<object>> _constructors = new ConcurrentDictionary<Type, Func<object>>();

        public static Func<TBase> GetConstructorDelegate<TBase>(this Type type)
        {
            return (Func<TBase>)GetConstructorDelegate(type, typeof(Func<TBase>));
        }

        public static Func<object> GetConstructorDelegate(this Type type)
        {
            return (Func<object>)GetConstructorDelegate(type, typeof(Func<object>));
        }

        public static Delegate GetConstructorDelegate(this Type type, Type delegateType)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (delegateType == null)
                throw new ArgumentNullException("delegateType");

            Type[] genericArguments = delegateType.GetGenericArguments();
            Type[] argTypes = genericArguments.Length > 1 ? genericArguments.Take(genericArguments.Length - 1).ToArray() : Type.EmptyTypes;

            ConstructorInfo constructor = type.GetConstructor(argTypes);
            if (constructor == null)
            {
                if (argTypes.Length == 0)
                {
                    throw new InvalidProgramException(
                        string.Format("Type '{0}' doesn't have a parameterless constructor.", type.Name));
                }
                throw new InvalidProgramException(string.Format("Type '{0}' doesn't have the requested constructor.",
                    type.Name));
            }

            var dynamicMethod = new DynamicMethod("DM$_" + type.Name, type, argTypes, type);
            ILGenerator ilGen = dynamicMethod.GetILGenerator();
            for (int i = 0; i < argTypes.Length; i++)
            {
                ilGen.Emit(OpCodes.Ldarg, i);
            }
            ilGen.Emit(OpCodes.Newobj, constructor);
            ilGen.Emit(OpCodes.Ret);
            return dynamicMethod.CreateDelegate(delegateType);
        }

        public static object CreateInstance(Type type)
        {
            Func<object> constructor;
            if (!_constructors.TryGetValue(type, out constructor))
            {
                constructor = GetConstructorDelegate(type);
                _constructors.TryAdd(type, constructor);
            }
            return constructor();
        }
    }
}