using System.ComponentModel;

using FastMember;

namespace DeepCloning
{
    public class FastDeepCloneInjection : DeepCloneInjection
    {
        protected override void SetValue(PropertyDescriptor prop, object component, object value)
        {
            var a = TypeAccessor.Create(component.GetType());
            a[component, prop.Name] = value;
        }

        protected override object GetValue(PropertyDescriptor prop, object component)
        {
            var a = TypeAccessor.Create(component.GetType(), true);
            return a[component, prop.Name];
        }
    }
}