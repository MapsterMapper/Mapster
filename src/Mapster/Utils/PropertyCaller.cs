using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Mapster.Utils
{
   
    public static class PropertyCaller<T>
    {
        public delegate void GenSetter(T target, Object value);
        public delegate Object GenGetter(T target);

        public static GenGetter CreateGetMethod(PropertyInfo pi)
        {
            var classType = typeof(T);
            var propType = pi.PropertyType;

            //If there is no getter, return nothing
            var getMethod = pi.GetGetMethod();
            if (getMethod == null)
            {
                return null;
            }

            var getter = new DynamicMethod(String.Concat("_Get", pi.Name, "_"), typeof(object), new[] { typeof(T) }, classType);
            var generator = getter.GetILGenerator();
            generator.DeclareLocal(propType);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, classType);
            generator.EmitCall(OpCodes.Callvirt, getMethod, null);
            if (!propType.IsClass)
                generator.Emit(OpCodes.Box, propType);
            generator.Emit(OpCodes.Ret);

            //Create the delegate and return it
            var genGetter = (GenGetter)getter.CreateDelegate(typeof(GenGetter));

            return genGetter;
        }

        public static GenSetter CreateSetMethod(PropertyInfo pi)
        {
            Type classType = typeof(T);
            Type propType = pi.PropertyType;

            //If there is no setter, return nothing
            MethodInfo setMethod = pi.GetSetMethod(true);
            if (setMethod == null)
            {
                return null;
            }

            //Create dynamic method
            var arguments = new[] { classType, typeof(object) };

            var setter = new DynamicMethod(String.Concat("_Set", pi.Name, "_"), typeof(void), arguments, classType);
            ILGenerator generator = setter.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, classType);
            generator.Emit(OpCodes.Ldarg_1);

            if (propType.IsClass)
                generator.Emit(OpCodes.Castclass, propType);
            else
                generator.Emit(OpCodes.Unbox_Any, propType);

            generator.EmitCall(OpCodes.Callvirt, setMethod, null);
            generator.Emit(OpCodes.Ret);

            //Create the delegate and return it
            var genSetter = (GenSetter)setter.CreateDelegate(typeof(GenSetter));

            return genSetter;
        }
    }

    public delegate void GenericSetter(object target, object value);
    public delegate object GenericGetter(object target);

    public static class PropertyCaller
    {
        ///
        /// Creates a dynamic setter for the property
        ///
        public static GenericSetter CreateSetMethod(PropertyInfo propertyInfo)
        {
            /*
            * If there's no setter return null
            */
            MethodInfo setMethod = propertyInfo.GetSetMethod(true);
            if (setMethod == null)
                return null;

            /*
            * Create the dynamic method
            */
            var arguments = new Type[2];
            arguments[0] = arguments[1] = typeof(object);

            var setter = new DynamicMethod(
              String.Concat("_Set", propertyInfo.Name, "_"),
              typeof(void), arguments, propertyInfo.DeclaringType);
            ILGenerator generator = setter.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            generator.Emit(OpCodes.Ldarg_1);

            if (propertyInfo.PropertyType.IsClass)
                generator.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
            else
                generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);

            generator.EmitCall(OpCodes.Callvirt, setMethod, null);
            generator.Emit(OpCodes.Ret);

            /*
            * Create the delegate and return it
            */
            return (GenericSetter)setter.CreateDelegate(typeof(GenericSetter));
        }

        ///
        /// Creates a dynamic getter for the property
        ///
        public static GenericGetter CreateGetMethod(PropertyInfo propertyInfo)
        {
            /*
            * If there's no getter return null
            */
            MethodInfo getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
                return null;

            /*
            * Create the dynamic method
            */
            var arguments = new Type[1];
            arguments[0] = typeof(object);

            var getter = new DynamicMethod(
              String.Concat("_Get", propertyInfo.Name, "_"),
              typeof(object), arguments, propertyInfo.DeclaringType);
            ILGenerator generator = getter.GetILGenerator();
            generator.DeclareLocal(typeof(object));
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            generator.EmitCall(OpCodes.Callvirt, getMethod, null);

            if (!propertyInfo.PropertyType.IsClass)
                generator.Emit(OpCodes.Box, propertyInfo.PropertyType);

            generator.Emit(OpCodes.Ret);

            /*
            * Create the delegate and return it
            */
            return (GenericGetter)getter.CreateDelegate(typeof(GenericGetter));
        }
    }
}
