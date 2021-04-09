using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Mapster.Utils
{
    internal static class DynamicTypeGenerator
    {
        private const string DynamicAssemblyName = "Mapster.Dynamic";

        private static readonly AssemblyBuilder _assemblyBuilder =
#if NET40
            AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(DynamicAssemblyName), AssemblyBuilderAccess.Run);
#else
            AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(DynamicAssemblyName), AssemblyBuilderAccess.Run);
#endif
        private static readonly ModuleBuilder _moduleBuilder = _assemblyBuilder.DefineDynamicModule("Mapster.Dynamic");
        private static readonly ConcurrentDictionary<Type, Type> _generated = new ConcurrentDictionary<Type, Type>();
        private static int _generatedCounter;

        public static Type? GetTypeForInterface(Type interfaceType, bool ignoreError)
        {
            try
            {
                return GetTypeForInterface(interfaceType);
            }
            catch (Exception)
            {
                if (ignoreError)
                    return null;
                throw;
            }
        }
        public static Type GetTypeForInterface(Type interfaceType)
        {
            if (!interfaceType.GetTypeInfo().IsInterface)
            {
                const string msg = "Cannot create dynamic type for {0}, because it is not an interface.\n" +
                                   "Target type full name: {1}";
                throw new InvalidOperationException(string.Format(msg, interfaceType.Name, interfaceType.FullName));
            }
            if (!interfaceType.GetTypeInfo().IsVisible)
            {
                const string msg = "Cannot adapt to interface {0}, because it is not accessible outside its assembly.\n" +
                                   "Interface full name: {1}";
                throw new InvalidOperationException(string.Format(msg, interfaceType.Name, interfaceType.FullName));
            }
            return _generated.GetOrAdd(interfaceType, CreateTypeForInterface);
        }

        private static Type CreateTypeForInterface(Type interfaceType)
        {
            TypeBuilder builder = _moduleBuilder.DefineType("GeneratedType_" + Interlocked.Increment(ref _generatedCounter));

            var args = new List<FieldBuilder>();
            int propCount = 0;
            foreach (Type currentInterface in interfaceType.GetAllInterfaces())
            {
                builder.AddInterfaceImplementation(currentInterface);
                foreach (PropertyInfo prop in currentInterface.GetProperties())
                {
                    propCount++;
                    FieldBuilder propField = builder.DefineField("_" + MapsterHelper.CamelCase(prop.Name), prop.PropertyType, FieldAttributes.Private);
                    CreateProperty(currentInterface, builder, prop, propField);
                    if (!prop.CanWrite)
                        args.Add(propField);
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

            if (propCount == 0)
                throw new InvalidOperationException($"No default constructor for type '{interfaceType.Name}', please use 'ConstructUsing' or 'MapWith'");

            if (args.Count == propCount)
            {
                var ctorBuilder = builder.DefineConstructor(MethodAttributes.Public, 
                    CallingConventions.Standard,
                    args.Select(it => it.FieldType).ToArray());
                var ctorIl = ctorBuilder.GetILGenerator();
                for (var i = 0; i < args.Count; i++)
                {
                    var arg = args[i];
                    ctorBuilder.DefineParameter(i + 1, ParameterAttributes.None, arg.Name.Substring(1));
                    ctorIl.Emit(OpCodes.Ldarg_0);
                    ctorIl.Emit(OpCodes.Ldarg_S, i + 1);
                    ctorIl.Emit(OpCodes.Stfld, arg);
                }
                ctorIl.Emit(OpCodes.Ret);
            }

#if NETSTANDARD2_0
            return builder.CreateTypeInfo()!;
#elif NETSTANDARD1_3
            return builder.CreateTypeInfo().AsType();
#else
            return builder.CreateType();
#endif
        }

        private static void CreateProperty(Type interfaceType, TypeBuilder builder, PropertyInfo prop, FieldBuilder propField)
        {
            const BindingFlags interfacePropMethodFlags = BindingFlags.Instance | BindingFlags.Public;
            // The property set and get methods require a special set of attributes.
            const MethodAttributes classPropMethodAttrs
                = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            PropertyBuilder propBuilder = builder.DefineProperty(prop.Name, PropertyAttributes.None, prop.PropertyType, null);

            if (prop.CanRead)
            {
                // Define the "get" accessor method for property.
                string getMethodName = "get_" + prop.Name;
                MethodBuilder propGet = builder.DefineMethod(getMethodName, classPropMethodAttrs, prop.PropertyType, null);
                ILGenerator propGetIl = propGet.GetILGenerator();
                propGetIl.Emit(OpCodes.Ldarg_0);
                propGetIl.Emit(OpCodes.Ldfld, propField);
                propGetIl.Emit(OpCodes.Ret);

                MethodInfo interfaceGetMethod = interfaceType.GetMethod(getMethodName, interfacePropMethodFlags)!;
                builder.DefineMethodOverride(propGet, interfaceGetMethod);
                propBuilder.SetGetMethod(propGet);
            }

            if (prop.CanWrite)
            {
                // Define the "set" accessor method for property.
                string setMethodName = "set_" + prop.Name;
                MethodBuilder propSet = builder.DefineMethod(setMethodName, classPropMethodAttrs, null, new[] { prop.PropertyType });
                ILGenerator propSetIl = propSet.GetILGenerator();
                propSetIl.Emit(OpCodes.Ldarg_0);
                propSetIl.Emit(OpCodes.Ldarg_1);
                propSetIl.Emit(OpCodes.Stfld, propField);
                propSetIl.Emit(OpCodes.Ret);

                MethodInfo interfaceSetMethod = interfaceType.GetMethod(setMethodName, interfacePropMethodFlags)!;
                builder.DefineMethodOverride(propSet, interfaceSetMethod);
                propBuilder.SetSetMethod(propSet);
            }
        }

        private static void CreateMethod(TypeBuilder builder, MethodInfo interfaceMethod)
        {
            Type[]? parameterTypes = null;
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
            ILGenerator classMethodIl = classMethod.GetILGenerator();
            classMethodIl.ThrowException(typeof(NotImplementedException));

            builder.DefineMethodOverride(classMethod, interfaceMethod);
        }
    }
}
