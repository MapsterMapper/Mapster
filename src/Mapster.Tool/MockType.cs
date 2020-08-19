using System;
using System.Globalization;
using System.Reflection;

namespace Mapster.Tool
{
    public class MockType : TypeInfo
    {
        public MockType(string ns, string name, Assembly assembly)
        {
            this.Namespace = ns;
            this.Name = name;
            this.Assembly = assembly;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return Array.Empty<object>();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return Array.Empty<object>();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        public override Module Module => this.Assembly.GetModules(false)[0];
        public override string Namespace { get; }
        public override string Name { get; }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return TypeAttributes.Public | TypeAttributes.Class;
        }

        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention,
            Type[] types, ParameterModifier[]? modifiers)
        {
            return null;
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return Array.Empty<ConstructorInfo>();
        }

        public override Type? GetElementType()
        {
            return null;
        }

        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
        {
            return null;
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return Array.Empty<EventInfo>();
        }

        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            return null;
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return Array.Empty<FieldInfo>();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return Array.Empty<MemberInfo>();
        }

        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention,
            Type[]? types, ParameterModifier[]? modifiers)
        {
            return null;
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return Array.Empty<MethodInfo>();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return Array.Empty<PropertyInfo>();
        }

        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args,
            ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
        {
            return null;
        }

        public override Type UnderlyingSystemType => this;

        protected override bool IsArrayImpl()
        {
            return false;
        }

        protected override bool IsByRefImpl()
        {
            return false;
        }

        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        protected override bool IsPointerImpl()
        {
            return false;
        }

        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        public override Assembly Assembly { get; }
        public override string AssemblyQualifiedName => this.FullName;
        public override Type? BaseType => null;
        public override string FullName => $"{this.Namespace}.{this.Name}";

        public override Guid GUID { get; } = Guid.NewGuid();

        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types,
            ParameterModifier[]? modifiers)
        {
            return null;
        }

        protected override bool HasElementTypeImpl()
        {
            return false;
        }

        public override Type? GetNestedType(string name, BindingFlags bindingAttr)
        {
            return null;
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return Array.Empty<Type>();
        }

        public override Type? GetInterface(string name, bool ignoreCase)
        {
            return null;
        }

        public override Type[] GetInterfaces()
        {
            return Array.Empty<Type>();
        }
    }
}
