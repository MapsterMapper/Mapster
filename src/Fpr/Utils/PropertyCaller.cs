using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fpr.Utils
{
    public static class PropertyCaller<TClass, TReturn> where TClass : class
    {
        private static readonly Dictionary<Type, Dictionary<Type, Dictionary<string, GenGetter>>> _dGets =
            new Dictionary<Type, Dictionary<Type, Dictionary<string, GenGetter>>>();

        private static readonly Dictionary<Type, Dictionary<Type, Dictionary<string, GenSetter>>> _dSets =
            new Dictionary<Type, Dictionary<Type, Dictionary<string, GenSetter>>>();

        public delegate void GenSetter(TClass target, TReturn value);
        public delegate TReturn GenGetter(TClass target);

        public static GenGetter CreateGetMethod(PropertyInfo pi)
        {
            //Create the locals needed.
            Type classType = typeof(TClass);
            Type returnType = typeof(TReturn);

            string propertyName = pi.Name;

            //Let’s return the cached delegate if we have one.
            if (_dGets.ContainsKey(classType) && _dGets[classType].ContainsKey(returnType) 
                && _dGets[classType][returnType].ContainsKey(propertyName))
            {
                return _dGets[classType][returnType][propertyName];
            }

            //If there is no getter, return nothing
            MethodInfo getMethod = pi.GetGetMethod();

            if (getMethod == null)
                return null;

            //Create the dynamic method to wrap the internal get method
            var getter = new DynamicMethod(String.Concat("Get", pi.Name, "_"), typeof(TReturn), new[] { typeof(TClass) }, pi.DeclaringType);

            ILGenerator gen = getter.GetILGenerator();

            gen.DeclareLocal(typeof(TReturn));
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, pi.DeclaringType);
            gen.EmitCall(OpCodes.Callvirt, getMethod, null);
            gen.Emit(OpCodes.Ret);

            //Create the delegate and return it
            var genGetter = (GenGetter)getter.CreateDelegate(typeof(GenGetter));

            //Cache the delegate for future use.
            Dictionary<string, GenGetter> tempPropDict;

            if (!_dGets.ContainsKey(classType))
            {
                tempPropDict = new Dictionary<string, GenGetter> {{propertyName, genGetter}};

                var tempDict = new Dictionary<Type, Dictionary<string, GenGetter>> {{returnType, tempPropDict}};

                _dGets.Add(classType, tempDict);
            }
            else
            {
                if (!_dGets[classType].ContainsKey(returnType))
                {
                    tempPropDict = new Dictionary<string, GenGetter> {{propertyName, genGetter}};
                    _dGets[classType].Add(returnType, tempPropDict);
                }
                else
                {
                    if (!_dGets[classType][returnType].ContainsKey(propertyName))
                        _dGets[classType][returnType].Add(propertyName, genGetter);
                }
            }

            //Return delegate to the caller.
            return genGetter;

        }


        public static GenSetter CreateSetMethod(PropertyInfo pi)
        {
            //Create the locals needed.
            Type classType = typeof(TClass);
            Type returnType = typeof(TReturn);
            string propertyName = pi.Name;

            //Let’s return the cached delegate if we have one.
            if (_dSets.ContainsKey(classType) && _dSets[classType].ContainsKey(returnType)
                && _dSets[classType][returnType].ContainsKey(propertyName))
            {
                    return _dSets[classType][returnType][propertyName];
            }

            //If there is no setter, return nothing
            MethodInfo setMethod = pi.GetSetMethod(true);

            if (setMethod == null)
                return null;

            //Create dynamic method

            Type[] arguments = { classType, returnType };

            var setter = new DynamicMethod(String.Concat("_Set", pi.Name, "_"), typeof(void), arguments, pi.DeclaringType);

            ILGenerator gen = setter.GetILGenerator();

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, pi.DeclaringType);
            gen.Emit(OpCodes.Ldarg_1);

            if (pi.PropertyType.IsClass)
                gen.Emit(OpCodes.Castclass, pi.PropertyType);

            gen.EmitCall(OpCodes.Callvirt, setMethod, null);

            gen.Emit(OpCodes.Ret);

            //Create the delegate
            var genSetter = (GenSetter)setter.CreateDelegate(typeof(GenSetter));

            //Cache the delegate for future use.
            Dictionary<string, GenSetter> tempPropDict;

            if (!_dSets.ContainsKey(classType))
            {
                tempPropDict = new Dictionary<string, GenSetter> {{propertyName, genSetter}};

                var tempDict = new Dictionary<Type, Dictionary<string, GenSetter>> {{returnType, tempPropDict}};

                _dSets.Add(classType, tempDict);
            }
            else
            {
                if (!_dSets[classType].ContainsKey(returnType))
                {
                    tempPropDict = new Dictionary<string, GenSetter> {{propertyName, genSetter}};

                    _dSets[classType].Add(returnType, tempPropDict);
                }
                else
                {
                    if (!_dSets[classType][returnType].ContainsKey(propertyName))
                        _dSets[classType][returnType].Add(propertyName, genSetter);
                }
            }
            //Return delegate to the caller.
            return genSetter;

        }
    }

    public static class PropertyCaller<T>
    {
        public delegate void GenSetter(T target, Object value);
        public delegate Object GenGetter(T target);

        private static readonly Dictionary<Type, Dictionary<Type, Dictionary<string, GenGetter>>> _dGets = new Dictionary<Type, Dictionary<Type, Dictionary<string, GenGetter>>>();
        private static readonly Dictionary<Type, Dictionary<Type, Dictionary<string, GenSetter>>> _dSets = new Dictionary<Type, Dictionary<Type, Dictionary<string, GenSetter>>>();

        public static GenGetter CreateGetMethod(PropertyInfo pi)
        {
            var classType = typeof(T);
            var propType = pi.PropertyType;
            var propName = pi.Name;

            Dictionary<Type, Dictionary<string, GenGetter>> i1;
            if (_dGets.TryGetValue(classType, out i1))
            {
                Dictionary<string, GenGetter> i2;
                if (i1.TryGetValue(propType, out i2))
                {
                    GenGetter i3;
                    if (i2.TryGetValue(propName, out i3))
                    {
                        return i3;
                    }
                }
            }

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

            Dictionary<string, GenGetter> tempPropDict;
            if (!_dGets.ContainsKey(classType))
            {
                tempPropDict = new Dictionary<string, GenGetter> {{propName, genGetter}};
                var tempDict = new Dictionary<Type, Dictionary<string, GenGetter>> {{propType, tempPropDict}};
                _dGets.Add(classType, tempDict);
            }
            else
            {
                if (!_dGets[classType].ContainsKey(propType))
                {
                    tempPropDict = new Dictionary<string, GenGetter> {{propName, genGetter}};
                    _dGets[classType].Add(propType, tempPropDict);
                }
                else
                {
                    if (!_dGets[classType][propType].ContainsKey(propName))
                    {
                        _dGets[classType][propType].Add(propName, genGetter);
                    }
                }
            }
            return genGetter;
        }

        public static GenSetter CreateSetMethod(PropertyInfo pi)
        {
            Type classType = typeof(T);
            Type propType = pi.PropertyType;
            string propName = pi.Name;

            Dictionary<Type, Dictionary<string, GenSetter>> i1;
            if (_dSets.TryGetValue(classType, out i1))
            {
                Dictionary<string, GenSetter> i2;
                if (i1.TryGetValue(propType, out i2))
                {
                    GenSetter i3;
                    if (i2.TryGetValue(propName, out i3))
                    {
                        return i3;
                    }
                }
            }

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

            Dictionary<string, GenSetter> tempPropDict;
            if (!_dSets.ContainsKey(classType))
            {
                tempPropDict = new Dictionary<string, GenSetter> {{propName, genSetter}};
                var tempDict = new Dictionary<Type, Dictionary<string, GenSetter>> {{propType, tempPropDict}};
                _dSets.Add(classType, tempDict);
            }
            else
            {
                if (!_dSets[classType].ContainsKey(propType))
                {
                    tempPropDict = new Dictionary<string, GenSetter> {{propName, genSetter}};
                    _dSets[classType].Add(propType, tempPropDict);
                }
                else
                {
                    if (!_dSets[classType][propType].ContainsKey(propName))
                    {
                        _dSets[classType][propType].Add(propName, genSetter);
                    }
                }
            }

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
