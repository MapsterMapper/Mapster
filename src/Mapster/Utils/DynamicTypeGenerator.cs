using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Mapster.Utils
{
    internal static class DynamicTypeGenerator
    {
        private const string DynamicAssemblyName = "MapsterGeneratedTypes";

        private static readonly AssemblyBuilder _assemblyBuilder =
#if NET40
            AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(DynamicAssemblyName), AssemblyBuilderAccess.Run);
#else
            AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(DynamicAssemblyName), AssemblyBuilderAccess.Run);
#endif
        private static readonly ModuleBuilder _moduleBuilder = _assemblyBuilder.DefineDynamicModule("Classes");
        private static readonly ConcurrentDictionary<Type, Type> _generated = new ConcurrentDictionary<Type, Type>();
        private static int _generatedCounter = 0;

        public static Type GetTypeForInterface(Type interfaceType)
        {
            CheckInterfaceType(interfaceType);
            return _generated.GetOrAdd(interfaceType, (key) => CreateTypeForInterface(key));
        }

        private static void CheckInterfaceType(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
            {
                const string msg = "Cannot create dynamic type for {0}, because it is not an interface.\n" +
                    "Target type full name: {1}";
                throw new InvalidOperationException(string.Format(msg, interfaceType.Name, interfaceType.FullName));
            }
            if (!interfaceType.IsVisible)
            {
                const string msg = "Cannot adapt to interface {0}, because it is not accessible outside its assembly.\n" +
                    "Interface full name: {1}";
                throw new InvalidOperationException(string.Format(msg, interfaceType.Name, interfaceType.FullName));
            }
        }

        private static Type CreateTypeForInterface(Type interfaceType)
        {
            TypeBuilder builder = _moduleBuilder.DefineType("GeneratedType_" + Interlocked.Increment(ref _generatedCounter));

            foreach (Type currentInterface in ReflectionUtils.GetAllInterfaces(interfaceType))
            {
                builder.AddInterfaceImplementation(currentInterface);
                foreach (PropertyInfo prop in currentInterface.GetProperties())
                {
                    CreateProperty(currentInterface, builder, prop);
                }
                foreach (MethodInfo method in currentInterface.GetMethods())
                {
                    // MethodAttributes.SpecialName are methods for property getters and setters.
                    if (!method.Attributes.HasFlag(MethodAttributes.SpecialName))
                    {
                        CreateMethod(builder, method);
                    }
                }
            }

#if NETSTANDARD2_0
            return builder.CreateTypeInfo();
#else
            return builder.CreateType();
#endif
        }

        private static void CreateProperty(Type interfaceType, TypeBuilder builder, PropertyInfo prop)
        {
            const BindingFlags interfacePropMethodFlags = BindingFlags.Instance | BindingFlags.Public;
            // The property set and get methods require a special set of attributes.
            const MethodAttributes classPropMethodAttrs
                = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            FieldBuilder propField = builder.DefineField("_" + prop.Name, prop.PropertyType, FieldAttributes.Private);
            PropertyBuilder propBuilder = builder.DefineProperty(prop.Name, PropertyAttributes.None, prop.PropertyType, null);

            if (prop.CanRead)
            {
                // Define the "get" accessor method for property.
                string getMethodName = "get_" + prop.Name;
                MethodBuilder propGet = builder.DefineMethod(getMethodName, classPropMethodAttrs, prop.PropertyType, null);
                ILGenerator propGetIL = propGet.GetILGenerator();
                propGetIL.Emit(OpCodes.Ldarg_0);
                propGetIL.Emit(OpCodes.Ldfld, propField);
                propGetIL.Emit(OpCodes.Ret);

                MethodInfo interfaceGetMethod = interfaceType.GetMethod(getMethodName, interfacePropMethodFlags);
                builder.DefineMethodOverride(propGet, interfaceGetMethod);
                propBuilder.SetGetMethod(propGet);
            }

            if (prop.CanWrite)
            {
                // Define the "set" accessor method for property.
                string setMethodName = "set_" + prop.Name;
                MethodBuilder propSet = builder.DefineMethod(setMethodName, classPropMethodAttrs, null, new Type[] { prop.PropertyType });
                ILGenerator propSetIL = propSet.GetILGenerator();
                propSetIL.Emit(OpCodes.Ldarg_0);
                propSetIL.Emit(OpCodes.Ldarg_1);
                propSetIL.Emit(OpCodes.Stfld, propField);
                propSetIL.Emit(OpCodes.Ret);

                MethodInfo interfaceSetMethod = interfaceType.GetMethod(setMethodName, interfacePropMethodFlags);
                builder.DefineMethodOverride(propSet, interfaceSetMethod);
                propBuilder.SetSetMethod(propSet);
            }
        }

        private static void CreateMethod(TypeBuilder builder, MethodInfo interfaceMethod)
        {
            Type[] parameterTypes = null;
            ParameterInfo[] parameters = interfaceMethod.GetParameters();
            if (parameters.Length > 0)
            {
                parameterTypes = new Type[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    parameterTypes[i] = parameters[i].ParameterType;
                }
            }

            MethodBuilder classMethod = builder.DefineMethod(
                interfaceMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                interfaceMethod.CallingConvention,
                interfaceMethod.ReturnType,
                parameterTypes);
            ILGenerator classMethodIL = classMethod.GetILGenerator();
            classMethodIL.ThrowException(typeof(NotImplementedException));

            builder.DefineMethodOverride(classMethod, interfaceMethod);
        }
    }
}
